#!/usr/bin/env bash
set -euo pipefail

usage() {
    echo "Usage: $0 <method> [--no-test] [--max-complexity <n>]" >&2
    exit 2
}

method=""
no_test=false
max_complexity=8

while [ $# -gt 0 ]; do
    case "$1" in
        --no-test) no_test=true ;;
        --max-complexity)
            [ $# -ge 2 ] || usage
            max_complexity="$2"
            shift
            ;;
        -*) usage ;;
        *) method="$1" ;;
    esac
    shift
done

[ -n "$method" ] || usage

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
results_dir="$repo_root/Test Results/method-metrics"

if [ "$no_test" = false ]; then
    rm -rf "$results_dir"

    if ! dotnet test "$repo_root/Nexora.slnx" -v q -nologo \
        --collect:"XPlat Code Coverage" --results-directory "$results_dir"; then
        echo "Warning: some tests failed or did not build, so coverage may be incomplete." >&2
    fi
fi

python3 -I - "$repo_root" "$results_dir" "$method" "$max_complexity" <<'EOF'
import os
import re
import sys
import xml.etree.ElementTree as ElementTree

repo_root, results_dir, method, max_complexity = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4])


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


reports = [
    os.path.join(directory, file)
    for directory, _, files in os.walk(results_dir)
    for file in files
    if file == "coverage.cobertura.xml"
]
if not reports:
    sys.exit(f"No coverage found in {results_dir}. Run without --no-test first.")

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
                    "name": f"{owner}.{name}",
                    "label": f"{owner}.{name}{signature}",
                    "file": file,
                    "complexity": int(node.get("complexity")),
                    "lines": {},
                }
            lines = methods[key]["lines"]
            for line in node.findall("lines/line"):
                number = int(line.get("number"))
                lines[number] = lines.get(number, False) or int(line.get("hits")) > 0

matched = [m for m in methods.values() if method.lower() in m["name"].lower()]
if not matched:
    sys.exit(f"No method matches '{method}'.")

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
