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
    /// <summary>
    /// Interaction logic for Localization.xaml
    /// </summary>
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
                var imageItem = new ImageListBoxItem();
                imageItem.Tag = resourceDictionary.Source.OriginalString;
                imageItem.Content = resourceDictionary.Source.OriginalString;

                if (resourceDictionary.Source.OriginalString.Contains("StringResource"))
                {
                    imageItem.Image = FindResource("Vector.Translate") as Canvas;

                    #region Name

                    var subs = resourceDictionary.Source.OriginalString.Substring(resourceDictionary.Source.OriginalString.IndexOf("StringResource"));
                    var pieces = subs.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);

                    if (pieces.Length == 2)
                    {
                        imageItem.Author = "Recognized as English";
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
            if (LocalizationHelper.Move(ResourceListBox.SelectedIndex, true))
            {
                var selectedIndex = ResourceListBox.SelectedIndex;

                var selected = ResourceListBox.Items[selectedIndex];

                ResourceListBox.Items.RemoveAt(selectedIndex);
                ResourceListBox.Items.Insert(selectedIndex - 1, selected);
            }
        }

        private void MoveDown_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (LocalizationHelper.Move(ResourceListBox.SelectedIndex, false))
            {
                var selectedIndex = ResourceListBox.SelectedIndex;

                var selected = ResourceListBox.Items[selectedIndex];

                ResourceListBox.Items.RemoveAt(selectedIndex);
                ResourceListBox.Items.Insert(selectedIndex + 1, selected);
            }
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.AddExtension = true;
            sfd.Filter = "Resource Dictionary (*.xaml)|*.xaml";
            sfd.Title = "Save Resource Dictionary";

            var source = ((ImageListBoxItem)ResourceListBox.SelectedItem).Content.ToString();
            var subs = source.Substring(source.IndexOf("StringResource"));

            sfd.FileName = subs;

            var result = sfd.ShowDialog();

            if (result.HasValue && result.Value)
                LocalizationHelper.SaveSelected(ResourceListBox.SelectedIndex, sfd.FileName);
        }

        private void Remove_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (LocalizationHelper.Remove(ResourceListBox.SelectedIndex))
            {
                ResourceListBox.Items.RemoveAt(ResourceListBox.SelectedIndex);
            }
        }

        private void Add_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = "Open a Resource Dictionay",
                Filter = "Resource Dictionay (*.xaml)|*.xaml;"
            };

            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                #region Validation

                var subs = ofd.FileName.Substring(ofd.FileName.IndexOf("StringResource"));

                if (Application.Current.Resources.MergedDictionaries.Any(x => x.Source.OriginalString.Contains(subs)))
                {
                    Dialog.Ok("Action Denied", "You can't add a resource with the same name.",
                        "Try renaming like: StringResource.[Language Code].xaml");

                    return;
                }

                var pieces = subs.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                if (pieces.Length != 3)
                {
                    Dialog.Ok("Action Denied", "Filename wrong format.",
                        "Try renaming like: StringResource.[Language Code].xaml");

                    return;
                }

                var culture = new CultureInfo(pieces[1]);

                if (culture.EnglishName.Contains("Unknown"))
                {
                    Dialog.Ok("Action Denied", "Unknown Language.", $"The {pieces[1]} was not recognized as a valid language code.");

                    return;
                }

                #endregion

                if (LocalizationHelper.ImportStringResource(ofd.FileName))
                {
                    var resourceDictionary = Application.Current.Resources.MergedDictionaries.LastOrDefault();

                    var imageItem = new ImageListBoxItem();
                    imageItem.Tag = resourceDictionary.Source.OriginalString;
                    imageItem.Content = resourceDictionary.Source.OriginalString;
                    imageItem.Image = FindResource("Vector.Translate") as Canvas;
                    imageItem.Author = "Recognized as " + pieces[1];
                    ResourceListBox.Items.Add(imageItem);
                }
            }
        }

        #endregion
    }
}
