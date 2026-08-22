# Smart Task Management System - Codex Instructions

## Project Requirements

- Build a Smart Task Management System.
- Backend must use ASP.NET Core 10 or 9.
- Database must use SQL Server.
- Use Entity Framework Core.
- Frontend must use Angular 18 or higher.
- Use Clean Architecture or N-Layer Architecture.

## Backend Rules

- Use async/await for database and service operations.
- Use DTOs for API requests and responses.
- Do not expose database entities directly from controllers.
- Use FluentValidation.
- Use JWT authentication.
- Implement refresh tokens.
- Implement role-based authorization.
- Use global exception handling.
- Use Serilog logging.
- Add Swagger/OpenAPI.
- Add health checks.
- Add CORS and basic rate limiting.
- Use consistent API response models.

## Roles

- Admin
- Project Manager
- Team Member

## Security Rules

- Never commit passwords, JWT secrets or AI API keys.
- Use appsettings.Development.json or User Secrets for local secrets.
- Validate all user input.
- Do not trust client-side authorization only.

## Development Rules

- Do not modify unrelated files.
- Implement one feature at a time.
- Run build and tests after every feature.
- Before changing architecture, explain the reason.
- Follow the existing project structure and naming conventions.
- Keep the code readable and maintainable.
- Add migration whenever the database model changes.

## Required Modules

1. Authentication and Authorization
2. Project Management
3. Task Management
4. Search, Filtering, Sorting and Pagination
5. Dashboard
6. AI Task Description Improvement
7. Angular Frontend
8. Documentation

## Important Workflow

First analyze the requirement and create a plan.
Do not write implementation code until explicitly requested.
After implementation, show:
- Files changed
- What was implemented
- Build result
- Test result
- Remaining issues