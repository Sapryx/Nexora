#!/usr/bin/env bash
set -euo pipefail

usage() {
    echo "Usage: $0 [<method or class>...] [--changed] [--no-test] [--max-complexity <n>]" >&2
    exit 2
}

targets=()
no_test=false
changed=false
max_complexity=8

while [ $# -gt 0 ]; do
    case "$1" in
        --no-test) no_test=true ;;
        --changed) changed=true ;;
        --max-complexity)
            [ $# -ge 2 ] || usage
            max_complexity="$2"
            shift
            ;;
        -*) usage ;;
        *) targets+=("$1") ;;
    esac
    shift
done

[ ${#targets[@]} -gt 0 ] || [ "$changed" = true ] || usage

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
results_dir="$repo_root/Test Results/method-metrics"

if [ "$no_test" = false ]; then
    rm -rf "$results_dir"

    if ! dotnet test "$repo_root/Nexora.slnx" -v q -nologo \
        --collect:"XPlat Code Coverage" --results-directory "$results_dir"; then
        echo "Warning: some tests failed or did not build, so coverage may be incomplete." >&2
    fi
fi

python3 -I - "$repo_root" "$results_dir" "$max_complexity" "$no_test" "$changed" "${targets[@]}" <<'EOF'
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ElementTree

repo_root, results_dir, max_complexity = sys.argv[1], sys.argv[2], int(sys.argv[3])
no_test, changed_only, targets = sys.argv[4] == "true", sys.argv[5] == "true", sys.argv[6:]


def source_method_name(generated_class, name):
    match = re.match(r"^<(.+)>d(__\d+)?$", generated_class)
    if name == "MoveNext" and match:
        name = match.group(1)
    match = re.match(r"^<(.+)>b__(.+)$", name)
    if match:
        return f"{match.group(1)} (lambda {match.group(2)})"
    match = re.match(r"^<(.+)>g__([^|]+)\|", name)
    if match:
        return f"{match.group(1)} (local {match.group(2)})"
    return name


def git(*args):
    return subprocess.run(["git", "-C", repo_root, *args], capture_output=True, text=True, check=True).stdout


def changed_lines():
    changes = {path: None for path in git("ls-files", "--others", "--exclude-standard").splitlines()}
    file = None
    for line in git("diff", "-U0", "--no-color", "HEAD").splitlines():
        if line.startswith("+++ "):
            file = line[6:] if line.startswith("+++ b/") else None
        elif line.startswith("@@") and file is not None:
            start, _, count = re.match(r"^@@ -\S+ \+(\d+)(,(\d+))? @@", line).groups()
            first, length = int(start), int(count or 1)
            changes.setdefault(file, set()).update(range(first, first + length))
    return changes


def newest_source_time():
    newest = 0
    for top in ("src", "tests"):
        for directory, subdirectories, files in os.walk(os.path.join(repo_root, top)):
            subdirectories[:] = [d for d in subdirectories if d not in ("bin", "obj")]
            for file in files:
                if file.endswith((".cs", ".axaml")):
                    newest = max(newest, os.path.getmtime(os.path.join(directory, file)))
    return newest


reports = [
    os.path.join(directory, file)
    for directory, _, files in os.walk(results_dir)
    for file in files
    if file == "coverage.cobertura.xml"
]
if not reports:
    sys.exit(f"No coverage found in {results_dir}. Run without --no-test first.")

if no_test and newest_source_time() > min(os.path.getmtime(report) for report in reports):
    print(
        "Warning: sources changed after the last coverage run; new or changed methods may be missing or stale."
        " Run without --no-test.",
        file=sys.stderr,
    )

methods = {}
for report in reports:
    root = ElementTree.parse(report).getroot()
    source = root.find("sources/source").text
    for cls in root.iter("class"):
        segments = cls.get("name").split("/")
        owner = re.sub(r"`\d+", "", ".".join(s for s in segments if not s.startswith("<")))
        generated_class = segments[-1] if segments[-1].startswith("<") else ""
        file = os.path.relpath(os.path.join(source, cls.get("filename")), repo_root)
        for node in cls.findall("methods/method"):
            key = (file, cls.get("name"), node.get("name"), node.get("signature"))
            if key not in methods:
                name = source_method_name(generated_class, node.get("name"))
                signature = node.get("signature") if name == node.get("name") else ""
                methods[key] = {
                    "owner": owner,
                    "name": f"{owner}.{name}",
                    "method": f"{owner}.{name.split(' (')[0]}",
                    "label": f"{owner}.{name}{signature}",
                    "file": file,
                    "complexity": int(node.get("complexity")),
                    "lines": {},
                }
            lines = methods[key]["lines"]
            for line in node.findall("lines/line"):
                number = int(line.get("number"))
                lines[number] = lines.get(number, False) or int(line.get("hits")) > 0

def names_match(name, target):
    name, target = name.lower(), target.lower()
    return name == target or name.endswith("." + target)


def target_matches(m, target):
    return names_match(m["method"], target) or names_match(m["owner"], target)


unmatched = [t for t in targets if not any(target_matches(m, t) for m in methods.values())]
if unmatched:
    sys.exit(f"No method or class matches {', '.join(repr(t) for t in unmatched)}.")

matched = [m for m in methods.values() if not targets or any(target_matches(m, t) for t in targets)]
if changed_only:
    changes = changed_lines()
    matched = [
        m for m in matched
        if m["file"] in changes and (changes[m["file"]] is None or changes[m["file"]] & m["lines"].keys())
    ]
    if not matched:
        print("No changed methods match.")
        sys.exit(0)

results = []
for m in matched:
    lines = m["lines"]
    coverage = sum(lines.values()) / len(lines) if lines else 1.0
    first_line = min(lines) if lines else 0
    passed = coverage == 1.0 and m["complexity"] <= max_complexity
    results.append((passed, coverage, -m["complexity"], m, first_line))

results.sort(key=lambda r: r[:3])
for passed, coverage, _, m, first_line in results:
    status = "PASS" if passed else "FAIL"
    print(
        f"{status}  {m['label']}  coverage={coverage * 100:.0f}%  complexity={m['complexity']}"
        f"  {m['file']}:{first_line}"
    )

sys.exit(0 if all(r[0] for r in results) else 1)
EOF
