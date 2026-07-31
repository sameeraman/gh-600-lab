# AGENTS.md

## Project Structure

- `src/api/` - ASP.NET Core 8 Todo API with EF Core.
- `src/frontend/` - React and Vite frontend.
- `infra/` - Azure infrastructure defined with Bicep.
- `.github/workflows/` - Build, test, security, and deployment workflows.

## Testing Commands

- API: `dotnet test src/api/Tests/TodoApi.Tests.csproj`
- Frontend: `cd src/frontend && npm test`
- Frontend build: `cd src/frontend && npm run build`
- E2E: `cd src/frontend && npx playwright test`

## Agent Guidelines

- Work on a branch and submit changes through a pull request.
- Do not push directly to `main`.
- Use read-only tools for reviews.
- Use edit and execute tools only for implementation or testing tasks.
- Keep changes scoped to the requested task.
- Report validation results and unresolved risks.
- Require human review for deployment and infrastructure changes.