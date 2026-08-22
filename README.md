# Smart Task Management System

## AI task description improvement

The backend exposes an authenticated endpoint that improves a task description without changing the task itself:

```http
POST /api/ai/improve-task-description
Content-Type: application/json
Authorization: Bearer <access-token>

{
  "description": "make login page"
}
```

The current provider is local Ollama. It is selected because GitHub Models is no longer an available inference service. Ollama runs locally and does not require an API key for local requests.

Install Ollama, pull a local model, and start the Ollama service. The default configuration uses `gemma3` at `http://localhost:11434/api/chat`.

The API reads configuration from normal ASP.NET Core configuration providers. User Secrets and environment variables take precedence over the checked-in configuration files.

Initialize User Secrets from the repository root:

```powershell
dotnet user-secrets init --project backend/src/SmartTaskManagement.Api
dotnet user-secrets set --project backend/src/SmartTaskManagement.Api "Ai:Provider" "Ollama"
dotnet user-secrets set --project backend/src/SmartTaskManagement.Api "Ai:Endpoint" "http://localhost:11434/api/chat"
dotnet user-secrets set --project backend/src/SmartTaskManagement.Api "Ai:Model" "gemma3"
dotnet user-secrets set --project backend/src/SmartTaskManagement.Api "Ai:TimeoutSeconds" "30"
```

For a provider endpoint that requires a key, store it only in User Secrets or the environment. Do not replace the placeholder with a real value in `appsettings.json` or `appsettings.Example.json`:

```powershell
dotnet user-secrets set --project backend/src/SmartTaskManagement.Api "Ai:ApiKey" "<provider-api-key>"
$env:Ai__ApiKey = "<provider-api-key>"
```

The endpoint is covered by the existing global fixed-window rate limiter. Provider timeouts, unavailable providers, and malformed responses are returned through the standard API response envelope without exposing provider credentials.
