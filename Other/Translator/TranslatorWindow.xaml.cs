using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Markup;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
using Translator.Util;
using XamlReader = System.Windows.Markup.XamlReader;

namespace Translator;

public partial class TranslatorWindow : Window
{
    private readonly List<ResourceDictionary> _resourceList = new();
    private IEnumerable<string> _cultures;
    private ObservableCollection<Translation> _translationList = new();
    private string _tempPath;
    private string _resourceTemplate;

    public TranslatorWindow()
    {
        InitializeComponent();
    }

    #region Events

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PrepareTempPath();

        OpenButton.IsEnabled = false;
        RefreshButton.IsEnabled = false;
        ToComboBox.IsEnabled = false;

        #region Languages

        FromComboBox.Text = "Loading...";
        ToComboBox.Text = "Loading...";

        StatusBand.Info("Downloading English resource file...");

        //We have to get english resource first in case we import first without refreshing
        await DownloadSingleResourceAsync("en");

        StatusBand.Info("Loading language codes...");

        _cultures = await GetProperCulturesAsync();
        var languageList = await Task.Factory.StartNew(() => _cultures.Select(x => new Culture { Code = x, Name = CultureInfo.GetCultureInfo(x).DisplayName }).ToList());
        //var languageList = CultureInfo.GetCultures(CultureTypes.AllCultures).Select(x => new Culture { Code = x.IetfLanguageTag, Name = x.EnglishName }).ToList();

        FromComboBox.ItemsSource = languageList;
        ToComboBox.ItemsSource = languageList;
        ToComboBox.Text = null;
        FromComboBox.SelectedIndex = languageList.FindIndex(x => x.Code == "en");

        StatusBand.Hide();

        #endregion

        OpenButton.IsEnabled = true;
        RefreshButton.IsEnabled = true;
        ToComboBox.IsEnabled = true;

