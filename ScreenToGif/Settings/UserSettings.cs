using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Util.InterProcessChannel;
using ScreenToGif.Windows.Other;
using XamlWriter = System.Windows.Markup.XamlWriter;

namespace ScreenToGif.Settings
{
    internal class UserSettings : INotifyPropertyChanged
    {
        #region Variables

        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly object Lock = new object();

        public static UserSettings All { get; } = new UserSettings();

        public Version Version => Assembly.GetEntryAssembly()?.GetName().Version;

        public string VersionText => Version?.ToStringShort() ?? "0.0";

        private static ResourceDictionary _local;
        private static ResourceDictionary _appData;
        private static readonly ResourceDictionary Default;

        #endregion
        
        static UserSettings()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            //Tries to load both settings files (from the local or AppData folder).
            LoadSettings();

            //Reads the default settings (it's loaded by default).
            Default = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.EndsWith("/Settings.xaml"));
        }

        
        private static void LoadSettings()
        {
            //Paths.
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");
            var appData = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif"), "Settings.xaml");

            //Only creates an empty AppData settings file if there's no local settings defined.
            if (!File.Exists(local) && !File.Exists(appData))
            {
                var directory = Path.GetDirectoryName(appData);

                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                //Just creates a resource dictionary without any properties. 
                File.WriteAllText(appData, "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"></ResourceDictionary>");
            }

            //Loads AppData settings.
            if (File.Exists(appData))
            {
                _appData = Load(appData) ?? new ResourceDictionary();
                Application.Current.Resources.MergedDictionaries.Add(_appData);
            }

            //Loads Local settings.
            if (File.Exists(local))
            {
                _local = Load(local);

                if (_local != null)
                    Application.Current.Resources.MergedDictionaries.Add(_local);
            }
        }

        private static ResourceDictionary Load(string path)
        {
            try
            {
                #region Load settings from disk

                var doc = XDocument.Parse(File.ReadAllText(path));
                var properties = (doc.Root?.Descendants() ?? doc.Descendants()).Where(w => w.Parent == doc.Root).Select(GetProperty).ToList();

                #endregion

                #region Migrate

                var version = properties.FirstOrDefault(f => f.Key == "Version")?.Value ?? "0.0";

                Migration.Migrate(properties, version);

                #endregion

                #region Parse settings

                //Since the settings were migrated, add the current version.
                var resource = new ResourceDictionary
                {
                    { "Version", All.VersionText }
                };

                foreach (var property in properties)
                {
                    var value = ParseProperty(property);
                    
                    if (value != null)
                        resource.Add(property.Key, value);
                }

                #endregion

                return resource;
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to load and migrate the settings.");
                return null;
            }
        }

        private static Property GetProperty(XElement node)
        {
            var attributes = node.Attributes().Select(s => new Property { Key = s.Name.LocalName, Value = s.Value }).ToList();

            var prop = new Property
            {
                NameSpace = node.Name.NamespaceName,
                Type = node.Name.LocalName,
                Key = attributes.FirstOrDefault(f => f.Key == "Key")?.Value,
                Attributes = attributes,
                Value = node.Value
            };
            
            if (node.Name.LocalName.StartsWith("Null"))
                return null;

            if (!node.HasElements)
                return prop;

            //Flatten out attributes that are divided into another tag.
            if (attributes.Count == 0 && (prop.Type ?? "").Contains("."))
            {
                var inner = node.Elements().Select(GetProperty).FirstOrDefault();

                if (inner != null)
                {
                    prop.Key = prop.Type.Split('.').LastOrDefault();
                    prop.Type = inner.Type;
                    prop.NameSpace = inner.NameSpace;
                    prop.Children = inner.Children;
                    return prop;
                }
            }

            foreach (var element in node.Elements())
            {
                var innerElement = GetProperty(element);

                if (innerElement != null)
                    prop.Children.Add(innerElement);
            }

            return prop;
        }

        private static object ParseProperty(Property property)
        {
            try
            {
                //Do you know any other way to achieve this? Contact me via Github.

                switch (property.Type)
                {
                    case "String":
                        property.Value = property.Value.StartsWith("{}") ? property.Value.Substring(2) : property.Value;

                        if (property.Value == "{x:Null}")
                            return null;

                        return property.Value;
                    case "Boolean":
                        return Convert.ToBoolean(property.Value);
                    case "Int32":
                        return Convert.ToInt32(property.Value);
                    case "Double":
                        return Convert.ToDouble(property.Value);
                    case "Decimal":
                        return Convert.ToDecimal(property.Value);

                    case "Export":
                        return Enum.Parse(typeof(Export), property.Value);
                    case "Key":
                        return (property.Value ?? "").Length == 0 ? null : Enum.Parse(typeof(Key), property.Value);
                    case "ProgressType":
                        return Enum.Parse(typeof(ProgressType), property.Value);
                    case "StylusTip":
                        return Enum.Parse(typeof(StylusTip), property.Value);
                    case "AppTheme":
                        return Enum.Parse(typeof(AppTheme), property.Value);
                    case "WindowState":
                        return Enum.Parse(typeof(WindowState), property.Value);
                    case "CopyType":
                        return Enum.Parse(typeof(CopyType), property.Value);
                    case "HorizontalAlignment":
                        return Enum.Parse(typeof(HorizontalAlignment), property.Value);
                    case "VerticalAlignment":
                        return Enum.Parse(typeof(VerticalAlignment), property.Value);
                    case "CaptureFrequency":
                        return Enum.Parse(typeof(CaptureFrequency), property.Value);
                    case "ProxyType":
                        return Enum.Parse(typeof(ProxyType), property.Value);
                    case "PasteBehavior":
                        return Enum.Parse(typeof(PasteBehavior), property.Value);
                    case "ReduceDelayType":
                        return Enum.Parse(typeof(ReduceDelayType), property.Value);
                    case "DuplicatesRemovalType":
                        return Enum.Parse(typeof(DuplicatesRemovalType), property.Value);
                    case "DuplicatesDelayType":
                        return Enum.Parse(typeof(DuplicatesDelayType), property.Value);
                    case "Orientation":
                        return Enum.Parse(typeof(Orientation), property.Value);
                    case "ObfuscationMode":
                        return Enum.Parse(typeof(ObfuscationMode), property.Value);
                    case "FadeToType":
                        return Enum.Parse(typeof(FadeToType), property.Value);
                    case "CompressionLevel":
                        return Enum.Parse(typeof(CompressionLevel), property.Value);
                    case "TaskTypeEnum":
                        return Enum.Parse(typeof(DefaultTaskModel.TaskTypeEnum), property.Value);
                    case "DelayUpdateType":
                        return Enum.Parse(typeof(DelayUpdateType), property.Value);
                    case "UploadType":
                        return Enum.Parse(typeof(UploadType), property.Value);
                    case "EncoderType":
                        return Enum.Parse(typeof(EncoderType), property.Value);
                    case "PartialExportType":
                        return Enum.Parse(typeof(PartialExportType), property.Value);
                    case "VideoSettingsMode":
                        return Enum.Parse(typeof(VideoSettingsMode), property.Value);
                    case "VideoCodecs":
                        return Enum.Parse(typeof(VideoCodecs), property.Value);
                    case "DitherMethods":
                        return Enum.Parse(typeof(DitherMethods), property.Value);
                    case "PredictionMethods":
                        return Enum.Parse(typeof(PredictionMethods), property.Value);
                    case "VideoCodecPresets":
                        return Enum.Parse(typeof(VideoCodecPresets), property.Value);
                    case "HardwareAcceleration":
                        return Enum.Parse(typeof(HardwareAcceleration), property.Value);
                    case "RateUnit":
                        return Enum.Parse(typeof(RateUnit), property.Value);
                    case "VideoPixelFormats":
                        return Enum.Parse(typeof(VideoPixelFormats), property.Value);
                    case "Framerates":
                        return Enum.Parse(typeof(Framerates), property.Value);
                    case "Vsyncs":
                        return Enum.Parse(typeof(Vsyncs), property.Value);
                    case "ScalingMethod":
                        return Enum.Parse(typeof(ScalingMethod), property.Value);
                    case "ColorQuantizationType":
                        return Enum.Parse(typeof(ColorQuantizationType), property.Value);

                    case "FontWeight":
                        return new FontWeightConverter().ConvertFrom(property.Value);
                    case "FontFamily":
                        return new FontFamilyConverter().ConvertFrom(property.Value);
                    case "FontStyle":
                        return new FontStyleConverter().ConvertFrom(property.Value);
                    case "ModifierKeys":
                        return new ModifierKeysConverter().ConvertFrom(property.Value);
                    case "Color":
                        return ColorConverter.ConvertFromString(property.Value);
                    case "DoubleCollection":
                        return DoubleCollection.Parse(property.Value);
                    case "Rect":
                        return Rect.Parse(property.Value);
                    case "DateTime":
                        return DateTime.Parse(property.Value);
                    case "TimeSpan":
                        return TimeSpan.Parse(property.Value);

                    case "ArrayList":
                    {
                        var array = new ArrayList();

                        foreach (var child in property.Children)
                            array.Add(ParseProperty(child));

                        return array;
                    }

                    default:
                        return DeserializeProperty(property);
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to parse property", property);
                return null;
            }
        }
        
        private static object DeserializeProperty(Property property)
        {
            //This is not good... :(
            var nameSpace = string.IsNullOrWhiteSpace(property.NameSpace) ? "" : Regex.Match(property.NameSpace, "(?<=namespace:)[a-zA-Z0-9\\.]*((?=;assembly)?)").Value;
            var type = Type.GetType((string.IsNullOrWhiteSpace(nameSpace) ? "System.Windows." : (nameSpace + ".")) + property.Type, true);

            //Does not work with enums.
            
            if (property.Children.Count == 0 && property.Attributes.Count(w => w.Key != "Key") == 0)
                return Convert.ChangeType(property.Value, type);

            var instance = Activator.CreateInstance(type);

            //Sub-properties.
            foreach (var att in property.Attributes.Where(w => w.Key != "Key"))
            {
                if (string.IsNullOrEmpty(att.Key))
                {
                    LogWriter.Log("Property not identified in children", att, property);
                    continue;
                }

                var info = type.GetProperty(att.Key);

                if (info == null)
                {
                    LogWriter.Log("Property not available in object", att, property);
                    continue;
                }

                att.Type = info.PropertyType.Name;

                if (info.PropertyType == typeof(int?))
                {
                    if (int.TryParse(att.Value, out var intValue))
                        info.SetValue(instance, intValue, null);

                    continue;
                }

                if (info.PropertyType == typeof(DateTime?))
                {
                    if (DateTime.TryParse(att.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var deteTimeValue))
                        info.SetValue(instance, deteTimeValue, null);

                    continue;
                }

                if (info.PropertyType == typeof(bool?))
                {
                    if (bool.TryParse(att.Value, out var boolValue))
                        info.SetValue(instance, boolValue, null);

                    continue;
                }

                if (att.Type.StartsWith("Nullable"))
                {
                    LogWriter.Log("Property not identified.", att, property);
                    continue;
                }

                var value = ParseProperty(att);

                if (value != null)
                    info.SetValue(instance, value, null);
            }

            //Sub-properties that are in expanded tags.
            foreach (var child in property.Children)
            {
                if (string.IsNullOrEmpty(child.Key))
                {
                    LogWriter.Log("Property not identified in children", child, property);
                    continue;
                }

                var info = type.GetProperty(child.Key);

                if (info == null)
                {
                    LogWriter.Log("Property not available in object in children", child, property);
                    continue;
                }

                child.Type = info.PropertyType.Name;

                if (info.PropertyType == typeof(int?))
                {
                    if (int.TryParse(child.Value, out var intValue))
                        info.SetValue(instance, intValue, null);

                    continue;
                }

                if (info.PropertyType == typeof(DateTime?))
                {
                    if (DateTime.TryParse(child.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var deteTimeValue))
                        info.SetValue(instance, deteTimeValue, null);

                    continue;
                }

                if (info.PropertyType == typeof(bool?))
                {
                    if (bool.TryParse(child.Value, out var boolValue))
                        info.SetValue(instance, boolValue, null);

                    continue;
                }

                if (child.Type.StartsWith("Nullable"))
                {
                    LogWriter.Log("Property not identified in children.", child, property);
                    continue;
                }

                var innerChild = ParseProperty(child);

                if (innerChild != null)
                    info.SetValue(instance, innerChild, null);
            }
            
            return instance;
        }

        
        public static void Save(bool canForce = false)
        {
            //Only writes if non-default values were created. Should not write the default dictionary.
            if (_local == null && _appData == null)
                return;
            
            try
            {
                //Filename (Local or AppData).
                var folder = _local != null ? AppDomain.CurrentDomain.BaseDirectory : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif");
                var filename = Path.Combine(folder, "Settings.xaml");
                var backup = filename + ".bak";
                
                //Create folder.
                if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                //Create the backup, in case the save operation fails.
                if (File.Exists(filename))
                    File.Copy(filename, backup, true);

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "\t",
                    OmitXmlDeclaration = true,
                    CheckCharacters = true,
                    CloseOutput = true,
                    ConformanceLevel = ConformanceLevel.Fragment,
                    Encoding = Encoding.UTF8
                };
                
                //Serialize and save to disk.
                using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = XmlWriter.Create(fileStream, settings))
                    XamlWriter.Save(_local ?? _appData, writer);

                CheckIfSavedCorrectly(filename, backup, true);

                File.Delete(backup);
            }
            catch (UnauthorizedAccessException u)
            {
                LogWriter.Log(u, "Unauthorized to save the settings.");

                if (canForce)
                    Retry(_local ?? _appData, _local != null);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to save the settings.");
            }
        }

        private static async void Retry(ResourceDictionary dic, bool isLocal)
        {
            try
            {
                if (!Dialog.Ask(LocalizationHelper.Get("S.SavingSettings.Title"), LocalizationHelper.Get("S.SavingSettings.Instruction"), LocalizationHelper.Get("S.SavingSettings.Message")))
                    return;

                //Get a new instance, but elevated.
                var process = ProcessHelper.RestartAsAdminAdvanced("-settings");
                await Task.Delay(500);
                
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "\t",
                    OmitXmlDeclaration = true,
                    CheckCharacters = true,
                    CloseOutput = true,
                    ConformanceLevel = ConformanceLevel.Fragment,
                    Encoding = Encoding.UTF8
                };

                //Serialize the settings and pass to the new instance via IPC.
                using (var stream = new StringWriter())
                {
                    using (var writer = XmlWriter.Create(stream, settings))
                    {
                        XamlWriter.Save(dic, writer);
                        SettingsPersistenceChannel.SendMessage(stream.ToString(), isLocal);
                    }
                }

                //Since the other instance only exists to save the settings (no interface is displayed), the process must be stopped.
                process.Kill();
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to retry to save the settings.");
            }
        }
        
        public void SaveFromAnotherInstance(string serialized, bool isLocal)
        {
            try
            {
                //Filename (Local or AppData).
                var folder = _local != null ? AppDomain.CurrentDomain.BaseDirectory : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif");
                var filename = Path.Combine(folder, "Settings.xaml");
                var backup = filename + ".bak";
                
                //Create folder.
                if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                //Create the backup, in case the save operation fails.
                if (File.Exists(filename))
                    File.Copy(filename, backup, true);

                //Serialize and save to disk.
                using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
                        writer.Write(serialized);

                CheckIfSavedCorrectly(filename, backup);
               
                File.Delete(backup);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to retry saving the settings.");
            }
        }
        
        private static void CheckIfSavedCorrectly(string filename, string backup, bool throwException = false)
        {
            try
            {
                var content = File.ReadAllText(filename);

                if (content.All(x => x == '\0'))
                {
                    LogWriter.Log("Settings disk persistence failed.", content);
                    File.Copy(backup, filename, true);
                    
                    if (throwException)
                        throw new UnauthorizedAccessException("The file had garbage inside it.");
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to check if the settings file was saved correctly.");
            }
        }

        
        public static void CreateLocalSettings()
        {
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");

            if (!File.Exists(local))
                File.WriteAllText(local, "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"></ResourceDictionary>");

            _local = new ResourceDictionary();
        }

        public static void RemoveLocalSettings()
        {
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");

            if (File.Exists(local))
                File.Delete(local);

            _local = null; //TODO: Should I remove from the merged dictionaries?
        }

        public static void RemoveAppDataSettings()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif", "Settings.xaml");

            if (File.Exists(appData))
                File.Delete(appData);

            _appData = null; //TODO: Should I remove from the merged dictionaries?
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
                    {
                        if (value != null)
                            _local.Add(key, value);
                    }
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
                    {
                        if (value != null)
                            _appData.Add(key, value);
                    }
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

        #region Startup

        public double StartupTop
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double StartupLeft
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double StartupHeight
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double StartupWidth
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public WindowState StartupWindowState
        {
            get => (WindowState)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Recorder

        public Rect SelectedRegion
        {
            get => (Rect)GetValue();
            set => SetValue(value);
        }

        public double SelectedRegionScale
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public int RecorderModeIndex
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int LatestFps
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public double RecorderLeft
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double RecorderTop
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public int RecorderWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int RecorderHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Board

        public int BoardWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int BoardHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public Color BoardColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int BoardStylusHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int BoardStylusWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip BoardStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        public bool BoardFitToCurve
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool BoardIsHighlighter
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int BoardEraserHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int BoardEraserWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip BoardEraserStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Insert

        public Color InsertFillColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int LatestFpsImport
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Video source

        public int VideoImporter
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Feedback

        public string LatestFeedbackEmail
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        #endregion


        #region Options • Application

        public bool SingleInstance
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool StartMinimized
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        /// <summary>
        /// The homepage of the app:
        /// 0 - Startup window.
        /// 1 - Recorder window.
        /// 2 - Webcam window.
        /// 3 - Board window.
        /// 4 - Editor window.
        /// </summary>
        public int StartUp
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool NotifyWhileClosingApp
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool DisableHardwareAcceleration
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool CheckForTranslationUpdates
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool CheckForUpdates
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool PortableUpdate
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool ForceUpdateAsAdmin
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool InstallUpdates
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool PromptToInstall
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool ShowNotificationIcon
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool KeepOpen
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        /// <summary>
        /// 0: Do nothing.
        /// 1: Open a new window.
        /// 2: Toggle Minimize/Maximize all windows.
        /// 3: Minimize all windows.
        /// 4: Maximize all windows.
        /// </summary>
        public int LeftClickAction
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        /// <summary>
        /// 0: None.
        /// 1: Startup
        /// 2: Screen recorder
        /// 3: Webcam recorder
        /// 4: Board recorder
        /// 5: Editor
        /// </summary>
        public int LeftOpenWindow
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int DoubleLeftClickAction
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int DoubleLeftOpenWindow
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int MiddleClickAction
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int MiddleOpenWindow
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        //Workarounds.

        public bool WorkaroundQuota
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Options • Recorder

        public bool NewRecorder
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool RecorderThinMode
        {
            get => (bool)GetValue(defaultValue: false);
            set => SetValue(value);
        }

        public bool Magnifier
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool AnimateRecorderBorder
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool EnableSelectionPanning
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool RecorderCompactMode
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool RecorderDisplayDiscard
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool FallThroughOtherScreens
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public CaptureFrequency CaptureFrequency
        {
            get => (CaptureFrequency)GetValue();
            set => SetValue(value);
        }

        /// <summary>
        /// The placyback speed of the capture frame, in the "manual" mode.
        /// </summary>
        public int PlaybackDelayManual
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        /// <summary>
        /// The placyback speed of the capture frame, in the "manual" mode.
        /// </summary>
        public int PlaybackDelayInteraction
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        /// <summary>
        /// The placyback speed of the capture frame, in the "per minute" mode.
        /// </summary>
        public int PlaybackDelayMinute
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        /// <summary>
        /// The placyback speed of the capture frame, in the "per hour" mode.
        /// </summary>
        public int PlaybackDelayHour
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool FixedFrameRate
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool OnlyCaptureChanges
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool UseDesktopDuplication
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool UseMemoryCache
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public CompressionLevel CaptureCompression
        {
            get => (CompressionLevel)GetValue();
            set => SetValue(value);
        }

        public int MemoryCacheSize
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool PreventBlackFrames
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public AppTheme MainTheme
        {
            get => (AppTheme)GetValue();
            set => SetValue(value);
        }

        public Color RecorderBackground
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color RecorderForeground
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color BoardGridBackground
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color BoardGridColor1
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color BoardGridColor2
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Rect BoardGridSize
        {
            get => (Rect)GetValue();
            set => SetValue(value);
        }

        public bool ShowCursor
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool UsePreStart
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int PreStartValue
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool AsyncRecording
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool RemoteImprovement
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        //Guidelines.
        public bool DisplayThirdsGuideline
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public double ThirdsGuidelineThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color ThirdsGuidelineColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public DoubleCollection ThirdsGuidelineStrokeDashArray
        {
            get => (DoubleCollection)GetValue();
            set => SetValue(value);
        }

        public bool DisplayCrosshairGuideline
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public double CrosshairGuidelineThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color CrosshairGuidelineColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public DoubleCollection CrosshairGuidelineStrokeDashArray
        {
            get => (DoubleCollection)GetValue();
            set => SetValue(value);
        }

        //Other.
        public bool RecorderRememberSize
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool RecorderRememberPosition
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool CursorFollowing
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int FollowBuffer
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int FollowBufferInvisible
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool NotifyRecordingDiscard
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Options • Editor

        public Color GridColor1
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color GridColor2
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Rect GridSize
        {
            get => (Rect)GetValue();
            set => SetValue(value);
        }

        public bool DisplayEncoder
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool EditorExtendChrome
        {
            get => (bool)GetValue(defaultValue: false);
            set => SetValue(value);
        }

        public bool AutomaticallySizeOnContent
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool AutomaticallyFitImage
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool NotifyFrameDeletion
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool NotifyProjectDiscard
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool NotifyWhileClosingEditor
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool TripleClickSelection
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool DrawOutlineOutside
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool DropFramesDuringPreviewIfBehind
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool SetHistoryLimit
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int HistoryLimit
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Options • Automated Tasks

        public ArrayList AutomatedTasksList
        {
            get => (ArrayList)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Options • Shortcuts

        public Key RecorderShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys RecorderModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key WebcamRecorderShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys WebcamRecorderModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key BoardRecorderShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys BoardRecorderModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key EditorShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys EditorModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key OptionsShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys OptionsModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key ExitShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys ExitModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }


        public Key StartPauseShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys StartPauseModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key StopShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys StopModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key DiscardShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys DiscardModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public Key FollowShortcut
        {
            get => (Key)GetValue(defaultValue: Key.None);
            set => SetValue(value);
        }

        public ModifierKeys FollowModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        public ModifierKeys DisableFollowModifiers
        {
            get => (ModifierKeys)GetValue(defaultValue: ModifierKeys.None);
            set => SetValue(value);
        }

        #endregion

        #region Options • Language

        public string LanguageCode
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Options • Storage

        public string TemporaryFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string LogsFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string TemporaryFolderResolved
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TemporaryFolder))
                    TemporaryFolder = "%temp%";

                return Environment.ExpandEnvironmentVariables(TemporaryFolder);
            }
        }

        public bool DeleteCacheWhenClosing
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool AskDeleteCacheWhenClosing
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool AutomaticCleanUp
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int AutomaticCleanUpDays
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Options • Cloud

        //Proxy.
        public ProxyType ProxyMode
        {
            get => (ProxyType)GetValue(defaultValue: ProxyType.Disabled);
            set => SetValue(value);
        }

        public string ProxyHost
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public int ProxyPort
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public string ProxyUsername
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string ProxyPassword
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        //Presets.
        public ArrayList UploadPresets
        {
            get => (ArrayList)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Options • Extras

        public string FfmpegLocation
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string GifskiLocation
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string SharpDxLocationFolder
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        #endregion


        #region Editor

        public double EditorTop
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double EditorLeft
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double EditorHeight
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double EditorWidth
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public WindowState EditorWindowState
        {
            get => (WindowState)GetValue();
            set => SetValue(value);
        }

        public PasteBehavior PasteBehavior
        {
            get => (PasteBehavior)GetValue();
            set => SetValue(value);
        }

        public bool LoopedPlayback
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • New Animation

        public int NewAnimationWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int NewAnimationHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public Color NewAnimationColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Save As

        public bool IsSaveTypeExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public Export SaveType
        {
            get => (Export)GetValue();
            set => SetValue(value);
        }

        public ArrayList ExportPresets
        {
            get => (ArrayList)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Reduce Frame Count 

        public int ReduceFactor
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int ReduceCount
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public ReduceDelayType ReduceDelay
        {
            get => (ReduceDelayType)GetValue();
            set => SetValue(value);
        }

        public bool ReduceApplyToAll
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Remove Duplicates

        public double DuplicatesSimilarity
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public DuplicatesRemovalType DuplicatesRemoval
        {
            get => (DuplicatesRemovalType)GetValue();
            set => SetValue(value);
        }

        public DuplicatesDelayType DuplicatesDelay
        {
            get => (DuplicatesDelayType)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Delay

        public int OverrideDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int IncrementDecrementDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int ScaleDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Caption

        public string CaptionText
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public bool IsCaptionFontGroupExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public FontFamily CaptionFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle CaptionFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight CaptionFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double CaptionFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color CaptionFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public bool IsCaptionOutlineGroupExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public double CaptionOutlineThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color CaptionOutlineColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public bool IsCaptionLayoutGroupExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public VerticalAlignment CaptionVerticalAligment
        {
            get => (VerticalAlignment)GetValue();
            set => SetValue(value);
        }

        public HorizontalAlignment CaptionHorizontalAligment
        {
            get => (HorizontalAlignment)GetValue();
            set => SetValue(value);
        }

        public double CaptionMargin
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion
        
        #region Editor • Key Strokes

        public bool IsKeyStrokesKeysExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool KeyStrokesIgnoreNonModifiers
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool KeyStrokesIgnoreInjected
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool KeyStrokesEarlier
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesEarlierBy
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public string KeyStrokesSeparator
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public bool KeyStrokesExtended
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesDelay
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public bool IsKeyStrokesFontExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public FontFamily KeyStrokesFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle KeyStrokesFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight KeyStrokesFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color KeyStrokesFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public bool IsKeyStrokesOutlineExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesOutlineThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color KeyStrokesOutlineColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color KeyStrokesBackgroundColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public bool IsKeyStrokesLayoutExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public VerticalAlignment KeyStrokesVerticalAligment
        {
            get => (VerticalAlignment)GetValue();
            set => SetValue(value);
        }

        public HorizontalAlignment KeyStrokesHorizontalAligment
        {
            get => (HorizontalAlignment)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesMargin
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesPadding
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double KeyStrokesMinHeight
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Free Text

        public string FreeTextText
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public bool IsFreeTextFontGroupExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public FontFamily FreeTextFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle FreeTextFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight FreeTextFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double FreeTextFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color FreeTextFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public TextAlignment FreeTextTextAlignment
        {
            get => (TextAlignment)GetValue();
            set => SetValue(value);
        }

        public string FreeTextTextDecoration
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public bool IsFreeTextShadowGroupExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }
        public Color FreeTextShadowColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double FreeTextShadowDirection
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double FreeTextShadowBlurRadius
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double FreeTextShadowOpacity
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double FreeTextShadowDepth
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Title Frame

        public string TitleFrameText
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public bool IsTitleFrameFontGroupExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int TitleFrameDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public FontFamily TitleFrameFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle TitleFrameFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight TitleFrameFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double TitleFrameFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color TitleFrameFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public VerticalAlignment TitleFrameVerticalAligment
        {
            get => (VerticalAlignment)GetValue();
            set => SetValue(value);
        }

        public HorizontalAlignment TitleFrameHorizontalAligment
        {
            get => (HorizontalAlignment)GetValue();
            set => SetValue(value);
        }

        public Color TitleFrameBackgroundColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double TitleFrameMargin
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Free Drawing

        public int FreeDrawingPenWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int FreeDrawingPenHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public Color FreeDrawingColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public StylusTip FreeDrawingStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        public bool FreeDrawingIsHighlighter
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool FreeDrawingFitToCurve
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int FreeDrawingEraserWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int FreeDrawingEraserHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip FreeDrawingEraserStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Shapes

        public double ShapesThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color ShapesOutlineColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double ShapesRadius
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public int ShapesDashes
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public Color ShapesFillColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Progress

        public ProgressType ProgressType
        {
            get => (ProgressType)GetValue();
            set => SetValue(value);
        }

        public bool IsProgressFontGroupExpanded
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public FontFamily ProgressFontFamily
        {
            get => (FontFamily)GetValue();
            set => SetValue(value);
        }

        public FontStyle ProgressFontStyle
        {
            get => (FontStyle)GetValue();
            set => SetValue(value);
        }

        public FontWeight ProgressFontWeight
        {
            get => (FontWeight)GetValue();
            set => SetValue(value);
        }

        public double ProgressFontSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public Color ProgressFontColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color ProgressColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int ProgressPrecision
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int ProgressStartNumber
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public bool ProgressShowTotal
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public string ProgressFormat
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public string ProgressDateFormat
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public double ProgressThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public VerticalAlignment ProgressVerticalAligment
        {
            get => (VerticalAlignment)GetValue();
            set => SetValue(value);
        }

        public HorizontalAlignment ProgressHorizontalAligment
        {
            get => (HorizontalAlignment)GetValue();
            set => SetValue(value);
        }

        public Orientation ProgressOrientation
        {
            get => (Orientation)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Mouse Clicks

        public Color MouseClicksColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double MouseClicksWidth
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double MouseClicksHeight
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Border

        public Color BorderColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double BorderLeftThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double BorderTopThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double BorderRightThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double BorderBottomThickness
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        //public Color BorderBackgroundColor
        //{
        //    get => (Color)GetValue();
        //    set => SetValue(value);
        //}

        //public double BorderLeftRadius
        //{
        //    get => (double)GetValue();
        //    set => SetValue(value);
        //}

        //public double BorderTopRadius
        //{
        //    get => (double)GetValue();
        //    set => SetValue(value);
        //}

        //public double BorderRightRadius
        //{
        //    get => (double)GetValue();
        //    set => SetValue(value);
        //}

        //public double BorderBottomRadius
        //{
        //    get => (double)GetValue();
        //    set => SetValue(value);
        //}

        #endregion

        #region Editor • Shadow

        public Color ShadowColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public Color ShadowBackgroundColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public double ShadowDirection
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double ShadowBlurRadius
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double ShadowOpacity
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double ShadowDepth
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Obfuscate

        public ObfuscationMode ObfuscationMode
        {
            get => (ObfuscationMode)GetValue();
            set => SetValue(value);
        }

        public bool ObfuscationInvertedSelection
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int PixelSize
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int BlurLevel
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public double DarkenLevel
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double LightenLevel
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public bool UseMedian
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int ObfuscationSmoothnessRadius
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public double ObfuscationSmoothnessOpacity
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Watermark

        public string WatermarkFilePath
        {
            get => (string)GetValue();
            set => SetValue(value);
        }

        public double WatermarkOpacity
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double WatermarkSize
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double WatermarkTop
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        public double WatermarkLeft
        {
            get => (double)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Cinemagraph

        public Color CinemagraphColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int CinemagraphEraserWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int CinemagraphEraserHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip CinemagraphEraserStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        public bool CinemagraphIsHighlighter
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public bool CinemagraphFitToCurve
        {
            get => (bool)GetValue();
            set => SetValue(value);
        }

        public int CinemagraphPenWidth
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int CinemagraphPenHeight
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public StylusTip CinemagraphStylusTip
        {
            get => (StylusTip)GetValue();
            set => SetValue(value);
        }

        #endregion

        #region Editor • Transitions

        public FadeToType FadeToType
        {
            get => (FadeToType)GetValue();
            set => SetValue(value);
        }

        public Color FadeToColor
        {
            get => (Color)GetValue();
            set => SetValue(value);
        }

        public int FadeTransitionLength
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int FadeTransitionDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int SlideTransitionLength
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        public int SlideTransitionDelay
        {
            get => (int)GetValue();
            set => SetValue(value);
        }

        #endregion

        #endregion
    }
}