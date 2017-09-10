using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Util;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ScreenToGif.FileWriters;

namespace ScreenToGif.Windows.Other
{
    public partial class Feedback : Window
    {
        public Feedback()
        {
            InitializeComponent();
        }

        #region Events

        private async void Feedback_Loaded(object sender, RoutedEventArgs e)
        {
            StatusBand.Info(TryFindResource("S.Feedback.IssueBug.Info") as string ?? "");
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
                if (!AttachmentListBox.Items.Cast<AttachmentListBoxItem>().Any(x => x.Attachment.Equals(fileName)))
                    AttachmentListBox.Items.Add(new AttachmentListBoxItem(fileName));
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            Send();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void RemoveButton_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AttachmentListBox.Items.RemoveAt(AttachmentListBox.SelectedIndex);
        }

        private void RemoveAllAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            AttachmentListBox.Items.Clear();
        }

        #endregion

        #region Methods

        private async void LoadFiles()
        {
            try
            {
                //Search for a Log folder and add the txt files as attachment.
                var logFolder = Path.Combine(UserSettings.All.LogsFolder, "ScreenToGif", "Logs");

                if (!Directory.Exists(logFolder)) return;

                var list = await Task.Factory.StartNew(() => Directory.GetFiles(logFolder).Select(s => Dispatcher.Invoke(() => new AttachmentListBoxItem(s))).ToList());

                Dispatcher.Invoke(() => list.ForEach(x => AttachmentListBox.Items.Add(x)));
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to search for the logs folder");
            }
        }

        private string BuildBody(string title, string message, string email, bool issue, bool suggestion)
        {
            var sb = new StringBuilder();
            sb.Append("<html xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\">");
            sb.Append("<head><meta content=\"en-us\" http-equiv=\"Content-Language\" />" +
                      "<meta content=\"text/html; charset=utf-16\" http-equiv=\"Content-Type\" />" +
                      "<title>Screen To Gif - Feedback</title>" +
                      "</head>");

            sb.AppendFormat("<style>{0}</style>", Util.Other.GetTextResource("ScreenToGif.Resources.Style.css"));

            sb.Append("<body>");
            sb.AppendFormat("<h1>{0}</h1>", title);
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

            sb.AppendFormat("<tr><td class=\"textcentered\">{0}</td>", Environment.UserName);
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", Environment.MachineName);
            sb.AppendFormat("<td class=\"textcentered\">{0:g}</td>", Global.StartupDateTime);
            sb.AppendFormat("<td class=\"textcentered\">{0:g}</td>", DateTime.Now);
            sb.AppendFormat("<td class=\"textcentered\">{0:d':'hh':'mm':'ss}</td>", Global.StartupDateTime != DateTime.MinValue ? DateTime.Now - Global.StartupDateTime : TimeSpan.Zero);
            sb.AppendFormat("<td class=\"textcentered\">{0}</td></tr>", Assembly.GetExecutingAssembly().GetName().Version.ToString(4));

            //Second overview row.
            sb.Append("<tr><th colspan=\"2\">Windows</th>");
            sb.Append("<th>Architecture</th>");
            sb.Append("<th>Used</th>");
            sb.Append("<th>Available</th>");
            sb.Append("<th>Total</th></tr>");

            var status = new Native.MemoryStatusEx(true);
            Native.GlobalMemoryStatusEx(ref status);

            sb.AppendFormat("<td class=\"textcentered\" colspan=\"2\">{0}</td>", Environment.OSVersion.Version);
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", Environment.Is64BitOperatingSystem ? "64 bits" : "32 Bits");
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", Humanizer.BytesToString(Environment.WorkingSet));
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", Humanizer.BytesToString(status.AvailablePhysicalMemory));
            sb.AppendFormat("<td class=\"textcentered\">{0}</td></tr>", Humanizer.BytesToString(status.TotalPhysicalMemory));

            //Third overview row.
            sb.Append("<tr><th colspan=\"4\">E-mail</th>");
            sb.Append("<th>Issue?</th>");
            sb.Append("<th>Suggestion?</th></tr>");

            sb.AppendFormat("<td colspan=\"4\" class=\"textcentered\">{0}</td>", email);
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", issue ? "Yes" : "No");
            sb.AppendFormat("<td class=\"textcentered\">{0}</td></tr></table></div></div>", suggestion ? "Yes" : "No");

            sb.Append("<h2>Details</h2><div><div><table>");
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
                StatusBand.Warning(FindResource("S.Feedback.Warning.Title") as string);
                TitleTextBox.Focus();
                return;
            }

            if (MessageTextBox.Text.Length == 0)
            {
                StatusBand.Warning(FindResource("S.Feedback.Warning.Message") as string);
                MessageTextBox.Focus();
                return;
            }

            #endregion

            StatusBand.Info(FindResource("S.Feedback.Sending").ToString());

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
                #region Temporary folder

                //If never configurated.
                if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder))
                    UserSettings.All.TemporaryFolder = Path.GetTempPath();

                #endregion

                var path = Path.Combine(UserSettings.All.TemporaryFolder, "ScreenToGif", "Feedback");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var name = Path.Combine(path, DateTime.Now.ToString("yy_MM_dd HH-mm-ss"));

                var title = TitleTextBox.Text;
                var message = MessageTextBox.Text;
                var email = MailTextBox.Text;
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
}