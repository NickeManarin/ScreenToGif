#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 0 ]; then
    echo "Usage: $0 (builds the amd64 Debian package under artifacts/linux)" >&2
    exit 64
fi

step=prerequisites
trap 'echo "Package build failed at $step. Check the output above, fix the prerequisite or network access, and rerun make deb." >&2' ERR
dotnet=${DOTNET:-dotnet}
for tool in "$dotnet" dpkg-deb strip; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "Package build prerequisites: $tool is missing. Install the .NET 9 SDK, dpkg and binutils tools, then rerun make deb." >&2
        exit 69
    fi
done

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_dir=$(cd -- "$script_dir/../.." && pwd)
output_dir="$repo_dir/artifacts/linux"
version=$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$repo_dir/Directory.Build.props")
if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+([.+~-][0-9A-Za-z.+~-]+)?$ ]]; then
    echo "Package build version: Directory.Build.props must contain one valid numeric Version. Fix it and rerun make deb." >&2
    exit 65
fi

mkdir -p "$output_dir"
work_dir=$(mktemp -d "$output_dir/deb-build.XXXXXX")
trap 'rm -rf -- "$work_dir"' EXIT
export DOTNET_CLI_HOME="$output_dir/dotnet-home"
export NUGET_PACKAGES="$output_dir/nuget/packages"
export NUGET_HTTP_CACHE_PATH="$output_dir/nuget/http-cache"
export NUGET_PLUGINS_CACHE_PATH="$output_dir/nuget/plugins-cache"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export XDG_DATA_HOME="$output_dir/xdg/data"
export XDG_CONFIG_HOME="$output_dir/xdg/config"
export XDG_CACHE_HOME="$output_dir/xdg/cache"
export TMPDIR="$work_dir"

step='self-contained linux-x64 publish (restore requires network access)'
"$dotnet" publish "$repo_dir/ScreenToGif.Linux/ScreenToGif.Linux.csproj" \
    --configuration Release --runtime linux-x64 --self-contained true \
    --artifacts-path "$output_dir/build" --output "$work_dir/publish"

step='package staging'
stage="$work_dir/package"
mkdir -p "$stage/DEBIAN" "$stage/usr/lib/screentogif" "$stage/usr/bin" \
    "$stage/usr/share/applications" "$stage/usr/share/icons/hicolor/256x256/apps" \
    "$stage/usr/share/doc/screentogif"
cp -a "$work_dir/publish/." "$stage/usr/lib/screentogif/"
find "$stage/usr/lib/screentogif" -name '*.pdb' -delete
rm -f "$stage/usr/lib/screentogif/screentogif.desktop" "$stage/usr/lib/screentogif/Resources/screentogif.png"
rmdir "$stage/usr/lib/screentogif/Resources" 2>/dev/null || true
ln -s ../lib/screentogif/ScreenToGif.Linux "$stage/usr/bin/screentogif"
sed 's/^Exec=.*/Exec=screentogif/' "$repo_dir/ScreenToGif.Linux/screentogif.desktop" > "$stage/usr/share/applications/screentogif.desktop"
install -m 644 "$repo_dir/ScreenToGif.Linux/Resources/screentogif.png" "$stage/usr/share/icons/hicolor/256x256/apps/screentogif.png"
{
    printf 'ScreenToGif\nCopyright (c) 2026 Nicke Manarin\nLicense: MS-PL\n\n'
    cat "$repo_dir/LICENSE.txt"
    printf '\n'
} > "$stage/usr/share/doc/screentogif/copyright"
cat <<EOF | gzip -n > "$stage/usr/share/doc/screentogif/changelog.Debian.gz"
screentogif ($version-1) unstable; urgency=low

  * Package the Linux application with its self-contained .NET runtime.

 -- Nicke Manarin <nicke@outlook.com.br>  $(LC_ALL=C date -R)
EOF
# NuGet's rendering libraries contain debug symbols; remove those from the staged copies.
strip --strip-unneeded "$stage/usr/lib/screentogif/libSkiaSharp.so" "$stage/usr/lib/screentogif/libHarfBuzzSharp.so"
mkdir -p "$stage/usr/share/lintian/overrides"
cat > "$stage/usr/share/lintian/overrides/screentogif" <<'EOF'
# The self-contained .NET runtime ships its own compression implementation with zlib.
screentogif: embedded-library zlib *usr/lib/screentogif/libSystem.IO.Compression.Native.so*
# Avalonia's upstream SkiaSharp native asset embeds freetype for font rendering.
screentogif: embedded-library freetype *usr/lib/screentogif/libSkiaSharp.so*
# Avalonia's upstream SkiaSharp native asset embeds libjpeg for image decoding.
screentogif: embedded-library libjpeg *usr/lib/screentogif/libSkiaSharp.so*
# Avalonia's upstream SkiaSharp native asset embeds libpng for image decoding.
screentogif: embedded-library libpng *usr/lib/screentogif/libSkiaSharp.so*
EOF
cat > "$stage/DEBIAN/control" <<EOF
Package: screentogif
Version: $version-1
Architecture: amd64
Section: video
Priority: optional
Maintainer: Nicke Manarin <nicke@outlook.com.br>
Homepage: https://nicke.tech/screentogif
Installed-Size: $(du -sk "$stage/usr" | cut -f1)
Depends: ffmpeg, libc6 (>= 2.35), libgcc-s1, libstdc++6, zlib1g, libicu78 | libicu76 | libicu74 | libicu72 | libicu70, libssl3t64 | libssl3, libfontconfig1, libfreetype6, libx11-6, libx11-xcb1, libxcb1, libxext6, libxfixes3, libxrender1, libice6, libsm6
Description: Screen, webcam and sketchboard recorder, with integrated editor
 Records and edits animated images and videos. Includes the .NET runtime.
 Screen recording needs an X11 session. Webcam recording needs membership
 in the video group and access to a video device.
EOF
find "$stage" -type d -exec chmod 755 {} +
find "$stage" -type f -exec chmod 644 {} +
chmod 755 "$stage/usr/lib/screentogif/ScreenToGif.Linux"
chmod 755 "$stage/usr/lib/screentogif/createdump"
step='dpkg-deb archive creation'
package="$output_dir/screentogif_${version}-1_amd64.deb"
dpkg-deb --build --root-owner-group -Zxz "$stage" "$work_dir/output.deb"
mv "$work_dir/output.deb" "$package"
echo "Built $package"
