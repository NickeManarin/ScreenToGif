using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class Localization : Window
    {
        public Localization()
        {
            InitializeComponent();
        }

        #region Events
        
        private void Localization_OnLoaded(object sender, RoutedEventArgs e)
        {
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

                    var subs = resourceDictionary.Source.OriginalString.Substring(resourceDictionary.Source.OriginalString.IndexOf("StringResources"));
                    var pieces = subs.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);

                    if (pieces.Length != 3)
                    {
                        imageItem.Author = "Not recognized";
                    }
                    else if (pieces.Length == 3)
                    {
                        imageItem.Author = "Recognized as " + pieces[1];
                    }

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

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
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

            if (result.HasValue && result.Value)
                LocalizationHelper.SaveSelected(ResourceListBox.SelectedIndex, sfd.FileName);

            CommandManager.InvalidateRequerySuggested();
        }

        private void Remove_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (LocalizationHelper.Remove(ResourceListBox.SelectedIndex))
                ResourceListBox.Items.RemoveAt(ResourceListBox.SelectedIndex);

            CommandManager.InvalidateRequerySuggested();
        }

        private void Add_Executed(object sender, ExecutedRoutedEventArgs e)
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

            #region Validation

            if (!ofd.FileName.Contains("StringResources"))
            {
                Dialog.Ok("Action Denied", "The name of file does not follow a valid pattern.",
                    "Try renaming like (without the []): StringResources.[Language Code].xaml");

                return;
            }

            var subs = ofd.FileName.Substring(ofd.FileName.IndexOf("StringResources"));

            if (Application.Current.Resources.MergedDictionaries.Any(x => x.Source != null && x.Source.OriginalString.Contains(subs)))
            {
                Dialog.Ok("Action Denied", "You can't add a resource with the same name.",
                    "Try renaming like: StringResources.[Language Code].xaml");

                return;
            }

            var pieces = subs.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (pieces.Length != 3)
            {
                Dialog.Ok("Action Denied", "Filename with wrong format.",
                    "Try renaming like: StringResources.[Language Code].xaml");

                return;
            }

            var culture = new CultureInfo(pieces[1]);

            if (culture.EnglishName.Contains("Unknown"))
            {
                Dialog.Ok("Action Denied", "Unknown Language.", $"The \"{pieces[1]}\" was not recognized as a valid language code.");

                return;
            }

            #endregion

            if (!LocalizationHelper.ImportStringResource(ofd.FileName)) return;

            var resourceDictionary = Application.Current.Resources.MergedDictionaries.LastOrDefault();

            var imageItem = new ImageListBoxItem
            {
                Tag = resourceDictionary?.Source.OriginalString ?? "Unknown",
                Content = resourceDictionary?.Source.OriginalString ?? "Unknown",
                Image = FindResource("Vector.Translate") as Canvas,
                Author = "Recognized as " + pieces[1]
            };

            ResourceListBox.Items.Add(imageItem);
            ResourceListBox.ScrollIntoView(imageItem);

            CommandManager.InvalidateRequerySuggested();
        }

        #endregion
    }
}
