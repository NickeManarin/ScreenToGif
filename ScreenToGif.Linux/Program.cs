using Avalonia;

namespace ScreenToGif.Linux;

internal static class Program
{
    public static bool StartInEditor { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Any(arg => arg is "--help" or "-h"))
        {
            Console.WriteLine("ScreenToGif Linux editor");
            Console.WriteLine("Open media, edit the frame timeline, save a project, and export with FFmpeg.");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run --project ScreenToGif.Linux [--editor]");
            return;
        }

        StartInEditor = args.Any(arg => arg == "--editor");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
