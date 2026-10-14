#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -gt 1 ]; then
    echo "Usage: $0 [package.deb] (DEB_TEST_IMAGES selects space-separated Docker images)" >&2
    exit 64
fi
if ! command -v docker >/dev/null 2>&1; then
    echo "Package test prerequisites: docker is missing. Install and start Docker, then rerun make deb-test." >&2
    exit 69
fi
script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_dir=$(cd -- "$script_dir/../.." && pwd)
version=$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$repo_dir/Directory.Build.props")
package=${1:-"$repo_dir/artifacts/linux/screentogif_${version}-1_amd64.deb"}
if [ ! -f "$package" ]; then
    echo "Package test input: no package at $package. Run make deb first, then rerun the test." >&2
    exit 66
fi
package=$(realpath -- "$package")
# libc is a loader alias rather than a SONAME; resolve its Linux SONAME in the container.
libraries=$(find "$repo_dir/ScreenToGif.Linux" -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' \
    -exec sed -n 's/.*\[DllImport("\(lib[^" ]*\)".*/\1/p' {} + | sort -u)
read -r -a images <<< "${DEB_TEST_IMAGES:-ubuntu:22.04 ubuntu:24.04 debian:12}"
if [ "${#images[@]}" -eq 0 ]; then
    echo "Package test images: select at least one image with DEB_TEST_IMAGES, then rerun make deb-test." >&2
    exit 64
fi
for image in "${images[@]}"; do
    echo "[$image] Starting package acceptance test"
    if ! docker run --rm -i --platform linux/amd64 \
        --mount "type=bind,source=$package,target=/package.deb,readonly" \
        --env "TEST_IMAGE=$image" --env "APP_LIBRARIES=$libraries" \
        "$image" bash -s <<'CONTAINER'
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive
step='bare package install'
trap 'echo "[$TEST_IMAGE] FAIL: $step. Fix the package or container/network prerequisite and rerun make deb-test." >&2' ERR
apt-get update -qq
apt-get install -y /package.deb

step='library resolution before test packages'
while IFS= read -r -d '' file; do
    # Managed DLLs are PE files; only native ELF files need ldd.
    if [ "$(od -An -N4 -tx1 "$file" | tr -d ' \n')" != 7f454c46 ]; then
        continue
    fi
    links=$(ldd "$file" 2>&1 || true)
    missing=$(printf '%s\n' "$links" | sed '/liblttng-ust.so.0/d' | sed -n '/not found/p')
    if [ -n "$missing" ]; then
        echo "$file: $missing" >&2
        false
    fi
done < <(find /usr/lib/screentogif -type f -print0)
while IFS= read -r library; do
    [ "$library" != libc ] || library=libc.so.6
    /sbin/ldconfig -p | awk -v name="$library" '$1 == name {found=1} END {exit !found}' || {
        echo "Missing DllImport library: $library" >&2
        false
    }
done <<< "$APP_LIBRARIES"

step='command-line help'
screentogif --help > /tmp/help.log 2>&1
cat /tmp/help.log
grep -qi usage /tmp/help.log

step='test prerequisites'
apt-get install -y xvfb xauth lintian desktop-file-utils
step='desktop validation'
desktop-file-validate /usr/share/applications/screentogif.desktop
step='app start under Xvfb'
xvfb-run -a bash -c '
    screentogif > /tmp/app.log 2>&1 &
    app_pid=$!
    trap "kill $app_pid 2>/dev/null || true; wait $app_pid 2>/dev/null || true" EXIT
    sleep 10
    if ! kill -0 "$app_pid" 2>/dev/null || grep -qi "unhandled exception" /tmp/app.log; then
        sed -n "1p" /tmp/app.log >&2
        exit 1
    fi
'
step='ffmpeg encode and ffprobe readback'
for codec in libx264 libvpx-vp9; do
    extension=mp4
    [ "$codec" != libvpx-vp9 ] || extension=webm
    ffmpeg -nostdin -hide_banner -loglevel error -f lavfi -i 'testsrc2=size=64x64:rate=10:duration=1' -c:v "$codec" -y "/tmp/clip.$extension"
    ffprobe -v error -show_streams "/tmp/clip.$extension" | grep 'codec_type=video'
done
step=lintian
lintian --allow-root /package.deb > /tmp/lintian.log 2>&1 || lintian_status=$?
cat /tmp/lintian.log
if grep -q '^E:' /tmp/lintian.log || [ "${lintian_status:-0}" -gt 1 ]; then
    false
fi
echo "[$TEST_IMAGE] PASS: bare install, libraries, help, Xvfb startup, MP4/WebM, lintian"
CONTAINER
    then
        echo "[$image] Package test failed. Read the labelled failing step above; check Docker/network access if the container did not start, then rerun make deb-test." >&2
        exit 1
    fi
done
