/* ============================================================
   RaceDay - Database Schema and Seed Data

   ============================================================ */

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

/* ---------- Drop tables if they already exist (clean rebuild) ---------- */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.RouteInfo', 'U') IS NOT NULL DROP TABLE dbo.RouteInfo;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* ============================================================
   TABLE: Users
   ============================================================ */
CREATE TABLE dbo.Users (
    UserID          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    Role            NVARCHAR(20)    NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
    Phone           NVARCHAR(20)    NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT (SYSDATETIME())
);
GO

/* ============================================================
   TABLE: Events
   ============================================================ */
CREATE TABLE dbo.Events (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID     INT             NOT NULL,
    Name            NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(1000)  NULL,
    EventDate       DATETIME2       NOT NULL,
    Province        NVARCHAR(50)    NOT NULL,
    Venue           NVARCHAR(150)   NOT NULL,
    EventType       NVARCHAR(20)    NOT NULL CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    CreatedAt       DATETIME2       NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID) REFERENCES dbo.Users(UserID)
);
GO

/* ============================================================
   TABLE: Categories
   ============================================================ */
CREATE TABLE dbo.Categories (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT             NOT NULL,
    Name            NVARCHAR(50)    NOT NULL,
    DistanceKm      DECIMAL(5,2)    NOT NULL,
    MaxParticipants INT             NOT NULL DEFAULT (500),
    EntryFee        DECIMAL(8,2)    NOT NULL DEFAULT (0.00),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID) ON DELETE CASCADE
);
GO

/* ============================================================
   TABLE: RouteInfo  (one route per category)
   ============================================================ */
CREATE TABLE dbo.RouteInfo (
    RouteID         INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID      INT             NOT NULL UNIQUE,
    RouteMapURL     NVARCHAR(255)   NULL,
    StartPoint      NVARCHAR(150)   NOT NULL,
    EndPoint        NVARCHAR(150)   NOT NULL,
    ElevationGainM  INT             NULL DEFAULT (0),
    Description     NVARCHAR(1000)  NULL,
    CONSTRAINT FK_RouteInfo_Categories FOREIGN KEY (CategoryID) REFERENCES dbo.Categories(CategoryID) ON DELETE CASCADE
);
GO

/* ============================================================
   TABLE: Enrolments
   ============================================================ */
CREATE TABLE dbo.Enrolments (
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID   INT             NOT NULL,
    CategoryID      INT             NOT NULL,
    EnrolmentDate   DATETIME2       NOT NULL DEFAULT (SYSDATETIME()),
    RaceNumber      NVARCHAR(10)    NULL,
    Status          NVARCHAR(20)    NOT NULL DEFAULT ('Pending') CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) REFERENCES dbo.Categories(CategoryID),
    CONSTRAINT UQ_Enrolment_Once UNIQUE (ParticipantID, CategoryID)
);
GO

/* ============================================================
   TABLE: Results  (one result per enrolment)
   ============================================================ */
CREATE TABLE dbo.Results (
    ResultID                INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID             INT             NOT NULL UNIQUE,
    FinishTime              TIME            NULL,
    Position                INT             NULL,
    Status                  NVARCHAR(20)    NOT NULL DEFAULT ('Finished') CHECK (Status IN ('Finished', 'DNF', 'DNS')),
    CapturedByOrganiserID   INT             NOT NULL,
    CapturedAt              DATETIME2       NOT NULL DEFAULT (SYSDATETIME()),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID) ON DELETE CASCADE,
    CONSTRAINT FK_Results_Organiser FOREIGN KEY (CapturedByOrganiserID) REFERENCES dbo.Users(UserID)
);
GO

/* ============================================================
   SEED DATA
   ============================================================ */

-- Organisers (2) and Participants (2)
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, Phone) VALUES
('Thandiwe Mokoena', 'thandiwe@raceday.co.za', 'HASHED_PWD_1', 'Organiser', '0821234567'),
('Johan van der Merwe', 'johan@raceday.co.za', 'HASHED_PWD_2', 'Organiser', '0837654321'),
('Lerato Dlamini', 'lerato@example.com', 'HASHED_PWD_3', 'Participant', '0731112222'),
('Sipho Nkosi', 'sipho@example.com', 'HASHED_PWD_4', 'Participant', '0729998888');
GO

-- Events (3)
INSERT INTO dbo.Events (OrganiserID, Name, Description, EventDate, Province, Venue, EventType) VALUES
(1, 'Comrades Marathon', 'Iconic ultramarathon between Pietermaritzburg and Durban.', '2027-06-13 05:30:00', 'KwaZulu-Natal', 'Pietermaritzburg City Hall', 'Run'),
(1, 'Cape Town Cycle Tour', 'Scenic cycling tour around the Cape Peninsula.', '2027-03-08 06:00:00', 'Western Cape', 'Green Point Common', 'Cycle'),
(2, 'Soweto Community Fun Walk', 'Community charity walk supporting local schools.', '2026-11-21 07:00:00', 'Gauteng', 'Soweto Theatre', 'Walk');
GO

-- Categories (across the 3 events)
INSERT INTO dbo.Categories (EventID, Name, DistanceKm, MaxParticipants, EntryFee) VALUES
(1, '87km Ultra', 87.00, 20000, 950.00),
(1, '10km Fun Run', 10.00, 5000, 250.00),
(2, '109km Cycle Race', 109.00, 15000, 750.00),
(2, '35km Mini Tour', 35.00, 6000, 400.00),
(3, '5km Fun Walk', 5.00, 2000, 100.00);
GO

-- Route info for a couple of categories
INSERT INTO dbo.RouteInfo (CategoryID, RouteMapURL, StartPoint, EndPoint, ElevationGainM, Description) VALUES
(1, 'https://maps.example.com/comrades-87km', 'Pietermaritzburg City Hall', 'Kingsmead Stadium, Durban', 1200, 'Classic "down run" route with rolling hills.'),
(3, 'https://maps.example.com/cycle-tour-109km', 'Green Point Common', 'Green Point Common', 850, 'Full Peninsula loop via Chapmans Peak.');
GO

-- Enrolments (participants entering categories)
INSERT INTO dbo.Enrolments (ParticipantID, CategoryID, RaceNumber, Status) VALUES
(3, 1, 'A1023', 'Confirmed'),
(4, 3, 'B2041', 'Confirmed'),
(3, 5, 'C3070', 'Pending');
GO

-- Sample results captured by an organiser
INSERT INTO dbo.Results (EnrolmentID, FinishTime, Position, Status, CapturedByOrganiserID) VALUES
(1, '08:32:15', 4521, 'Finished', 1),
(2, '03:45:02', 812, 'Finished', 1);
GO
