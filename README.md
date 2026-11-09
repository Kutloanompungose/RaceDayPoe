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
