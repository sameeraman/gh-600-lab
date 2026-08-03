# Security Review

Review the selected changes for:
1. **Authentication & Authorization** — missing auth checks, privilege escalation
2. **Secret Exposure** — hardcoded secrets, tokens, keys in code or logs
3. **Injection Vulnerabilities** — SQL injection, command injection, XSS
4. **Dependency Risk** — known CVEs, outdated packages, typosquatting
5. **Workflow Security** — permission escalation, workflow injection, unsafe checkout
6. **Infrastructure Security** — public endpoints without auth, missing HTTPS, overly permissive network rules

Return findings with:
- Severity (Critical/High/Medium/Low)
- File path and line number
- Description of the vulnerability
- Recommended fix with code example where applicable