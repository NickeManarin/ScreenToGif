using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class CaptureShellCoordinatorTests
{
    [Fact]
    public void OpenAndCloseHidesThenRestoresSameStartupHost()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);

        Assert.True(coordinator.Open(CaptureShellKind.Recorder));
        Assert.False(host.StartupVisible);
        Assert.Equal(CaptureShellKind.Recorder, coordinator.ActiveKind);
        Assert.Single(host.Created);
        Assert.Equal(CaptureShellKind.Recorder, host.Created[0].Kind);
        Assert.True(host.Created[0].WasShown);

        host.Created[0].Close();

        Assert.True(host.StartupVisible);
        Assert.Equal(1, host.RestoreCount);
        Assert.Null(coordinator.ActiveKind);
    }

    [Fact]
    public void RepeatedVisitsCreateOneShellAtATimeWithoutLeakingState()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);

        Assert.True(coordinator.Open(CaptureShellKind.Board));
        Assert.False(coordinator.Open(CaptureShellKind.Webcam));
        Assert.Single(host.Created);
        Assert.Equal(1, host.Created[0].ActivateCount);

        host.Created[0].Close();
        Assert.True(coordinator.Open(CaptureShellKind.Webcam));
        host.Created[1].Close();

        Assert.Equal(2, host.Created.Count);
        Assert.Equal(2, host.HideCount);
        Assert.Equal(2, host.RestoreCount);
        Assert.True(host.StartupVisible);
        Assert.Null(coordinator.ActiveKind);
    }

    [Fact]
    public void ShowFailureRestoresStartupAndAllowsRetry()
    {
        var host = new FakeHost { FailNextShow = true };
        var coordinator = new CaptureShellCoordinator(host);

        Assert.Throws<InvalidOperationException>(() => coordinator.Open(CaptureShellKind.Recorder));
        Assert.True(host.StartupVisible);
        Assert.Null(coordinator.ActiveKind);

        Assert.True(coordinator.Open(CaptureShellKind.Recorder));
        Assert.Equal(2, host.Created.Count);
    }

    [Fact]
    public void A_shell_that_handed_its_recording_to_the_editor_closes_startup_instead_of_restoring_it()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);

        Assert.True(coordinator.Open(CaptureShellKind.Recorder));
        host.Created[0].HandedOffToEditor = true;
        host.Created[0].Close();

        Assert.Equal(1, host.CloseCount);
        Assert.Equal(0, host.RestoreCount);
        Assert.False(host.StartupVisible);
        Assert.Null(coordinator.ActiveKind);
    }

    [Fact]
    public void A_shell_that_closed_without_a_recording_brings_startup_back()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);

        Assert.True(coordinator.Open(CaptureShellKind.Recorder));
        host.Created[0].Close();

        Assert.Equal(0, host.CloseCount);
        Assert.Equal(1, host.RestoreCount);
        Assert.True(host.StartupVisible);
    }

    [Fact]
    public async Task A_hand_off_that_fails_deletes_the_recording_and_brings_startup_back()
    {
        var workspace = EditorWorkspace.Create();
        var root = workspace.RootPath;
        var batch = workspace.CreateBatch(EditorArtifactKind.Recordings);
        var framePath = Path.Combine(batch, "0.png");
        await File.WriteAllBytesAsync(framePath, [0x89, 0x50, 0x4E, 0x47]);
        var project = new LoadedProject(workspace, [new EditorFrame(framePath, 100)]);

        var opening = new InvalidOperationException("The editor could not open the recording.");
        var failure = await RecordingHandOff.TransferAsync(project, _ => throw opening);

        // Nothing took ownership, so the recording is deleted rather than stranded on disk.
        Assert.Same(opening, failure);
        Assert.False(Directory.Exists(root));

        // The other half of the clause: the shell reports the hand-off only once the editor holds
        // the recording, so a failure leaves the flag clear and Startup comes back.
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);
        Assert.True(coordinator.Open(CaptureShellKind.Recorder));
        Assert.False(host.Created[0].HandedOffToEditor);
        host.Created[0].Close();

        Assert.True(host.StartupVisible);
        Assert.Equal(1, host.RestoreCount);
        Assert.Equal(0, host.CloseCount);
    }

    [Fact]
    public async Task A_hand_off_that_completes_leaves_the_recording_to_the_editor()
    {
        var workspace = EditorWorkspace.Create();
        var root = workspace.RootPath;
        var batch = workspace.CreateBatch(EditorArtifactKind.Recordings);
        var framePath = Path.Combine(batch, "0.png");
        await File.WriteAllBytesAsync(framePath, [0x89, 0x50, 0x4E, 0x47]);
        var project = new LoadedProject(workspace, [new EditorFrame(framePath, 100)]);

        EditorProjectContent? taken = null;
        var failure = await RecordingHandOff.TransferAsync(project, handedOver =>
        {
            taken = handedOver.TransferOwnership();
            return Task.CompletedTask;
        });

        Assert.Null(failure);
        Assert.NotNull(taken);
        Assert.True(File.Exists(framePath));

        taken?.Workspace.Dispose();
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void Shells_that_produce_nothing_never_close_startup()
    {
        foreach (var kind in new[] { CaptureShellKind.Webcam, CaptureShellKind.Board })
        {
            var host = new FakeHost();
            var coordinator = new CaptureShellCoordinator(host);

            Assert.True(coordinator.Open(kind));
            Assert.False(host.Created[0].HandedOffToEditor);
            host.Created[0].Close();

            Assert.Equal(0, host.CloseCount);
            Assert.True(host.StartupVisible);
        }
    }

    [Fact]
    public void AShellThatRecordedNothingGivesStartupBackAndHandsTheEditorNothing()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);

        Assert.True(coordinator.Open(CaptureShellKind.Webcam));
        host.Created[0].Close();

        Assert.Equal(1, host.Created[0].TakeRecordingCount);
        Assert.Empty(host.Adopted);
        Assert.Equal(1, host.RestoreCount);
        Assert.True(host.StartupVisible);
    }

    [Fact]
    public void AShellThatRecordedFramesHandsThemToTheEditorAndLeavesStartupClosed()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);
        using var workspace = EditorWorkspace.Create(Path.Combine(Path.GetTempPath(), $"stg-shell-{Guid.NewGuid():N}"));
        var recording = new LoadedProject(workspace, []);

        Assert.True(coordinator.Open(CaptureShellKind.Webcam));
        host.Created[0].Recording = recording;
        host.Created[0].Close();

        Assert.Same(recording, Assert.Single(host.Adopted));
        Assert.Equal(0, host.RestoreCount);
        Assert.False(host.StartupVisible);
        Assert.Null(coordinator.ActiveKind);
    }

    [Fact]
    public void ARecordingTheEditorRefusesIsReleasedAndStartupComesBack()
    {
        var host = new FakeHost { RefuseAdoption = true };
        var coordinator = new CaptureShellCoordinator(host);
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"stg-shell-{Guid.NewGuid():N}");
        var recording = new LoadedProject(EditorWorkspace.Create(workspaceRoot), []);

        Assert.True(coordinator.Open(CaptureShellKind.Webcam));
        host.Created[0].Recording = recording;
        host.Created[0].Close();

        Assert.Empty(host.Adopted);
        Assert.Equal(1, host.RestoreCount);
        Assert.True(host.StartupVisible);
        Assert.False(Directory.Exists(workspaceRoot));
    }

    [Fact]
    public void AfterHandingOverARecordingTheCoordinatorCanOpenAnotherShell()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);
        using var workspace = EditorWorkspace.Create(Path.Combine(Path.GetTempPath(), $"stg-shell-{Guid.NewGuid():N}"));

        Assert.True(coordinator.Open(CaptureShellKind.Webcam));
        host.Created[0].Recording = new LoadedProject(workspace, []);
        host.Created[0].Close();

        Assert.True(coordinator.Open(CaptureShellKind.Webcam));
        Assert.Equal(2, host.Created.Count);
    }

    [Fact]
    public void AnAdoptionThatThrowsIsReportedAndTheRecordingIsReleasedInsteadOfEscapingTheCloseHandler()
    {
        var host = new FakeHost { AdoptionThrows = true };
        var coordinator = new CaptureShellCoordinator(host);
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"stg-shell-{Guid.NewGuid():N}");
        var recording = new LoadedProject(EditorWorkspace.Create(workspaceRoot), []);

        Assert.True(coordinator.Open(CaptureShellKind.Webcam));
        host.Created[0].Recording = recording;
        host.Created[0].Close();

        Assert.IsType<IOException>(Assert.Single(host.Reported));
        Assert.Empty(host.Adopted);
        Assert.Equal(1, host.RestoreCount);
        Assert.False(Directory.Exists(workspaceRoot));
    }

    [Fact]
    public void AShellOpenedToReplaceTheProjectHandsItsRecordingOverForReplacement()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);
        using var workspace = EditorWorkspace.Create(Path.Combine(Path.GetTempPath(), $"stg-adoption-{Guid.NewGuid():N}"));

        coordinator.Open(CaptureShellKind.Board);
        host.Created[0].Recording = new LoadedProject(workspace, []);
        host.Created[0].Close();

        Assert.Equal([CaptureShellAdoption.ReplaceProject], host.Adoptions);
    }

    [Fact]
    public void AShellOpenedToAppendHandsItsRecordingOverForAppending()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);
        using var workspace = EditorWorkspace.Create(Path.Combine(Path.GetTempPath(), $"stg-adoption-{Guid.NewGuid():N}"));

        coordinator.Open(CaptureShellKind.Board, CaptureShellAdoption.AppendToProject);
        host.Created[0].Recording = new LoadedProject(workspace, []);
        host.Created[0].Close();

        Assert.Equal([CaptureShellAdoption.AppendToProject], host.Adoptions);
    }

    [Fact]
    public void AskingForAShellThatIsAlreadyOpenDoesNotChangeWhatItsRecordingIsFor()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);
        using var workspace = EditorWorkspace.Create(Path.Combine(Path.GetTempPath(), $"stg-adoption-{Guid.NewGuid():N}"));

        coordinator.Open(CaptureShellKind.Board, CaptureShellAdoption.AppendToProject);
        Assert.False(coordinator.Open(CaptureShellKind.Board));
        host.Created[0].Recording = new LoadedProject(workspace, []);
        host.Created[0].Close();

        Assert.Equal([CaptureShellAdoption.AppendToProject], host.Adoptions);
    }

    [Fact]
    public void TheNextShellStartsFromReplacementWhateverTheLastOneWasFor()
    {
        var host = new FakeHost();
        var coordinator = new CaptureShellCoordinator(host);
        using var first = EditorWorkspace.Create(Path.Combine(Path.GetTempPath(), $"stg-adoption-{Guid.NewGuid():N}"));
        using var second = EditorWorkspace.Create(Path.Combine(Path.GetTempPath(), $"stg-adoption-{Guid.NewGuid():N}"));

        coordinator.Open(CaptureShellKind.Board, CaptureShellAdoption.AppendToProject);
        host.Created[0].Recording = new LoadedProject(first, []);
        host.Created[0].Close();
        coordinator.Open(CaptureShellKind.Webcam);
        host.Created[1].Recording = new LoadedProject(second, []);
        host.Created[1].Close();

        Assert.Equal([CaptureShellAdoption.AppendToProject, CaptureShellAdoption.ReplaceProject], host.Adoptions);
    }

    private sealed class FakeHost : ICaptureShellHost
    {
        public List<FakeShell> Created { get; } = [];

        public bool StartupVisible { get; private set; } = true;

        public bool FailNextShow { get; set; }

        public int HideCount { get; private set; }

        public int RestoreCount { get; private set; }

        public int CloseCount { get; private set; }

        public bool RefuseAdoption { get; set; }

        public bool AdoptionThrows { get; set; }

        public List<Exception> Reported { get; } = [];

        public List<LoadedProject> Adopted { get; } = [];

        public List<CaptureShellAdoption> Adoptions { get; } = [];

        public ICaptureShellWindow CreateShell(CaptureShellKind kind)
        {
            var shell = new FakeShell(kind, FailNextShow);
            FailNextShow = false;
            Created.Add(shell);
            return shell;
        }

        public bool AdoptRecording(LoadedProject recording, CaptureShellAdoption adoption)
        {
            if (AdoptionThrows)
                throw new IOException("Synthetic adoption failure.");
            if (RefuseAdoption)
                return false;

            Adopted.Add(recording);
            Adoptions.Add(adoption);
            return true;
        }

        public void ReportShellFailure(Exception failure) => Reported.Add(failure);

        public void HideStartup()
        {
            StartupVisible = false;
            HideCount++;
        }

        public void RestoreStartup()
        {
            StartupVisible = true;
            RestoreCount++;
        }

        public void CloseStartup()
        {
            StartupVisible = false;
            CloseCount++;
        }
    }

    private sealed class FakeShell(CaptureShellKind kind, bool failShow) : ICaptureShellWindow
    {
        public event EventHandler? Closed;

        public CaptureShellKind Kind { get; } = kind;

        public bool HandedOffToEditor { get; set; }

        public LoadedProject? Recording { get; set; }

        public int TakeRecordingCount { get; private set; }

        public LoadedProject? TakeRecording()
        {
            TakeRecordingCount++;
            var recording = Recording;
            Recording = null;
            return recording;
        }

        public bool WasShown { get; private set; }

        public int ActivateCount { get; private set; }

        public void Show()
        {
            if (failShow)
                throw new InvalidOperationException("Synthetic show failure.");

            WasShown = true;
        }

        public void Activate() => ActivateCount++;

        public void Close() => Closed?.Invoke(this, EventArgs.Empty);
    }
}
