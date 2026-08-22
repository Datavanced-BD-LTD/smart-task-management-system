# Smart Task Management System - Architecture and Design Baseline

Status: design only. No implementation code is included in this document.

This design is based on the six-page assignment brief and is intended to be the
baseline for implementation, testing, README documentation, and API collection
work.

## 1. Complete requirements analysis

### 1.1 Confirmed functional scope

The system must provide:

- Registration, login, logout, JWT authentication, refresh tokens, and role-based authorization.
- Three roles: Admin, Project Manager, and Team Member.
- Project creation, update, deletion, details, listing, keyword search, sorting, and pagination.
- Multiple tasks per project.
- Task creation, update, deletion, assignment, status, priority, due date, search, sorting, and pagination.
- Task statuses: To Do, In Progress, Completed, and Cancelled.
- Task priorities: Low, Medium, High, and Critical.
- A dashboard with project and task counts, status and priority breakdowns, completed versus pending work, and upcoming due tasks.
- Keyword search, filtering, sorting, and pagination wherever lists are shown.
- An authenticated API feature that improves task descriptions using an AI provider.
- A responsive Angular UI for login, dashboard, project management, task management, and search.

### 1.2 Confirmed quality and platform constraints

The solution must use:

- ASP.NET Core 9 or 10, Entity Framework Core, SQL Server, JWT, and Swagger/OpenAPI.
- Angular 18 or later, TypeScript, and standalone components.
- Clean Architecture or N-Layer Architecture.
- SOLID principles and design patterns where they improve maintainability.
- Async/await, FluentValidation, global exception handling, Serilog logging, health checks, and consistent API responses.
- HTTPS, CORS, secure configuration, input validation, password hashing, and basic rate limiting.
- Angular lazy loading, route guards, HTTP interceptors, reactive forms, and responsive design.

### 1.3 Required submission artifacts

- Complete source code in a GitHub repository.
- SQL database script or EF Core migrations.
- `README.md` containing the overview, setup, technology stack, API overview, and folder structure.
- `PROMPTS.md` containing AI prompt design, prompt structure, examples, validation, and safety considerations.
- An API collection, either Postman or a Swagger export.

### 1.4 Recommended assumptions where the brief is intentionally open

These are product decisions rather than blockers:

1. Use a Clean Architecture modular monolith. Microservices would add operational complexity without helping this assignment.
2. A project has a Project Manager owner and an explicit `ProjectMembers` list. This gives authorization a reliable project boundary.
3. Admin has system-wide read and management access. Project Managers manage projects they own and their project tasks. Team Members see projects they belong to and update tasks assigned to them, primarily their status.
4. Project and task deletion use soft deletion. This protects reporting and auditability while still meeting the delete requirement.
5. Refresh tokens are issued in a secure, HttpOnly cookie, while short-lived access tokens are held in memory by the Angular application.
6. AI improvement is a stateless assistive operation. The improved description is returned to the client and is only persisted when the user saves the task.
7. Search initially uses parameterized SQL Server queries over title, name, and description. SQL Server full-text search can be added later if the dataset requires it.

No blocking clarification is required to begin design or implementation. The team-member permission policy and project-membership behavior should be confirmed with the product owner before the first UI is finalized.

## 2. Recommended architecture

### 2.1 Overall shape

Use a Clean Architecture modular monolith with feature-oriented application code.

```text
Angular SPA
    |
    | HTTPS / JSON / JWT
    v
ASP.NET Core API
    |
    +-- API layer: controllers, filters, middleware, OpenAPI
    |
    +-- Application layer: use cases, DTOs, validators, authorization rules
    |
    +-- Domain layer: entities, enums, business rules, domain exceptions
    |
    +-- Infrastructure layer: EF Core, SQL Server, JWT, AI provider, logging
    |
    v
SQL Server

External services: AI provider, optional distributed cache/telemetry
```

The API is the only server-side entry point. Domain and application code do not
depend on ASP.NET Core, EF Core, SQL Server, or a specific AI provider.

### 2.2 Layer responsibilities

| Layer | Responsibility | Must not contain |
|---|---|---|
| Domain | Entities, value objects, enums, invariants, domain services, domain exceptions | EF Core, HTTP, JWT, provider SDKs |
| Application | Commands, queries, DTOs, validation, authorization policies, interfaces, mapping, transaction boundaries | SQL queries tied to a provider, controller concerns |
| Infrastructure | DbContext, EF configurations, migrations, token service, password hashing, AI adapter, time provider, persistence | UI behavior or endpoint-specific HTTP logic |
| API | Controllers, request binding, middleware, authentication setup, Swagger, rate limiting, response formatting | Business rules and direct EF queries |

