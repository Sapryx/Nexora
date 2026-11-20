#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
data="${1:-$HOME/nexora.perf.data}"
app="$root/src/Nexora/bin/Release/net10.0/Nexora"
server="https://debuginfod.archlinux.org"
store="/usr/lib/debug/.build-id"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

if (( $(sysctl -n kernel.perf_event_paranoid) > 1 )); then
    sudo sysctl -q kernel.perf_event_paranoid=1
fi

dotnet build "$root/src/Nexora" -c Release -v q -nologo

echo "Recording. Reproduce the FPS drops, then close the app or press Ctrl+C."
trap : INT
DOTNET_PerfMapEnabled=1 DOTNET_EnableWriteXorExecute=0 \
    perf record -F 999 -g -o "$data" -- "$app" || true
trap - INT

perf report -i "$data" --no-children --sort dso -g none -q --stdio --percent-limit 0.1 2>/dev/null \
    | awk '{print $2}' > "$work/dsos"

perf buildid-list -i "$data" --with-hits 2>/dev/null | while read -r id path; do
    [[ ${#id} -eq 40 && -f "$path" ]] || continue
    grep -qxF "$(basename "$path")" "$work/dsos" || continue
    dest="$store/${id:0:2}/${id:2}.debug"

    if [[ -e "$dest" ]] || readelf -S --wide "$path" | grep -qE '\.symtab|\.debug_info'; then
        continue
    fi

    echo "Downloading symbols for $path"

    if curl -fsSL -o "$work/$id" "$server/buildid/$id/debuginfo"; then
        sudo install -Dm644 "$work/$id" "$dest"
    else
        echo "  not available"
    fi
done

hotspot "$data"
