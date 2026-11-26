#!/usr/bin/env bash
set -euo pipefail

source "$(dirname "${BASH_SOURCE[0]}")/sandbox.sh"

stream='
def call_summary: .input.command // .input.file_path // .input.pattern // .input.skill // .input.description // "" | tostring;
def result_text: if (.content | type) == "string" then .content else [.content[]? | select(.type == "text").text] | join("\n") end;

if .type == "assistant" then
  .message.content[]?
  | if .type == "text" then "\n" + .text + "\n\n"
    elif .type == "tool_use" then command_line(.name; call_summary)
    else empty end
elif .type == "user" then
  .message.content[]?
  | select(.type == "tool_result")
  | (if .is_error then color("31") else color("2") end) as $style
  | result_text | render_output($style)
else empty end'

context='
(map(select(.type == "assistant" and .parent_tool_use_id == null)) | last) as $last
| (map(select(.type == "result")) | last) as $result
| select($last != null and $result != null)
| $result.modelUsage[$last.message.model].contextWindow as $window
| select($window != null)
| ($last.message.usage
    | .input_tokens + .cache_creation_input_tokens + .cache_read_input_tokens + .output_tokens) as $used
| "=== Context: \($used) / \($window) tokens (\($used * 1000 / $window | round / 10)%)\n"'

run_agent() {
  sbx exec "$1" claude -p "$2" \
    --dangerously-skip-permissions \
    --append-system-prompt "$(cat "$HOME/.claude/CLAUDE.md")" \
    --disallowedTools "Bash(git push:*)" \
    --settings '{"attribution": {"commit": "", "pr": ""}}' \
    --model claude-opus-5-5 \
    --effort medium \
    --no-session-persistence \
    --verbose \
    --output-format stream-json
}

run_ralph claude anthropic 'sbx run claude -- auth login' nexora-ralph \
  "$HOME/.claude/skills:/home/agent/.claude/skills:ro" "$@"
