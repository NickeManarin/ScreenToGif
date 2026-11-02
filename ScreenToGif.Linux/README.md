# ScreenToGif for Linux

This is the Avalonia-based Linux application. It records a region of the screen, a webcam, or a drawing on the Board, edits the result on a timeline, and exports it. The original `GifRecorder.sln` remains the Windows application.

Detailed behavior and limits are documented for the [Recorder](docs/recorder.md), the [Webcam recorder](docs/webcam.md), the [Board](docs/board.md), and each editor ribbon tab in [`docs/editor`](docs/editor/).

## Requirements

To build from source, install:

- a 64-bit Linux desktop session (X11 or Wayland);
- the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or newer;
- [FFmpeg](https://ffmpeg.org/), including both `ffmpeg` and `ffprobe` on `PATH`.

On Ubuntu, once the Microsoft .NET package feed is configured, the usual development dependencies are:

```bash
sudo apt update
sudo apt install dotnet-sdk-9.0 ffmpeg
# Additional runtime packages for Wayland screen recording:
sudo apt install pipewire xdg-desktop-portal gstreamer1.0-pipewire gstreamer1.0-gl gstreamer1.0-plugins-base libgstreamer-plugins-base1.0-0
```

Wayland recording needs GStreamer 1.24 or later, its PipeWire and OpenGL plugins, and a graphics driver supporting DMA-BUF export/import. The PipeWire plugin must support `DMA_DRM` negotiation (verified here with PipeWire/GStreamer plugin 1.6.2); installing a recent GStreamer library alone does not upgrade that plugin. Its GPU path preserves fractional-scale source geometry; the software-rendering option affects the app UI. Wayland also needs the portal backend supplied by the desktop environment, such as `xdg-desktop-portal-gnome` on GNOME. Record or Snap requests consent to share one complete monitor or window; the Recorder frame cannot select an arbitrary rectangle. The packaged app offers administrator-protected reuse of a once-approved source; this optional setting needs `pkexec` and a desktop supporting portal persistence. See [Recorder](docs/recorder.md#reuse-an-approved-wayland-source) for setup, forgetting a source, cursor behavior and selection limits.

Most desktop distributions already include the native libraries Avalonia needs. For a minimal installation, install the distribution equivalents of `fontconfig`, `freetype`, `libX11`, `libX11-xcb`, `libXrender`, `libICE`, and `libSM` as well.

FFmpeg is required at runtime for animated-image/video import and for every export. MP4 and WebM export additionally require an FFmpeg build containing the `libx264` and `libvpx-vp9` encoders. Check the setup with:

```bash
dotnet --version
ffmpeg -version
ffprobe -version
ffmpeg -hide_banner -encoders | grep -E 'libx264|libvpx-vp9'
```

If FFmpeg is installed outside `PATH`, point the app at it explicitly:

```bash
export SCREENTOGIF_FFMPEG=/path/to/ffmpeg
export SCREENTOGIF_FFPROBE=/path/to/ffprobe
```

## Build and run

Run these commands from the repository root:

```bash
dotnet restore ScreenToGif.Linux.sln
dotnet build ScreenToGif.Linux.sln --no-restore
dotnet test ScreenToGif.Linux.Tests/ScreenToGif.Linux.Tests.csproj --no-restore
dotnet run --project ScreenToGif.Linux/ScreenToGif.Linux.csproj --no-build
```

The default launch opens the StartUp window. Its Recorder, Webcam, Board, and Editor destinations are reachable, and the Options button opens the Linux application settings. Closing Editor returns to StartUp after any unsaved-work confirmation; the tray's Exit command exits the application. To open the editor, webcam recorder, or Board directly:

```bash
dotnet run --project ScreenToGif.Linux/ScreenToGif.Linux.csproj --no-build -- --editor
dotnet run --project ScreenToGif.Linux/ScreenToGif.Linux.csproj --no-build -- --webcam
dotnet run --project ScreenToGif.Linux/ScreenToGif.Linux.csproj --no-build -- --board
```

### Make shortcuts

From this directory, the included Makefile provides the same workflow:

```bash
cd ScreenToGif.Linux
make build
make test
make run
make editor
```

Set `CONFIGURATION=Release` on any target when needed, for example `make publish CONFIGURATION=Release`. The published files are written to `artifacts/linux/<configuration>` at the repository root.

### Check desktop compatibility

The opt-in native-window check needs Python 3, `xdotool`, and `gdbus`, in addition to the build/runtime dependencies. From an unlocked desktop, run it separately from other UI smoke tests:

```bash
./ScreenToGif.Linux/scripts/check-desktop.sh /tmp/screentogif-desktop-evidence
```

The check uses temporary settings, opens only test-owned windows, and retains screenshots, a surface index, and test results in the given directory. It exercises the Board toolbar, Editor return paths, main window sizes, editor tabs, Options pages, dialogs, and Board-to-Editor media flow. Separate runs check command-line/settings startup and controlled webcam recording with generated video. It activates its own Editor for focus checks after verifying the window's process ID, and sends no desktop-wide keyboard or pointer input. A Wayland desktop runs the current UI through XWayland; the test does not establish native Wayland screen capture. The normal test suite skips these UI checks unless their opt-in environment variables are set.

To repeat at a fractional scale without changing desktop settings, set `AVALONIA_GLOBAL_SCALE_FACTOR=1.25` for that command. The surface index records the actual scale used.

On an X11 session with a window manager and compositor, set `SCREENTOGIF_X11_RECORDING_SMOKE=1` on the desktop check to also record a changing test-owned window, pause/resume, stop into Editor and independently compare its APNG export. This needs `xvfb` and `x11-xserver-utils` for the controlled X11 fixtures.

The check covers application-owned dialogs and popups. The system's native open/save pickers need a separate visual check: GNOME can deny screenshots of their Wayland windows, even when accessibility can identify and cancel them.

### Check native Wayland recording

The controlled capture tests use a private `dbus-daemon` and GStreamer generated video. Install `dbus`, `xvfb`, `x11-xserver-utils`, `xdotool`, `gstreamer1.0-plugins-base` and `gstreamer1.0-plugins-good` to run the full suite. The Good plugins provide independent rotation/crop references for the native pixel tests.

The separate live check requires an unlocked GNOME Wayland desktop, the runtime packages above, and `python3-gi`, `python3-cairo`, and `gir1.2-gtk-4.0` for its native changing-content window:

```bash
bash ScreenToGif.Linux/scripts/check-wayland-recording.sh /tmp/screentogif-wayland-evidence
```

In the sharing dialog choose only **ScreenToGif Wayland test source**. The check records that window, pauses/resumes, opens Editor, plays and reverses the frames, then compares independently decoded APNG pixels and millisecond delays with the edited project. It retains projects, APNG, screenshots and results. Set `SCREENTOGIF_WAYLAND_CURSOR=1` to request an embedded cursor. Add `SCREENTOGIF_WAYLAND_CURSOR_PROBE=1`, then move the pointer to the center of the source's red square after Share; the check compares the marker's pixels with the cursor setting. `SCREENTOGIF_WAYLAND_MANUAL=1` checks three explicit Snaps, no autonomous frames, and recovery of those frames after closing the owned source. `SCREENTOGIF_WAYLAND_LIFECYCLE=1` instead checks cancel/decline, close during preparation, retry, discard/close confirmation, restart and Exit during sharing. Follow its terminal instructions: cancel the first dialog, allow the next to close automatically, then share the test source in each subsequent dialog. `SCREENTOGIF_WAYLAND_REUSE=1` checks administrator cancellation/enable, first consent, an automatic second session, and forgetting a source. It restores the original administrator policy and needs the installed helper. Follow its terminal instructions for the authentication and sharing dialogs. Run each variant separately with a fresh evidence directory; do not combine Manual, lifecycle and reuse variants.

These opt-in tests are skipped in ordinary test runs; a skip is not native-capture evidence. A UI scale override changes the app's rendering, not the compositor's stream scale; fractional desktop capture needs its own native run.

## Install a local desktop launcher

Publish the application and install its per-user desktop entry:

```bash
cd ScreenToGif.Linux
make install-desktop CONFIGURATION=Release
```

This copies the icon and launcher to `~/.local/share`, so no root access is needed. Launch **ScreenToGif** from the applications menu afterwards. To install an already-built executable instead, run:

```bash
./scripts/install-desktop-entry.sh /absolute/path/to/ScreenToGif.Linux
```

## Build, test and install a Debian package

The package supports amd64 machines running Ubuntu 22.04+ or Debian 12+. It bundles the .NET runtime, so people installing it need no .NET SDK or runtime. A package build requires the .NET 9 SDK, `dpkg-deb` (from `dpkg`), `strip` (from `binutils`), and network access for NuGet restore. Testing also requires Docker installed and running, permission to access its daemon, and network access to pull images and install packages inside them.

From the repository root:

```bash
make -C ScreenToGif.Linux deb
make -C ScreenToGif.Linux deb-test
```

`make deb` builds a Release package with the portable `linux-x64` runtime and writes `artifacts/linux/screentogif_<version>-1_amd64.deb`, taking the version from `Directory.Build.props`. Build output and caches stay under `artifacts/`. `make deb-test` builds the package and tests it in disposable Ubuntu 22.04, Ubuntu 24.04 and Debian 12 containers: installation on a bare image, native library resolution, command-line help, ten seconds of startup under Xvfb, MP4 and WebM encoding with FFmpeg, and lintian. The package is mounted read-only; the containers are removed afterwards. To test just one release, run `DEB_TEST_IMAGES=ubuntu:24.04 make -C ScreenToGif.Linux deb-test`.

Install the resulting package on your desktop from the repository root (replace the version in the filename with the one built):

```bash
sudo apt install ./artifacts/linux/screentogif_2.43.2-1_amd64.deb
screentogif --help
```

APT installs FFmpeg as a dependency, including the encoders used for MP4 and WebM export. Launch ScreenToGif from the applications menu or run `screentogif`. Screen recording uses X11 or, on Wayland, the desktop sharing portal and the recommended GStreamer packages. Webcam recording requires access to the camera device, normally through membership in the `video` group; after changing group membership, log out and back in.

Remove the application with `sudo apt remove screentogif`. Removing or purging the package leaves each user's settings and saved projects alone.

## Current scope

The Recorder captures a rectangle of the screen at a chosen frame rate, or one frame at a time, and hands the result to the Editor. On Wayland it records a full monitor or window chosen through desktop consent, using PipeWire and GStreamer; moving the Recorder does not change that source. Window snapping, user-interaction capture, cursor following, guidelines, and desktop-wide hotkeys are not available. Options also leaves out the Windows capture back ends, thin mode and the Old and New interface choice, which Linux has no counterpart for; [`docs/recorder.md`](docs/recorder.md) lists each unavailable control beside the subsystem it needs and each setting Options leaves out, and [`docs/recorder-followups.md`](docs/recorder-followups.md) describes what each unavailable capability would take.

The Webcam recorder discovers the machine's cameras through V4L2, previews the selected one, and records it into the Editor. It needs read access to a `/dev/video*` node, which on most distributions means membership in the `video` group. See [`docs/webcam.md`](docs/webcam.md).

The Editor imports still images, animated GIF/APNG files, and common video formats; supports frame selection, reordering, deletion, and timing changes; saves self-contained `.stg-linux` projects; and exports GIF, APNG, MP4, and WebM. Options provides the Linux application settings.

The Board draws with a pen, a point eraser and a stroke eraser, records frames while you draw, and hands them to the Editor from the Startup window, configured startup or tray action, and the Editor's New and Insert groups. Insert lets you choose where the recording enters the timeline. Stroke selection is not available on Linux. [`docs/board.md`](docs/board.md) describes the workflow and every place the Board departs from its Windows counterpart.

The Linux application does not support Windows `.stg` compatibility or the Windows editor's advanced annotation/effects commands.
