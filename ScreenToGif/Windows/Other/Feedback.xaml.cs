using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util.Extensions;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Windows.Other;

public partial class Feedback : Window
{
    private ObservableCollection<AttachmentListBoxItem> _fileList = new();

    public Feedback()
    {
        InitializeComponent();
    }

    #region Events

    private async void Feedback_Loaded(object sender, RoutedEventArgs e)
    {
        Cursor = Cursors.AppStarting;
        MainGrid.IsEnabled = false;

        await Task.Factory.StartNew(LoadFiles);

        MainGrid.IsEnabled = true;
        Cursor = Cursors.Arrow;
    }

    private void AddAttachmentButton_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Multiselect = true
        };

        var result = ofd.ShowDialog(this);

        if (!result.Value)
            return;

        foreach (var fileName in ofd.FileNames)
        {
            if (!_fileList.Any(x => x.Attachment.Equals(fileName)))
                _fileList.Add(new AttachmentListBoxItem(fileName));
        }
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        Send();
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        var preview = new FeedbackPreview
        {
            Owner = this, 
            Html = BuildBody(TitleTextBox.Text, MessageTextBox.Text, EmailTextBox.Text, IssueCheckBox.IsChecked == true, SuggestionCheckBox.IsChecked == true)
        };
        preview.ShowDialog();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void RemoveButton_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _fileList.RemoveAt(AttachmentListBox.SelectedIndex);
    }

    private void RemoveAllAttachmentButton_Click(object sender, RoutedEventArgs e)
    {
        _fileList.Clear();
    }

    #endregion

    #region Methods

    private async void LoadFiles()
    {
        try
        {
            var logFolder = Path.Combine(UserSettings.All.LogsFolder, "ScreenToGif", "Logs");
            var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xaml");
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenToGif", "Settings.xaml");

            var list = new List<string>();

            //Search for file inside the log folder.
            if (Directory.Exists(logFolder))
                list.AddRange(await Task.Factory.StartNew(() => Directory.GetFiles(logFolder).ToList()));

            //Add the Settings file too.
            if (File.Exists(local))
                list.Add(local);

            if (File.Exists(appData))
                list.Add(appData);

            Dispatcher.Invoke(() => AttachmentListBox.ItemsSource = _fileList = new ObservableCollection<AttachmentListBoxItem>(list.Select(s => new AttachmentListBoxItem(s))));
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to load the default attachments");
        }
    }

    private string GetProcessor()
    {
        var sb = new StringBuilder();
        sb.Append("<tr><th>Processor</th>");
        sb.Append("<th>Cores</th></tr>");

        try
        {
            using (var searcher = new ManagementObjectSearcher("select * from Win32_Processor"))
            {
                foreach (var obj in searcher.Get())
                {
                    sb.AppendFormat("<td class=\"textcentered\">{0}</td>", obj["Name"]);
                    sb.AppendFormat("<td class=\"textcentered\">{0} ({1})</td></tr>", obj["NumberOfCores"], obj["NumberOfLogicalProcessors"]);
                }
            }
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Error while trying to find a processor.");

            sb.AppendFormat("<tr><td colspan=\"2\" class=\"textcentered\">No processor found ({0})</td></tr>", e.Message);
        }

        return sb.ToString();
    }

    private string GetGraphicsAdapter()
    {
        var sb = new StringBuilder();
        sb.Append("<tr><th>Graphics adapter</th>");
        sb.Append("<th>Driver version</th></tr>");

        try
        {
            using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
            {
                foreach (var obj in searcher.Get())
                {
                    sb.AppendFormat("<td class=\"textcentered\">{0} @ {1}Hz</td>", obj["Name"], obj["CurrentRefreshRate"]);
                    sb.AppendFormat("<td class=\"textcentered\">{0}</td></tr>", obj["DriverVersion"]);
                }
            }
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Error while trying to find a graphics adapter.");

            sb.AppendFormat("<tr><td class=\"textcentered\">No graphics adapter found ({0})</td></tr>", e.Message);
        }

        return sb.ToString();
    }

    private string BuildBody(string title, string message, string email, bool issue, bool suggestion)
    {
        var sb = new StringBuilder();
        sb.Append("<html xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\">");
        sb.Append("<head><meta content=\"en-us\" http-equiv=\"Content-Language\" />" +
                  "<meta content=\"text/html; charset=utf-16\" http-equiv=\"Content-Type\" />" +
                  "<title>ScreenToGif - Feedback</title>" +
                  "</head>");

        sb.AppendFormat("<style>{0}</style>", Util.Other.GetTextResource("ScreenToGif.Resources.Style.css"));

        sb.Append("<body>");
        sb.AppendFormat("<h1>{0}</h1>", (title ?? "").Length > 0 ? title : "Title of the feedback");
        sb.Append("<div id=\"content\"><div>");
        sb.Append("<h2>Overview</h2>");
        sb.Append("<div id=\"overview\"><table>");

        //First overview row.
        sb.Append("<tr><th>User</th>");
        sb.Append("<th>Machine</th>");
        sb.Append("<th>Startup</th>");
        sb.Append("<th>Date</th>");
        sb.Append("<th>Running</th>");
        sb.Append("<th>Version</th></tr>");

        var culture = new CultureInfo("pt-BR");

        sb.AppendFormat("<tr><td class=\"textcentered\">{0}</td>", Environment.UserName);
        sb.AppendFormat("<td class=\"textcentered\">{0}</td>", Environment.MachineName);
        sb.AppendFormat(culture, "<td class=\"textcentered\">{0:g}</td>", Global.StartupDateTime);
        sb.AppendFormat(culture, "<td class=\"textcentered\">{0:g}</td>", DateTime.Now);
        sb.AppendFormat(culture, "<td class=\"textcentered\">{0:d':'hh':'mm':'ss}</td>", Global.StartupDateTime != DateTime.MinValue ? DateTime.Now - Global.StartupDateTime : TimeSpan.Zero);
        sb.AppendFormat("<td class=\"textcentered\">{0}</td></tr>", Assembly.GetExecutingAssembly().GetName().Version?.ToString(4));

        //Second overview row.
        sb.Append("<tr><th colspan=\"2\">Windows</th>");
        sb.Append("<th>Architecture</th>");
        sb.Append("<th>Used</th>");
        sb.Append("<th>Available</th>");
        sb.Append("<th>Total</th></tr>");

        var status = new MemoryStatusEx(true);
        Kernel32.GlobalMemoryStatusEx(ref status);

        sb.AppendFormat("<td class=\"textcentered\" colspan=\"2\">{0}</td>", Environment.OSVersion.Version);
        sb.AppendFormat("<td class=\"textcentered\">{0}</td>", Environment.Is64BitOperatingSystem ? "64 bits" : "32 Bits");
        sb.AppendFormat("<td class=\"textcentered\">{0}</td>", Humanizer.BytesToString(Environment.WorkingSet));
        sb.AppendFormat("<td class=\"textcentered\">{0}</td>", Humanizer.BytesToString(status.AvailablePhysicalMemory));
        sb.AppendFormat("<td class=\"textcentered\">{0}</td></tr>", Humanizer.BytesToString(status.TotalPhysicalMemory));

        //Third overview row.
        sb.Append("<tr><th colspan=\"3\">E-mail</th>");
        sb.Append("<th>.Net version</th>");
        sb.Append("<th>Issue?</th>");
        sb.Append("<th>Suggestion?</th></tr>");

        sb.AppendFormat("<td colspan=\"3\" class=\"textcentered\">{0}</td>", (email ?? "").Length > 0 ? email : "example@outlook.com");
        sb.AppendFormat("<td class=\"textcentered\">{0}</td>", FrameworkHelper.QueryFrameworkVersion());
        sb.AppendFormat("<td class=\"textcentered\">{0}</td>", issue ? "Yes" : "No");
        sb.AppendFormat("<td class=\"textcentered\">{0}</td></tr></table></div></div>", suggestion ? "Yes" : "No");

        //Processors.
        sb.Append("<br><h2>Processors</h2><table>");
        sb.Append(GetProcessor());
        sb.Append(GetGraphicsAdapter());
        sb.Append("</table>");

        //System.Windows.Forms.SystemInformation.PowerStatus.BatteryChargeStatus == System.Windows.Forms.BatteryChargeStatus.NoSystemBattery

        //Monitors.
        sb.Append("<br><h2>Monitors</h2><table>");
        sb.Append("<tr><th>Name</th>");
        sb.Append("<th>Bounds</th>");
        sb.Append("<th>Working area</th>");
        sb.Append("<th>DPI/Scale</th>");
        sb.Append("<th>Graphics adapter</th>");
        sb.Append("<th>Primary?</th></tr>");

        foreach (var monitor in MonitorHelper.AllMonitors)
        {
            sb.AppendFormat("<td class=\"textcentered\">{0} ({1})</td>", monitor.FriendlyName, monitor.Name);
            sb.AppendFormat("<td class=\"textcentered\">{0}:{1} • {2}x{3}</td>", monitor.Bounds.Left, monitor.Bounds.Top, monitor.Bounds.Width, monitor.Bounds.Height);
            sb.AppendFormat("<td class=\"textcentered\">{0}:{1} • {2}x{3}</td>", monitor.WorkingArea.Left, monitor.WorkingArea.Top, monitor.WorkingArea.Width, monitor.WorkingArea.Height);
            sb.AppendFormat("<td class=\"textcentered\">{0}dpi / {1:#00}%</td>", monitor.Dpi, monitor.Dpi / 96d * 100d);
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", monitor.AdapterName);
            sb.AppendFormat("<td class=\"textcentered\">{0}</td></tr>", monitor.IsPrimary ? "Yes" : "No");
        }

        sb.Append("</table>");

        if (MonitorHelper.AllMonitors.Count > 0)
        {
            sb.AppendFormat("");

            //sb.Append("<svg>" +
            //          "<circle cx=\"40\" cy=\"40\" r=\"24\" style=\"stroke:#006600; fill:#00cc00\"/>" +
            //          "<rect id=\"box\" x=\"0\" y=\"0\" width=\"50\" height=\"50\" style=\"stroke:#006600; fill:#00cc00\"/>" +
            //          "</svg>");
        }

        //Drives.
        sb.Append("<br><h2>Drives</h2><table>");
        sb.Append("<tr><th>Root</th>");
        sb.Append("<th>Used</th>");
        sb.Append("<th>Free</th>");
        sb.Append("<th>Total</th>");
        sb.Append("<th>Format</th>");
        sb.Append("<th>Type</th>");
        sb.Append("<th>Ready?</th></tr>");

        foreach (var drive in DriveInfo.GetDrives())
        {
            #region Try get the size

            var total = 0L;
            var available = 0L;
            var format = "";

            try
            {
                total = drive.TotalSize;
                available = drive.AvailableFreeSpace;
                format = drive.DriveFormat;
            }
            catch (Exception e)
            {
                //LogWriter.Log(e, "Not possible to get driver details");
            }

            #endregion

            var used = total - available;
            var usedPerc = Math.Round(MathExtensions.CrossMultiplication((decimal)total, used, null), 1);
            var avaiPerc = Math.Round(MathExtensions.CrossMultiplication((decimal)total, available, null), 1);
                
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", drive.Name);
            sb.AppendFormat("<td class=\"textRight\">({0} %) {1}</td>", usedPerc, Humanizer.BytesToString(used, "N1"));
            sb.AppendFormat("<td class=\"textRight\">({0} %) {1}</td>", avaiPerc, Humanizer.BytesToString(available, "N1"));
            sb.AppendFormat("<td class=\"textRight\">{0}</td>", Humanizer.BytesToString(total, "N1"));
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", format);
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", drive.DriveType);
            sb.AppendFormat("<td class=\"textcentered\">{0}</td></tr>", drive.IsReady ? "Yes" : "No");
        }

        sb.Append("<table>");

        //Details.
        sb.Append("<br><h2>Details</h2><div><div><table>");
        sb.Append("<tr id=\"ProjectNameHeaderRow\"><th class=\"messageHeader\">Message</th></tr>");
        sb.Append("<tr name=\"MessageRowClassProjectName\">");
        sb.AppendFormat("<td class=\"messageCell\">{0}</td></tr></table>", message.Replace(Environment.NewLine, "<br>"));
        sb.Append("</div></div></div></body></html>");

        return sb.ToString();
    }

    private void Send()
    {
        StatusBand.Hide();

        #region Validation

        if (TitleTextBox.Text.Length == 0)
        {
            StatusBand.Warning(LocalizationHelper.Get("S.Feedback.Warning.Title"));
            TitleTextBox.Focus();
            return;
        }

        if (MessageTextBox.Text.Length == 0)
        {
            StatusBand.Warning(LocalizationHelper.Get("S.Feedback.Warning.Message"));
            MessageTextBox.Focus();
            return;
        }

        if (EmailTextBox.Text.Length == 0)
        {
            StatusBand.Warning(LocalizationHelper.Get("S.Feedback.Warning.Email"));
            EmailTextBox.Focus();
            return;
        }

        #endregion

        StatusBand.Info(LocalizationHelper.Get("S.Feedback.Sending"));

        Cursor = Cursors.AppStarting;
        MainGrid.IsEnabled = false;
        MainGrid.UpdateLayout();

        Persist();

        Cursor = Cursors.Arrow;
        MainGrid.IsEnabled = true;
    }

    private async void Persist()
    {
        try
        {
            var path = Path.Combine(UserSettings.All.TemporaryFolderResolved, "ScreenToGif", "Feedback");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var name = Path.Combine(path, DateTime.Now.ToString("yy_MM_dd HH-mm-ss"));

            var title = TitleTextBox.Text;
            var message = MessageTextBox.Text;
            var email = EmailTextBox.Text;
            var issue = IssueCheckBox.IsChecked == true;
            var suggestion = SuggestionCheckBox.IsChecked == true;

            await Task.Factory.StartNew(() => File.WriteAllText(name + ".html", BuildBody(title, message, email, issue, suggestion)));

            if (AttachmentListBox.Items.Count <= 0)
            {
                DialogResult = true;
                return;
            }

            if (Directory.Exists(name))
                Directory.Delete(name);

            Directory.CreateDirectory(name);

            foreach (var item in AttachmentListBox.Items.OfType<AttachmentListBoxItem>())
            {
                var sourceName = Path.GetFileName(item.Attachment);
                var destName = Path.Combine(name, sourceName);

                if (item.Attachment.StartsWith(UserSettings.All.LogsFolder))
                    File.Move(item.Attachment, destName);
                else
                    File.Copy(item.Attachment, destName, true);
            }

            ZipFile.CreateFromDirectory(name, name + ".zip");

            Directory.Delete(name, true);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Persist feedback");

            Dialog.Ok("Feedback", "Error while creating the feedback", ex.Message);
        }
    }

    #endregion
}