# Smart Task Management System

## Project overview

Smart Task Management System is a role-aware project and task management application. It provides an ASP.NET Core 10 REST API backed by SQL Server and an Angular standalone-component client. The API is the security boundary: project ownership, project membership, task assignment, and role permissions are enforced in the backend application layer.

The repository includes the assignment implementation, EF Core migrations, automated backend tests, Angular unit tests, a Postman collection, and AI prompt documentation.

## Main features

- User registration, login, logout, current-user lookup, JWT access tokens, and rotating HttpOnly refresh tokens.
- Admin, Project Manager, and Team Member roles.
- Project CRUD with ownership-scoped access, search, sorting, and pagination.
- Project membership management for active Team Members.
- Task CRUD, assignment/unassignment, status transitions, priority updates, due dates, search, filters, sorting, and pagination.
- Dashboard summary scoped to the authenticated user's role and project/task visibility.
- AI-assisted task description improvement through a local Ollama provider.
- Consistent API response and paginated response envelopes.
- FluentValidation, global exception handling, Serilog request logging, Swagger/OpenAPI, SQL Server health checks, CORS, and rate limiting.
- Angular Material UI with standalone components, reactive forms, route guards, authentication interceptor, responsive project/task/dashboard views, and loading/error/empty states.

## Technology stack

| Area | Technology |
|---|---|
| Backend | ASP.NET Core 10, C# |
| Architecture | Clean Architecture modular monolith |
| Persistence | Entity Framework Core 10, SQL Server |
| Authentication | JWT bearer access token, hashed rotating refresh token in HttpOnly cookie |
| Validation | FluentValidation |
| API documentation | Swagger/OpenAPI via Swashbuckle |
| Logging | Serilog console sink |
| Health | ASP.NET Core health checks and EF Core SQL Server check |
| Frontend | Angular 22, standalone components, strict TypeScript |
| UI | Angular Material 22 |
| Frontend tests | Angular CLI and Vitest |
| AI | Ollama `/api/chat` endpoint, default model `gemma3` |

Versions validated in the current development environment:

- .NET SDK `10.0.400`
- Node.js `v26.1.0`
- npm `11.13.0`
- Local Angular CLI `22.1.5` (no global CLI installation is required)

## Backend architecture

The backend is split into four projects:

- `SmartTaskManagement.Api`: controllers, HTTP/authentication setup, Swagger, CORS, rate limiting, response formatting, middleware, and health endpoints.
- `SmartTaskManagement.Application`: feature services, DTO contracts, validators, authorization/resource checks, store abstractions, and response models.
- `SmartTaskManagement.Domain`: entities, enums, domain rules, and domain exceptions. `TaskItem` is used instead of `Task` to avoid a collision with `System.Threading.Tasks.Task`.
- `SmartTaskManagement.Infrastructure`: EF Core `ApplicationDbContext`, SQL Server configurations, migrations, stores, password hashing, JWT/refresh-token services, Ollama adapter, DI, seeding, and health checks.

Controllers do not expose EF entities. Read operations use DTO projections/queries, and application services perform role and resource authorization before mutations or scoped reads.

## Frontend architecture

The Angular client uses standalone components and lazy-loaded feature routes:

- `core`: authentication, token/session handling, guards, interceptor, API/error models, and shared data-access models.
- `shared`: reusable navigation and UI pieces.
- `features/auth`: login and registration.
- `features/dashboard`: scoped dashboard summary cards and breakdowns.
- `features/projects`: project list/details/forms and project membership operations.
- `features/tasks`: task list/forms, filters, assignment, status, priority, and pagination.
- `layout`: authenticated application shell.

The access token is kept by the frontend session service; the refresh token is managed by the backend as an HttpOnly cookie. The client attaches access tokens, performs one guarded refresh flow after a 401, and routes unauthenticated users to login. UI role checks only improve the user experience; backend authorization remains authoritative.

## Repository and folder structure

