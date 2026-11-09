RaceDay – Event Management Platform

RaceDay lets Organisers publish running, walking and cycling events and capture official results, and lets Participants find events, enrol in a category and follow their personal race history.

Part	What	Where
1	Design, ERD, SQL script	docs/
2	RESTful API (ASP.NET Core Web API, EF Core, JWT)	RaceDay.Api/ – see README.part2.md
3	MVC web app, Azure Blob Storage, Docker, CI/CD	RaceDay.Web/ (branch part-3-mvc)


1. Architecture

 Browser ──HTTP──▶  RaceDay.Web (ASP.NET Core MVC)  ──HTTP + JWT──▶  RaceDay.Api (Web API)  ──▶  SQL Server
   ▲                  • Razor views, Bootstrap 5                        • Controllers, EF Core            
   │                  • Session (token, id, name, role)                 • Business rules + validation     
   │                  • NO database, NO Azure SDK                       • Azure.Storage.Blobs  ──▶  Azure Blob Storage
   └────── images are loaded straight from the public blob URL ◀───────────────────────────────────────────┘

	•	The MVC project contains no database access and no business rules – every operation is a call to the Part 2 API through one typed client (Services/RaceDayApi.cs).
	•	The MVC project never talks to Azure. Uploads are posted to the API (POST /api/events/{id}/banner, POST /api/users/me/picture), which validates the file and stores it in Blob Storage. The API returns the blob URL, which the views display.
	•	After login the API's JWT, the user id, name and role are kept in server-side session; the same expiry is applied to both.

MVC structure (RaceDay.Web/)
Folder	Purpose
Controllers/	AccountController (register, login, logout), OrganiserController, ParticipantController, HomeController. All inherit AppController (flash messages, session-expiry handling, no-cache headers).
Services/	RaceDayApi (typed HttpClient wrapper for every API endpoint), SessionState (who is logged in), ApiResult (success/failure with user-safe messages), Formatting (ordinals, race times, status colours).
Filters/	[SessionAuthorize(role)] – redirects anonymous users to Login and wrong-role users to Access denied.
Models/	Api/ DTO shapes returned by the API, and ViewModels.cs with validation attributes.
Views/	Razor views, one folder per controller plus Shared/ (layout, event card, status badge).
wwwroot/	css/site.css (RaceDay theme), js/site.js, favicon.
Requirement	Implementation
MVC app consuming the API; no DB/business logic	RaceDay.Web – only RaceDayApi makes calls
Register / log in through the API	Account/Register, Account/Login
Session keeps auth state and role on every page	SessionState + SessionAuthorize filter
Distinct interface per role	Organiser nav: Dashboard · My Events · New Event. Participant nav: Find Events · My Enrolments · My Results · Profile
Unauthenticated users blocked	Filter redirects to login (covered by AccessControlTests)
Log out and redirect	POST /Account/Logout clears the session → Login page
Organiser dashboard	Organiser/Dashboard – events created, enrolments per event (bars), upcoming dates
Create / edit / delete events (name, description, date, location, distance, type)	Organiser/Events, CreateEvent, EditEvent
Upload event banner	Event form → API → Azure Blob Storage
Category management	Organiser/Categories, EditCategory (name = age/distance label, distance, limit, fee)
View enrolments (participant, category, status)	Organiser/Enrolments – grouped by category, colour-coded status
Capture and publish results	Organiser/Results – status (Finished/DNF/DNS), time h:mm:ss, position; saving publishes immediately
Participant home with filters	Participant/Events – search, type, province, date range
Event details + categories	Participant/EventDetails (banner from Blob Storage)
Enrol and choose category	Enrol button per category – recorded immediately via the API
My enrolments + status	Participant/MyEnrolments (cancel supported)
My Results ("47th out of 312 finishers")	Participant/MyResults
Profile + profile picture	Participant/Profile – edit name/phone, upload picture (Blob Storage)
Colour-coded statuses	Green Confirmed, amber Pending, red Cancelled, blue Finished
Graceful errors	API errors are turned into friendly messages; global exception handler + custom 404 page; no raw errors shown
Docker	RaceDay.Web/Dockerfile (multi-stage, non-root)
CI/CD	.github/workflows/ci.yml builds API + MVC, runs tests, builds both images

API additions made for Part 3 (so the MVC app needs no logic of its own): GET /api/events/mine (organiser dashboard numbers), POST /api/events/{id}/banner, POST /api/users/me/picture, an optional headline distanceKm on events, bannerUrl/profilePictureUrl fields, and eventDate/totalFinishers on results.

3. Run it

Option A – everything with ONE command (Docker)

Requires Docker Desktop only.

docker compose up --build

Service	URL
RaceDay website	http://localhost:8080
API + Swagger	http://localhost:5100/swagger
Blob emulator (Azurite)	http://localhost:10000

This starts SQL Server, the Azurite Blob emulator, the API and the MVC app. The database schema is created automatically on first start (give SQL Server ~30 s). Uploaded images are stored in the Azurite blob container, exactly as they would be in Azure.

Option B – only the MVC container (API already running)

docker build -t raceday-web ./RaceDay.Web
docker run --rm -p 8080:8080 --add-host=host.docker.internal:host-gateway -e Api__BaseUrl=http://host.docker.internal:5100/ raceday-web

