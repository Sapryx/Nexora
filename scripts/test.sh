#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if [ "${1:-}" = "--all" ]; then
    shift
    exec dotnet test Nexora.slnx "$@"
fi

changed="$({ git diff --name-only HEAD; git ls-files --others --exclude-standard; } 2> /dev/null || true)"
filter="Category!=Slow"

is_changed() {
    local path="$1"
    local file

    while IFS= read -r file; do
        if [ -n "$file" ] && [[ "$file" == "$path"* ]]; then
            return 0
        fi
    done <<< "$changed"

    return 1
}

while IFS= read -r test_file; do
    mapfile -t covered < <(sed -n 's/^\s*\[Trait("Covers", "\([^"]*\)")\]\s*$/\1/p' "$test_file")

    for path in "$test_file" "${covered[@]}"; do
        if is_changed "$path"; then
            echo "Including slow tests in $test_file: its covered code changed."
            for value in "${covered[@]}"; do
                filter+="|Covers=$value"
            done
            break
        fi
    done
done < <(grep -rl --include='*.cs' --exclude-dir=bin --exclude-dir=obj '\[Trait("Covers", ' tests)

exec dotnet test Nexora.slnx --filter "$filter" "$@"
