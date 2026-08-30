# Smart Task Management System – Review Plan

This plan is for a 10-minute recruiter or technical-review demonstration. Keep the browser, Swagger, terminal windows, and test output ready before the meeting.

## 1. Pre-review checklist

- Confirm the repository is on the expected branch and the working tree is clean.
- Start SQL Server LocalDB:

  ```powershell
  SqlLocalDB start MSSQLLocalDB
  SqlLocalDB info MSSQLLocalDB
  ```

- Confirm Ollama is running and the required model is available:

  ```powershell
  Invoke-RestMethod http://localhost:11434/api/tags
  ollama list
  ```

  If necessary, run `ollama serve` and `ollama pull gemma3`.

- Confirm local secrets exist privately in `local-secrets/`; never show the password, JWT signing key, tokens, or connection string during the review.
- Start the API with the HTTPS profile:

  ```powershell
  dotnet run --project .\backend\src\SmartTaskManagement.Api\SmartTaskManagement.Api.csproj --launch-profile https
  ```

- Start Angular:

  ```powershell
  cd .\frontend\smart-task-client
  npm start
  ```

## 2. URLs and route conventions

| Resource | URL |
|---|---|
| Angular | `http://localhost:4200` |
| API | `https://localhost:7173` |
| Swagger | `https://localhost:7173/swagger/index.html` |
| Live health | `https://localhost:7173/health/live` |
| Ready health | `https://localhost:7173/health/ready` |

The backend uses the routes below. This distinction is intentional:

- Authentication and projects: `/api/v1/...`
- Tasks: `/api/...`
- Dashboard: `/api/dashboard/...`
- AI: `/api/ai/...`

## 3. Ten-minute demonstration sequence

### 0:00–1:00 — Architecture and repository

Show the repository tree and explain the dependency direction:

```text
backend/
  src/
    SmartTaskManagement.Api             HTTP, middleware, Swagger
    SmartTaskManagement.Application     use cases, DTOs, validation
    SmartTaskManagement.Domain          entities, enums, domain rules
    SmartTaskManagement.Infrastructure  EF Core, SQL Server, providers
  tests/SmartTaskManagement.Tests       backend tests
frontend/smart-task-client              Angular standalone application
docs/                                   assignment and submission artifacts
```

Explain that controllers do not expose EF entities. Application services contain authorization and business rules, while Infrastructure contains persistence and external-provider adapters.

### 1:00–1:30 — Operations and API readiness

Open Swagger and the two health endpoints. Point out:

- OpenAPI documentation and bearer authorization.
- Global exception handling and consistent response envelopes.
- SQL Server readiness check.
- Serilog request logging and fixed-window rate limiting.

### 1:30–2:15 — Admin authentication

Open `http://localhost:4200/auth/login` and sign in with the locally configured Admin credentials. Do not display the password.

Optionally show these Swagger endpoints:

```http
POST /api/v1/auth/login
GET  /api/v1/auth/me
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
```

Mention that access tokens are short-lived, refresh tokens are rotated and stored as hashes, and role claims are used for coarse authorization.

### 2:15–3:15 — Project management

In Angular:

1. Open Projects and create a project named `Review Demo Project`.
2. Show the project list, search box, sorting, and paginator.
3. Open project details.
4. Point out that project manager identity is shown as a name/email, not a raw GUID.

Relevant endpoints:

```http
POST   /api/v1/projects
GET    /api/v1/projects
GET    /api/v1/projects/{projectId}
PUT    /api/v1/projects/{projectId}
DELETE /api/v1/projects/{projectId}
```

### 3:15–4:00 — Friendly member management

Register a temporary Team Member in a private browser window, then return to the Admin session.

On the project details page:

1. Open Add member.
2. Search by name or email.
3. Select the friendly result showing full name, email, and role.
4. Add the member and show the member list.

The UI uses:

```http
GET  /api/v1/projects/{projectId}/available-members?keyword={keyword}&pageNumber=1&pageSize=20
POST /api/v1/projects/{projectId}/members
GET  /api/v1/projects/{projectId}/members
DELETE /api/v1/projects/{projectId}/members/{userId}
```

Emphasize that the user ID is retained only for API submission and is not used as the visible label.

### 4:00–5:30 — Task creation and management

1. Open the project task page.
2. Create a task with a title, description, future due date, `High` priority, and the Team Member as assignee.
3. Show the task table with friendly assignee and creator names.
4. Change priority to `Critical`.
5. Change status to `In Progress`.
6. Demonstrate keyword, status, priority, assignee, due-date, sorting, and pagination controls.

Relevant endpoints:

