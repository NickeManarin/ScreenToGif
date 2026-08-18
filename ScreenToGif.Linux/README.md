# ScreenToGif for Linux

This is the Avalonia-based Linux application. It is an editor-first port: it
imports media, edits a frame timeline, saves `.stg-linux` projects, and exports
GIF, APNG, MP4, and WebM. The original `GifRecorder.sln` remains the Windows
application.

## Requirements

To build from source, install:

- a 64-bit Linux desktop session (X11 or Wayland);
- the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or newer;
- [FFmpeg](https://ffmpeg.org/), including both `ffmpeg` and `ffprobe` on
  `PATH`.

On Ubuntu, once the Microsoft .NET package feed is configured, the usual
development dependencies are:

```bash
sudo apt update
sudo apt install dotnet-sdk-9.0 ffmpeg
```

Most desktop distributions already include the native libraries Avalonia needs.
For a minimal installation, install the distribution equivalents of
`fontconfig`, `freetype`, `libX11`, `libX11-xcb`, `libXrender`, `libICE`, and
`libSM` as well.

FFmpeg is required at runtime for animated-image/video import and for every
export. MP4 and WebM export additionally require an FFmpeg build containing the
`libx264` and `libvpx-vp9` encoders. Check the setup with:

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

The default launch opens the StartUp window. To open the editor directly:

```bash
dotnet run --project ScreenToGif.Linux/ScreenToGif.Linux.csproj --no-build -- --editor
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

Set `CONFIGURATION=Release` on any target when needed, for example
`make publish CONFIGURATION=Release`. The published files are written to
`artifacts/linux/<configuration>` at the repository root.

## Install a local desktop launcher

Publish the application and install its per-user desktop entry:

```bash
cd ScreenToGif.Linux
make install-desktop CONFIGURATION=Release
```

This copies the icon and launcher to `~/.local/share`, so no root access is
needed. Launch **ScreenToGif** from the applications menu afterwards. To install
an already-built executable instead, run:

```bash
./scripts/install-desktop-entry.sh /absolute/path/to/ScreenToGif.Linux
```

## Current scope

Recorder, Webcam, Board, and Options remain visible but disabled while their
Linux implementations are pending. Windows `.stg` compatibility, desktop
capture, and the Windows editor's advanced annotation/effects commands are not
part of this Linux application yet.
