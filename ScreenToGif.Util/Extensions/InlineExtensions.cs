using System.Windows;
using System.Windows.Documents;
using FontFamily = System.Windows.Media.FontFamily;

namespace ScreenToGif.Util.Extensions;

public static class InlineExtensions
{
    public static Run WithResource(this Run run, string id)
    {
        run.SetResourceReference(Run.TextProperty, id);
        return run;
    }

    public static Hyperlink WithLink(this Hyperlink hyperlink, string link)
    {
        if (string.IsNullOrWhiteSpace(link))
            return hyperlink;

        hyperlink.NavigateUri = new Uri(link);
        hyperlink.Inlines.Add(new Run(link));
        hyperlink.RequestNavigate += (sender, args) =>
        {
            try
            {
                ProcessHelper.StartWithShell(args.Uri.AbsoluteUri);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Trying to navigate to " + link);
            }
        };

        return hyperlink;
    }

    public static Paragraph WithKeyLink(this Paragraph paragraph, string id, string link, bool isStatic = false)
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

    public static Paragraph WithLineBreak(this Paragraph paragraph)
    {
        paragraph.Inlines.Add(new LineBreak());
        return paragraph;
    }
}