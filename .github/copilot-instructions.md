# Repository Instructions

## Architecture

- `src/api` is an ASP.NET Core 8 Web API using Entity Framework Core.
- Production data uses Azure SQL; local development and tests use EF Core InMemory.
- `src/frontend` is a React application built with Vite.
- `infra` contains Azure Bicep infrastructure.
- `.github/workflows` contains CI/CD workflows.

## Conventions

- Use async/await for database and service operations.
- Keep controllers thin; business logic belongs in services.
- Scope every todo operation by the authenticated user ID.
- Keep frontend API calls relative to `/api`.
- Make focused changes and preserve existing public APIs where possible.

## Testing

- API: `dotnet test src/api/Tests/TodoApi.Tests.csproj`
- Frontend: `cd src/frontend && npm ci && npm test`
- Frontend build: `cd src/frontend && npm run build`
- Bicep: `az bicep build --file infra/main.bicep`

Run relevant tests after every implementation change.

## Security

- Never commit credentials, tokens, or connection-string secrets.
- Use managed identity for Azure service authentication.
- Never trust a user ID supplied in a request body.
- Do not weaken authentication, authorization, or branch protections.
- Infrastructure and production deployment changes require human review.