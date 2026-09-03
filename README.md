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