```text
SmartTaskManagementSystem/
├── AGENTS.md
├── README.md
├── PROMPTS.md
├── .gitignore
├── backend/
│   ├── SmartTaskManagement.slnx
│   ├── src/
│   │   ├── SmartTaskManagement.Api/
│   │   │   ├── Controllers/ (Auth, Projects, ProjectMembers, Tasks, Dashboard, Ai)
│   │   │   ├── Middleware/GlobalExceptionHandler.cs
│   │   │   ├── Models/ApiResponseFactory.cs
│   │   │   ├── Properties/launchSettings.json
│   │   │   ├── Program.cs
│   │   │   └── appsettings*.json
│   │   ├── SmartTaskManagement.Application/
│   │   │   ├── Abstractions/ (Authentication, Ai, Dashboard, Projects, Tasks)
│   │   │   ├── Common/ (Models, Exceptions)
│   │   │   ├── Features/ (Auth, Projects, Tasks, Dashboard, Ai)
│   │   │   └── DependencyInjection.cs
│   │   ├── SmartTaskManagement.Domain/
│   │   │   ├── Entities/
│   │   │   ├── Enums/
│   │   │   ├── Policies/
│   │   │   ├── Constants/
│   │   │   └── Exceptions/
│   │   └── SmartTaskManagement.Infrastructure/
│   │       ├── Authentication/
│   │       ├── Ai/
│   │       ├── Dashboard/
│   │       ├── Projects/
│   │       ├── Tasks/
│   │       ├── Persistence/ (Configurations, Migrations, DbContext, design-time factory)
│   │       ├── Seeding/
│   │       └── DependencyInjection.cs
│   └── tests/SmartTaskManagement.Tests/
│       ├── SmokeTests.cs
│       ├── TaskServiceTests.cs
│       ├── DashboardServiceTests.cs
│       └── AiTaskDescriptionTests.cs
├── database/
├── docs/
│   ├── Smart Task Management System Assignment.pdf
│   ├── SmartTaskManagement.postman_collection.json
│   ├── SmartTaskManagement.postman_environment.example.json
│   └── architecture-and-design.md
└── frontend/
    ├── AGENTS.md
    └── smart-task-client/
        ├── angular.json
        ├── package.json
        ├── package-lock.json
        ├── public/
        └── src/
            ├── environments/
            ├── app/core/ (guards, interceptors, models, services)
            ├── app/shared/ (components, models)
            ├── app/features/ (auth, dashboard, projects, tasks)
            ├── app/layout/
            ├── app.config.ts
            ├── app.routes.ts
            ├── main.ts
            └── styles.scss
```

## Prerequisites

- .NET SDK 10.x.
- SQL Server or SQL Server LocalDB. The checked-in development connection uses `(localdb)\MSSQLLocalDB` and trusted authentication; replace it with a configured SQL Server connection when required.
- Node.js 20+ and npm. The current environment was verified with Node.js `v26.1.0` and npm `11.13.0`.
- Ollama only if the AI endpoint is used.
- PowerShell or an equivalent shell.

The Angular CLI is installed locally through `package.json`. Run commands through npm rather than installing a global CLI.

## SQL Server setup

1. Start SQL Server or install/start SQL Server LocalDB.
2. Configure `ConnectionStrings:DefaultConnection` in User Secrets or an environment variable. Do not commit a connection string containing a password.
3. Ensure the configured account can create/update the application database.
4. Apply migrations explicitly when the target database is configured:

```powershell
cd backend
dotnet ef database update `
  --project src/SmartTaskManagement.Infrastructure/SmartTaskManagement.Infrastructure.csproj `
  --startup-project src/SmartTaskManagement.Api/SmartTaskManagement.Api.csproj
```

Development startup has `Authentication:ApplyMigrationsOnStartup` enabled and seeds the three roles. Keep automatic migration disabled for controlled production deployments and apply migrations as a release step instead.

## User Secrets and environment variables

The API project has a User Secrets ID. Initialize it once, then set local-only values. The examples below contain placeholders only; replace them locally without committing the resulting values:

```powershell
dotnet user-secrets init --project backend/src/SmartTaskManagement.Api

dotnet user-secrets set --project backend/src/SmartTaskManagement.Api `
  "ConnectionStrings:DefaultConnection" "<sql-server-connection-string>"
