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
        
        private async void Localization_Loaded(object sender, RoutedEventArgs e)
        {
            AddButton.IsEnabled = false;
            SaveButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            DownButton.IsEnabled = false;
            UpButton.IsEnabled = false;
            OkButton.IsEnabled = false;

            var actualIndex = 0;
            foreach (var resourceDictionary in Application.Current.Resources.MergedDictionaries)
            {
                //If it's not a localization resource, ignore it.
                if (resourceDictionary.Source?.OriginalString.Contains("StringResources") != true)
                {
                    actualIndex++;
                    continue;
                }

                var imageItem = new ImageListBoxItem
                {
                    Content = resourceDictionary.Source.OriginalString,
                    Image = FindResource("Vector.Translate") as Canvas,
                    Index = actualIndex++,
                    ShowMarkOnSelection = false
                };

                #region Language code

                var pieces = resourceDictionary.Source.OriginalString.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                if (pieces.Length == 3 || pieces.Length == 4)
                    imageItem.Author = LocalizationHelper.GetWithFormat("S.Localization.Recognized", "Recognized as {0}", pieces[1]);
                else
                    imageItem.Author = LocalizationHelper.Get("S.Localization.NotRecognized");

                #endregion

                ResourceListBox.Items.Add(imageItem);
            }

            //Selects the last item on the list.
            ResourceListBox.SelectedItem = ResourceListBox.Items.Cast<ImageListBoxItem>().LastOrDefault(w => w.IsEnabled);
            
            if (ResourceListBox.SelectedItem != null)
                ResourceListBox.ScrollIntoView(ResourceListBox.SelectedItem);

            StatusBand.Info(LocalizationHelper.Get("S.Localization.GettingCodes"));

            _cultures = await GetProperCulturesAsync();

            AddButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;
            DownButton.IsEnabled = true;
            UpButton.IsEnabled = true;
            OkButton.IsEnabled = true;

            StatusBand.Hide();
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
            if (!(ResourceListBox.SelectedItem is ImageListBoxItem item))
                return;

            if (LocalizationHelper.Move(item.Index))
            {
                var selectedIndex = ResourceListBox.SelectedIndex;
                var selected = ResourceListBox.Items[selectedIndex];

                ResourceListBox.Items.RemoveAt(selectedIndex);
                ResourceListBox.Items.Insert(selectedIndex - 1, selected);
                ResourceListBox.SelectedItem = selected;

                //Reflects the new index to the item.
                UpdateIndexes();
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private void MoveDown_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!(ResourceListBox.SelectedItem is ImageListBoxItem item))
                return;

            if (LocalizationHelper.Move(item.Index, false))
            {
                var selectedIndex = ResourceListBox.SelectedIndex;
                var selected = ResourceListBox.Items[selectedIndex];

                ResourceListBox.Items.RemoveAt(selectedIndex);
                ResourceListBox.Items.Insert(selectedIndex + 1, selected);
                ResourceListBox.SelectedItem = selected;

                //Reflects the new index to the item.
                UpdateIndexes();
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private async void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StatusBand.Info(LocalizationHelper.Get("S.Localization.Exporting"));

            if (!(ResourceListBox.SelectedItem is ImageListBoxItem selected))
                return;

            var source = selected.Content.ToString();
            var subs = source.Substring(source.IndexOf("StringResources", StringComparison.InvariantCulture));

            var sfd = new SaveFileDialog
            {
                AddExtension = true,
                Filter = LocalizationHelper.Get("S.Localization.File.Resource") + " (*.xaml)|*.xaml",
                Title = LocalizationHelper.Get("S.Localization.SaveResource"),
                FileName = subs
            };

            var result = sfd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                try
                {
                    //Pass the UI parameters to the task.
                    var fileName = sfd.FileName;
                    var index = selected.Index;

                    await Task.Factory.StartNew(() => LocalizationHelper.SaveSelected(index, fileName));
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Impossible to save the resource");
                    Dialog.Ok("Impossible to Save", "Impossible to save the Xaml file", ex.Message, Icons.Warning);
                }
            }

            StatusBand.Hide();
            CommandManager.InvalidateRequerySuggested();
        }

        private void Remove_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!(ResourceListBox.SelectedItem is ImageListBoxItem item))
                return;

            if (LocalizationHelper.Remove(item.Index))
            {
                var current = ResourceListBox.SelectedIndex;
                ResourceListBox.Items.RemoveAt(ResourceListBox.SelectedIndex);

                //Adjust the actual index of the rest of the items.
                for (var index = current; index < ResourceListBox.Items.Count; index++)
                {
                    if (ResourceListBox.Items[index] is ImageListBoxItem res)
                        res.Index --;
                }
            }
            
            CommandManager.InvalidateRequerySuggested();
        }

        private async void Add_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = LocalizationHelper.Get("S.Localization.OpenResource"),
                Filter = LocalizationHelper.Get("S.Localization.File.Resource") + " (*.xaml)|*.xaml;"
            };

            var result = ofd.ShowDialog();

            if (!result.HasValue || !result.Value) 
                return;

            #region Validations

            var position = ofd.FileName.IndexOf("StringResources", StringComparison.InvariantCulture);
            var subs = position > -1 ? ofd.FileName.Substring(position) : "";
            var pieces = subs.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            
            //Wrong filename format.
            if (position < 0 || pieces.Length != 3)
            {
                Dialog.Ok(Title, LocalizationHelper.Get("S.Localization.Warning.Name"), LocalizationHelper.Get("S.Localization.Warning.Name.Info"));
                StatusBand.Hide();
                return;
            }

            //Repeated language code.
            if (Application.Current.Resources.MergedDictionaries.Any(x => x.Source != null && x.Source.OriginalString.Contains(subs)))
            {
                Dialog.Ok(Title, LocalizationHelper.Get("S.Localization.Warning.Repeated"), LocalizationHelper.Get("S.Localization.Warning.Repeated.Info"));
                StatusBand.Hide();
                return;
            }

            try
            {
                var properCulture = await Task.Factory.StartNew(() => CheckSupportedCulture(pieces[1]));

                if (properCulture != pieces[1])
                {
                    Dialog.Ok(Title, LocalizationHelper.Get("S.Localization.Warning.Redundant"), LocalizationHelper.GetWithFormat("S.Localization.Warning.Redundant.Info", 
                        "The \"{0}\" code is redundant. Try using \"{1}\" instead.", pieces[1], properCulture));
                    StatusBand.Hide();
                    return;
                }
            }
            catch (CultureNotFoundException cn)
            {
                LogWriter.Log(cn, "Impossible to validade the resource name, culture not found");
                Dialog.Ok(Title, LocalizationHelper.Get("S.Localization.Warning.Unknown"), LocalizationHelper.GetWithFormat("S.Localization.Warning.Unknown.Info",
                    "The \"{0}\" and its family were not recognized as valid language codes.", pieces[1]));
                StatusBand.Hide();
                return;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to validade the resource name");
                Dialog.Ok(Title, LocalizationHelper.Get("S.Localization.Warning.NotPossible"), ex.Message);
                StatusBand.Hide();
                return;
            }

            #endregion

            StatusBand.Info(LocalizationHelper.Get("S.Localization.Importing"));

            try
            {
                var fileName = ofd.FileName;

                await Task.Factory.StartNew(() => LocalizationHelper.ImportStringResource(fileName));
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to import the resource");
                Dialog.Ok(Title, LocalizationHelper.Get("S.Localization.Warning.NotPossible"), ex.Message);
                StatusBand.Hide();
                return;
            }

            var resourceDictionary = Application.Current.Resources.MergedDictionaries.LastOrDefault();

            var imageItem = new ImageListBoxItem
            {
                Content = resourceDictionary?.Source.OriginalString ?? "...",
                Image = FindResource("Vector.Translate") as Canvas,
                Author = LocalizationHelper.GetWithFormat("S.Localization.Recognized", "Recognized as {0}", pieces[1]),
                Index = Application.Current.Resources.MergedDictionaries.Count - 1,
                ShowMarkOnSelection = false
            };

            StatusBand.Hide();

            ResourceListBox.Items.Add(imageItem);
            ResourceListBox.ScrollIntoView(imageItem);

            UpdateIndexes();

            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region Methods 

        private string CheckSupportedCulture(string cultureName)
        {
            //Using HashSet, because we can check if it contains string in O(1) time.
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
                var downloadedCodes = GetLanguageCodesOffline();
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

        private void UpdateIndexes()
        {
            var actualIndex = 0;
            for (var index = 0; index < Application.Current.Resources.MergedDictionaries.Count; index++)
            {
                var resourceDictionary = Application.Current.Resources.MergedDictionaries[index];

                //If it's not a localization resource, ignore it.
                if (resourceDictionary.Source?.OriginalString.Contains("StringResources") != true)
                    continue;

                if (ResourceListBox.Items[actualIndex] is ImageListBoxItem res)
                {
                    res.Index = index;
                    actualIndex++;
                }
            }
        }

        #endregion
    }
}