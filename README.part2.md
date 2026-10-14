## Part 2: RESTful API (ASP.NET Core Web API)

### Tech
ASP.NET Core 8 Web API, Entity Framework Core 8 (Code-First, SQL Server), JWT authentication with role-based authorisation, BCrypt password hashing, Swagger (Swashbuckle), xUnit tests, GitHub Actions CI.

### Project layout
```
RaceDay.sln
RaceDay.Api/        Controllers, Models, DTOs, Data (AppDbContext), Services (TokenService), Program.cs
RaceDay.Tests/      xUnit tests (in-memory database, real HTTP pipeline)
.github/workflows/  validate-docs.yml (Part 1) + ci.yml (build and test)
docs/               Part 1 ERD, endpoint plan, SQL script
```

### Run it
1. Edit `RaceDay.Api/appsettings.json` -> `ConnectionStrings:DefaultConnection` to point at your SQL Server.
   Change `Jwt:Key` to your own secret (32+ characters).
2. Create the database from the models (Code-First):
   ```
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialCreate --project RaceDay.Api
   dotnet ef database update --project RaceDay.Api
   ```
   (Drop any existing `RaceDayDB` created by the Part 1 script first, otherwise `database update` fails with "already exists".)
3. `dotnet run --project RaceDay.Api` and open `/swagger`.
4. In Swagger: register -> login -> click **Authorize** -> paste the token.

### Authentication and roles
- `POST /api/auth/register` (role = Organiser | Participant) and `POST /api/auth/login`.
- Login returns a JWT holding the user id and role; send it as `Authorization: Bearer <token>`.
- Passwords are hashed with BCrypt; the original password is never stored.
- Organiser-only endpoints return 403 to Participants and vice versa; no token returns 401.
- Organisers can only modify events (and their categories, routes, results) that they own.

### Tests
`dotnet test` runs unit/integration tests covering registration and login, role enforcement (401/403), events, categories, enrolments (duplicate, full category, ownership) and results.

### Roles
- **Organiser:** creates, edits and deletes their own events; manages categories and route info; views enrolments for their events; captures and corrects results.
- **Participant:** registers, browses events and categories, enrols in a category, views and cancels their own enrolments, views their own results.

### CI/CD
GitHub Actions (`.github/workflows/ci.yml`) builds the solution and runs all tests on every push.

![Green build](docs/green-build.png)   <!-- TODO: add your screenshot to docs/ -->

### Demo video
<!-- TODO: paste your unlisted YouTube link here -->