dotnet user-secrets set --project backend/src/SmartTaskManagement.Api `
  "Authentication:Jwt:SigningKey" "<long-random-signing-key-at-least-32-characters>"
dotnet user-secrets set --project backend/src/SmartTaskManagement.Api `
  "Authentication:SeedAdmin:Email" "<development-admin-email>"
dotnet user-secrets set --project backend/src/SmartTaskManagement.Api `
  "Authentication:SeedAdmin:Password" "<strong-development-admin-password>"
```

For local Ollama, no API key is required:

```powershell
dotnet user-secrets set --project backend/src/SmartTaskManagement.Api "Ai:Provider" "Ollama"
dotnet user-secrets set --project backend/src/SmartTaskManagement.Api "Ai:Endpoint" "http://localhost:11434/api/chat"
dotnet user-secrets set --project backend/src/SmartTaskManagement.Api "Ai:Model" "gemma3"
dotnet user-secrets set --project backend/src/SmartTaskManagement.Api "Ai:TimeoutSeconds" "30"
```

ASP.NET Core environment-variable names use double underscores:

```powershell
$env:ConnectionStrings__DefaultConnection = "<sql-server-connection-string>"
$env:Authentication__Jwt__SigningKey = "<long-random-signing-key>"
$env:Authentication__SeedAdmin__Email = "<development-admin-email>"
$env:Authentication__SeedAdmin__Password = "<strong-development-admin-password>"
$env:Ai__Provider = "Ollama"
$env:Ai__Endpoint = "http://localhost:11434/api/chat"
$env:Ai__Model = "gemma3"
```

Never place passwords, JWT signing keys, AI keys, private keys, or credential-bearing connection strings in checked-in configuration, frontend environment files, Postman collections, or source code.

## EF Core migrations

Run EF commands from `backend`:

```powershell
dotnet ef migrations list `
  --project src/SmartTaskManagement.Infrastructure/SmartTaskManagement.Infrastructure.csproj `
  --startup-project src/SmartTaskManagement.Api/SmartTaskManagement.Api.csproj

dotnet ef migrations has-pending-model-changes `
  --project src/SmartTaskManagement.Infrastructure/SmartTaskManagement.Infrastructure.csproj `
  --startup-project src/SmartTaskManagement.Api/SmartTaskManagement.Api.csproj

# Only after configuring a valid target SQL Server:
dotnet ef database update `
  --project src/SmartTaskManagement.Infrastructure/SmartTaskManagement.Infrastructure.csproj `
  --startup-project src/SmartTaskManagement.Api/SmartTaskManagement.Api.csproj
```

Migrations are stored in `backend/src/SmartTaskManagement.Infrastructure/Persistence/Migrations` and cover authentication, project management, project membership, and the TaskItem table. No database update is performed by the submission verification commands.

## Run the backend

```powershell
cd backend
dotnet restore SmartTaskManagement.slnx
dotnet build SmartTaskManagement.slnx
dotnet test SmartTaskManagement.slnx
dotnet run --project src/SmartTaskManagement.Api/SmartTaskManagement.Api.csproj --launch-profile https
```

The HTTPS development API is `https://localhost:7173`; the HTTP profile is `http://localhost:5010`. Swagger is available at `/swagger` when enabled. Liveness and readiness endpoints are `/health/live` and `/health/ready`.

## Run the frontend

```powershell
cd frontend/smart-task-client
npm install
npm start
```

The Angular development server is normally available at `http://localhost:4200`. The development API base URL is configured in `src/environments/environment.development.ts` as `https://localhost:7173/api`, matching the backend launch profile. The backend CORS configuration allows the local Angular origins.

```powershell
npm test -- --watch=false
npm run build
```

## Production API base URL configuration

No production API host is committed or assumed. Before a production build, edit:

```text
frontend/smart-task-client/src/environments/environment.ts
```

Set `apiBaseUrl` to the deployed API origin plus `/api`, for example `https://<your-api-host>/api`. Do not place credentials or other secrets in this file. Review backend CORS `AllowedOrigins` to include the deployed Angular origin, then build with `npm run build`.

## Ollama and AI configuration

