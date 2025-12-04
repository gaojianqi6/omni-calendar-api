## OmniCalendar Backend (ASP.NET Core API)

This is the **backend API** for OmniCalendar, implemented as a clean, database‑agnostic ASP.NET Core Web API.  
It uses **Clerk** for authentication and **Supabase PostgreSQL** purely as a hosted Postgres database.

### Tech Stack

- **Runtime**: .NET 10 (ASP.NET Core Web API)
- **Language**: C#
- **Database**: PostgreSQL (currently hosted on Supabase)
- **ORM**: Entity Framework Core + Npgsql provider
- **Auth**: Clerk (JWT Bearer, `Microsoft.AspNetCore.Authentication.JwtBearer`)
- **HTTP Docs**: Swagger / OpenAPI
- **Tests**: xUnit (+ Moq)

### Project Structure (Backend)

- `OmniCalendar.Api/`
  - `Domain/Entities/` – EF Core entities mirroring the PostgreSQL schema (`users`, `categories`, `tags`, `tasks`, `task_notes`, `task_tags`, `holiday_cache`).
  - `Infrastructure/OmniCalendarDbContext.cs` – DbContext + fluent mappings and indexes.
  - `Application/`
    - `Auth/` – `CurrentUserService` (maps Clerk JWT → `users` row).
    - `Tasks/` – task DTOs and `TaskService` (create, query by date range, today).
    - `Dashboard/` – `DashboardService` (XP, rank, today/total stats).
    - `Holidays/` – `HolidayService` (Calendarific + DB cache).
  - `Api/Controllers/`
    - `TasksController` – `/api/tasks` endpoints.
    - `DashboardController` – `/api/dashboard/summary`.
    - `HolidaysController` – `/api/holidays`.
- `OmniCalendar.Api.Tests/` – xUnit test project.

### Configuration

Edit `OmniCalendar.Api/appsettings.json`:

- **Database connection**:
  - `ConnectionStrings:Database` – point this at your Postgres instance (Supabase or other).
- **Clerk auth**:
  - `Clerk:Issuer` – your Clerk issuer URL (e.g. `https://<your-clerk-domain>.clerk.accounts.dev`).
  - `Clerk:Audience` – audience that your frontend tokens are issued for.
- **Calendarific**:
  - `Calendarific:ApiKey` – your Calendarific API key.
  - `Calendarific:BaseUrl` – usually `https://calendarific.com/api/v2`.

### Running the API Locally

From the **backend root** (`OmniCalendar` folder, which contains `OmniCalendar.Api/`):

```bash
cd /Users/jerome/Projects/omni-calendar-projects/OmniCalendar
dotnet restore OmniCalendar.Api/OmniCalendar.Api.csproj
dotnet run --project OmniCalendar.Api
```

By default the API will listen on the standard ASP.NET Core ports (see `launchSettings.json`), typically `https://localhost:7145` or similar.

### Using OpenAPI to See and Test Endpoints

1. Start the API (see above).
2. Open a browser to:
   - `http://127.0.0.1:5145/openapi/v1.json`
3. You will see the OpenAPI JSON document describing:
   - All controllers and endpoints (`/api/tasks`, `/api/dashboard/summary`, `/api/holidays`, `/health`).
   - Request/response schemas for DTOs.
4. To test endpoints:
   - **Health (no auth)**:
     - `GET http://127.0.0.1:5145/health`
   - **Authenticated APIs**:
     - Use tools like VS Code REST client, Postman, or `curl`, and
     - Attach an `Authorization: Bearer <your-clerk-jwt>` header using a token issued by Clerk when calling `/api/...`.

### Running Tests (xUnit)

From the **backend root**:

```bash
cd /Users/jerome/Projects/omni-calendar-projects/OmniCalendar
dotnet test OmniCalendar.Api.Tests/OmniCalendar.Api.Tests.csproj
```

This runs all tests in `OmniCalendar.Api.Tests`, including logic for rank calculation and can be extended with service‑level tests (task ordering, holiday parsing, etc.).

### What This Backend Provides

- **Auth integration**:
  - Validates **Clerk** JWTs via standard JWT Bearer auth.
  - Automatically ensures there is a corresponding `users` row for each Clerk user.
- **Tasks API** (`/api/tasks`):
  - Create tasks/events with due date, priority, recurrence rule, category, tags.
  - Query tasks by date range (used for Todo list, “this year” range).
  - Get **today’s tasks** ordered by priority (Home page “Today’s Event”).
- **Dashboard API** (`/api/dashboard/summary`):
  - Returns rank, XP, today stats (done/left), and totals (done/left).
- **Holidays API** (`/api/holidays`):
  - Fetches holidays for a given country/year from Calendarific.
  - Caches raw JSON responses in `holiday_cache` to avoid repeated API calls.

Frontend **settings** (language, second calendar, theme) are currently handled entirely on the frontend and are not persisted server‑side.


