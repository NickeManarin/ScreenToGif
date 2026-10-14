<p align="center">
  <a href="https://github.com/NickeManarin/ScreenToGif" target="_blank">
    <img align="center" alt="screen recorder" src="https://nickemanarin.github.io/ScreenToGif-Website/logos/ms-icon-144x144.png" />
  </a>
</p>

<p align="center">
  <a href="https://github.com/NickeManarin/ScreenToGif/stargazers" target="_blank">
    <img alt="GitHub stars" src="https://img.shields.io/github/stars/NickeManarin/ScreenToGif.svg" />
  </a>

  <a href="https://github.com/NickeManarin/ScreenToGif/releases" target="_blank">
    <img alt="All releases" src="https://img.shields.io/github/downloads/NickeManarin/ScreenToGif/total.svg" />
  </a>

  <a href="https://chocolatey.org/packages/screentogif" target="_blank">
    <img alt="All Chocolatey releases" src="https://img.shields.io/chocolatey/dt/screentogif.svg" />
  </a>
</p>

<h1 align="center">
  ScreenToGif 🎬
  <a href="http://www.screentogif.com/" target="_blank">screentogif.com</a>
</h1>

<p align="center">This tool allows you to record a selected area of your <strong>screen</strong>, live feed from your <strong>webcam</strong> or live drawings from a <strong>sketchboard</strong>. Afterward, you can edit and save the animation as a gif, apng, video, psd or png image.</p>

<p align="center">
  <a href="https://github.com/NickeManarin/ScreenToGif/releases" target="_blank">
    <img align="center" alt="download" src="https://nickemanarin.github.io/ScreenToGif-Website/wiki/download-now.png"/>
  </a>
</p>

<p align="center">
  <a href="https://www.microsoft.com/en-us/p/screentogif/9n3sqk8pds8g" target="_blank">
    <img align="center" alt="download Microsoft Store" src="https://nickemanarin.github.io/ScreenToGif-Website/wiki/download-store.png"/>
  </a>

  <a href="https://chocolatey.org/packages/screentogif" target="_blank">
    <img align="center" alt="Download from Chocolatey" src="https://nickemanarin.github.io/ScreenToGif-Website/wiki/download-chocolatey.png"/>
  </a>
</p>

<p align="center">
  <g-emoji ios-version="6.0" fallback-src="https://assets-cdn.github.com/images/icons/emoji/unicode/26a0.png" alias="warning">⚠️</g-emoji>
  Attention, it requires <a href="https://dotnet.microsoft.com/en-us/download/dotnet/9.0/runtime">.NET 9 Desktop Runtime</a> (or above). 
  <g-emoji ios-version="6.0" fallback-src="https://assets-cdn.github.com/images/icons/emoji/unicode/26a0.png" alias="warning">⚠️</g-emoji>
</p>

<p align="center">
  <a href="https://github.com/NickeManarin/ScreenToGif/releases/latest" target="_blank">
    <img alt="Latest GitHub release" src="https://img.shields.io/github/release/nickemanarin/screentogif.svg" />
  </a>

  <a href="https://chocolatey.org/packages/screentogif" target="_blank">
    <img alt="Latest Chocolatey release" src="https://img.shields.io/chocolatey/v/screentogif.svg" />
  </a>

  <a href="https://github.com/NickeManarin/ScreenToGif/wiki/Help" target="_blank">
    <img alt="Documentation WIP" src="https://img.shields.io/badge/Docs-WIP-red.svg" />
  </a>

  <a href="https://github.com/NickeManarin/ScreenToGif/issues" target="_blank">
    <img alt="Issues" src="https://img.shields.io/github/issues/NickeManarin/ScreenToGif.svg" />
  </a>

  <a style="text-decoration:none" href="https://discord.gg/XgEqDHX">
    <img src="https://img.shields.io/discord/318260719680356352.svg" alt="Discord" />
  </a>
</p>

## Linux application

The original `GifRecorder.sln` remains the Windows application. Linux support uses a separate Avalonia application in `ScreenToGif.Linux/`. The Linux application records a region of the screen or a webcam, edits the result on a timeline, and exports it. The Board interface remains a navigable preview.

See [the Linux build and run guide](ScreenToGif.Linux/README.md) for prerequisites, distribution notes, Makefile shortcuts, publishing, and desktop-launcher installation.

Launching the Linux application opens a Windows-like StartUp shortcut window. Recorder, Webcam, Board, and Editor are reachable from that window, and Options opens the Linux application settings. Use `--editor` or `--webcam` to bypass StartUp and open the editor or the webcam recorder directly.

The Recorder captures a rectangle of the screen and hands the result to the Editor; see [its guide](ScreenToGif.Linux/docs/recorder.md) for what it does and does not do. Webcam discovers the machine's cameras, previews the selected one, and records it into the Editor; see [the webcam recorder guide](ScreenToGif.Linux/docs/webcam.md). Board remains a navigable interface preview: it shows the intended control arrangement but does not accept drawing, record frames, create projects, or hand work to the Editor. Unavailable controls remain disabled and explain the missing capability in tooltips.

