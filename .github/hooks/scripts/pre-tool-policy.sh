#!/bin/bash
# Pre-tool policy hook
# Blocks dangerous shell commands from being executed by agents
# This runs BEFORE the tool executes — can deny or allow

# Read the tool input from stdin
INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | jq -r '.toolName // empty')
TOOL_ARGS=$(echo "$INPUT" | jq -r '.toolArgs // empty')

# Block direct pushes to main/production branches
if echo "$TOOL_ARGS" | grep -q "git push.*main\|git push.*prod"; then
  echo '{"permissionDecision": "deny", "permissionDecisionReason": "Direct push to main/prod is not allowed. Open a pull request instead."}'
  exit 0
fi

# Block force pushes
if echo "$TOOL_ARGS" | grep -q "git push.*--force\|git push.*-f"; then
  echo '{"permissionDecision": "deny", "permissionDecisionReason": "Force push is not allowed. Use a PR-based workflow."}'
  exit 0
fi

# Block secrets/credentials in commands
if echo "$TOOL_ARGS" | grep -qiE "(password|secret|token|api.key)="; then
  echo '{"permissionDecision": "deny", "permissionDecisionReason": "Detected potential secret in command arguments. Use environment variables or secrets."}'
  exit 0
fi

# Block rm -rf on critical paths
if echo "$TOOL_ARGS" | grep -q "rm -rf /\|rm -rf \.\|rm -rf \*"; then
  echo '{"permissionDecision": "deny", "permissionDecisionReason": "Destructive recursive delete is not allowed."}'
  exit 0
fi

# Allow all other commands
echo '{"permissionDecision": "allow"}'