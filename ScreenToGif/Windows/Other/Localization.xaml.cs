using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class Localization : Window
    {
        private IEnumerable<string> _cultures;

        public Localization()
        {
            InitializeComponent();
        }

        #region Events
        
        private async void Localization_OnLoaded(object sender, RoutedEventArgs e)
        {
            StatusBand.Info("Getting resources...");

            AddButton.IsEnabled = false;
            SaveButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            DownButton.IsEnabled = false;
            UpButton.IsEnabled = false;
            OkButton.IsEnabled = false;

            foreach (var resourceDictionary in Application.Current.Resources.MergedDictionaries)
            {
                var imageItem = new ImageListBoxItem
                {
                    Tag = resourceDictionary.Source?.OriginalString ?? "Settings",
                    Content = resourceDictionary.Source?.OriginalString ?? "Settings"
                };

                if (resourceDictionary.Source == null)
                {
                    imageItem.IsEnabled = false;
                    imageItem.Image = FindResource("Vector.No") as Canvas;
                    imageItem.Author = "This is a settings dictionary.";
                }
                else if (resourceDictionary.Source.OriginalString.Contains("StringResources"))
                {
                    imageItem.Image = FindResource("Vector.Translate") as Canvas;

                    #region Name

                    //var subs = resourceDictionary.Source.OriginalString.Substring(resourceDictionary.Source.OriginalString.IndexOf("StringResources"));
                    var pieces = resourceDictionary.Source.OriginalString.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);

                    if (pieces.Length == 3 || pieces.Length == 4)
                        imageItem.Author = "Recognized as " + pieces[1];
                    else
                        imageItem.Author = "Not recognized";

                    #endregion   
                }
                else
                {
                    imageItem.IsEnabled = false;
                    imageItem.Image = FindResource("Vector.No") as Canvas;
                    imageItem.Author = "This is a style dictionary.";
                }

                ResourceListBox.Items.Add(imageItem);
            }

            ResourceListBox.SelectedIndex = ResourceListBox.Items.Count - 1;
            ResourceListBox.ScrollIntoView(ResourceListBox.SelectedItem);

            SaveButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;
            DownButton.IsEnabled = true;
            UpButton.IsEnabled = true;
            OkButton.IsEnabled = true;

            StatusBand.Info("Getting language codes...");

            _cultures = await GetProperCulturesAsync();

            StatusBand.Hide();
            AddButton.IsEnabled = true;
            SizeToContent = SizeToContent.Width;
            MaxHeight = double.PositiveInfinity;

            CommandManager.InvalidateRequerySuggested();
        }

        private void MoveUp_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResourceListBox.SelectedIndex > 0;
        }

        private void MoveDown_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResourceListBox.SelectedIndex < ResourceListBox.Items.Count - 1;
        }

        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResourceListBox.SelectedIndex != -1;
        }

        private void Remove_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResourceListBox.SelectedIndex != -1;
        }

        private void Add_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void MoveUp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (LocalizationHelper.Move(ResourceListBox.SelectedIndex))
            {
                var selectedIndex = ResourceListBox.SelectedIndex;

                var selected = ResourceListBox.Items[selectedIndex];

                ResourceListBox.Items.RemoveAt(selectedIndex);
                ResourceListBox.Items.Insert(selectedIndex - 1, selected);
                ResourceListBox.SelectedItem = selected;
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private void MoveDown_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (LocalizationHelper.Move(ResourceListBox.SelectedIndex, false))
            {
                var selectedIndex = ResourceListBox.SelectedIndex;

                var selected = ResourceListBox.Items[selectedIndex];

                ResourceListBox.Items.RemoveAt(selectedIndex);
                ResourceListBox.Items.Insert(selectedIndex + 1, selected);
                ResourceListBox.SelectedItem = selected;
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private async void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StatusBand.Info("Exporting resource...");

            var sfd = new SaveFileDialog
            {
                AddExtension = true,
                Filter = "Resource Dictionary (*.xaml)|*.xaml",
                Title = "Save Resource Dictionary"
            };

            var source = ((ImageListBoxItem)ResourceListBox.SelectedItem).Content.ToString();
            var subs = source.Substring(source.IndexOf("StringResources"));

            sfd.FileName = subs;

            var result = sfd.ShowDialog();
            var fileName = sfd.FileName;
            //We have to access UI components here (can't do that in the task below)
            var index = ResourceListBox.SelectedIndex;

            if (result.HasValue && result.Value)
            {
                try
                {
                    await Task.Factory.StartNew(() => LocalizationHelper.SaveSelected(index, fileName));
                }
                catch (Exception ex)
                {
                    Dialog.Ok("Impossible to Save", "Impossible to save the Xaml file", ex.Message, Icons.Warning);
                }
            }

            StatusBand.Hide();
            CommandManager.InvalidateRequerySuggested();
        }

        private void Remove_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (LocalizationHelper.Remove(ResourceListBox.SelectedIndex))
                ResourceListBox.Items.RemoveAt(ResourceListBox.SelectedIndex);

            CommandManager.InvalidateRequerySuggested();
        }

        private async void Add_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = "Open a Resource Dictionary",
                Filter = "Resource Dictionay (*.xaml)|*.xaml;"
            };

            var result = ofd.ShowDialog();

            if (!result.HasValue || !result.Value) return;

            StatusBand.Info("Validating resource name...");

            #region Validation

            if (!ofd.FileName.Contains("StringResources"))
            {
                Dialog.Ok("Action Denied", "The name of file does not follow a valid pattern.",
                    "Try renaming like (without the []): StringResources.[Language Code].xaml");

                StatusBand.Hide();
                return;
            }

            var subs = ofd.FileName.Substring(ofd.FileName.IndexOf("StringResources"));

            if (Application.Current.Resources.MergedDictionaries.Any(x => x.Source != null && x.Source.OriginalString.Contains(subs)))
            {
                Dialog.Ok("Action Denied", "You can't add a resource with the same name.", "Try renaming like: StringResources.[Language Code].xaml");

                StatusBand.Hide();
                return;
            }

            var pieces = subs.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (pieces.Length != 3)
            {
                Dialog.Ok("Action Denied", "Filename with wrong format.", "Try renaming like: StringResources.[Language Code].xaml");

                StatusBand.Hide();
                return;
            }
            var cultureName = pieces[1];

            string properCulture;
            try
            {
                properCulture = await Task.Factory.StartNew(() => CheckSupportedCulture(cultureName));
            }
            catch (CultureNotFoundException)
            {
                Dialog.Ok("Action Denied", "Unknown Language.", $"The \"{cultureName}\" and its family were not recognized as a valid language codes.");

                StatusBand.Hide();
                return;
            }
            catch (Exception ex)
            {
                Dialog.Ok("Action Denied", "Error checking culture.", ex.Message);

                StatusBand.Hide();
                return;
            }

            if (properCulture != cultureName)
            {
                Dialog.Ok("Action Denied", "Redundant Language Code.", $"The \"{cultureName}\" code is redundant. Try using \'{properCulture}\" instead");

                StatusBand.Hide();
                return;
            }

            #endregion

            StatusBand.Info("Importing resource...");

            var fileName = ofd.FileName;

            try
            {
                await Task.Factory.StartNew(() => LocalizationHelper.ImportStringResource(fileName));
            }
            catch(Exception ex)
            {
                Dialog.Ok("Localization", "Localization - Importing Xaml Resource", ex.Message);

                StatusBand.Hide();
                await Task.Factory.StartNew(GC.Collect);
                return;
            }

            var resourceDictionary = Application.Current.Resources.MergedDictionaries.LastOrDefault();

            var imageItem = new ImageListBoxItem
            {
                Tag = resourceDictionary?.Source.OriginalString ?? "Unknown",
                Content = resourceDictionary?.Source.OriginalString ?? "Unknown",
                Image = FindResource("Vector.Translate") as Canvas,
                Author = "Recognized as " + pieces[1]
            };

            StatusBand.Hide();

            ResourceListBox.Items.Add(imageItem);
            ResourceListBox.ScrollIntoView(imageItem);

            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region Methods 

        private string CheckSupportedCulture(string cultureName)
        {
            //Using HashSet, because we can check if it contains string in O(1) time
            //Only creating it takes some time, but it's better than performing Contains multiple times on the list in the loop below.
            var cultureHash = new HashSet<string>(_cultures);

            if (cultureHash.Contains(cultureName))
                return cultureName;

            var t = CultureInfo.GetCultureInfo(cultureName);

            while (t != CultureInfo.InvariantCulture)
            {
                if (cultureHash.Contains(t.Name))
                    return t.Name;

                t = t.Parent;
            }

            return null;
        }

        private async Task<IEnumerable<string>> GetProperCulturesAsync()
        {
            var allCodes = await Task.Factory.StartNew(() => CultureInfo.GetCultures(CultureTypes.AllCultures).Where(x => !string.IsNullOrEmpty(x.Name)).Select(x => x.Name));

            try
            {
                var downloadedCodes = await GetLanguageCodesAsync();
                var properCodes = await Task.Factory.StartNew(() => allCodes.Where(x => downloadedCodes.Contains(x)));

                return properCodes ?? allCodes;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Dialog.Ok("Translator", "Translator - Getting Language Codes", ex.Message + Environment.NewLine + "Loading all local language codes."));
            }

            GC.Collect();
            return allCodes;
        }

        private async Task<IEnumerable<string>> GetLanguageCodesAsync()
        {
            var path = await GetLanguageCodesPathAsync();

            if (string.IsNullOrEmpty(path))
                throw new WebException("Can't get language codes. Path to language codes is null");

            var request = (HttpWebRequest)WebRequest.Create(path);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";
            request.Proxy = WebHelper.GetProxy();

            var response = (HttpWebResponse)await request.GetResponseAsync();

            using (var resultStream = response.GetResponseStream())
            {
                if (resultStream == null)
                    throw new WebException("Empty response from server when getting language codes");

                using (var reader = new StreamReader(resultStream))
                {
                    var result = await reader.ReadToEndAsync();

                    var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result),
                        new System.Xml.XmlDictionaryReaderQuotas());

                    var json = await Task<XElement>.Factory.StartNew(() => XElement.Load(jsonReader));
                    var languages = json.Elements();

                    return await Task.Factory.StartNew(() => languages.Where(x => x.XPathSelectElement("defs")?.Value != "0").Select(x => x.XPathSelectElement("lang")?.Value));
                }
            }
        }

        private async Task<string> GetLanguageCodesPathAsync()
        {
            var request = (HttpWebRequest)WebRequest.Create("https://datahub.io/core/language-codes/datapackage.json");
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";
            request.Proxy = WebHelper.GetProxy();

            var response = (HttpWebResponse)await request.GetResponseAsync();

            using (var resultStream = response.GetResponseStream())
            {
                if (resultStream == null)
                    throw new WebException("Empty response from server when getting language codes path");

                using (var reader = new StreamReader(resultStream))
                {
                    var result = await reader.ReadToEndAsync();

                    var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result),
                        new System.Xml.XmlDictionaryReaderQuotas());

                    var json = await Task<XElement>.Factory.StartNew(() => XElement.Load(jsonReader));

                    return await Task.Factory.StartNew(() => json.XPathSelectElement("resources")?.Elements().First(x => x.XPathSelectElement("name")?.Value == "ietf-language-tags_json").XPathSelectElement("path")?.Value);
                }
            }
        }

        #endregion
    }
}