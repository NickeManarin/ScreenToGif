using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Xaml;
using ScreenToGif.Util.Writers;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Util
{
    public static class LocalizationHelper
    {
        public static void SelectCulture(string culture)
        {
            if (String.IsNullOrEmpty(culture))
                return;

            if (culture.Equals("auto"))
            {
                CultureInfo ci = CultureInfo.InstalledUICulture;
                culture = ci.Name;
            }

            //Copy all MergedDictionarys into a auxiliar list.
            var dictionaryList = Application.Current.Resources.MergedDictionaries.ToList();

            //Search for the specified culture.     
            string requestedCulture = string.Format("/Resources/Localization/StringResources.{0}.xaml", culture);
            var resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == requestedCulture);

            if (resourceDictionary == null)
            {
                //If not found, select our default language.             
                requestedCulture = "/Resources/Localization/StringResources.xaml";
                resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == requestedCulture);
            }

            //If we have the requested resource, remove it from the list and place at the end.     
            //Then this language will be our string table to use.      
            if (resourceDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }

            //Inform the threads of the new culture.     
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
        }

        public static void SaveDefaultResource(string path)
        {
            //Copy all MergedDictionarys into a auxiliar list.
            var dictionaryList = Application.Current.Resources.MergedDictionaries.ToList();

            try
            {
                //Search for the specified culture.     
                var resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == "/Resources/Localization/StringResources.xaml");

                if (resourceDictionary == null)
                    throw new CultureNotFoundException("String resource not found.");

                if (String.IsNullOrEmpty(path))
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
                if (String.IsNullOrEmpty(path))
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

                if (Application.Current.Resources.MergedDictionaries[selectedIndex].Source.OriginalString.Contains("StringResource.xaml"))
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