### 2.3 Application organization

Organize the Application project by feature rather than by a large global
`Services` folder:

- `Auth`: Register, Login, Refresh, Logout, Current User.
- `Projects`: List, Get Details, Create, Update, Delete, Members.
- `Tasks`: List, Get Details, Create, Update, Delete, Assign, Change Status.
- `Dashboard`: Summary and upcoming due tasks.
- `Ai`: Improve task description.

Use CQRS-lite: separate command and query handlers, but keep one deployable API
and one database. A mediator library is optional; if used, use it for request
dispatch and pipeline behaviors, not as a substitute for clear boundaries.

### 2.4 Recommended patterns

- **CQRS-lite** for separating state changes from read models and list queries.
- **Adapter** for the AI provider, exposed to Application as `IAiDescriptionImprover`.
- **Policy-based authorization** for resource-scoped checks such as project ownership and task assignment.
- **Query object or specification** for validated filters, sort fields, and pagination.
- **Unit of Work** through the application transaction boundary and EF Core `DbContext`.
- **Options pattern** for JWT, CORS, rate limiting, and AI configuration.
- **Pipeline behaviors or decorators** for validation, logging, and transaction handling if a mediator is adopted.

Avoid a generic repository for every table. EF Core already provides a unit of
work and change tracking; repositories should only be added for meaningful
aggregate boundaries or provider-specific queries.

### 2.5 Security design

- Access JWT lifetime: short, such as 10-15 minutes.
- Refresh token lifetime: longer, such as 7-30 days, with rotation on every refresh.
- Store only a hash of the refresh token in SQL Server. Revoke the old token when it is rotated and revoke the token family if reuse is detected.
- Hash passwords with the ASP.NET Core password hasher or another well-maintained password hashing implementation. Never store plaintext or reversible passwords.
- Use policies for role checks and resource checks; do not rely only on hidden Angular buttons.
- Serve production traffic over HTTPS. Enable HSTS in production.
- Restrict CORS to configured Angular origins and allowed headers/methods.
- Apply ASP.NET Core rate limiting to login, refresh, registration, and AI endpoints, with stricter limits for authentication and AI calls.
- Validate DTOs with FluentValidation and enforce maximum lengths, allowed enum values, page-size limits, and safe sort-field allowlists.
- Keep secrets in user secrets, environment variables, or a managed secret store. Do not commit JWT keys, database passwords, or AI keys.
- Do not log access tokens, refresh tokens, passwords, raw AI credentials, or sensitive prompt content.
- Use secure, HttpOnly, SameSite refresh cookies with `Secure` enabled in production. Configure Angular requests with credentials as needed and protect cookie-based refresh flows with a SameSite/origin strategy or CSRF token.

### 2.6 Cross-cutting API behavior

- Use `ProblemDetails` for errors with a correlation/trace ID.
- Use a consistent success envelope, for example `data` plus optional `meta`.
- Use a paged response containing `items`, `page`, `pageSize`, `totalCount`, and `totalPages`.
- Return `201 Created` for creates, `200 OK` for reads and updates, `204 No Content` for successful deletes when no body is needed, `400` for validation, `401` for unauthenticated requests, `403` for forbidden requests, `404` for missing resources, and `409` for concurrency/conflict failures.
- Add global exception middleware that maps known domain/application errors and hides internal details.
- Use Serilog structured logs with request IDs, user IDs where safe, endpoint names, elapsed time, and exception details.
- Add liveness and readiness health checks. Readiness should verify the database and any required external dependency configuration.

## 3. Database design

### 3.1 Relational model

Use SQL Server with GUID primary keys, UTC timestamps, readable string values for
statuses and priorities, and `rowversion` columns for optimistic concurrency.

| Table | Key columns | Purpose |
|---|---|---|
| `Users` | `UserId`, `Email` | Application users and password hashes |
| `Roles` | `RoleId`, `Name` | Seeded roles: Admin, ProjectManager, TeamMember |
| `UserRoles` | `UserId`, `RoleId` | Many-to-many role assignment |
| `RefreshTokens` | `RefreshTokenId`, `UserId` | Hashed, rotated, revocable refresh-token records |
| `Projects` | `ProjectId`, `ProjectManagerId` | Project information and ownership |
| `ProjectMembers` | `ProjectId`, `UserId` | Explicit project access boundary |
| `Tasks` | `TaskId`, `ProjectId` | Work items belonging to a project |
| `AuditLogs` | `AuditLogId` | Optional but recommended record of security and important mutations |

