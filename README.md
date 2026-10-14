# RaceDay: Part 2 (RESTful API)

RaceDay is an event management system for South African road running, walking and cycling events. Part 1 covered the planning and database design (ERD, endpoint plan and SQL script in `/docs`). **Part 2 is the ASP.NET Core Web API** that powers the system. It implements the Part 1 endpoint plan, enforces role-based access, is tested with unit tests, and is built and tested automatically with GitHub Actions. Part 3 will be an MVC front end that consumes this API.

---

## Table of contents
1. [Technology](#technology)
2. [Project structure](#project-structure)
3. [Setup and run instructions](#setup-and-run-instructions)
4. [User roles](#user-roles)
5. [Authentication and authorisation](#authentication-and-authorisation)
6. [API endpoints](#api-endpoints)
7. [Swagger](#swagger)
8. [Unit tests](#unit-tests)
9. [CI/CD](#cicd)

---

## Technology

| Area | Choice |
|---|---|
| Framework | ASP.NET Core 8 Web API (C#) |
| Database | Microsoft SQL Server, accessed with Entity Framework Core 8 (Code-First) |
| Authentication | JWT bearer tokens |
| Password security | BCrypt hashing (BCrypt.Net-Next) |
| API documentation | Swagger / Swashbuckle with XML comments |
| Testing | xUnit, `WebApplicationFactory`, EF Core InMemory provider |
| CI/CD | GitHub Actions |

---

## Project structure

```
RaceDay.sln
RaceDay.Api/
  Controllers/      Auth, Users, Events, Categories, Routes, Enrolments, Results
  Models/           EF Core entities (User, Event, Category, RouteInfo, Enrolment, Result)
  DTOs/             Request and response objects with validation rules
  Data/             AppDbContext (relationships, constraints, cascade rules)
  Services/         TokenService (creates JWTs) and claim helpers
  Program.cs        Service registration, JWT setup, Swagger, middleware pipeline
  appsettings.json  Connection string and JWT settings
RaceDay.Tests/      xUnit tests
.github/workflows/
  ci.yml            Builds the solution and runs all tests on every push
  validate-docs.yml Part 1 repository structure check
docs/               Part 1 ERD, API endpoint plan and SQL script
```

The EF Core models and `AppDbContext` match the Part 1 ERD and SQL script: the same tables (`Users`, `Events`, `Categories`, `RouteInfo`, `Enrolments`, `Results`), keys, foreign keys, unique constraints and cascade rules.

---

## Setup and run instructions

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Microsoft SQL Server (LocalDB, SQL Server Express or full SQL Server) and SSMS
- Git

### 1. Clone the repository and switch to the Part 2 branch
```
git clone https://github.com/<your-username>/<your-repo>.git
cd <your-repo>
git checkout part2-api
```

### 2. Configure the connection string and JWT key
Open `RaceDay.Api/appsettings.json`:
- Set `ConnectionStrings:DefaultConnection` to point at your SQL Server. The default targets LocalDB: `Server=(localdb)\MSSQLLocalDB;Database=RaceDayDB;...`
- Set `Jwt:Key` to your own secret of at least 32 characters.

### 3. Create the database (Code-First)
```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project RaceDay.Api
dotnet ef database update --project RaceDay.Api
```
If you previously ran the Part 1 SQL script, drop the existing `RaceDayDB` first, otherwise `database update` will fail with "already exists".

### 4. Run the API
```
dotnet run --project RaceDay.Api
```
Swagger opens in the browser at `/swagger`.

### 5. Try it in Swagger
1. `POST /api/auth/register` to create an Organiser and a Participant.
2. `POST /api/auth/login` and copy the `token` from the response.
3. Click **Authorize** (top right), paste the token and confirm.
4. Call the endpoints. Log in as the other role to see the role restrictions in action.

### Run the tests
```
dotnet test
```

---

## User roles

### Organiser
Manages the events they host. An Organiser can:
- Create, edit and delete **their own** events.
- Create, edit and delete categories (e.g. 5km, 10km, 21km) with a distance, maximum number of participants and entry fee for their events.
- Add and update route information for a category.
- View all enrolments for their events.
- Capture and correct race results (finish time, position, status).
- View and update their own profile.

### Participant
Takes part in events. A Participant can:
- Register, log in, and view and update their own profile.
- Browse events and view their categories and route details.
- Enrol in an event by selecting a category (a race number is assigned automatically).
- View their enrolment history and cancel their own enrolments.
- View their own results and performance history.

Both roles can view events and categories and manage their own profile.

---

## Authentication and authorisation

- **Registration** requires a full name, email, password (minimum 8 characters) and a role (`Organiser` or `Participant`). Emails are unique.
- **Password security:** passwords are hashed with BCrypt. The original password is never stored or returned.
- **Login** returns a signed **JWT** containing the user's id and role. The client sends it on every later request as `Authorization: Bearer <token>`, which is how the API keeps the user's authenticated state and role between requests. Tokens expire after 60 minutes by default.
- **Role-based access** is enforced with `[Authorize(Roles = "...")]` on the endpoints:
  - No token gives **401 Unauthorized**.
  - A valid token for the wrong role gives **403 Forbidden** (e.g. a Participant trying to create an event).
- **Ownership checks:** an Organiser can only modify events, categories, routes and results that belong to their own events (otherwise 403). A Participant can only cancel their own enrolments.

---

## API endpoints

All routes are prefixed with `/api`. "Public" endpoints need no token.

### Authentication
| Method | Route | Role | Description |
|---|---|---|---|
| POST | `/auth/register` | Public | Register as an Organiser or Participant |
| POST | `/auth/login` | Public | Log in and receive a JWT |

### Users
| Method | Route | Role | Description |
|---|---|---|---|
| GET | `/users/me` | Any logged-in user | View own profile |
| PUT | `/users/me` | Any logged-in user | Update own profile |

### Events
| Method | Route | Role | Description |
|---|---|---|---|
| GET | `/events` | Public | List upcoming events (`?includePast=true` for all) |
| GET | `/events/{id}` | Public | Event details with its categories |
| POST | `/events` | Organiser | Create an event |
| PUT | `/events/{id}` | Organiser (owner) | Update an event |
| DELETE | `/events/{id}` | Organiser (owner) | Delete an event and its categories, enrolments and results |
| GET | `/events/{id}/enrolments` | Organiser (owner) | List enrolments for an event |
| GET | `/events/{id}/results` | Public | List results for an event |

### Categories
| Method | Route | Role | Description |
|---|---|---|---|
| GET | `/events/{eventId}/categories` | Public | List categories of an event |
| POST | `/events/{eventId}/categories` | Organiser (owner) | Add a category |
| PUT | `/categories/{id}` | Organiser (owner) | Update a category |
| DELETE | `/categories/{id}` | Organiser (owner) | Delete a category |

### Route info
| Method | Route | Role | Description |
|---|---|---|---|
| GET | `/categories/{categoryId}/route` | Public | View a category's route |
| POST | `/categories/{categoryId}/route` | Organiser (owner) | Add route info (one per category) |
| PUT | `/routes/{id}` | Organiser (owner) | Update route info |

### Enrolments
| Method | Route | Role | Description |
|---|---|---|---|
| POST | `/enrolments` | Participant | Enrol in a category (409 if already enrolled or the category is full) |
| GET | `/enrolments/me` | Participant | View own enrolment history |
| DELETE | `/enrolments/{id}` | Participant (owner) | Cancel own enrolment |

### Results
| Method | Route | Role | Description |
|---|---|---|---|
| POST | `/results` | Organiser (owner) | Capture a result (409 if one already exists) |
| PUT | `/results/{id}` | Organiser (owner) | Correct a result |
| GET | `/results/me` | Participant | View own results |

### Validation and error responses
Request bodies are validated (required fields, email format, password length, allowed `role` / `eventType` / result `status` values, numeric ranges), returning **400 Bad Request** with details. Other responses used: **404 Not Found**, **409 Conflict**.

---

## Swagger

Swagger is available at `/swagger` when the API is running. Every endpoint shows a description of its purpose, the data it expects and the responses it returns (status codes). The **Authorize** button lets you paste the JWT so you can test protected endpoints directly in the browser, with no extra tools.

---

## Unit tests

The `RaceDay.Tests` project runs the real API pipeline in memory with an EF Core InMemory database, so tests exercise routing, validation, authentication and authorisation together. Each test registers its own users, so tests do not depend on each other.

The tests cover:
- **Registration and login:** success for both roles, duplicate email (409), invalid role (400), short password (400), wrong password and unknown user (401), and no password hash in responses.
- **Profile:** both roles can view and update their profile; no token gives 401.
- **Token creation:** the JWT contains the correct user id and role claims.
- **Events:** Organiser creates, updates and deletes their own events; Participants and anonymous users are blocked (403 / 401); an Organiser cannot change another Organiser's event; both roles can view events.
- **Categories:** Organiser manages categories; Participants are blocked; both roles can view them; invalid input returns 400.
- **Enrolments:** a Participant can enrol and gets a race number; duplicate enrolment and a full category return 409; Organisers cannot enrol; Participants can only cancel their own enrolment; only the owning Organiser can view an event's enrolments.
- **Results:** the Organiser captures and corrects results; Participants are blocked; duplicates return 409; validation rules; a Participant sees their own results.

Run them locally with `dotnet test`.

---

## CI/CD

A GitHub Actions workflow (`.github/workflows/ci.yml`) runs on **every push** and on pull requests. It checks out the code, installs .NET 8, restores packages, builds the solution in Release mode and runs all unit tests. The Part 1 `validate-docs.yml` workflow is still included.

**Successful green build:**

![Green GitHub Actions build](docs/green-build.png)

---

RaceDay — Event Management System

Part 1: Planning & Database

1. System Description

RaceDay is a full-stack web-based event management system designed for the South African road running, walking, and cycling community.

The system allows Event Organisers to create and manage sporting events, categories, participant enrolments, and race results. Participants can create accounts, browse upcoming events, enter available race categories, view their enrolment history, and track their race performance over time.

The system is designed around the types of events commonly held in South Africa, such as road races, fun runs, marathons, half-marathons, cycling events, and community walking events. A key design decision was to separate Events from Categories, allowing one event to offer multiple distances such as 5 km, 10 km, 21 km, and 42.2 km.

Part 1 focuses on the planning and database design of the RaceDay system. The following documents are included in the /docs folder:

* Entity Relationship Diagram (ERD)
* API Endpoint Plan
* SQL Database Script

⸻

2. User Roles

2.1 Organiser

An Organiser is responsible for managing the events hosted on the RaceDay system.

An Organiser can:

* Create new events.
* Edit existing event information.
* Delete events when required.
* Create and manage event categories.
* Set different race distances and entry information.
* View participants enrolled in their events.
* Capture participant race results.
* Correct participant results when necessary.
* Manage route information associated with an event.

2.2 Participant

A Participant is a person who uses RaceDay to discover and participate in sporting events.

A Participant can:

* Create an account.
* Browse upcoming events.
* View event information.
* View available race categories.
* Enter an event.
* Select a race category.
* View their enrolment history.
* View their race results.
* Track their performance history over time.
* Use route and weather information to prepare for race day.

⸻

3. Repository Structure

The repository is organised as follows:

RaceDay/
│
├── .github/
│   └── workflows/
│       └── validate-docs.yml
│
├── docs/
│   ├── RaceDay_ERD.png
│   ├── RaceDay_API_Endpoint_Plan.md
│   └── RaceDay_Schema.sql
│
└── README.md

The /docs folder contains the main Part 1 documentation and database deliverables.

⸻

4. Part 1 Deliverables

4.1 Entity Relationship Diagram

The ERD shows the main entities in the RaceDay database and the relationships between them.

The ERD includes entities such as:

* Organiser
* Participant
* Event
* Category
* Route
* Enrolment
* Result

The diagram is available in:

docs/RaceDay_ERD.png

4.2 API Endpoint Plan

The API Endpoint Plan describes the proposed API operations that will be used by the RaceDay application.

The plan includes endpoints for areas such as:

* Organisers
* Participants
* Events
* Categories
* Enrolments
* Results
* Routes

The endpoint plan is available in:

docs/RaceDay_API_Endpoint_Plan.md

4.3 SQL Database Script

The SQL script creates the RaceDay database, tables, relationships, constraints, and sample data.

The SQL script is available in:

docs/RaceDay_Schema.sql

⸻

5. Database Setup Instructions

Prerequisites

The following software is required:

* Microsoft SQL Server
* SQL Server Management Studio (SSMS)
* Git
* A GitHub account for accessing the repository

Running the Database Script

1. Open SQL Server Management Studio (SSMS).
2. Connect to a local or clean SQL Server instance.
3. Open the following file:

docs/RaceDay_Schema.sql

4. Execute the script by pressing F5 or selecting the Execute button.
5. The script will:
    * Create the RaceDayDB database.
    * Create the required database tables.
    * Create primary keys.
    * Create foreign key relationships.
    * Add the required constraints.
    * Insert sample Organisers.
    * Insert sample Participants.
    * Insert sample Events.
    * Insert sample Categories.
    * Insert sample Route information.
    * Insert sample Enrolments.
    * Insert sample Results.
6. After execution, confirm that the database was created successfully.
7. In SSMS, expand:

Databases → RaceDayDB → Tables

8. Confirm that the RaceDay tables are displayed.

⸻

6. ERD ↔ SQL Consistency

The database design represented in the ERD corresponds with the database structure implemented in the SQL script.

The entities, relationships, primary keys, and foreign keys represented in the ERD are implemented in:

docs/RaceDay_Schema.sql

No deliberate differences were made between the ERD and the SQL database design.

The ERD is therefore intended to provide a visual representation of the database structure that is implemented by the SQL script.

⸻

7. API Design

The planned RaceDay API will provide access to the main functionality of the system.

Examples of planned operations include:

Resource	Example Operations
Participants	Create, view, update
Organisers	Create, view, update
Events	Create, view, update, delete
Categories	Create, view, update, delete
Enrolments	Create, view, update
Results	Create, view, update
Routes	Create, view, update

The API will allow the future frontend application to communicate with the RaceDay database through structured endpoints.

⸻

8. Sample South African Event Context

RaceDay is designed with the South African road-event environment in mind.

The system can support different types of events, including:

* Road running events
* Marathons
* Half-marathons
* 10 km races
* 5 km fun runs
* Walking events
* Cycling events
* Charity races
* Community sporting events

The database structure is flexible enough to allow an organiser to create an event and then provide multiple categories for participants.

For example, a single event could offer:

5 km Fun Run
10 km Road Race
21 km Half Marathon
42.2 km Marathon

This approach prevents the database from having to create a completely separate event record for every distance.

⸻

9. CI/CD Validation

A GitHub Actions workflow is included in:

.github/workflows/validate-docs.yml

The workflow runs automatically when changes are pushed to a branch or when a pull request is created.

The workflow checks that:

* The /docs folder exists.
* An ERD image or PDF exists inside /docs.
* An API endpoint plan document exists inside /docs.
* An SQL script exists inside /docs.
* README.md exists at the repository root.

If all required files are found, the workflow displays:

✅ Repository structure meets Part 1 requirements.

If a required file or folder is missing, the workflow fails and displays an error explaining what is missing.

⸻

10. Future Development

Part 1 establishes the foundation for the RaceDay system.

Future parts of the project can build on this foundation by adding:

* A backend API.
* Database connectivity.
* Authentication and authorisation.
* An organiser dashboard.
* A participant dashboard.
* Event search and filtering.
* Event registration functionality.
* Race-result management.
* Route information.
* Weather information.
* A responsive web frontend.

The database and API design created in Part 1 will provide the foundation for these future features.

⸻

11. Conclusion

RaceDay provides a structured approach to managing road running, walking, and cycling events in South Africa.

The Part 1 database design identifies the main users, events, categories, enrolments, routes, and results required by the system. The ERD provides a visual representation of these relationships, while the SQL script implements the database structure.

The repository also includes an API endpoint plan and GitHub Actions validation workflow to ensure that the required Part 1 documentation and database files are maintained in the correct repository structure.



