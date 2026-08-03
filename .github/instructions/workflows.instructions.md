---
applyTo: ".github/workflows/**"
---

# Workflow Instructions

- Always use least-privilege permissions
- Pin action versions to full SHA or major version tag
- Use `--no-ask-user` flag for Copilot CLI in CI
- Set `fail-fast: false` for matrix agent jobs
- Upload artifacts with `if-no-files-found: error` for critical outputs
- Include concurrency control for PR-triggered workflows
- Use environment protection rules for deployment workflows
- Never expose secrets in step summaries or logs