### 3.2 Core table fields

#### `Users`

- `UserId` uniqueidentifier, primary key.
- `Email` nvarchar(256), required, normalized, unique index.
- `PasswordHash` nvarchar(500), required.
- `FirstName` nvarchar(100), required.
- `LastName` nvarchar(100), required.
- `IsActive` bit, required, default true.
- `LastLoginAtUtc` datetime2, nullable.
- `CreatedAtUtc`, `UpdatedAtUtc` datetime2, required.
- `RowVersion` rowversion.

#### `Roles` and `UserRoles`

- `Roles.RoleId` int primary key and `Roles.Name` nvarchar(50), unique.
- Seed exactly `Admin`, `ProjectManager`, and `TeamMember` roles.
- `UserRoles` uses a composite primary key `(UserId, RoleId)` and foreign keys to both tables.

#### `RefreshTokens`

- `RefreshTokenId` uniqueidentifier, primary key.
- `UserId` uniqueidentifier, required foreign key to `Users`.
- `TokenHash` varbinary or a fixed-length encoded string, required, unique index.
- `FamilyId` uniqueidentifier, required, for reuse detection and family revocation.
- `ExpiresAtUtc`, `CreatedAtUtc` datetime2, required.
- `RevokedAtUtc` datetime2, nullable.
- `ReplacedByTokenId` uniqueidentifier, nullable.
- `CreatedByIp`, `RevokedByIp` nvarchar(64), nullable.
- `RevocationReason` nvarchar(200), nullable.

#### `Projects`

- `ProjectId` uniqueidentifier, primary key.
- `Name` nvarchar(200), required.
- `Description` nvarchar(2000), nullable.
- `ProjectManagerId` uniqueidentifier, required foreign key to `Users`.
- `CreatedByUserId` uniqueidentifier, required foreign key to `Users`.
- `CreatedAtUtc`, `UpdatedAtUtc` datetime2, required.
- `IsDeleted` bit, default false; `DeletedAtUtc`, `DeletedByUserId` nullable.
- `RowVersion` rowversion.

#### `ProjectMembers`

- `ProjectId` and `UserId` uniqueidentifiers, composite primary key.
- `AddedByUserId` uniqueidentifier, required.
- `AddedAtUtc` datetime2, required.
- Foreign keys to `Projects` and `Users`.
- Unique composite key prevents duplicate membership.

#### `Tasks`

Use the table name `Tasks` in SQL Server and the domain name `TaskItem` in .NET
to avoid confusion with `System.Threading.Tasks.Task`.

- `TaskId` uniqueidentifier, primary key.
- `ProjectId` uniqueidentifier, required foreign key to `Projects`.
- `Title` nvarchar(200), required.
- `Description` nvarchar(max) or a bounded nvarchar length, required or nullable according to the UI policy.
- `AssignedToUserId` uniqueidentifier, nullable foreign key to `Users`.
- `Status` nvarchar(20), required, default `To Do`.
- `Priority` nvarchar(20), required, default `Medium`.
- `DueDateUtc` date or datetime2, nullable. Use `date` if time-of-day is not a requirement.
- `CreatedByUserId` uniqueidentifier, required.
- `CreatedAtUtc`, `UpdatedAtUtc` datetime2, required.
- `CompletedAtUtc` datetime2, nullable and set when status becomes Completed.
- `IsDeleted` bit, default false; `DeletedAtUtc`, `DeletedByUserId` nullable.
- `RowVersion` rowversion.

#### `AuditLogs` - recommended

- `AuditLogId` bigint or uniqueidentifier, primary key.
- `UserId` nullable foreign key for anonymous security events.
- `Action`, `EntityType`, `EntityId` as indexed strings.
- `OccurredAtUtc` datetime2, required.
- `IpAddress`, `CorrelationId`, and a redacted JSON detail payload.

Do not store full passwords, bearer tokens, refresh tokens, or unredacted AI
provider credentials in audit records.

### 3.3 Relationships and delete behavior

