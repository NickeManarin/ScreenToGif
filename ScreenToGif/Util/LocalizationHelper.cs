using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Xaml;
using ScreenToGif.FileWriters;
using ScreenToGif.Windows.Other;

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

            if (string.IsNullOrEmpty(culture))
                return;

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
            string requestedCulture = $"/Resources/Localization/StringResources.{culture}.xaml";
            var requestedResource = dictionaryList.FirstOrDefault(d => d.Source?.OriginalString == requestedCulture);

            #endregion

            #region Generic Branch Fallback

            //Fallback to a more generic version of the language. Example: pt-BR to pt.
            if (requestedResource == null && culture.Length > 2 && !culture.StartsWith("en"))
            {
                culture = culture.Substring(0, 2); //TODO: Support for language code like syr-SY (3 initial letters)
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

            //Inform the threads of the new culture.     
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

            GC.Collect(2);
        }

        public static void SaveDefaultResource(string path)
        {
            //Copy all MergedDictionarys into a auxiliar list.
            var dictionaryList = Application.Current.Resources.MergedDictionaries.ToList();

            try
            {
                //Search for the specified culture.     
                var resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == "/Resources/Localization/StringResources.en.xaml");

                if (resourceDictionary == null)
                    throw new CultureNotFoundException("String resource not found.");

                if (string.IsNullOrEmpty(path))
                    throw new ArgumentException("Path is null.");

                using (var writer = new StreamWriter(path, false))
                    System.Windows.Markup.XamlWriter.Save(resourceDictionary, writer);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Save Xaml Resource Error");

                Dialog.Ok("Impossible to Save", "Impossible to save the Xaml file", ex.Message, Dialog.Icons.Warning);
            }
        }

        public static bool ImportStringResource(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentException("Path is null");

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // Read in ResourceDictionary File
                    var dictionary = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(fs);
                    dictionary.Source = new Uri(path);

                    // Add in newly loaded Resource Dictionary
                    Application.Current.Resources.MergedDictionaries.Add(dictionary);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Import Xaml Resource Error");

                Dialog.Ok("Impossible to Import", "Impossible to import the Xaml file", ex.Message, Dialog.Icons.Warning);

                return false;
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

                //Recover selected dictionary.
                var dictionaryAux = Application.Current.Resources.MergedDictionaries[selectedIndex];

                //Remove from the current list.
                Application.Current.Resources.MergedDictionaries.Remove(Application.Current.Resources.MergedDictionaries[selectedIndex]);

                //Insert at the upper position.
                Application.Current.Resources.MergedDictionaries.Insert(toUp ? selectedIndex - 1 : selectedIndex + 1, dictionaryAux);

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

                using (var writer = new StreamWriter(path, false))
                    System.Windows.Markup.XamlWriter.Save(Application.Current.Resources.MergedDictionaries[selectedIndex], writer);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Save Xaml Resource Error");

                Dialog.Ok("Impossible to Save", "Impossible to save the Xaml file", ex.Message, Dialog.Icons.Warning);
            }
        }

        public static bool Remove(int selectedIndex)
        {
            try
            {
                if (selectedIndex == -1 || selectedIndex > Application.Current.Resources.MergedDictionaries.Count - 1)
                    return false;

                if (Application.Current.Resources.MergedDictionaries[selectedIndex].Source.OriginalString.Contains("StringResources.xaml"))
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
    }
}