The implemented provider is Ollama. `OllamaAiTaskDescriptionProvider` sends a JSON `POST` request to `Ai:Endpoint`, by default `http://localhost:11434/api/chat`, with `model`, `messages`, `stream: false`, and a low-temperature generation option. It expects Ollama's `message.content` response field.

Install Ollama using the official installer for the development operating system, then pull and run the configured model:

```powershell
ollama pull gemma3
ollama serve
ollama list
```

The required default model is `gemma3`; if another installed model is selected, set `Ai:Model` to that exact model name. Ollama must be running locally before calling `POST /api/ai/improve-task-description`. The AI endpoint is authenticated and uses the global fixed-window rate limiter. Provider failures are mapped to safe API errors, and provider credentials are not logged or returned.

## API response conventions

Successful responses use:

```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "data": {},
  "errors": null,
  "traceId": "..."
}
```

Paged endpoints return this shape inside `data`:

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 0,
  "totalPages": 0
}
```

Task status values are numeric: `0 ToDo`, `1 InProgress`, `2 Completed`, `3 Cancelled`. Task priority values are numeric: `0 Low`, `1 Medium`, `2 High`, `3 Critical`.

## API endpoint overview

The running server origin is `https://localhost:7173` in the HTTPS development profile. Route prefixes are intentionally mixed in the current controllers.

### Authentication (`/api/v1/auth`)

| Method | Route | Access |
|---|---|---|
| POST | `/api/v1/auth/register` | Anonymous; creates a Team Member |
| POST | `/api/v1/auth/login` | Anonymous; returns access token and sets refresh cookie |
| POST | `/api/v1/auth/refresh` | Anonymous with refresh cookie; rotates cookie and returns access token |
| POST | `/api/v1/auth/logout` | Anonymous-safe; revokes the refresh cookie when present |
| GET | `/api/v1/auth/me` | Authenticated |
| GET | `/api/v1/auth/admin-check` | Admin only |

### Projects and membership (`/api/v1`)

| Method | Route | Access |
|---|---|---|
| POST | `/api/v1/projects` | Admin or Project Manager |
| PUT | `/api/v1/projects/{projectId}` | Admin or owning Project Manager |
| DELETE | `/api/v1/projects/{projectId}` | Admin or owning Project Manager |
| GET | `/api/v1/projects/{projectId}` | Scoped by role/membership |
| GET | `/api/v1/projects` | Scoped list; `search`, `sortBy`, `sortDirection`, `page`, `pageSize` |
| GET | `/api/v1/projects/{projectId}/members` | Admin, owning Project Manager, or project member |
| POST | `/api/v1/projects/{projectId}/members` | Admin or owning Project Manager |
| DELETE | `/api/v1/projects/{projectId}/members/{userId}` | Admin or owning Project Manager |

### Tasks (`/api`)

| Method | Route | Access |
|---|---|---|
| POST | `/api/projects/{projectId}/tasks` | Admin or owning Project Manager |
| GET | `/api/projects/{projectId}/tasks` | Scoped by role/membership; supports keyword, filters, sorting, pagination |
| GET | `/api/tasks/{taskId}` | Scoped by role/membership |
| PUT | `/api/tasks/{taskId}` | Admin or owning Project Manager; Team Members receive 403 |
| DELETE | `/api/tasks/{taskId}` | Admin or owning Project Manager |
| PATCH | `/api/tasks/{taskId}/assignment` | Admin or owning Project Manager |
| PATCH | `/api/tasks/{taskId}/status` | Admin, owning Project Manager, or assigned project Team Member |
| PATCH | `/api/tasks/{taskId}/priority` | Admin or owning Project Manager |

Task list query parameters are `keyword`, `status`, `priority`, `assignedUserId`, `dueDateFrom`, `dueDateTo`, `pageNumber`, `pageSize`, `sortColumn`, and `sortDirection`. Safe sort columns are `title`, `status`, `priority`, `dueDate`, and `createdAt`; the default is `createdAt` descending and the maximum page size is 100.

### Dashboard, AI, and operations