```text
User 1 --- * RefreshToken
User * --- * Role through UserRole
User 1 --- * Project as ProjectManager
Project * --- * User through ProjectMember
Project 1 --- * TaskItem
User 1 --- * TaskItem as AssignedTo / CreatedBy
```

- Use soft deletion for projects and tasks. All normal queries filter `IsDeleted = false`.
- Restrict or set-null user foreign keys where hard deletion could break history.
- Do not cascade delete a project into task history. If a project is soft-deleted, its tasks become inaccessible through normal project queries.
- Enforce that an assigned user is active and is a member of the project, unless the user is an Admin or a deliberate policy allows otherwise.
- Enforce that a Project Manager owns or has authority over the project before changing its tasks.

### 3.4 Indexes and query considerations

- Unique index on normalized `Users.Email`.
- Index `Projects(ProjectManagerId, IsDeleted, UpdatedAtUtc)`.
- Index `ProjectMembers(UserId, ProjectId)` and `ProjectMembers(ProjectId, UserId)`.
- Index `Tasks(ProjectId, IsDeleted, Status, Priority)`.
- Index `Tasks(ProjectId, IsDeleted, DueDateUtc)` for upcoming due work.
- Index `Tasks(AssignedToUserId, IsDeleted, Status)` for Team Member views.
- Unique index on `RefreshTokens.TokenHash`; index `RefreshTokens.UserId, ExpiresAtUtc`.
- Validate and allowlist sort columns in application code. Never concatenate an unchecked sort field into SQL.
- Begin with parameterized `Contains`/`LIKE` search. Consider SQL Server full-text indexes when task volume or search quality justifies it.

### 3.5 Migration and seed strategy

- Store EF Core migrations in `SmartTaskManagement.Infrastructure/Persistence/Migrations`.
- Seed the three roles through an idempotent startup or migration-safe seeder.
- Create a development-only administrator through configuration or a documented seed command; never ship a shared default password.
- Keep production migration execution explicit and observable rather than silently applying destructive changes at every startup.

## 4. Backend project structure

```text
backend/
  SmartTaskManagement.sln
  src/
    SmartTaskManagement.Api/
      Controllers/
        AuthController.cs
        ProjectsController.cs
        TasksController.cs
        DashboardController.cs
        AiController.cs
        UsersController.cs
      Middleware/
        ExceptionHandlingMiddleware.cs
        CorrelationIdMiddleware.cs
      Authorization/
        Policies.cs
        ResourceAuthorizationHandlers.cs
      Contracts/
        Common/
        Auth/
        Projects/
        Tasks/
        Dashboard/
        Ai/
      OpenApi/
      Extensions/
      Program.cs
      appsettings.json
      appsettings.Development.json

    SmartTaskManagement.Application/
      Abstractions/
        Authentication/
        Persistence/
        Ai/
        Services/
      Common/
        Errors/
        Behaviors/
        Models/
        Mapping/
      Features/
        Auth/
          Commands/Register/
          Commands/Login/
          Commands/RefreshToken/
          Commands/Logout/
          Queries/CurrentUser/
        Projects/
          Commands/CreateProject/
          Commands/UpdateProject/
          Commands/DeleteProject/
          Queries/GetProject/
          Queries/ListProjects/
          Commands/ManageMembers/
        Tasks/
          Commands/CreateTask/
          Commands/UpdateTask/
          Commands/DeleteTask/
          Commands/AssignTask/
          Commands/ChangeTaskStatus/
          Queries/GetTask/
          Queries/ListTasks/
        Dashboard/
        Ai/
      DependencyInjection.cs

    SmartTaskManagement.Domain/
      Common/
        Entity.cs
        AuditableEntity.cs
      Entities/
        User.cs
        Role.cs
        RefreshToken.cs
        Project.cs
        ProjectMember.cs
        TaskItem.cs
        AuditLog.cs
      Enums/
        TaskStatus.cs
        TaskPriority.cs
      Exceptions/
      DomainRules/

    SmartTaskManagement.Infrastructure/
      Persistence/
        ApplicationDbContext.cs
        Configurations/
        Migrations/
        Seed/
      Authentication/
        JwtTokenService.cs
        RefreshTokenService.cs
        PasswordHasher.cs
      Ai/
        GitHubModelsDescriptionImprover.cs
        AiOptions.cs
      Services/
        SystemClock.cs
        CurrentUserService.cs
      DependencyInjection.cs

  tests/
    SmartTaskManagement.Domain.UnitTests/
    SmartTaskManagement.Application.UnitTests/
    SmartTaskManagement.Api.IntegrationTests/
    SmartTaskManagement.ArchitectureTests/
```

