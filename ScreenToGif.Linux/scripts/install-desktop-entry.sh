#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 /absolute/path/to/ScreenToGif.Linux" >&2
    exit 64
fi

app_path=$1

if [ ! -x "$app_path" ]; then
    echo "The application executable must exist and be executable: $app_path" >&2
    exit 66
fi

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
data_dir=${XDG_DATA_HOME:-"$HOME/.local/share"}
applications_dir="$data_dir/applications"
icons_dir="$data_dir/icons/hicolor/256x256/apps"
desktop_file="$applications_dir/screentogif.desktop"

mkdir -p "$applications_dir" "$icons_dir"
icon_file="$icons_dir/screentogif.png"
install -m 644 "$script_dir/../Resources/screentogif.png" "$icon_file"
sed -e "s|^Exec=.*|Exec=$app_path %U|" -e "s|^Icon=.*|Icon=$icon_file|" "$script_dir/../screentogif.desktop" > "$desktop_file"
rm -f "$applications_dir/ScreenToGif.Linux.desktop"

if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$applications_dir"
fi

echo "Installed ScreenToGif's desktop entry and icon for $app_path"