        ToComboBox.Focus();
    }

    private void TutorialHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start("https://github.com/NickeManarin/ScreenToGif/wiki/Localization");
        }
        catch (Exception ex)
        {
            Dialog.Ok("Translator", "Tutorial", "Error while trying to open the tutorial link");
        }
    }

    private void NewLineHyperlink_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText("&#10;");
    }

    private void ComboBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return || e.Key == Key.Enter)
        {
            e.Handled = true;
            RefreshButton.Focus();
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        var baseCulture = FromComboBox.SelectedValue as string;

        if (ToComboBox.SelectedValue is not string specificCulture)
        {
            StatusBand.Info("You need to select a target language to load the translations.");
            return;
        }

        HeaderLabel.Content = "Downloading resources...";
        StatusBand.Info("Downloading selected translations...");

        await DownloadResourcesAsync(baseCulture, specificCulture);
        ShowTranslations(baseCulture, specificCulture);

        HeaderLabel.Content = "Translator";
        BaseDataGrid.IsEnabled = true;
        StatusBand.Hide();
    }

    private void Itens_GotFocus(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TextBox ue)
            return;

        ue.Dispatcher.BeginInvoke(DispatcherPriority.Send, () => ue.SelectAll());

        BaseDataGrid.SelectedItem = ((FrameworkElement)sender).DataContext;
    }

    private void Item_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var source = e.OriginalSource as TextBox;

        if (source == null)
            return;

        //Back, up.
        if (e.Key == Key.Up || e.Key is Key.Enter or Key.Return && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
        {
            source.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
            BaseDataGrid.BeginEdit();

            var current = DataGridHelper.GetDataGridCell(BaseDataGrid.CurrentCell);

            current?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));

            e.Handled = true;
            return;
        }

        //Back, left.
        if ((e.Key == Key.Left && (source.CaretIndex == 0 || source.IsReadOnly)) || (e.Key == Key.Tab && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))))
        {
            source.MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));
            BaseDataGrid.BeginEdit();

            var current = DataGridHelper.GetDataGridCell(BaseDataGrid.CurrentCell);

            current?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));

            e.Handled = true;
            return;
        }

        //Next, down.
        if (e.Key == Key.Down || e.Key == Key.Enter || e.Key == Key.Return)
        {
            source.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
            BaseDataGrid.BeginEdit();

            var current = DataGridHelper.GetDataGridCell(BaseDataGrid.CurrentCell);

            current?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));

            e.Handled = true;
            return;
        }

        //Next, right. OLD (e.Key == Key.Right && (source.CaretIndex == source.Text.Length - 1 || source.IsReadOnly)) ||
        if (e.Key == Key.Tab)
        {
            source.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            BaseDataGrid.BeginEdit();

            var current = DataGridHelper.GetDataGridCell(BaseDataGrid.CurrentCell);

            current?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            e.Handled = true;
            return;
        }
    }

    private void Load_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }

    private void Export_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = BaseDataGrid.IsEnabled && ToComboBox.SelectedValue != null && BaseDataGrid.Items.Count > 0;
    }

    private async void Load_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            AddExtension = true,
            CheckFileExists = true,
            Title = "Open a Resource Dictionary",
            Filter = "Resource Dictionary (*.xaml)|*.xaml;",
            InitialDirectory = Path.GetFullPath(_tempPath)
        };

        var result = ofd.ShowDialog();

        if (!result.HasValue || !result.Value)
            return;

        //Will save the file to other folder.
        var tempFile = Path.Combine(_tempPath, "Temp", Path.GetFileName(ofd.FileName));

        Directory.CreateDirectory(Path.Combine(_tempPath, "Temp"));

        //Replaces the special chars.
        var text = await Task.Factory.StartNew(() => File.ReadAllText(ofd.FileName, Encoding.UTF8).Replace("&#", "&amp;#").Replace("<!--<!--", "<!--").Replace("-->-->", "-->"));
        await Task.Factory.StartNew(() => File.WriteAllText(tempFile, text, Encoding.UTF8));

        var dictionary = await Task.Factory.StartNew(() => new ResourceDictionary { Source = new Uri(Path.GetFullPath(tempFile), UriKind.Absolute) });
        _resourceList.Add(dictionary);

        var baseCulture = FromComboBox.SelectedValue as string;
        var specificCulture = Path.GetFileName(ofd.FileName).Replace("StringResources.", "").Replace(".xaml", "");

        string properCulture;

        //Catching here, because we can access UI thread easily here to show dialogs
        try
        {
            properCulture = await Task.Factory.StartNew(() => CheckSupportedCulture(specificCulture));
        }
        catch (CultureNotFoundException)
        {
            Dialog.Ok("Action Denied", "Unknown Language.",
                $"The \"{specificCulture}\" and its family were not recognized as a valid language codes.");

            return;
        }
        catch (Exception ex)
        {
            Dialog.Ok("Action Denied", "Error checking culture.", ex.Message);

            return;
        }

        if (properCulture != specificCulture)
        {
            Dialog.Ok("Action Denied", "Redundant Language Code.",
                $"The \"{specificCulture}\" code is redundant. Try using \'{properCulture}\" instead");

            return;
        }

        ToComboBox.SelectedValue = specificCulture;

        ShowTranslations(baseCulture, specificCulture);

        BaseDataGrid.IsEnabled = true;
    }

    private async void Export_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            AddExtension = true,
            Filter = "Resource Dictionary (*.xaml)|*.xaml",
            Title = "Save Resource Dictionary",
            FileName = $"StringResources.{ToComboBox.SelectedValue}.xaml"
        };

        var result = sfd.ShowDialog();

        if (!result.HasValue || !result.Value)
            return;

        BaseDataGrid.IsEnabled = false;
        StatusBand.Info("Exporting translation...");

        var fileName = sfd.FileName;
        var saved = await Task.Factory.StartNew(() => ExportTranslation(fileName));

        BaseDataGrid.IsEnabled = true;

        if (saved)
            StatusBand.Info("Translation saved!");
        else
            StatusBand.Hide();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (BaseDataGrid.Items.Count > 0 && !Dialog.Ask("Translator", "Do you really wish to close?", "Don't forget to export your translation, if you started translating but not exported yet."))
            e.Cancel = true;
    }

    #endregion

    #region Methods

    private void PrepareTempPath()
    {
        _tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ScreenToGif", "Resources");

        if (!Directory.Exists(_tempPath))
            Directory.CreateDirectory(_tempPath);
    }

    private async Task DownloadSingleResourceAsync(string culture)
    {
        try
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/NickeManarin/ScreenToGif/contents/ScreenToGif/Resources/Localization");
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";

            var response = (HttpWebResponse)await request.GetResponseAsync();

            await using (var resultStream = response.GetResponseStream())
            {
                using (var reader = new StreamReader(resultStream))
                {
                    var result = await reader.ReadToEndAsync();

                    var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result),
                        new System.Xml.XmlDictionaryReaderQuotas());

                    var json = await Task<XElement>.Factory.StartNew(() => XElement.Load(jsonReader));

                    var element = json.XPathSelectElement("/").Elements().FirstOrDefault(x => x.XPathSelectElement("name").Value.EndsWith(culture + ".xaml"));

                    if (element == null)
                        throw new WebException("File not found");

                    var name = element.XPathSelectElement("name").Value;
                    var downloadUrl = element.XPathSelectElement("download_url").Value;

                    await DownloadFileAsync(new Uri(downloadUrl), name);

                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
        catch (WebException web)
        {
            Dispatcher.Invoke(() => Dialog.Ok("Translator", "Translator - Downloading Single Resource", web.Message +
                Environment.NewLine + "Trying to load files already downloaded."));

            await LoadFilesAsync();
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => Dialog.Ok("Translator", "Translator - Downloading Single Resource", ex.Message));
        }

        GC.Collect();
    }

    private async Task DownloadResourcesAsync(string baseCulture, string specificCulture)
    {
        try
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/NickeManarin/ScreenToGif/contents/ScreenToGif/Resources/Localization");
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";

            var response = (HttpWebResponse)await request.GetResponseAsync();

            await using (var resultStream = response.GetResponseStream())
            {
                using (var reader = new StreamReader(resultStream))
                {
                    var result = await reader.ReadToEndAsync();

                    var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result),
                        new System.Xml.XmlDictionaryReaderQuotas());

                    var json = await Task<XElement>.Factory.StartNew(() => XElement.Load(jsonReader));

                    foreach (var element in json.XPathSelectElement("/").Elements())
                    {
                        var name = element.XPathSelectElement("name").Value;

                        if (string.IsNullOrEmpty(name) || (!name.EndsWith(baseCulture + ".xaml") && !name.EndsWith(specificCulture + ".xaml")))
                            continue;

                        var downloadUrl = element.XPathSelectElement("download_url").Value;

                        await DownloadFileAsync(new Uri(downloadUrl), name);
                    }

                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
        catch (WebException web)
        {
            Dispatcher.Invoke(() => Dialog.Ok("Translator", "Translator - Downloading Resources", web.Message +
                Environment.NewLine + "Trying to load files already downloaded."));

            await LoadFilesAsync();
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => Dialog.Ok("Translator", "Translator - Downloading Resources", ex.Message));
        }

        GC.Collect();
    }

    private async Task DownloadFileAsync2(Uri uri, string name)
    {
        try
        {
            var file = Path.Combine(Dispatcher.Invoke<string>(() => _tempPath), name);

            if (File.Exists(file))
                File.Delete(file);

            using (var webClient = new WebClient { Credentials = CredentialCache.DefaultNetworkCredentials })
                await webClient.DownloadFileTaskAsync(uri, file);

            //Saves the template for later, when exporting the translation.
            if (name.EndsWith("en.xaml"))
            {
                using (var sr = new StreamReader(file, Encoding.UTF8))
                {
                    _resourceTemplate = await sr.ReadToEndAsync();
                }
            }

            await using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var dictionary = await Task.Factory.StartNew(() => (ResourceDictionary)XamlReader.Load(fs, new ParserContext { XmlSpace = "preserve" }));
                //var dictionary = new ResourceDictionary();
                dictionary.Source = await Task.Factory.StartNew(() => new Uri(Path.GetFullPath(file), UriKind.Absolute));

                _resourceList.Add(dictionary);

                if (name.EndsWith("en.xaml"))
                    Application.Current.Resources.MergedDictionaries.Add(dictionary);
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => Dialog.Ok("Translator", "Translator - Downloading File", ex.Message));
        }
    }

    private async Task DownloadFileAsync(Uri uri, string name)
    {
        try
        {
            var file = Path.Combine(Dispatcher.Invoke<string>(() => _tempPath), name);

            if (File.Exists(file))
                File.Delete(file);

            using (var webClient = new WebClient { Credentials = CredentialCache.DefaultNetworkCredentials })
                await webClient.DownloadFileTaskAsync(uri, file);

            //Replaces the special chars.
            var text = await Task.Factory.StartNew(() => File.ReadAllText(file, Encoding.UTF8).Replace("&#", "&amp;#"));
            await Task.Factory.StartNew(() => File.WriteAllText(file, text, Encoding.UTF8));


            //Saves the template for later, when exporting the translation.
            if (name.EndsWith("en.xaml"))
                _resourceTemplate = text;

            var dictionary = await Task.Factory.StartNew(() => new ResourceDictionary { Source = new Uri(Path.GetFullPath(file), UriKind.Absolute) });

            _resourceList.Add(dictionary);

            //if (name.EndsWith("en.xaml"))
            //    Application.Current.Resources.MergedDictionaries.Add(dictionary);

            //using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            //{
            //    var dictionary = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(stream, new ParserContext { XmlSpace = "preserve" });
            //}
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => Dialog.Ok("Translator", "Translator - Downloading File", ex.Message));
        }
    }

    private async Task LoadFilesAsync()
    {
        try
        {
            var files = await Task.Factory.StartNew(() => Directory.EnumerateFiles(_tempPath, "*.xaml"));

            foreach (var file in files)
            {
                //Replaces the special chars.
                var text = await Task.Factory.StartNew(() => File.ReadAllText(file, Encoding.UTF8).Replace("&#", "&amp;#"));
                await Task.Factory.StartNew(() => File.WriteAllText(file, text, Encoding.UTF8));

                //Saves the template for later, when exporting the translation.
                if (file.EndsWith("en.xaml"))
                    _resourceTemplate = text;

                var dictionary = await Task.Factory.StartNew(() => new ResourceDictionary { Source = new Uri(Path.GetFullPath(file), UriKind.Absolute) });

                _resourceList.Add(dictionary);
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => Dialog.Ok("Translator", "Translator - Loading Offline File", ex.Message));
        }
    }

    private void ShowTranslations(string baseCulture, string specificCulture)
    {
        //var baseCulture = FromComboBox.SelectionBoxItem as Culture;
        //var specificCulture = ToComboBox.SelectionBoxItem as Culture;

        if (baseCulture == null)
        {
            _translationList = null;
            BaseDataGrid.ItemsSource = null;
            return;
        }

        var baseResource = _resourceList.FirstOrDefault(x => x.Source?.OriginalString.EndsWith(baseCulture + ".xaml") ?? false);
        //var baseResource = Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source?.OriginalString.EndsWith(baseCulture + ".xaml") ?? false);

        if (baseResource == null)
            return;

        if (specificCulture == null)
        {
            _translationList = new ObservableCollection<Translation>(baseResource.Keys.Cast<string>().Select(y => new Translation
            {
                Key = y,
                BaseText = (string)baseResource[y]
            }).OrderBy(o => o.Key).ToList());

            BaseDataGrid.ItemsSource = _translationList;
            return;
        }

        var specificResource = _resourceList.LastOrDefault(x => x.Source?.OriginalString.EndsWith(specificCulture + ".xaml") ?? false);

        if (specificResource == null)
        {
            _translationList = new ObservableCollection<Translation>(baseResource.Keys.Cast<string>().Select(y => new Translation
            {
                Key = y,
                BaseText = (string)baseResource[y]
            }).OrderBy(o => o.Key).ToList());

            BaseDataGrid.ItemsSource = _translationList;
            return;
        }

        _translationList = new ObservableCollection<Translation>(baseResource.Keys.Cast<string>().Select(y => new Translation
        {
            Key = y,
            BaseText = (string)baseResource[y],
            SpecificText = (string)specificResource[y]
        }).OrderBy(o => o.Key).ToList());

        BaseDataGrid.ItemsSource = _translationList;
    }

    private bool ExportTranslation(string path)
    {
        try
        {
            var lines = _resourceTemplate.Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                var keyIndex = lines[i].IndexOf(":Key=", StringComparison.Ordinal);

                if (lines[i].TrimStart().StartsWith("<!--") || keyIndex == -1)
                    continue;

                var keyAux = lines[i].Substring(keyIndex + 6);
                var key = keyAux.Substring(0, keyAux.IndexOf("\"", StringComparison.Ordinal));

                var translated = _translationList.FirstOrDefault(x => x.Key == key);

                //"    <s:String x:Key=\"Size\">Size</s:String>"
                if (string.IsNullOrWhiteSpace(translated?.SpecificText))
                    lines[i] = $"    <!--{lines[i].TrimStart()}-->"; //Comment the line.
                else
                    lines[i] = $"    <s:String x:Key=\"{key}\">{translated.SpecificText}</s:String>";
            }

            if (File.Exists(path))
                File.Delete(path);

            File.WriteAllText(path, string.Join(Environment.NewLine, lines).Replace("&amp;#", "&#"), Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => Dialog.Ok("Translator", "Translator - Saving Translation", ex.Message));
            return false;
        }
    }

    private string CheckSupportedCulture(string cultureName)
    {
        //Using HashSet, because we can check if it contains string in O(1) time
        //Only creating it takes some time,
        //but it's better than performing Contains multiple times on the list in the loop below
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

        throw new CultureNotFoundException();
    }

    private async Task<IEnumerable<string>> GetProperCulturesAsync()
    {
        var allCodes = await Task.Factory.StartNew(() => CultureInfo.GetCultures(CultureTypes.AllCultures).Where(x => !string.IsNullOrEmpty(x.Name)).Select(x => x.Name));

        try
        {
            var downloadedCodes = GetLanguageCodesOffline();
            var properCodes = await Task.Factory.StartNew(() => allCodes.Where(x => downloadedCodes.Contains(x)));
                
            return properCodes ?? allCodes;
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => Dialog.Ok("Translator", "Translator - Getting Language Codes", ex.Message +
                Environment.NewLine + "Loading all local language codes."));
        }

        GC.Collect();
        return allCodes;
    }

    private List<string> GetLanguageCodesOffline()
    {
        //I'm taking a shortcut in here.
        return ("af;af-NA;agq;ak;am;ar;ar-AE;ar-BH;ar-DJ;ar-DZ;ar-EG;ar-ER;ar-IL;ar-IQ;ar-JO;ar-KM;ar-KW;ar-LB;ar-LY;ar-MA;ar-MR;ar-OM;ar-PS;ar-QA;ar-SA;ar-SD;ar-SO;" +
                "ar-SS;ar-SY;ar-TD;ar-TN;ar-YE;as;asa;ast;az;az-Cyrl;bas;be;bem;bez;bg;bm;bn;bn-IN;bo;bo-IN;br;brx;bs;bs-Cyrl;ca;ca-FR;ccp;ce;ceb;cgg;chr;cs;cu;cy;da;" +
                "dav;de;de-AT;de-CH;de-IT;de-LI;de-LU;dje;dsb;dua;dyo;dz;ebu;ee;ee-TG;el;en;en-001;en-150;en-AE;en-AG;en-AI;en-AT;en-AU;en-BB;en-BE;en-BI;en-BM;en-BS;" +
                "en-BW;en-BZ;en-CA;en-CC;en-CH;en-CK;en-CM;en-CX;en-DE;en-DK;en-DM;en-ER;en-FI;en-FJ;en-FK;en-GB;en-GD;en-GG;en-GH;en-GI;en-GM;en-GU;en-GY;en-HK;en-IE;" +
                "en-IL;en-IM;en-IN;en-IO;en-JE;en-JM;en-KE;en-KI;en-KN;en-KY;en-LC;en-LR;en-LS;en-MG;en-MH;en-MO;en-MP;en-MS;en-MT;en-MU;en-MW;en-MY;en-NA;en-NF;en-NG;" +
                "en-NL;en-NR;en-NU;en-NZ;en-PG;en-PH;en-PK;en-PN;en-PW;en-RW;en-SB;en-SC;en-SD;en-SE;en-SG;en-SH;en-SI;en-SL;en-SS;en-SX;en-SZ;en-TK;en-TO;en-TT;en-TV;" +
                "en-TZ;en-UG;en-VC;en-VU;en-WS;en-ZA;en-ZM;en-ZW;eo;es;es-419;es-AR;es-BO;es-BR;es-BZ;es-CL;es-CO;es-CR;es-CU;es-DO;es-EC;es-GQ;es-GT;es-HN;es-MX;es-NI;" +
                "es-PA;es-PE;es-PH;es-PR;es-PY;es-SV;es-US;es-UY;es-VE;et;eu;ewo;fa;ff;ff-Latn-GH;ff-Latn-GM;ff-Latn-GN;ff-Latn-LR;ff-Latn-MR;ff-Latn-NG;ff-Latn-SL;fi;fil;" +
                "fo;fo-DK;fr;fr-BE;fr-BI;fr-CA;fr-CD;fr-CH;fr-CI;fr-CM;fr-DJ;fr-DZ;fr-GF;fr-GN;fr-HT;fr-KM;fr-LU;fr-MA;fr-MG;fr-ML;fr-MR;fr-MU;fr-RE;fr-RW;fr-SC;fr-SN;fr-SY;" +
                "fr-TD;fr-TN;fr-VU;fur;fy;ga;gd;gl;gsw;gu;guz;gv;ha;haw;he;hi;hr;hr-BA;hsb;hu;hy;ia;id;ig;ii;is;it;it-CH;ja;jgo;jmc;jv;ka;kab;kam;kde;kea;khq;ki;kk;kkj;kl;kln;" +
                "km;kn;ko;ko-KP;kok;ks;ksb;ksf;ksh;ku;kw;ky;lag;lb;lg;lkt;ln;ln-AO;lo;lrc;lrc-IQ;lt;lu;luo;luy;lv;mas;mas-TZ;mer;mfe;mg;mgh;mgo;mi;mk;ml;mn;mni;mr;ms;ms-BN;ms-SG;" +
                "mt;mua;my;mzn;naq;nb;nd;nds;nds-NL;ne;ne-IN;nl;nl-AW;nl-BE;nl-BQ;nl-CW;nl-SR;nl-SX;nmg;nn;nnh;nus;nyn;om;om-KE;or;os;os-RU;pa;pa-Arab;pl;prg;ps;ps-PK;pt;pt-AO;" +
                "pt-CV;pt-GW;pt-LU;pt-MO;pt-MZ;pt-PT;pt-ST;pt-TL;rm;rn;ro;ro-MD;rof;ru;ru-BY;ru-KG;ru-KZ;ru-MD;ru-UA;rw;rwk;sah;saq;sbp;sd;sd-Deva;se;se-FI;se-SE;seh;ses;sg;shi;" +
                "shi-Latn;si;sk;sl;smn;sn;so;so-DJ;so-ET;so-KE;sq;sq-MK;sq-XK;sr;sr-Cyrl-BA;sr-Cyrl-ME;sr-Cyrl-XK;sr-Latn;sr-Latn-BA;sr-Latn-ME;sr-Latn-XK;sv;sv-FI;sw;sw-CD;sw-KE;" +
                "sw-UG;ta;ta-LK;ta-MY;ta-SG;te;teo;teo-KE;tg;th;ti;ti-ER;tk;to;tr;tr-CY;tt;twq;tzm;ug;uk;ur;ur-IN;uz;uz-Arab;uz-Cyrl;vai;vai-Latn;vi;vo;vun;wae;wo;xh;xog;yav;yi;yo;" +
                "yo-BJ;zgh;zh;zh-Hans-HK;zh-Hans-MO;zh-Hant;zu").Split(';').ToList();
    }

    private async Task<IEnumerable<string>> GetLanguageCodesAsync()
    {
        var path = await GetLanguageCodesPathAsync();

        if (string.IsNullOrEmpty(path))
            throw new WebException("Can't get language codes. Path to language codes is null");

        var request = (HttpWebRequest)WebRequest.Create(path);
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";

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

                return await Task.Factory.StartNew(() => languages.Where(x => x.XPathSelectElement("defs").Value != "0").Select(x => x.XPathSelectElement("lang").Value));
            }
        }
    }

    private async Task<string> GetLanguageCodesPathAsync()
    {
        var request = (HttpWebRequest)WebRequest.Create("https://datahub.io/core/language-codes/datapackage.json");
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";

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

                return await Task.Factory.StartNew(() => json.XPathSelectElement("resources").Elements().First(x => x.XPathSelectElement("name").Value == "ietf-language-tags_json").XPathSelectElement("path").Value);
            }
        }
    }

    #endregion
}

internal class Culture
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string CodeName => Code.PadRight(3) + " - " + Name;
}

internal class Translation
{
    public string Key { get; set; }
    public string BaseText { get; set; }
    public string SpecificText { get; set; }
}