```http
POST   /api/projects/{projectId}/tasks
GET    /api/projects/{projectId}/tasks
GET    /api/tasks/{taskId}
PUT    /api/tasks/{taskId}
DELETE /api/tasks/{taskId}
PATCH  /api/tasks/{taskId}/assignment
PATCH  /api/tasks/{taskId}/status
PATCH  /api/tasks/{taskId}/priority
```

Example task-list query:

```http
GET /api/projects/{projectId}/tasks?keyword=review&status=1&priority=3&pageNumber=1&pageSize=10&sortColumn=dueDate&sortDirection=asc
```

### 5:30–6:45 — Authorization demonstration

Log out and sign in as the temporary Team Member.

Show in Angular that the member can:

- View the project where they are a member.
- View the assigned task.
- Update the assigned task status.

Use Swagger with the Team Member token, kept private, to demonstrate that these operations return `403 Forbidden`:

```http
PUT    /api/tasks/{taskId}
PATCH  /api/tasks/{taskId}/assignment
PATCH  /api/tasks/{taskId}/priority
DELETE /api/tasks/{taskId}
```

Explain that Angular hides unavailable actions for usability, but the Application service enforces the rules independently of the frontend.

### 6:45–7:30 — Dashboard

Open `/dashboard` and show:

- Total projects and tasks.
- Completed and pending tasks.
- Tasks grouped by status and priority.
- Upcoming incomplete tasks due within seven days.

Endpoint:

```http
GET /api/dashboard/summary?upcomingDays=7
```

Explain that Admin data is global, Project Manager data is scoped to owned/managed projects, and Team Member task statistics are scoped to assigned tasks.

### 7:30–8:30 — AI description improvement

With an authenticated user, call the Swagger endpoint:

```http
POST /api/ai/improve-task-description
```

Request:

```json
{
  "description": "make login page"
}
```

Show that the response contains only the improved description. Explain that the provider is abstracted behind `IAiTaskDescriptionService`, uses `IHttpClientFactory`, supports cancellation and timeouts, and is configured for local Ollama with the `gemma3` model.

If Ollama is unavailable, skip this step and state that it is a local optional dependency.

### 8:30–10:00 — Verification and questions

Run or show the latest results:

```powershell
dotnet restore
dotnet build
dotnet test

cd .\frontend\smart-task-client
npm install
npm test -- --watch=false
npm run build
```

Close by showing the README, architecture document, migrations, Postman collection, and this review plan.

## 4. Role summary to explain

| Role | Main capabilities |
|---|---|
| Admin | Full project, membership, task, dashboard, and administrative access |
| Project Manager | Manage owned or managed projects and their tasks |
| Team Member | View member projects; update status only for assigned tasks |

Always state that backend authorization is the final authority. UI action visibility is only a usability feature.

## 5. Likely reviewer questions

### Why Clean Architecture?

It keeps business rules independent from HTTP, EF Core, SQL Server, Angular, and Ollama. The application layer can be tested without requiring the production adapters.

### How are users prevented from seeing other projects?

Project and task queries apply role-aware scope in the application/store layer. The frontend does not make security decisions.

### How are refresh tokens secured?

Only a hash is persisted. Tokens are rotated, associated with a token family, and revoked on logout or replay detection.

### What happens when Ollama is unavailable?

The provider timeout or availability failure is converted into a safe API error. Provider credentials or internal details are not returned or logged.

### Where are secrets configured?

Use User Secrets or environment variables. Never commit `appsettings.Development.json`, `local-secrets/`, passwords, signing keys, tokens, or API keys.

## 6. Backup plan for live-demo failures

- If Angular fails: continue using Swagger and show the Angular source structure.
- If Ollama fails: explain the configuration and continue with the remaining features.
- If a port is occupied, inspect it before starting another process:

  ```powershell
  Get-NetTCPConnection -LocalPort 7173 -State Listen
  Get-NetTCPConnection -LocalPort 4200 -State Listen
  ```

- If the HTTPS certificate is untrusted:

  ```powershell
  dotnet dev-certs https --trust
  ```

- Do not delete the database or run destructive commands during the review. Capture the error and continue with prepared screenshots or Swagger examples.

## 7. Final submission checklist

- [ ] Backend and frontend build successfully.
- [ ] Backend and frontend tests have current results recorded.
- [ ] SQL Server LocalDB setup is documented.
- [ ] Ollama and `gemma3` setup is documented.
- [ ] Swagger and Postman artifacts use actual routes.
- [ ] README and architecture documentation are available.
- [ ] No secrets or local credential files are tracked.
- [ ] Repository is clean and the expected commit is pushed.