| Method | Route | Access |
|---|---|---|
| GET | `/api/dashboard/summary?upcomingDays=7` | Authenticated; role-scoped aggregation |
| POST | `/api/ai/improve-task-description` | Authenticated and rate limited |
| GET | `/health/live` | Anonymous liveness check |
| GET | `/health/ready` | Anonymous readiness check including SQL Server |
| GET | `/` | Anonymous service status |

See `docs/SmartTaskManagement.postman_collection.json` for ready-to-import examples using the exact routes and DTO field names.

## Default roles and permissions

| Capability | Admin | Project Manager | Team Member |
|---|---|---|---|
| Register/login/logout/refresh own account | Yes | Yes | Yes |
| View all projects and tasks | Yes | No | No |
| Create projects | Yes | Yes | No |
| Manage projects | All | Owned projects | No |
| View projects | All | Owned projects | Member projects |
| Manage project members | All | Owned projects | No |
| Create/manage tasks | All | Owned projects | No |
| View project tasks | All | Owned projects | Member projects |
| Full task PUT update | Yes | Owned projects | No; returns 403 |
| Assign/unassign tasks | All | Owned projects | No |
| Update task status | All | Owned projects | Assigned tasks only |
| Update task priority | All | Owned projects | No |
| Delete tasks | All | Owned projects | No |
| Dashboard | All data | Owned project scope | Role-scoped data |
| AI description improvement | Yes | Yes | Yes |

Registration assigns the Team Member role. Admin and Project Manager users are seeded/configured through backend setup rather than self-selected during registration.

## Test and build commands

Backend:

```powershell
cd backend
dotnet restore SmartTaskManagement.slnx
dotnet build SmartTaskManagement.slnx
dotnet test SmartTaskManagement.slnx
```

Frontend:

```powershell
cd frontend/smart-task-client
npm install
npm test -- --watch=false
npm run build
```

Repository hygiene:

```powershell
git diff --check
```

## Known limitations

- Ollama is a local dependency for the AI feature; there is no hosted AI fallback in this implementation.
- The AI response is returned for review and is not persisted automatically to a task.
- Production deployment infrastructure, domain names, TLS certificates, and a production API URL are intentionally not included.
- Production should use an external secret store and an explicit migration/release step rather than development startup seeding/migration behavior.
- The repository contains unit/service tests and Angular component/service tests; a full browser end-to-end suite and hosted SQL Server integration environment are not included.
- Swagger is enabled in development or when `Swagger:Enabled` is true; protect it appropriately in production.
- The EF CLI installed in the verification environment may be one patch version behind the runtime; this does not affect the application build.

## Final submission checklist

- [ ] Configure SQL Server and apply the EF Core migrations in the target environment.
- [ ] Configure a strong JWT signing key, admin seed credentials, and connection string through User Secrets/environment variables or a managed secret store.
- [ ] Verify no passwords, JWT keys, API keys, private keys, tokens, or SQL credentials are committed.
- [ ] Install Ollama, run `ollama pull gemma3`, start `ollama serve`, and verify `Ai:Endpoint`/`Ai:Model` if AI is demonstrated.
- [ ] Set `frontend/smart-task-client/src/environments/environment.ts` to the real production API base URL before deployment.
- [ ] Configure production CORS to allow only the deployed Angular origin.
- [ ] Run backend restore/build/test and frontend install/test/build from a clean checkout.
- [ ] Import and validate the Postman collection and environment example with placeholders replaced locally.
- [ ] Confirm Swagger, `/health/live`, and `/health/ready` in the configured environment.
- [ ] Review `PROMPTS.md` for AI prompt, validation, and safety documentation.
- [ ] Commit the final documentation and API collection changes, then push only after reviewing the commit and remote state.

## Submission instructions

For a fresh checkout:

1. Configure backend User Secrets and SQL Server.
2. Apply migrations or start the development API with the documented development configuration.
3. Start Ollama and pull `gemma3` if AI verification is required.
4. Start the backend with the HTTPS profile.
5. Set the frontend production API URL only for a deployment build, or use the checked-in development API URL for local work.
6. Run the Angular client.
7. Import the Postman collection and environment example, replace placeholder IDs/credentials locally, log in, and exercise the protected endpoints.
8. Run the final test/build/security checks and review the Git diff before submission.
