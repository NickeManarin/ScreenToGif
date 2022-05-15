using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;

namespace ScreenToGif.Util;

internal static class FeedbackHelper
{
    internal static bool Send(string html, List<string> files = null)
    {
        //If returns null, try sending via email.
        var response = SendToServer(html, files?.FirstOrDefault());

        if (response == true)
            return true;

        if (response == false)
            return false;

        //Please, don't try to log with this email and password. :/
        //Every time someone does this, I have to change the password and the Feedback feature stops working until I update the app.
        var passList = (Secret.Password ?? "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var t in passList)
        {
            if (SendInternal(t, html, files ?? new List<string>()))
                return true;
        }

        return false;
    }

    private static bool SendInternal(string pass, string html, List<string> files)
    {
        try
        {
            using var smtp = new SmtpClient
            {
                Timeout = 6 * 60 * 1000, //Minutes, seconds, milliseconds
                Port = Secret.Port,
                Host = Secret.Host,
                EnableSsl = true,
                UseDefaultCredentials = true,
                Credentials = new NetworkCredential(Secret.Email, pass)
            };

            using var mail = new MailMessage
            {
                From = new MailAddress("screentogif@outlook.com"),
                Subject = "ScreenToGif - Feedback",
                IsBodyHtml = true
            };

            mail.To.Add("nicke@outlook.com.br");
            mail.Body = html;

            foreach (var file in files)
                mail.Attachments.Add(new Attachment(file));

            //smtp.SendCompleted += (sender, args) =>
            //{
            //    if (args.Error != null)
            //        throw args.Error;
            //};

            smtp.Send(mail);

            return true;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while sending email");
            return false;
        }
    }

    /// <summary>
    /// Tries to send the feedback message to the api.
    /// </summary>
    /// <returns>If null, the app should try sending the feedback via email.</returns>
    private static bool? SendToServer(string message, string file)
    {
        if (string.IsNullOrWhiteSpace(Secret.ServerAddress))
            return null;

        using var client = new HttpClient
        {
            BaseAddress = new Uri(Secret.ServerAddress)
        };

        try
        {
            var multiContent = new MultipartFormDataContent
            {
                {new StringContent(message), "message"},
                {new ByteArrayContent(File.ReadAllBytes(file)), "file", file}
            };

            var result = client.PostAsync("api/v1/relay/send", multiContent).Result;

            if (result == null || result.StatusCode == HttpStatusCode.BadRequest)
                return null;

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }
}