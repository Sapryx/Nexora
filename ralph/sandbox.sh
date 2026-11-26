stream_helpers='
def color($code): "\u001b[\($code)m";
def reset: color("0");
def clip: if length > 160 then .[:160] + "..." else . end;
def nonblank_lines: gsub("\r"; "") | split("\n") | map(select(test("\\S")));
def command_line($name; $command):
  ($command | nonblank_lines) as $lines
  | color("1;36") + "> " + $name + reset + " " + color("36") + ($lines[0] // "" | clip)
    + (if ($lines | length) > 1 then color("2") + " (+\($lines | length - 1) lines)" else "" end) + reset + "\n";
def render_output($style):
  nonblank_lines as $lines
  | ($lines[:3] | map($style + "  " + clip + reset + "\n") | join(""))
    + (if ($lines | length) > 3 then color("2") + "  ... \($lines | length - 3) more lines" + reset + "\n" else "" end);
'

todo_count='[.[] | select(.state == "todo")] | length'
todo_list='
to_entries[]
| select(.value.state == "todo")
| "\(.key). \(.value.description)\n"
  + (.value.steps | map("   - \(.)\n") | join(""))
  + (.value.out_of_scope // [] | if length > 0 then "   Out of scope:\n" + (map("   - \(.)\n") | join("")) else "" end)'

remaining() {
  if [ -f feature-list.json ]; then
    jq "$todo_count" < feature-list.json
  else
    echo 0
  fi
}

prompt() {
  cat PROMPT.md
  printf '\n## Checks\n\nThey %s. The end of their output:\n\n```\n%s\n```\n' "$1" "$(tail -n 60 <<< "$2")"
  printf '\n## Todo entries\n\nNumbers are indexes in `feature-list.json`.\n\n%s\n' "$(jq -rj "$todo_list" < feature-list.json)"
}

prepare_sandbox() {
  local agent="$1" sandbox="$2" root="$3" state="$4" skills_mount="$5"

  echo "=== Preparing sandbox $sandbox."
  mkdir -p "$state/nuget" "$state/artifacts"
  docker build -q --provenance=false --build-arg "AGENT=$agent" -t "$sandbox" - < ralph/Dockerfile > /dev/null
  docker save -o "$state/image.tar" "$sandbox"
  sbx template load "$state/image.tar" > /dev/null
  rm "$state/image.tar"
  sbx rm -f "$sandbox" > /dev/null 2>&1 || true
  sbx create --no-progress --skills off --name "$sandbox" --pull never -t "$sandbox" \
    -e TZ="$(timedatectl show -p Timezone --value)" \
    -e GIT_AUTHOR_NAME="$(git config user.name)" \
    -e GIT_AUTHOR_EMAIL="$(git config user.email)" \
    -e GIT_COMMITTER_NAME="$(git config user.name)" \
    -e GIT_COMMITTER_EMAIL="$(git config user.email)" \
    -e GIT_CONFIG_COUNT=1 \
    -e GIT_CONFIG_KEY_0=protocol.allow \
    -e GIT_CONFIG_VALUE_0=never \
    "$agent" "$root" \
    "$state/nuget:/home/agent/.nuget" \
    "$state/artifacts:/home/agent/artifacts" \
    "$skills_mount" \
    > /dev/null
}

run_iterations() {
  local sandbox="$1" iterations="$2" logs="$3"
  local verify='dotnet build Nexora.slnx -v q -nologo && scripts/test.sh -v q -nologo --logger "console;verbosity=minimal"'
  local i todo log checks verdict instructions

  for ((i = 1; i <= iterations; i++)); do
    todo="$(remaining)"
    if [ "$todo" -eq 0 ]; then
      echo "=== No todo entries left after $((i - 1)) iterations."
      return
    fi

    log="$logs/$(date +%Y%m%d-%H%M%S)-$i.log"
    echo "=== Ralph iteration $i/$iterations (sandbox), $todo todo entries (log: $log)"

    if checks="$(sbx exec "$sandbox" sh -c "$verify" < /dev/null 2>&1 | tr -d '\r')"; then
      verdict="passed"
    else
      verdict="failed"
    fi
    echo "=== Checks $verdict."

    instructions="$(prompt "$verdict" "$checks")"
    run_agent "$sandbox" "$instructions" < /dev/null \
      | grep --line-buffered '^{' \
      | tee "$log" \
      | jq --unbuffered -rj "$stream_helpers$stream"

    jq -rjs "$context" < "$log"
  done

  echo "=== Reached $iterations iterations, $(remaining) todo entries left."
}

run_ralph() {
  local agent="$1" service="$2" login="$3" sandbox="$4" skills_mount="$5"
  shift 5
  local iterations="${1:-10}" root state

  if [[ "$#" -gt 1 || ! "$iterations" =~ ^[1-9][0-9]*$ ]]; then
    echo "Usage: $0 [iterations]" >&2
    exit 2
  fi

  if ! command -v jq > /dev/null; then
    echo "Install jq first." >&2
    exit 1
  fi

  if ! sbx secret ls --global --quiet --service "$service" | grep -Fx "$service" > /dev/null; then
    echo "Sign in to $agent for sandboxes first: $login" >&2
    exit 1
  fi

  root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
  state="$HOME/.$sandbox"
  mkdir -p "$state/logs"
  cd "$root"
  prepare_sandbox "$agent" "$sandbox" "$root" "$state" "$skills_mount"
  trap "sbx stop '$sandbox' > /dev/null 2>&1 || true" EXIT
  trap 'printf "\n=== Interrupted.\n"; exit 130' INT TERM
  sbx policy allow network --sandbox "$sandbox" api.nuget.org > /dev/null
  run_iterations "$sandbox" "$iterations" "$state/logs"
}