Open http://localhost:8080. Api__BaseUrl is the only setting you need; point it at a deployed API (e.g. https://my-raceday-api.azurewebsites.net/) to use a hosted back end. The containerised app behaves identically to dotnet run – the same configuration keys are used in both cases (Api:BaseUrl ⇄ Api__BaseUrl).

Option C – locally without Docker

Prerequisites: .NET 8 SDK, SQL Server / LocalDB.

# 1. API  (http://localhost:5100)
dotnet ef migrations add Part3BlobAndDistance --project RaceDay.Api   # new columns added in Part 3
dotnet ef database update --project RaceDay.Api
dotnet run --project RaceDay.Api --launch-profile RaceDay.Api

# 2. MVC app (http://localhost:5200) – in a second terminal
dotnet run --project RaceDay.Web

RaceDay.Web/appsettings.json → Api:BaseUrl is http://localhost:5100/ by default.

If you created the database in Part 2 with InitialCreate, the extra Part3BlobAndDistance migration adds the new columns (Events.BannerUrl, Events.DistanceKm, Users.ProfilePictureUrl).

4. Azure Blob Storage setup

	1.	In the Azure Portal create a Storage account (Standard, LRS is fine).
	2.	Settings → Configuration → set Allow Blob anonymous access = Enabled (so the browser can display banners and avatars).
	3.	Security + networking → Access keys → copy a connection string.
	4.	Give it to the API (never commit it):

dotnet user-secrets init --project RaceDay.Api
dotnet user-secrets set "AzureBlob:ConnectionString" "<your connection string>" --project RaceDay.Api

or, with Docker Compose, create a .env file next to docker-compose.yml:

AZURE_STORAGE_CONNECTION_STRING=<your connection string>
AZURE_BLOB_PUBLIC_BASE_URL=

The API creates the container raceday-images (public read for blobs) on first upload and stores files as events/{eventId}/{guid}.png and users/{userId}/{guid}.jpg. Only JPG, PNG, WEBP and GIF files up to 5 MB are accepted.


Setting (API)	Meaning
AzureBlob:ConnectionString	Storage account connection string
AzureBlob:ContainerName	Default raceday-images
AzureBlob:PublicBaseUrl	Optional – only used with emulators so the browser gets a reachable URL
Database:AutoCreate	true creates the schema on start-up (used by Docker Compose)
	1.	Show the structure – solution explorer: RaceDay.Api, RaceDay.Web, tests. Open Services/RaceDayApi.cs and point out that the MVC project has no DbContext and no Azure SDK.
	2.	Start it – docker compose up --build; show the containers running; open http://localhost:8080.
	3.	Unauthenticated access – visit /Organiser/Dashboard → redirected to Login.
	4.	Organiser – register as Organiser → dashboard (empty) → Create event with a banner image → add two categories (e.g. 10km, 5km Fun Walk).
	5.	Participant – in a private window register as Participant → Find Events (use the filters) → open the event (banner served from Blob Storage) → Enrol → My Enrolments (green Confirmed badge).
	6.	Profile – upload a profile picture; show it in the navbar. In the Azure Portal (or Azurite) show the blob that was created.
	7.	Results – as Organiser open Enrolments (participant, category, status), then edit the event date to a past date, open Results, enter a time (0:48:30) and position (1), Publish.
	8.	My Results – as Participant show "1st out of 1 finisher", time and category.
	9.	Role separation – show the different navigation bars; try the Organiser URL as a Participant → Access denied. Log out → back to Login; press Back → still protected.
	10.	Docker & CI – show RaceDay.Web/Dockerfile (multi-stage, non-root), the run command, the green GitHub Actions run (build API, build MVC, tests, Docker build).
Tip: enrolment is only allowed for future events, so create the demo event with a future date, enrol, and then edit the date into the past to capture results.

6. Tests and CI

dotnet test RaceDay.sln

	•	RaceDay.Tests – API integration tests (auth, roles, events, categories, enrolments, results, image upload with a fake blob store, events/mine).
	•	RaceDay.Web.Tests – formatting helpers (ordinals, "47th out of 312 finishers", race-time parsing, status colours) and access-control tests (protected pages redirect to login; friendly 404).

7. Git workflow

Part 3 work lives on the branch part-3-mvc (Part 2 is the main baseline).

git remote add origin https://github.com/<your-username>/<your-repo>.git   # if not already set
git push -u origin main
git push -u origin part-3-mvc

8. Troubleshooting
Symptom	Fix
"We can't reach the RaceDay service"	The API isn't running or Api__BaseUrl is wrong. In Docker use host.docker.internal, not localhost.
Image upload says storage is not configured	Set AzureBlob:ConnectionString for the API (section 4).
Images upload but don't display	Enable Allow Blob anonymous access on the storage account.
docker compose API restarts at first start	SQL Server is still starting – it retries for about a minute.
Apple-silicon Mac and the SQL Server image	Enable Use Rosetta in Docker Desktop, or use Option C with LocalDB/Azure SQL.
Logged out after a container restart	Mount a volume for keys: add -v raceday-keys:/app/keys to docker run.