Testing priorities:

- Domain tests for status transitions and invariants.
- Application tests for validation, scoping, and role/resource authorization.
- Integration tests for authentication, refresh-token rotation, EF queries, concurrency, and endpoint response contracts.
- Architecture tests to prevent Domain from referencing Infrastructure or API.
- Angular unit and component tests for guards, interceptors, forms, and feature behavior.

## 5. Angular project structure

Use standalone components, lazy feature routes, signals for local/view state,
and `OnPush` change detection. Avoid global state management until the feature
set justifies it; a small set of feature data-access services is sufficient for
this assignment.

```text
frontend/
  angular.json
  package.json
  src/
    app/
      app.component.ts
      app.config.ts
      app.routes.ts

      core/
        auth/
          auth.service.ts
          auth.store.ts
          auth.guard.ts
          role.guard.ts
          token-store.service.ts
        http/
          auth.interceptor.ts
          error.interceptor.ts
          loading.interceptor.ts
        layout/
          app-shell/
          top-nav/
          side-nav/
        services/
          notification.service.ts
          api-error.service.ts

      shared/
        components/
          data-table/
          pagination/
          loading-state/
          empty-state/
          confirm-dialog/
        forms/
        models/
          api-response.models.ts
          pagination.models.ts
        validators/
        pipes/

      features/
        auth/
          pages/login-page/
          pages/register-page/
          auth.routes.ts
        dashboard/
          pages/dashboard-page/
          components/stat-card/
          components/task-breakdown-chart/
          data-access/dashboard-api.service.ts
          models/dashboard.models.ts
          dashboard.routes.ts
        projects/
          pages/project-list-page/
          pages/project-details-page/
          pages/project-form-page/
          components/project-filters/
          components/project-members/
          data-access/projects-api.service.ts
          models/project.models.ts
          projects.routes.ts
        tasks/
          pages/task-list-page/
          pages/task-details-page/
          pages/task-form-page/
          components/task-filters/
          components/task-status-control/
          components/task-assignee-control/
          components/description-improver/
          data-access/tasks-api.service.ts
          data-access/ai-api.service.ts
          models/task.models.ts
          tasks.routes.ts

    assets/
    environments/
```

Angular behavior recommendations:

- Store the access token in memory; use an HttpOnly refresh cookie rather than localStorage for the refresh token.
- The auth interceptor attaches the access token and handles one refresh-and-retry cycle after a 401. Prevent concurrent refresh calls with a shared refresh request.
- Route guards protect authenticated areas and role-specific routes. Guards improve UX; the API remains authoritative.
- Reactive forms handle registration, login, projects, tasks, and validation messages.
- Feature API services own HTTP calls and map server DTOs to UI models.
- List pages keep search, filters, sort, and pagination in query parameters so views are bookmarkable and browser navigation works.
- Use accessible labels, keyboard navigation, visible focus states, responsive tables/cards, and clear loading/error/empty states.
- Lazy-load `auth`, `dashboard`, `projects`, and `tasks` routes independently.

## 6. API endpoint list

Base URL: `/api/v1`

All authenticated endpoints require a valid access JWT unless noted otherwise.
All list endpoints accept validated `page`, `pageSize`, `sortBy`, and
`sortDirection` parameters. The server enforces a maximum page size.

### 6.1 Authentication

| Method | Endpoint | Purpose | Access |
|---|---|---|---|
| POST | `/auth/register` | Create a Team Member or allowed self-service account | Anonymous, rate limited |
| POST | `/auth/login` | Authenticate and issue access token plus refresh cookie | Anonymous, rate limited |
| POST | `/auth/refresh` | Rotate refresh token and issue a new access token | Refresh cookie, rate limited |
| POST | `/auth/logout` | Revoke current refresh token and clear cookie | Authenticated |
| GET | `/auth/me` | Return current user and roles | Authenticated |

### 6.2 Users and project members

| Method | Endpoint | Purpose | Access |
|---|---|---|---|
| GET | `/users/assignable?projectId={id}` | Return active users who can be assigned to a project task | Admin or project manager for that project |
| GET | `/projects/{projectId}/members` | List project members | Admin or project participants |
| POST | `/projects/{projectId}/members` | Add a Team Member to a project | Admin or project manager owner |
| DELETE | `/projects/{projectId}/members/{userId}` | Remove a Team Member from a project | Admin or project manager owner |

