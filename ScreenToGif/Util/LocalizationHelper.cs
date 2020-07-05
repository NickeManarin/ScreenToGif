using ScreenToGif.Windows.Other;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Deals with localization behaviors.
    /// </summary>
    public static class LocalizationHelper
    {
        public static void SelectCulture(string culture)
        {
            #region Validation

            //If none selected, fallback to english.
            if (string.IsNullOrEmpty(culture))
                culture = "en";

            if (culture.Equals("auto") || culture.Length < 2)
            {
                var ci = CultureInfo.InstalledUICulture;
                culture = ci.Name;
            }

            #endregion

            //Copy all MergedDictionarys into a auxiliar list.
            var dictionaryList = Application.Current.Resources.MergedDictionaries.ToList();

            #region Selected Culture

            //Search for the specified culture.
            var requestedCulture = $"/Resources/Localization/StringResources.{culture}.xaml";
            var requestedResource = dictionaryList.FirstOrDefault(d => d.Source?.OriginalString == requestedCulture);

            #endregion

            #region Generic Branch Fallback

            //Fallback to a more generic version of the language. Example: pt-BR to pt.
            while (requestedResource == null && !string.IsNullOrEmpty(culture))
            {
                culture = CultureInfo.GetCultureInfo(culture).Parent.Name;
                requestedCulture = $"/Resources/Localization/StringResources.{culture}.xaml";
                requestedResource = dictionaryList.FirstOrDefault(d => d.Source?.OriginalString == requestedCulture);
            }

            #endregion

            #region English Fallback

            //If not present, fall back to english.
            if (requestedResource == null)
            {
                culture = "en";
                requestedCulture = "/Resources/Localization/StringResources.en.xaml";
                requestedResource = dictionaryList.FirstOrDefault(d => d.Source?.OriginalString == requestedCulture);
            }

            #endregion

            //If we have the requested resource, remove it from the list and place at the end.
            //Then this language will be our current string table.
            Application.Current.Resources.MergedDictionaries.Remove(requestedResource);
            Application.Current.Resources.MergedDictionaries.Add(requestedResource);

            //Inform the threads of the new culture.
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

            #region English Fallback of the Current Language

            //Only non-English resources need a fallback, because the English resource is evergreen.
            if (culture.StartsWith("en"))
                return;

            var englishResource = dictionaryList.FirstOrDefault(d => d.Source?.OriginalString == "/Resources/Localization/StringResources.en.xaml");

            if (englishResource != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(englishResource);
                Application.Current.Resources.MergedDictionaries.Insert(Application.Current.Resources.MergedDictionaries.Count - 1, englishResource);
            }

            #endregion

            GC.Collect(0);

            if (!UserSettings.All.CheckForTranslationUpdates)
                return;

            //Async, fire and forget.
            Task.Factory.StartNew(() => CheckForUpdates(culture));
        }

        /// <summary>
        /// This is what happens:
        /// 
        ///Get date of available resource
        ///  if resource available is newer than assembly
        ///      if there is already a translation downloaded
        ///          if current translation is older than available
        ///              Download latest, overwriting current
        ///          if current translation is newer
        ///              Don't download
        ///      if there no translation downloaded already
        ///          Download latest
        ///  if resource available is older than assembly
        ///      Don't download, erase current translation
        /// </summary>
        /// <param name="culture">The culture that should be searched for updates.</param>
        internal static void CheckForUpdates(string culture)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolderResolved))
                    return;

                var folder = Path.Combine(UserSettings.All.TemporaryFolderResolved, "ScreenToGif", "Localization");
                var file = Path.Combine(folder, $"StringResources.{culture}.new.xaml");

                Directory.CreateDirectory(folder);

                //Get when the available resource was updated.
                var updated = GetWhenResourceWasUpdated(culture);

                //If resource available is older than assembly.
                if (!updated.HasValue || updated <= File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location))
                {
                    if (File.Exists(file))
                        File.Delete(file);

                    return;
                }

                //If a translation was previously downloaded.
                if (File.Exists(file))
                {
                    //If current translation is older than the available one.
                    if (new FileInfo(file).LastWriteTimeUtc < updated.Value.ToUniversalTime())
                        DownloadLatest(file, culture);
                }
                else
                {
                    DownloadLatest(file, culture);
                }

                //If a new translation was not downloaded (now or previously), ignore the following code. 
                if (!File.Exists(file))
                    return;

                //Removes any resource that was added by this updater.
                var listToRemove = Application.Current.Resources.MergedDictionaries.Where(w => w.Source?.OriginalString.EndsWith(".new.xaml") == true).ToList();

                foreach (var rem in listToRemove)
                    Application.Current.Resources.MergedDictionaries.Remove(rem);

                //Load the resource from the file, not replacing the current resource, but putting right after it.
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (fs.Length == 0)
                        throw new InvalidDataException("File is empty");

                    //Reads the ResourceDictionary file
                    var dictionary = (ResourceDictionary)XamlReader.Load(fs);
                    dictionary.Source = new Uri(Path.Combine(file));

                    //Add in newly loaded Resource Dictionary.
                    Application.Current.Resources.MergedDictionaries.Add(dictionary);
                }
            }
            catch (WebException)
            {
                //Ignore it.
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Check for an updated localization recource");
            }
        }

        /// <summary>
        /// Checks when the available resource file was updated.
        /// </summary>
        /// <param name="culture">The culture of the resource to be checked.</param>
        /// <returns>The date when the resource file was last updated.</returns>
        private static DateTime? GetWhenResourceWasUpdated(string culture)
        {
            //Gets the latest commit that changed the translation resource.
            var req = (HttpWebRequest)WebRequest.Create($"https://api.github.com/repos/NickeManarin/ScreenToGif/commits?path=ScreenToGif/Resources/Localization/StringResources.{culture}.xaml&page=1&per_page=1");
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";
            req.Proxy = WebHelper.GetProxy();

            var res = (HttpWebResponse)req.GetResponse();

            using (var resultStream = res.GetResponseStream())
            {
                if (resultStream == null)
                    return null;

                using (var reader = new StreamReader(resultStream))
                {
                    var result = reader.ReadToEnd();
                    var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result), new XmlDictionaryReaderQuotas());
                    var release = XElement.Load(jsonReader);

                    //Gets the date of of the last commit that changed the translation file.
                    var dateText = release.FirstNode.XPathSelectElement("commit")?.XPathSelectElement("committer")?.XPathSelectElement("date")?.Value;

                    //If was not possible to convert the time, keep using the current resource.
                    if (!DateTime.TryParse(dateText, out DateTime modificationDate))
                        return null;

                    //If the current resource is newer then the available one, keep using the current.
                    return modificationDate;
                }
            }
        }

        /// <summary>
        /// Downloads the available localization resource.
        /// </summary>
        /// <param name="file">The destination path of the recource.</param>
        /// <param name="culture">The culture of the resource to be downloaded.</param>
        private static void DownloadLatest(string file, string culture)
        {
            var request = (HttpWebRequest)WebRequest.Create($"https://api.github.com/repos/NickeManarin/ScreenToGif/contents/ScreenToGif/Resources/Localization/StringResources.{culture}.xaml");
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";
            request.Proxy = WebHelper.GetProxy();

            var response = (HttpWebResponse)request.GetResponse();

            using (var resultStream = response.GetResponseStream())
            {
                if (resultStream == null)
                    return;

                using (var reader = new StreamReader(resultStream))
                {
                    var result = reader.ReadToEnd();
                    var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result), new XmlDictionaryReaderQuotas());
                    var release = XElement.Load(jsonReader);

                    //When creating a GET request with a direct path, the 'content' element is available as a base64 string.
                    var contentBase64 = release.XPathSelectElement("content")?.Value;

                    if (string.IsNullOrWhiteSpace(contentBase64))
                        return;

                    if (File.Exists(file))
                        File.Delete(file);

                    File.WriteAllText(file, Encoding.UTF8.GetString(Convert.FromBase64String(contentBase64)).Replace("&#x0d;", "\r"));
                }
            }
        }

        public static void SaveDefaultResource(string path)
        {
            //Copy all MergedDictionarys into a auxiliar list.
            var dictionaryList = Application.Current.Resources.MergedDictionaries.ToList();

            try
            {
                //Search for the specified culture.
                var resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source?.OriginalString == "/Resources/Localization/StringResources.en.xaml");

                if (resourceDictionary == null)
                    throw new CultureNotFoundException("String resource not found.");

                if (string.IsNullOrEmpty(path))
                    throw new ArgumentException("Path is null.");

                var settings = new XmlWriterSettings { Indent = true };

                using (var writer = XmlWriter.Create(path, settings))
                    System.Windows.Markup.XamlWriter.Save(resourceDictionary, writer);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Save Xaml Resource Error");

                Dialog.Ok("Impossible to Save", "Impossible to save the Xaml file", ex.Message, Icons.Warning);
            }
        }

        public static void ImportStringResource(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentException("Path is null");

                var destination = Path.Combine(Path.GetTempPath(), Path.GetFileName(path));

                if (File.Exists(destination))
                    File.Delete(destination);

                File.WriteAllText(destination, File.ReadAllText(path).Replace("&#x0d;", "\r"));

                using (var fs = new FileStream(destination, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (fs.Length == 0)
                        throw new InvalidDataException("File is empty");

                    //Reads the ResourceDictionary file
                    var dictionary = (ResourceDictionary)XamlReader.Load(fs);
                    dictionary.Source = new Uri(destination);

                    //Add in newly loaded Resource Dictionary.
                    Application.Current.Resources.MergedDictionaries.Add(dictionary);
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Import Resource");
                //Rethrowing, because it's more useful to catch later
                throw;
            }
        }

        public static List<ResourceDictionary> GetLocalizations()
        {
            //Copy all MergedDictionarys into a auxiliar list.
            var dictionaryList = Application.Current.Resources.MergedDictionaries.ToList();

            return dictionaryList.Where(x => x.Source.OriginalString.Contains("StringResource")).ToList();
        }

        public static bool Move(int selectedIndex, bool toUp = true)
        {
            try
            {
                if (toUp && selectedIndex < 1)
                    return false;

                if (!toUp && selectedIndex > Application.Current.Resources.MergedDictionaries.Count - 1)
                    return false;

                //Recover the selected dictionary.
                var dictionaryAux = Application.Current.Resources.MergedDictionaries[selectedIndex];

                //Remove from the current list.
                Application.Current.Resources.MergedDictionaries.Remove(Application.Current.Resources.MergedDictionaries[selectedIndex]);

                //Detect the index of the next localization.
                var newIndex = -1;

                if (toUp)
                {
                    //Search for the index of the previous localization resource.
                    for (var i = selectedIndex - 1; i >= 0; i--)
                    {
                        if (Application.Current.Resources.MergedDictionaries[i].Source?.OriginalString?.Contains("StringResources") == true)
                        {
                            newIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    //Search for the index of the next localization resource.
                    for (var i = selectedIndex; i < Application.Current.Resources.MergedDictionaries.Count; i++)
                    {
                        if (Application.Current.Resources.MergedDictionaries[i].Source?.OriginalString?.Contains("StringResources") == true)
                        {
                            newIndex = i + 1;
                            break;
                        }
                    }
                }
                
                //Insert at the new position.
                Application.Current.Resources.MergedDictionaries.Insert(newIndex, dictionaryAux);

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Move Resource", selectedIndex);
                return false;
            }
        }

        public static void SaveSelected(int selectedIndex, string path)
        {
            try
            {
                if (selectedIndex < 0 || selectedIndex > Application.Current.Resources.MergedDictionaries.Count - 1)
                    throw new IndexOutOfRangeException("Index out of range while trying to save the resource dictionary.");

                var settings = new XmlWriterSettings { Indent = true };

                using (var writer = XmlWriter.Create(path, settings))
                    System.Windows.Markup.XamlWriter.Save(Application.Current.Resources.MergedDictionaries[selectedIndex], writer);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Save Resource", selectedIndex);
                //Rethrowing, because it's more useful to catch later
                throw;
            }
        }

        public static bool Remove(int selectedIndex)
        {
            try
            {
                if (selectedIndex == -1 || selectedIndex > Application.Current.Resources.MergedDictionaries.Count - 1)
                    return false;

                //Don't allow the user to delete resources that are not localizations.
                if (Application.Current.Resources.MergedDictionaries[selectedIndex].Source?.OriginalString?.Contains("StringResources") != true)
                    return false;

                //Remove from the current list.
                Application.Current.Resources.MergedDictionaries.RemoveAt(selectedIndex);

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Remove Resource", selectedIndex);
                return false;
            }
        }

        /// <summary>
        /// Gets a resource as string.
        /// </summary>
        /// <param name="key">The key of the string resource.</param>
        /// <param name="removeNewLines">If true, it removes any kind of new lines.</param>
        /// <returns>A string resource, usually a localized string.</returns>
        public static string Get(string key, bool removeNewLines = false)
        {
            if (removeNewLines)
                return (Application.Current.TryFindResource(key) as string ?? "").Replace("\n", " ").Replace("\\n", " ").Replace("\r", " ").Replace("&#10;", " ").Replace("&#x0d;", " ");

            return Application.Current.TryFindResource(key) as string;
        }

        /// <summary>
        /// Gets a resource as string and applies the format.
        /// </summary>
        /// <param name="key">The key of the string resource.</param>
        /// <param name="values">The values for the string format.</param>
        /// <returns>A string resource, usually a localized string.</returns>
        public static string GetWithFormat(string key, params object[] values)
        {
            return string.Format(Thread.CurrentThread.CurrentUICulture, Application.Current.TryFindResource(key) as string ?? "", values);
        }

        /// <summary>
        /// Gets a resource as string.
        /// </summary>
        /// <param name="key">The key of the string resource.</param>
        /// <param name="defaultValue">The default value in english.</param>
        /// <param name="removeNewLines">If true, it removes any kind of new lines.</param>
        /// <returns>A string resource, usually a localized string.</returns>
        public static string Get(string key, string defaultValue, bool removeNewLines = false)
        {
            if (removeNewLines)
                return (Application.Current.TryFindResource(key) as string ?? defaultValue).Replace("\n", " ").Replace("\\n", " ").Replace("\r", " ").Replace("&#10;", " ").Replace("&#x0d;", " ");

            return Application.Current.TryFindResource(key) as string ?? defaultValue;
        }

        /// <summary>
        /// Gets a resource as string and applies the format.
        /// </summary>
        /// <param name="key">The key of the string resource.</param>
        /// <param name="defaultValue">The default value in english.</param>
        /// <param name="values">The values for the string format.</param>
        /// <returns>A string resource, usually a localized string.</returns>
        public static string GetWithFormat(string key, string defaultValue, params object[] values)
        {
            return string.Format(Thread.CurrentThread.CurrentUICulture, Application.Current.TryFindResource(key) as string ?? defaultValue, values);
        }

        /// <summary>
        /// Gets a resource as string.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="key">The key of the string resource.</param>
        /// <param name="values">The possible values that composite the key name.</param>
        /// <returns>A string resource, usually a localized string.</returns>
        public static string GetWithIndex(int index, string key, params string[] values)
        {
            return Application.Current.TryFindResource(key + values[index]) as string;
        }
    }
}