using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Util;
using ScreenToGif.Util.Enum;
using ScreenToGif.Util.Writers;

namespace ScreenToGif.Windows.Other
{
    public partial class Feedback : Window
    {
        public Feedback()
        {
            InitializeComponent();
        }

        #region Events

        private void Feedback_OnLoaded(object sender, RoutedEventArgs e)
        {
            //Search for a Log folder and add the txt files as attachment.
            var logFolder = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Logs");

            if (Directory.Exists(logFolder))
            {
                var files = Directory.GetFiles(logFolder);

                foreach (string file in files)
                {
                    AttachmentListBox.Items.Add(new AttachmentListBoxItem(file));
                }
            }
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

            foreach (string fileName in ofd.FileNames)
            {
                if (!AttachmentListBox.Items.Cast<AttachmentListBoxItem>().Any(x => x.Attachment.Equals(fileName)))
                    AttachmentListBox.Items.Add(new AttachmentListBoxItem(fileName));
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SuppressWarning();

            #region Validation

            if (TitleTextBox.Text.Length == 0)
            {
                ShowWarning("You need to inform the title of the feedback.", MessageIcon.Warning);
                TitleTextBox.Focus();
                return;
            }

            if (MessageTextBox.Text.Length == 0)
            {
                ShowWarning("You need to inform the message of the feedback.", MessageIcon.Warning);
                MessageTextBox.Focus();
                return;
            }

            #endregion

            #region UI

            ShowWarning(FindResource("Feedback.Sending").ToString(), MessageIcon.Info);

            Cursor = Cursors.AppStarting;
            MainGrid.IsEnabled = false;
            MainGrid.UpdateLayout();

            #endregion

            var smtp = new SmtpClient
            {
                Timeout = (5 * 60 * 1000), //Minutes, seconds, miliseconds
                Port = Secret.Port,
                Host = Secret.Host,
                EnableSsl = true,
                UseDefaultCredentials = true,
                Credentials = new NetworkCredential(Secret.Email, Secret.Password)
            };

            var mail = new MailMessage
            {
                From = new MailAddress("screentogif@outlook.com"),
                Subject = "Screen To Gif - Feedback",
                IsBodyHtml = true
            };

            mail.To.Add("nicke@outlook.com.br");

            #region Text

            var sb = new StringBuilder();
            sb.Append("<html xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\">");
            sb.Append("<head><meta content=\"en-us\" http-equiv=\"Content-Language\" />" +
                "<meta content=\"text/html; charset=utf-16\" http-equiv=\"Content-Type\" />" +
                "<title>Screen To Gif - Feedback</title>" +
                "</head>");

            sb.AppendFormat("<style>{0}</style>", Util.Other.GetTextResource("ScreenToGif.Resources.Style.css"));

            sb.Append("<body>");
            sb.AppendFormat("<h1>{0}</h1>", TitleTextBox.Text);
            sb.Append("<div id=\"content\"><div>");
            sb.Append("<h2>Overview</h2>");
            sb.Append("<div id=\"overview\"><table><tr>");
            sb.Append("<th _locid=\"UserTableHeader\">User</th>");

            if (MailTextBox.Text.Length > 0)
                sb.Append("<th _locid=\"FromTableHeader\">Mail</th>");

            sb.Append("<th _locid=\"VersionTableHeader\">Version</th>");
            sb.Append("<th _locid=\"WindowsTableHeader\">Windows</th>");
            sb.Append("<th _locid=\"BitsTableHeader\">Instruction Size</th>");
            sb.Append("<th _locid=\"MemoryTableHeader\">Working Memory</th>");
            sb.Append("<th _locid=\"IssueTableHeader\">Issue?</th>");
            sb.Append("<th _locid=\"SuggestionTableHeader\">Suggestion?</th></tr>");
            sb.AppendFormat("<tr><td class=\"textcentered\">{0}</td>", Environment.UserName);

            if (MailTextBox.Text.Length > 0)
                sb.AppendFormat("<td class=\"textcentered\">{0}</td>", MailTextBox.Text);

            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", Environment.OSVersion);
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", Environment.Is64BitOperatingSystem ? "64 bits" : "32 Bits");
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", Humanizer.BytesToString(Environment.WorkingSet));
            sb.AppendFormat("<td class=\"textcentered\">{0}</td>", IssueCheckBox.IsChecked.Value ? "Yes" : "No");
            sb.AppendFormat("<td class=\"textcentered\">{0}</td></tr></table></div></div>", SuggestionCheckBox.IsChecked.Value ? "Yes" : "No");

            sb.Append("<h2>Details</h2><div><div><table>");
            sb.Append("<tr id=\"ProjectNameHeaderRow\"><th></th><th class=\"messageCell\" _locid=\"MessageTableHeader\">Message</th></tr>");
            sb.Append("<tr name=\"MessageRowClassProjectName\"><td class=\"IconInfoEncoded\"><a name=\"MyProjectMessage\"></a></td>");
            sb.AppendFormat("<td class=\"messageCell\">{0}</td></tr></table>", MessageTextBox.Text);
            sb.Append("</div></div></div></body></html>");

            #endregion

            mail.Body = sb.ToString();

            foreach (AttachmentListBoxItem attachment in AttachmentListBox.Items)
            {
                mail.Attachments.Add(new Attachment(attachment.Attachment));
            }

            smtp.SendCompleted += Smtp_OnSendCompleted;
            smtp.SendMailAsync(mail);
        }

        private void Smtp_OnSendCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ShowWarning("Send Error: " + e.Error.Message, MessageIcon.Error);

                LogWriter.Log(e.Error, "Send Feedback Error");

                Cursor = Cursors.Arrow;
                MainGrid.IsEnabled = true;
                return;
            }

            Close();
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

        #region Warning

        private void ShowWarning(string message, MessageIcon icon)
        {
            var iconName = icon == MessageIcon.Info ?
                "Vector.Info" : icon == MessageIcon.Error ?
                "Vector.Error" : "Vector.Warning";

            Dispatcher.Invoke(() =>
            {
                WarningViewBox.Child = (Canvas)FindResource(iconName);
                WarningTextBlock.Text = message;

                WarningGrid.BeginStoryboard(FindResource("ShowWarningStoryboard") as Storyboard);
            });
        }

        private void SuppressWarning()
        {
            WarningTextBlock.Text = "";
            WarningGrid.BeginStoryboard(FindResource("HideWarningStoryboard") as Storyboard);
        }

        #endregion
    }
}
