---
applyTo: "src/api/**"
---

# API Development Instructions

- All controller actions must be async
- Use dependency injection for services
- Return appropriate HTTP status codes (201 for create, 204 for delete, 404 for not found)
- Validate input in controllers before calling services
- Use Entity Framework Core for data access
- Include XML documentation for public API methods
- All new endpoints require corresponding unit tests