These membership endpoints are recommended because task assignment needs a
well-defined authorization boundary. If membership is intentionally omitted,
replace it with an explicit rule that Team Members can only see tasks assigned
to themselves.

### 6.3 Projects

| Method | Endpoint | Purpose | Access |
|---|---|---|---|
| GET | `/projects` | List visible projects with `search`, filters, sorting, and pagination | Authenticated, scoped by role |
| POST | `/projects` | Create a project | Admin or Project Manager |
| GET | `/projects/{projectId}` | Get project details and summary fields | Authenticated, scoped by role/membership |
| PUT | `/projects/{projectId}` | Update project name/description/manager | Admin or owning Project Manager |
| DELETE | `/projects/{projectId}` | Soft-delete a project | Admin or owning Project Manager |

### 6.4 Tasks

| Method | Endpoint | Purpose | Access |
|---|---|---|---|
| GET | `/projects/{projectId}/tasks` | List project tasks with keyword, status, priority, assignee, due-date filters, sorting, and pagination | Authenticated, scoped by role/membership |
| POST | `/projects/{projectId}/tasks` | Create a task in a project | Admin or owning Project Manager |
| GET | `/tasks/{taskId}` | Get task details | Authenticated, scoped by role/membership/assignment |
| PUT | `/tasks/{taskId}` | Update editable task fields | Admin or owning Project Manager; assigned Team Member only for allowed fields |
| PATCH | `/tasks/{taskId}/assignment` | Assign or unassign a task | Admin or owning Project Manager |
| PATCH | `/tasks/{taskId}/status` | Change status with transition validation | Admin, Project Manager, or assigned Team Member according to policy |
| DELETE | `/tasks/{taskId}` | Soft-delete a task | Admin or owning Project Manager |

Suggested list query fields:

- Projects: `search`, `page`, `pageSize`, `sortBy=name|createdAt|updatedAt`, `sortDirection`.
- Tasks: `search`, `status`, `priority`, `assignedToUserId`, `dueFrom`, `dueTo`, `page`, `pageSize`, `sortBy=title|status|priority|dueDate|createdAt`, `sortDirection`.

### 6.5 Dashboard

| Method | Endpoint | Purpose | Access |
|---|---|---|---|
| GET | `/dashboard/summary` | Total projects, total tasks, status counts, priority counts, completed versus pending, and upcoming due tasks | Authenticated, role-scoped |
| GET | `/dashboard/upcoming-due` | Paged list of upcoming or overdue tasks | Authenticated, role-scoped |

Optional query fields are `projectId`, `from`, `to`, and `daysAhead`. The API
must calculate counts from the caller's visible data, not expose global counts
to a Team Member.

### 6.6 AI

| Method | Endpoint | Purpose | Access |
|---|---|---|---|
| POST | `/ai/task-description/improve` | Return a clearer, professional, expanded, actionable task description | Authenticated, rate limited |

Recommended request fields: title, current description, project context, and
desired constraints. Recommended response fields: improved description,
assumptions, and provider-independent metadata such as model availability. Do
not allow the AI endpoint to mutate a task directly; the user reviews and saves
the result through the normal task update flow.

### 6.7 Operations

| Method | Endpoint | Purpose | Access |
|---|---|---|---|
| GET | `/health/live` | Process liveness | Anonymous |
| GET | `/health/ready` | Dependency readiness, including SQL Server | Anonymous or restricted at the edge |
| GET | `/swagger` | Interactive API documentation | Development or explicitly protected environments |

### 6.8 API response contract

Successful single-resource responses should use a consistent `data` envelope.
List responses should use `data.items` plus `data.meta` for pagination. Errors
should use `ProblemDetails` with `status`, `title`, `detail` when safe,
`instance`, `traceId`, and field-level validation errors where applicable.

Every mutating endpoint should support optimistic concurrency using the resource
version, such as an `If-Match` header or a version value in the request. A stale
update returns `409 Conflict`.

## 7. Role and permission matrix

Legend: `Full` means create/read/update/delete within the stated scope, `Read`
means read-only, `Own` means resources owned by the Project Manager, `Member`
means projects the Team Member belongs to, and `Assigned` means tasks assigned
to that Team Member.

