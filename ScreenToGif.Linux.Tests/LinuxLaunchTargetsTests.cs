using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class LinuxLaunchTargetsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"screentogif-launch-{Guid.NewGuid():N}");

    [Theory]
    [InlineData(LinuxStartupWindow.Startup, null)]
    [InlineData(LinuxStartupWindow.Recorder, CaptureShellKind.Recorder)]
    [InlineData(LinuxStartupWindow.Webcam, CaptureShellKind.Webcam)]
    [InlineData(LinuxStartupWindow.Board, CaptureShellKind.Board)]
    [InlineData(LinuxStartupWindow.Editor, null)]
    public void EveryStartupChoiceOpensItsOwnShell(LinuxStartupWindow choice, CaptureShellKind? expected) =>
        Assert.Equal(expected, LinuxLaunchTargets.ShellFor(choice));

    [Theory]
    [InlineData(LinuxTrayWindow.None, null)]
    [InlineData(LinuxTrayWindow.Startup, null)]
    [InlineData(LinuxTrayWindow.Recorder, CaptureShellKind.Recorder)]
    [InlineData(LinuxTrayWindow.Webcam, CaptureShellKind.Webcam)]
    [InlineData(LinuxTrayWindow.Board, CaptureShellKind.Board)]
    [InlineData(LinuxTrayWindow.Editor, null)]
    public void EveryTrayChoiceOpensItsOwnShell(LinuxTrayWindow choice, CaptureShellKind? expected) =>
        Assert.Equal(expected, LinuxLaunchTargets.ShellFor(choice));

    [Fact]
    public void EveryCaptureShellCanBeChosenAtStartupAndFromTheTray()
    {
        // A shell added to CaptureShellKind but left out of either table would be offered nowhere, or
        // fall back to StartUp without saying so, which is how Screen recorder sat disabled.
        var fromStartup = Enum.GetValues<LinuxStartupWindow>().Select(LinuxLaunchTargets.ShellFor).OfType<CaptureShellKind>().ToArray();
        var fromTray = Enum.GetValues<LinuxTrayWindow>().Select(LinuxLaunchTargets.ShellFor).OfType<CaptureShellKind>().ToArray();

        foreach (var kind in Enum.GetValues<CaptureShellKind>())
        {
            Assert.Single(fromStartup, kind);
            Assert.Single(fromTray, kind);
        }
    }

    [Fact]
    public void TheTrayMenuOffersEveryWindowTheClickSettingsCanOpen()
    {
        var offered = LinuxLaunchTargets.TrayMenuEntries.Select(entry => entry.Window).ToArray();
        var choosable = Enum.GetValues<LinuxTrayWindow>().Where(window => window != LinuxTrayWindow.None).ToArray();

        Assert.Equal(choosable.Order(), offered.Order());
        Assert.Equal(offered.Length, LinuxLaunchTargets.TrayMenuEntries.Select(entry => entry.Header).Distinct().Count());
    }

    [Fact]
    public async Task ARecorderStartupAndTrayChoiceSurvivesTheSettingsFile()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new LinuxApplicationSettingsStore(path);

        await store.SaveAsync(new LinuxApplicationSettings
        {
            StartupWindow = LinuxStartupWindow.Recorder,
            LeftClickAction = LinuxTrayAction.OpenWindow,
            LeftClickWindow = LinuxTrayWindow.Recorder
        });
        var loaded = store.Load();

        Assert.Equal(LinuxStartupWindow.Recorder, loaded.StartupWindow);
        Assert.Equal(LinuxTrayWindow.Recorder, loaded.LeftClickWindow);
        Assert.Equal(LinuxStartupWindow.Recorder, loaded.Copy().StartupWindow);
        Assert.Equal(LinuxTrayWindow.Recorder, loaded.Copy().LeftClickWindow);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
