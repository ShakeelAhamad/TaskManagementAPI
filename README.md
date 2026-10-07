# Task Management API

A REST API built with ASP.NET Core 10, Entity Framework Core, and SQL Server. It provides user registration and login, JWT access tokens, rotating refresh tokens, role-based access control, and task management.

## Features

- Register and authenticate users.
- Store passwords as BCrypt hashes.
- Issue signed JWT access tokens and persist refresh tokens in SQL Server.
- Create, list, update, and complete tasks.
- Restrict regular users to their own tasks; let Managers and Admins view all tasks.
- Provide Admin-only user, role, task-deletion, and statistics operations.
- Maintain the database schema with Entity Framework Core migrations.

## Technology

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10
- Microsoft SQL Server
- JWT Bearer authentication
- BCrypt.Net-Next
- OpenAPI document in the Development environment

## Getting started

### Download the project

Choose either Git clone or a ZIP download. Replace the example repository URL and folder names with the actual URL/name of your GitHub repository.

#### Option 1: Clone with Git

1. Install Git if it is not already installed.
2. Open PowerShell or a terminal in the folder where you want the project.
3. Clone the repository:

   ```powershell
   git clone 
   ```https://github.com/ShakeelAhamad/TaskManagementAPI.git

4. Change to the downloaded project folder. If the API project is in a nested folder, change into the folder containing `TaskManagementAPI.csproj`:

   ```powershell
   cd <YOUR_REPOSITORY>
   cd TaskManagementAPI
   ```

   The second `cd` is only needed if `TaskManagementAPI.csproj` is inside a `TaskManagementAPI` subfolder. Confirm the current folder contains the project file:

   ```powershell
   Get-ChildItem TaskManagementAPI.csproj
   ```

#### Option 2: Download as a ZIP

1. Open the repository page on GitHub.
2. Select **Code** > **Download ZIP**.
3. Extract the ZIP file using File Explorer.
4. Open PowerShell in the extracted folder, then change to the directory containing `TaskManagementAPI.csproj`. For example:

   ```powershell
   cd "$HOME\Downloads\<YOUR_REPOSITORY>\TaskManagementAPI"
   Get-ChildItem TaskManagementAPI.csproj
   ```

### Run the project step by step

Complete these steps from the directory that contains `TaskManagementAPI.csproj`:

1. **Install prerequisites.** Install the .NET 10 SDK and start a SQL Server instance. Git is needed only when cloning the repository.
2. **Set up local configuration.** Follow [Configure the database and JWT](#configure-the-database-and-jwt) below to configure the SQL Server connection and JWT signing secret. Use your own SQL Server instance details and a strong random secret.
3. **Restore NuGet dependencies:**

   ```powershell
   dotnet restore
   ```

4. **Create or update the database schema:**

   ```powershell
   dotnet ef database update
   ```

   If the `dotnet ef` command is unavailable, install the EF Core command-line tool as described under [Prerequisites](#prerequisites), then retry.
5. **Start the API:**

   ```powershell
   dotnet run --launch-profile http
   ```

6. **Confirm the API is running.** The local base URL is `http://localhost:5037`. In Development, the OpenAPI document is at `http://localhost:5037/openapi/v1.json`.
7. **Test the API.** Use the request examples below or `TaskManagementAPI.http` in an HTTP client that supports `.http` request files. Register a user, log in, then pass the returned access token as `Authorization: Bearer <access-token>` when calling protected endpoints.

To stop the running API, press `Ctrl+C` in the terminal.

### Prerequisites

1. Install the .NET 10 SDK.
2. Install and start SQL Server (LocalDB, SQL Server Express, or a full SQL Server instance).
3. Install the `dotnet-ef` CLI tool if it is not already available:

   ```powershell
   dotnet tool install --global dotnet-ef
   ```

### Configure the database and JWT

From the project directory, configure local secrets. Replace the connection string with one suitable for your SQL Server instance. Use a newly generated, sufficiently long random secret for JWT signing; never use a sample or commit a real secret.

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=TaskManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "JwtSettings:SecretKey" "<replace-with-a-long-random-secret>"
dotnet user-secrets set "JwtSettings:Issuer" "TaskManagementAPI"
dotnet user-secrets set "JwtSettings:Audienc" "TaskManagementAPI"
dotnet user-secrets set "JwtSettings:AccessTokenExpiryMinutes" "30"
dotnet user-secrets set "JwtSettings:RefreshTokenExpiryDays" "7"
```

The application currently reads the audience setting using the key `JwtSettings:Audienc` (spelled without the final `e`) in both token generation and validation. Keep this spelling in configuration unless the application code is changed to use a corrected key consistently.

User Secrets override values from `appsettings.json` for local development. For deployed environments, provide the equivalent ASP.NET Core configuration environment variables, for example `ConnectionStrings__DefaultConnection` and `JwtSettings__SecretKey`. Keep production secrets in a secret manager or protected deployment configuration.

### Apply migrations and run

Run these commands from the directory containing `TaskManagementAPI.csproj`:

```powershell
dotnet restore
dotnet ef database update
dotnet run --launch-profile http
```

The HTTP development base URL is `http://localhost:5037`. An HTTPS profile is also available at `https://localhost:7221` (and HTTP at port `5037`). Use HTTPS when testing the refresh-token cookie: the cookie is marked `Secure`.

In Development, the OpenAPI document is available at:

```text
http://localhost:5037/openapi/v1.json
```

The project maps an OpenAPI document; it does not configure a Swagger UI page.

## Authentication and registration flow

1. Send `POST /api/auth/register` with a username, email, and password. Public registration assigns the `User` role. Registration returns a user profile, not tokens.
2. Send `POST /api/auth/login` with the registered email and password.
3. The response contains an access token, refresh token, user profile, and token expiry timestamps. The refresh token is also written to an HTTP-only, Secure, SameSite Strict cookie scoped to the refresh endpoint.
4. Send the access token on protected requests in the header `Authorization: Bearer <access-token>`.
5. When the access token expires, send the refresh token to `POST /api/auth/refresh-token`. The API returns a new access/refresh token pair and revokes/replaces the previous refresh token.
6. To sign out/revoke a refresh token, send an authenticated request to `POST /api/auth/revoke-token`.

The access token is an HMAC-SHA256 signed JWT. It includes the subject/user ID, email, username, role, and token ID claims. The configured issuer, audience, signing key, and lifetime are checked by the JWT Bearer middleware. Refresh tokens are random values stored in the database; refresh rotates the token rather than reusing it.

### Example: register

```http
POST http://localhost:5037/api/auth/register
Content-Type: application/json

{
  "username": "alex",
  "email": "alex@example.com",
  "password": "ChangeThisPassword123!"
}
```

### Example: login

```http
POST http://localhost:5037/api/auth/login
Content-Type: application/json

{
  "email": "alex@example.com",
  "password": "ChangeThisPassword123!"
}
```

### Example: refresh tokens

```http
POST http://localhost:5037/api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "<refresh-token>"
}
```

Example response shape:

```json
{
  "accessToken": "<jwt-access-token>",
  "refreshToken": "<refresh-token>",
  "user": {
    "id": 1,
    "username": "alex",
    "email": "alex@example.com",
    "createdAt": "2026-01-01T00:00:00Z",
    "role": "User"
  },
  "accessTokenExpiresAt": "2026-01-01T00:30:00Z",
  "refreshTokenExpiresAt": "2026-01-08T00:00:00Z"
}
```

### Example: authenticated task request

```http
GET http://localhost:5037/api/tasks
Authorization: Bearer <jwt-access-token>
```

## API endpoints

All routes below use the base URL `http://localhost:5037` while running locally. Replace it with your deployment URL (for example, `https://api.example.com`) after deployment.

### Authentication — `/api/auth`

| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | `/register` | Public | Register a user with the default `User` role. |
| POST | `/login` | Public | Verify credentials and issue access and refresh tokens. |
| POST | `/refresh-token` | Public, valid refresh token required | Rotate refresh token and issue a new access token. |
| POST | `/revoke-token` | Authenticated | Revoke the supplied refresh token. |

The refresh and revoke actions currently bind to `RefreshTokenResponseDto`; a request containing a `refreshToken` property is sufficient for these actions. The separate `RefreshTokenRequestDto` is defined but is not currently used by those controller actions.

### Tasks — `/api/tasks`

All task routes require a valid JWT.

| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | `/` | Any authenticated role | Create a task owned by the signed-in user. |
| GET | `/` | User: own tasks; Manager/Admin: all tasks | List tasks. |
| GET | `/{id}` | User: own task; Manager/Admin: any task | Get one task. |
| PUT | `/{id}` | User: own task; Admin: any task | Update task fields. Managers update only their own tasks. |
| DELETE | `/{id}` | Admin only | Delete any task. |
| PATCH | `/{id}/toggle` | Any authenticated role, own task | Toggle completion status. |

Task priority values are `1` (Low), `2` (Medium), and `3` (High). A task's `dueDate` is optional.

### Admin — `/api/admin`

All routes require the `Admin` role.

| Method | Route | Purpose |
|---|---|---|
| GET | `/` | List users. |
| GET | `/users/{id}` | Get one user and their task count. |
| POST | `/users` | Create a user, optionally assigning a role. |
| PUT | `/users/{id}/role` | Change a user's role. |
| DELETE | `/users/{id}` | Delete a user and their tasks. |
| GET | `/stats` | Get user/task counts and user counts by role. |

## Roles and permissions

| Role | Permissions |
|---|---|
| `User` | Manage own tasks; cannot list or view other users' tasks. |
| `Manager` | View all tasks; create tasks and update/toggle only their own tasks. |
| `Admin` | Manage users and roles, view all tasks, update any task, and delete tasks. |

The API seeds the `Admin`, `Manager`, and `User` roles through the initial database migration. Public self-registration does not allow a caller to choose an elevated role; Admins can assign a role through the protected admin endpoints.

## Project structure

```text
TaskManagementAPI/
├── Constants/       Role name constants used by authorization
├── Controllers/     HTTP routes, request handling, and response status codes
├── DTOs/            Request and response data transfer objects
├── Data/            EF Core ApplicationDbContext and entity mappings
├── Extensions/      Shared ClaimsPrincipal helpers
├── Migrations/      Versioned SQL Server schema changes and model snapshot
├── Models/          EF Core entities persisted to the database
├── Repositories/    Generic and entity-specific data access abstractions
├── Services/        Authentication and task business logic
├── Program.cs       Dependency injection, JWT, authorization, and middleware
└── TaskManagementAPI.csproj
```

### Request processing, step by step

1. **Controller** receives an HTTP request, binds the JSON body to a DTO, and enforces route-level authorization.
2. **Service** applies business rules, including task ownership checks, password verification, and token creation/rotation.
3. **Repository** performs entity-specific or generic data operations through Entity Framework Core.
4. **`ApplicationDbContext`** tracks and saves entities to SQL Server using the configured relationships and migrations.
5. **Response DTO** maps the result into the API response without returning internal entity fields such as a password hash.

### Controllers

- **`AuthController`** — Registration, login, refresh-token rotation, and token revocation.
- **`TasksController`** — Authenticated task creation, retrieval, updates, completion toggling, and Admin-only deletion.
- **`AdminController`** — Admin-only user administration, role assignment, and system statistics.

Controllers bind and validate DTOs, call the service layer, and translate results into HTTP responses. The task controller derives the current user's ID and role from JWT claims.

### Services

- **`IAuthService` / `AuthService`** — Registration, BCrypt password hashing/verification, JWT creation, refresh-token persistence/rotation, and revocation.
- **`ITaskService` / `TaskService`** — Task ownership checks, task business rules, mapping from entities to response DTOs, and task operations for regular users and elevated roles.

Services coordinate repositories and keep most application behavior out of the HTTP controllers.

### Repositories

- **`IRepository<T>` / `Repository<T>`** — Shared Entity Framework operations such as lookup, add, update, remove, and saving changes.
- **`IUserRepository` / `UserRepository`** — User lookup, duplicate email/username checks, and loading related role/token data.
- **`ITaskRepository` / `TaskRepository`** — User-scoped and all-task queries, task ownership checks, and task counts.
- **`IRefreshTokenRepository` / `RefreshTokenRepository`** — Refresh-token lookup and cleanup of old tokens.

Repository implementations use `ApplicationDbContext`; scoped repository and service registrations are configured in `Program.cs`.

### DTOs

DTOs define the API boundary instead of exposing database entities directly.

- **`RegisterDto`** — Username, email, password, and optional role field; public registration defaults to `User`.
- **`LoginDto`** — Email and password.
- **`LoginResponseDto`** — Access token, refresh token, user profile, and expiry timestamps.
- **`RefreshTokenRequestDto`** — Refresh-token input type, currently unused by the controller actions.
- **`RefreshTokenResponseDto`** — New token pair and expiry timestamps; currently also used as the refresh/revoke action request type.
- **`CreateTaskDto`** — Task title, description, priority, and optional due date.
- **`UpdateTaskDto`** — Updateable task fields; nullable completion, priority, and due date fields can be omitted.
- **`TaskResponseDto`** — Task data returned by task endpoints.
- **`UserResponseDto`** — Public user profile data, including assigned role, without the password hash.
- **`ChangeRoleDto`** — Role name to assign to a user.

### Models and data context

- **`User`** — Username, email, BCrypt password hash, creation time, role relationship, tasks, and refresh tokens.
- **`Role`** — Role name/description and related users.
- **`TaskItem`** — Task details, completion state, priority, optional due date, creation time, and owning user.
- **`RefreshToken`** — Token value, expiry/creation/revocation metadata, IP metadata, replacement token, and owning user. Computed properties expose whether it is expired, revoked, or active.
- **`ApplicationDbContext`** — EF Core sets, relationships, unique indexes, cascade behavior, and seeded role data.

### Extensions and constants

- **`ClaimsPrincipalExtensions`** — `GetUserId()` reads and parses the user ID from the JWT name-identifier or `sub` claim.
- **`RoleConstants`** — Shared `Admin`, `Manager`, and `User` role names used by services and authorization policies.

### Migrations

- **`20260912105732_InitialCreateAddedUserAadTaskEntities`** — Creates the roles, users, and tasks tables, their relationships/indexes, and seeds the three default roles.
- **`20260913095428_AddRefreshTokens`** — Creates the refresh-token table, its user relationship and unique token index, and enforces required user/task fields and unique user email/username indexes.
- **`ApplicationDbContextModelSnapshot`** — EF Core's snapshot of the current model used to generate future migrations.

After creating a new migration, apply it to the configured SQL Server database with `dotnet ef database update`.

## Example task payloads

Create a task:

```http
POST http://localhost:5037/api/tasks
Authorization: Bearer <jwt-access-token>
Content-Type: application/json

{
  "title": "Prepare sprint plan",
  "description": "Collect and prioritize the next sprint tasks.",
  "priority": 2,
  "dueDate": "2026-12-01T17:00:00Z"
}
```

Update a task:

```http
PUT http://localhost:5037/api/tasks/1
Authorization: Bearer <jwt-access-token>
Content-Type: application/json

{
  "title": "Prepare updated sprint plan",
  "description": "Review estimates and confirm the sprint backlog.",
  "isCompleted": false,
  "priority": 3,
  "dueDate": "2026-12-02T17:00:00Z"
}
```

## Configuration reference

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection used by Entity Framework Core. |
| `JwtSettings:SecretKey` | Secret used to sign and validate JWTs. Keep private and use a strong random value. |
| `JwtSettings:Issuer` | JWT issuer; must match between token creation and validation. |
| `JwtSettings:Audienc` | JWT audience (current application key spelling); must match between creation and validation. |
| `JwtSettings:AccessTokenExpiryMinutes` | Access-token lifetime in minutes. |
| `JwtSettings:RefreshTokenExpiryDays` | Refresh-token lifetime in days. |
