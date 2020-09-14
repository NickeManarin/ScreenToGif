using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using ScreenToGif.Util;

namespace ScreenToGif.Settings
{
    internal class UserSettings : INotifyPropertyChanged
    {
        //Locations: Static, AppData and Local

        //Load, property by property.
        //  How to format/ident correctly files?
        //  How to deal with complex objects? Arrays with other content inside.
        //Detect versions.
        //Execute migrations
        //  Plan what happens when:
        //      A property is added
        //      A property is changed (name, content format or type)
        //      A property is removed
        //Load all properties to the dictionary. 
        //Save, property by property
        //  Save in other custom format (Name, Type, Value).
        //  Save all new properties first to the Local then AppData files.
        //  If a property is set again to the original value, ignore it (dont save it).

        #region Variables

        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly object Lock = new object();

        public static UserSettings All { get; } = new UserSettings();

        public string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToStringShort() ?? "0.0";

        private static ResourceDictionary _local;
        private static ResourceDictionary _appData;
        private static readonly ResourceDictionary Default;

        #endregion

        static UserSettings()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            //Tries to load both settings files (local or from appdata folder).
            LoadSettings();

            //Reads the default settings (it's loaded by default).
            Default = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.EndsWith("/Settings.xaml"));
        }

        private static void LoadSettings()
        {
            //Paths.
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");
            var appData = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");

            //Loads AppData settings.
            if (File.Exists(appData))
            {
                _appData = Load(appData);
                Application.Current.Resources.MergedDictionaries.Add(_appData);
            }

            //Loads Local settings.
            if (File.Exists(local))
            {
                _local = Load(local);
                Application.Current.Resources.MergedDictionaries.Add(_local);
            }
        }

        private static ResourceDictionary Load(string path)
        {
            var final = "";
            
            try
            {
                #region Migrate

                using (var reader = File.OpenText(path))
                {
                    var line = "";
                    var property = "";
                    var appVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version();
                    var settingsVersion = new Version();

                    while ((line = reader.ReadLine()) != null)
                    {
                        //Ignore first, empty or last lines.
                        if (string.IsNullOrWhiteSpace(line) || line.Contains("ResourceDictionary"))
                        {
                            if (line.StartsWith("<ResourceDictionary"))
                            {
                                var index = line.IndexOf("Version", StringComparison.InvariantCulture);
                                
                                if (index >= 0)
                                {
                                    var part = line.Substring(index + 9);
                                    index = part.IndexOf("\"", StringComparison.InvariantCulture);
                                    settingsVersion = new Version(part.Substring(0, index));

                                    //Only apply migration (up or down) if the resource version is different from the app.
                                    if (appVersion != settingsVersion)
                                        break;
                                }
                                else
                                {
                                    line = line.Replace(">", $" Version=\"{appVersion}\">");
                                }
                            }

                            final += (final.Length > 0 ? Environment.NewLine : "") + line;
                            continue;
                        }

                        //There's three types of properties:
                        //1 line, with content:     < ></ >     Has key
                        //1 line, without content:  < />        No key
                        //2 lines, with content:    < > X </ >  Has key

                        //<StylusTip x:Key="BoardStylusTip">Rectangle</StylusTip>
                        //<sc:ArrayList Capacity="2" x:Key="AutomatedTasksList">
                        //  <stgm:MouseClicksModel ForegroundColor="#78FFFF00" Width="12" Height="12" TaskType="MouseClicks" IsEnabled="True" />
                        //  <stgm:BorderModel Color="#FF000000" LeftThickness="1" TopThickness="1" RightThickness="1" BottomThickness="1" TaskType="Border" IsEnabled="True" />
                        //</sc:ArrayList>

                        //Start of property.
                        if (line.Contains("<") && line.Contains("x:Key"))
                        {
                            property = line.TrimStart();

                            //End of simple property.
                            if (line.Contains("</"))
                                final += Environment.NewLine + Migrate(property, appVersion, settingsVersion);

                            continue;
                        }

                        property += Environment.NewLine + line;

                        //End of complex property.
                        if (line.Contains("</"))
                            final += Environment.NewLine + Migrate(property, appVersion, settingsVersion, true);
                    }
                }

                #endregion
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to migrate the settings.");    
            }
            
            #region Read



            #endregion

            //<s:Double x:Key="ObfuscationSmoothnessOpacity">100</s:Double>
            //<s:Double xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:s="clr-namespace:System;assembly=mscorlib" x:Key="ObfuscationSmoothnessOpacity">100</s:Double>

            //using (var fs = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(line)))
            //{
            //    var a = System.Windows.Markup.XamlReader.Load(fs);
            //}

            return null;
        }

        private static List<string> ReadNamespaces(string line)
        {
            var parts = line.Replace("<ResourceDictionary ", "").Replace("/>", "").Split(new []{ ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return parts.ToList();
        }

        private static string Migrate(string data, Version appVersion, Version settingsVersion, bool hasComplex = false)
        {
            if (hasComplex)
            {

                return data;
            }

            //Detect type: '<[] '
            //Detect key: 'x:Key="[]"'
            //Detect value: '>[]<'

            return data;
        }

        private static object GetValue([CallerMemberName] string key = "", object defaultValue = null)
        {
            if (Default == null)
                return defaultValue;

            if (Application.Current == null || Application.Current.Resources.Count == 0)
                return Default[key];

            if (Application.Current.Resources.Contains(key))
                return Application.Current.Resources[key];

            return Default[key] ?? defaultValue;
        }

        private static void SetValue(object value, [CallerMemberName] string key = "")
        {
            lock (Lock)
            {
                //Updates or inserts the value to the Local resource.
                if (_local != null)
                {
                    if (_local.Contains(key))
                    {
                        _local[key] = value;

                        //If the value is being set to null, remove it.
                        if (value == null && (!Default.Contains(key) || Default[key] == null))
                            _local.Remove(key);
                    }
                    else
                        _local.Add(key, value);
                }

                //Updates or inserts the value to the AppData resource.
                if (_appData != null)
                {
                    if (_appData.Contains(key))
                    {
                        _appData[key] = value;

                        //If the value is being set to null, remove it.
                        if (value == null && (!Default.Contains(key) || Default[key] == null))
                            _appData.Remove(key);
                    }
                    else
                        _appData.Add(key, value);
                }

                //Updates/Adds the current value of the resource.
                if (Application.Current.Resources.Contains(key))
                    Application.Current.Resources[key] = value;
                else
                    Application.Current.Resources.Add(key, value);

                All.OnPropertyChanged(key);
            }
        }

        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }





        #region Properties

        public double StartupTop
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion
    }
}