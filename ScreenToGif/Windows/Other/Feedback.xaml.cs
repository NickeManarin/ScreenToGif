using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class Feedback : Window
    {
        public Feedback()
        {
            InitializeComponent();
        }

        private void Feedback_OnLoaded(object sender, RoutedEventArgs e)
        {
            //Search for a Log folder and add the txt files as attachment.
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var smtp = new SmtpClient();
            smtp.Timeout = (60 * 5 * 1000);
            smtp.Port = Secret.Port;
            smtp.Host = Secret.Host;
            smtp.EnableSsl = false;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(Secret.Email, Secret.Password);

            var mail = new MailMessage();
            mail.From = new MailAddress("screentogif@outlook.com");
            mail.To.Add("nicke@outlook.com.br");
            mail.IsBodyHtml = true;

            var sb = new StringBuilder();
            sb.Append("<div style='width:auto; height:auto; padding: 3px;'>");
            sb.AppendFormat("<p>Type: {0} | {1}</p>", IssueCheckBox.IsChecked.Value ? "Issue" : "null", SuggestionCheckBox.IsChecked.Value ? "Suggestion" : "null");

            if (MailTextBox.Text.Length > 0)
                sb.AppendFormat("<p>From: {0}</p>", TitleTextBox.Text);

            sb.Append("<p></p>");
            sb.AppendFormat("<p>Title: {0}</p>", TitleTextBox.Text);
            sb.AppendFormat("<p>Message: {0}</p>", MessageTextBox.Text);
            sb.Append("<p></p>");

            sb.AppendFormat("<p>Version: {0}</p>", Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            sb.AppendFormat("<p>Windows: {0}</p>", Environment.OSVersion);
            sb.AppendFormat("<p>Domain: {0}</p>", Environment.UserDomainName);
            sb.Append("</div>");

            mail.Body = "";
            mail.Subject = "Screen To Gif - Feedback";

            smtp.Send(mail);

            //After sending, close.
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