| Capability | Admin | Project Manager | Team Member |
|---|---|---|---|
| Register/login/logout/refresh own account | Full | Full | Full |
| Read own profile | Full | Full | Full |
| View all projects | Full | Own plus member projects | Member projects |
| Create project | Full | Full | No |
| Update project | Full | Own | No |
| Delete project | Full | Own | No |
| Search/sort/page visible projects | Full | Own/member scope | Member scope |
| Manage project members | Full | Own | No |
| View all tasks | Full | Own project tasks | Assigned tasks, or member-project tasks if policy allows |
| Create task | Full | Own project | No in baseline policy |
| Update task title/description/priority/due date | Full | Own project | No in baseline policy |
| Change task status | Full | Own project | Assigned tasks |
| Assign/unassign task | Full | Own project | No |
| Delete task | Full | Own project | No |
| Search/filter/sort/page visible tasks | Full | Own project scope | Assigned/member scope |
| View system-wide dashboard | Full | No | No |
| View scoped dashboard | Full | Own project scope | Assigned/member scope |
| Use AI description improvement | Full | Full | Full |

The baseline deliberately makes Project Managers responsible for backlog and
assignment control, while Team Members can update execution status. If the
business wants collaborative task creation, allow Team Members to create tasks
in member projects, but retain the same project and membership checks.

Authorization must be enforced in the API with role and resource policies. The
Angular role matrix only controls navigation and affordances; it is not a
security boundary.

## 8. Development milestones

Each milestone should end with a demonstrable increment and a small testable
definition of done.

| Milestone | Scope | Exit criteria |
|---|---|---|
| 1. Architecture and contracts | Confirm entities, permissions, API envelope, query parameters, error format, and UI routes | This document is accepted; solution boundaries and API contracts are stable enough to scaffold |
| 2. Platform foundation | Create .NET solution, Angular workspace, SQL Server connection, EF Core context, migrations, configuration, logging, exception handling, Swagger, health checks, CORS, HTTPS, and rate limiting | Empty API and Angular shell run locally; health, Swagger, and a migration work |
| 3. Authentication and authorization | Registration, login, password hashing, JWT access token, refresh rotation/revocation, logout, role seeding, current-user endpoint, Angular auth pages, token interceptor, refresh flow, and guards | All three roles can authenticate; protected endpoints reject missing/invalid tokens; refresh reuse is handled safely |
| 4. Project module | Project entity, CRUD, soft deletion, ownership, membership, scoped project list, search, sort, and pagination | Admin and Project Manager flows work; Team Member cannot mutate projects outside policy; integration tests cover scoping |
| 5. Task module | Task entity, CRUD, assignment, status, priority, due date, validation, concurrency, scoped queries, filters, search, sort, and pagination | End-to-end project/task workflow works with correct role checks and status values |
| 6. Dashboard and read models | Summary counts, breakdowns, completed versus pending, upcoming due tasks, and Angular dashboard cards/charts | Counts are correct for Admin, Project Manager, and Team Member scopes; larger lists remain paged |
| 7. Angular feature UI | Lazy-loaded routes, responsive shell, project pages, task pages, reactive forms, table/filter/pagination components, loading/error/empty states, and accessibility pass | Core workflows are usable on desktop and mobile widths; client-side navigation and guards work |
| 8. AI feature | Provider adapter, prompt template, timeout/retry policy, rate limit, input/output validation, safe fallback, review-before-save UI, and `PROMPTS.md` | The endpoint returns useful actionable text, handles provider failure without leaking secrets, and is documented |
| 9. Hardening and submission | Unit/integration/architecture tests, security review, logging review, performance checks, migrations/script, README, Swagger/Postman export, seed instructions, and GitHub cleanup | A fresh checkout can be configured and run from the README; deliverables are complete and no secrets or debug artifacts are committed |

### 8.1 Definition of done for the assignment

- All required workflows are covered by API and Angular tests at an appropriate level.
- The API enforces every role and resource boundary independently of the UI.
- List endpoints are bounded, indexed, and safe against unchecked sort/filter input.
- Refresh tokens rotate and revoke correctly; passwords and secrets are never logged.
- The AI feature is isolated behind an interface and can be disabled or replaced by configuration.
- EF Core migrations or a database script can create the database from a clean environment.
- `README.md`, `PROMPTS.md`, and the API collection describe the actual delivered behavior.
- A reviewer can understand the structure, run the application, authenticate, and exercise the main flows without reading implementation internals.
