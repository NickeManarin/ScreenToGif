using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ScreenToGif.Util;

namespace ScreenToGif.Extensions
{
    internal static class InlineExtensions
    {
        internal static Run WithResource(this Run run, string id)
        {
            run.SetResourceReference(Run.TextProperty, id);
            return run;
        }

        internal static Hyperlink WithLink(this Hyperlink hyperlink, string link)
        {
            if (string.IsNullOrWhiteSpace(link))
                return hyperlink;

            hyperlink.NavigateUri = new Uri(link);
            hyperlink.Inlines.Add(new Run(link));
            hyperlink.RequestNavigate += (sender, args) =>
            {
                try
                {
                    Process.Start(args.Uri.AbsoluteUri);
                }
                catch (Exception e)
                {
                    LogWriter.Log(e, "Trying to navigate to " + link);
                }
            };

            return hyperlink;
        }

        internal static Paragraph WithKeyLink(this Paragraph paragraph, string id, string link, bool isStatic = false)
        {
            paragraph.KeepTogether = true;
            paragraph.TextAlignment = TextAlignment.Left;
            paragraph.FontFamily = new FontFamily("Segoe UI");

            paragraph.Inlines.Add(isStatic ? new Run(id) : new Run().WithResource(id));
            paragraph.Inlines.Add(new Run(" "));
            paragraph.Inlines.Add(new Hyperlink().WithLink(link));
            paragraph.Margin = new Thickness(0);
            return paragraph;
        }

        internal static Paragraph WithLineBreak(this Paragraph paragraph)
        {
            paragraph.Inlines.Add(new LineBreak());
            return paragraph;
        }
    }
}