On Ubuntu, install the .NET 9 SDK and FFmpeg (`ffmpeg` and `ffprobe` must be on `PATH`). No additional .NET workload is required.

```bash
dotnet build ScreenToGif.Linux.sln
dotnet test ScreenToGif.Linux.Tests/ScreenToGif.Linux.Tests.csproj
dotnet run --project ScreenToGif.Linux/ScreenToGif.Linux.csproj
dotnet run --project ScreenToGif.Linux/ScreenToGif.Linux.csproj -- --editor
```

The Linux Recorder supports:

* capturing a region per second, per minute, per hour, or one frame at a time with Snap;
* pausing and resuming;
* stamping each frame with the delay it was actually on screen when recording per second at a free frame rate, and with the fixed delay that mode calls for otherwise;
* recording the pointer and, optionally, counting down before starting;
* opening the result in the Editor.

The Linux editor supports:

* importing still images, animated GIF/APNG files, and common video formats through the picker or drag-and-drop;
* selecting one or more frames, reordering, deleting, and retiming them;
* saving self-contained `.stg-linux` projects;
* exporting GIF, APNG, MP4, and WebM.

Screen recording needs an X11 session; under a native Wayland session the Recorder's Record control is disabled and names the portal-based backend it would need. Webcam capture goes through FFmpeg's V4L2 input and needs read access to a `/dev/video*` node, so a sandboxed package would need a PipeWire or desktop-portal path first. The Linux application does not support sketchboard drawing and recording, Windows `.stg` compatibility, or the Windows editor's advanced annotation/effects commands. Editing imported media depends on none of X11, Wayland, or desktop portal capture.
	
<h2>Would you like to help the project?</h2>

 * PayPal donation: [![PayPal page](https://img.shields.io/badge/donate-Paypal-fd8200.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=JCY2BGLULSWVJ&lc=US&item_name=ScreenToGif&item_number=screentogif&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted)
 * Patreon subscription: [![Patreon subscription](https://img.shields.io/badge/subscribe-Patreon-orange.svg)](https://www.patreon.com/nicke)
 * Ko-fi donation: [![ko-fi](https://www.ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/B0B7Y5Z9)
 * Flattr subscription: https://flattr.com/@NickeManarin/domain/screentogif.com 
 * Steam wishlist: [![Steam wishlist](https://img.shields.io/badge/donate-Steam-171a21.svg)](http://steamcommunity.com/id/nickesm/wishlist)
 * GOG Galaxy wishlist: https://www.gog.com/u/Nickesm/wishlist
 * Amazon wishlist: https://www.amazon.com/hz/wishlist/ls/2S54SRWY2K8KF?ref_=wl_share
 * Feedback (reporting bugs, ideas, etc) [![Author's Twitter](https://img.shields.io/badge/Twitter-%40NickeManarin-blue.svg)](https://twitter.com/NickeManarin)
 * [Anyone can still contribute to the localization of the app/website/installer](https://github.com/NickeManarin/ScreenToGif/blob/master/LOCALIZATION.md)
 * Create a review. :)


<h2>Please, avoid selling this app as yours</h2>
I don't care if you copy the source code to use in your project, but please avoid simply changing the name and selling as your work. 
That's not why I'm sharing the source code, at all.

<h2>Screenshots</h2>

<p align="center">
 <img align="center" alt="start up" src="https://nickemanarin.github.io/ScreenToGif-Website/media/Recorder.png" />
</p>

<p align="center">
 <img align="center" alt="start up" src="https://nickemanarin.github.io/ScreenToGif-Website/media/Startup.png" />
</p>

<p align="center">
  <img align="center" alt="editor" src="https://nickemanarin.github.io/ScreenToGif-Website/media/Editor.gif" />
</p>

<p align="center">
  <img align="center" alt="option" src="https://nickemanarin.github.io/ScreenToGif-Website/media/Options.gif" />
</p>

<p align="center">
  <img align="center" alt="keystrokes" src="https://nickemanarin.github.io/ScreenToGif-Website/media/Keys.gif" />
</p>

<h2>Mentions</h2>

<table>
	<tr>
		<th>Website</th>
	</tr>
	<tr>
		<td><a href="https://www.chip.de/downloads/Screen-To-Gif_65993193.html">Chip</a></td>
	</tr>
	<tr>
		<td><a href="https://www.softpedia.com/get/Multimedia/Graphic/Graphic-Others/Screen-to-Gif.shtml">Softpedia</a></td>
	</tr>
	<tr>
		<td><a href="https://www.portablefreeware.com/index.php?id=2895">PortableFreeware</a></td>
	</tr>
</table>

<h2>The creator also distributes this app via these websites</h2>

* [Chocolatey](https://chocolatey.org/packages/screentogif)
* [FOSSHUB](https://www.fosshub.com/ScreenToGif.html)
* [Microsoft Store](https://www.microsoft.com/en-us/p/screentogif/9n3sqk8pds8g)
