/* =========================================================
   RaceDay Database Script
   PROG6212 POE Part 1
   =========================================================
   This script is fully self-contained and idempotent:
   it drops and recreates RaceDayDB every time it is run,
   so it can be safely re-run on a clean SQL Server instance.
   ========================================================= */

USE master;
GO

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

/* =========================================================
   TABLES
   ========================================================= */

CREATE TABLE dbo.Role (
    RoleId      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    NVARCHAR(20) NOT NULL UNIQUE
);

CREATE TABLE dbo.[User] (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    RoleId          INT NOT NULL,
    FullName        NVARCHAR(100) NOT NULL,
    Email           NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255) NOT NULL,
    PhoneNumber     NVARCHAR(20) NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_User_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role(RoleId)
);

CREATE TABLE dbo.Event (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId     INT NOT NULL,
    EventName       NVARCHAR(150) NOT NULL,
    Description     NVARCHAR(MAX) NULL,
    EventDate       DATE NOT NULL,
    Location        NVARCHAR(150) NOT NULL,
    EventType       NVARCHAR(20) NOT NULL, -- Run, Walk, Cycle
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Event_Organiser FOREIGN KEY (OrganiserId) REFERENCES dbo.[User](UserId)
);

CREATE TABLE dbo.Category (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT NOT NULL,
    CategoryName    NVARCHAR(50) NOT NULL,
    Distance        DECIMAL(5,2) NOT NULL,
    MaxParticipants INT NOT NULL DEFAULT 100,
    EntryFee        DECIMAL(10,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_Category_Event FOREIGN KEY (EventId) REFERENCES dbo.Event(EventId)
);

CREATE TABLE dbo.Enrolment (
    EnrolmentId     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId   INT NOT NULL,
    CategoryId      INT NOT NULL,
    EnrolmentDate   DATETIME NOT NULL DEFAULT GETDATE(),
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Confirmed', -- Pending, Confirmed
    CONSTRAINT FK_Enrolment_Participant FOREIGN KEY (ParticipantId) REFERENCES dbo.[User](UserId),
    CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Category(CategoryId),
    CONSTRAINT UQ_Enrolment_ParticipantCategory UNIQUE (ParticipantId, CategoryId)
);

CREATE TABLE dbo.Result (
    ResultId                INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId             INT NOT NULL,
    FinishTime              TIME NULL,
    Position                INT NOT NULL,
    CapturedByOrganiserId   INT NOT NULL,
    CapturedAt              DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentId) REFERENCES dbo.Enrolment(EnrolmentId),
    CONSTRAINT FK_Result_Organiser FOREIGN KEY (CapturedByOrganiserId) REFERENCES dbo.[User](UserId)
);
GO

/* =========================================================
   SEED DATA
   ========================================================= */

-- Roles
INSERT INTO dbo.Role (RoleName) VALUES ('Organiser'), ('Participant');
GO

-- Organisers (2)
INSERT INTO dbo.[User] (RoleId, FullName, Email, PasswordHash, PhoneNumber)
VALUES
((SELECT RoleId FROM dbo.Role WHERE RoleName = 'Organiser'), 'Thabo Nkosi', 'thabo.nkosi@raceday.co.za', 'HashedPassword123!', '0821234567'),
((SELECT RoleId FROM dbo.Role WHERE RoleName = 'Organiser'), 'Lindiwe Dlamini', 'lindiwe.dlamini@raceday.co.za', 'HashedPassword123!', '0827654321');
GO

-- Participants (2)
INSERT INTO dbo.[User] (RoleId, FullName, Email, PasswordHash, PhoneNumber)
VALUES
((SELECT RoleId FROM dbo.Role WHERE RoleName = 'Participant'), 'Sipho Mokoena', 'sipho.mokoena@gmail.com', 'HashedPassword123!', '0731112222'),
((SELECT RoleId FROM dbo.Role WHERE RoleName = 'Participant'), 'Amanda van Wyk', 'amanda.vanwyk@gmail.com', 'HashedPassword123!', '0739998888');
GO

-- Events (3)
INSERT INTO dbo.Event (OrganiserId, EventName, Description, EventDate, Location, EventType)
VALUES
((SELECT UserId FROM dbo.[User] WHERE Email = 'thabo.nkosi@raceday.co.za'), 'Joburg City 10K', 'Annual road race through Johannesburg CBD.', '2026-11-08', 'Johannesburg, Gauteng', 'Run'),
((SELECT UserId FROM dbo.[User] WHERE Email = 'thabo.nkosi@raceday.co.za'), 'Cape Coastal Cycle Challenge', 'Scenic coastal cycling event.', '2026-12-05', 'Cape Town, Western Cape', 'Cycle'),
((SELECT UserId FROM dbo.[User] WHERE Email = 'lindiwe.dlamini@raceday.co.za'), 'Durban Beachfront Fun Walk', 'Family-friendly fun walk along the beachfront.', '2026-10-18', 'Durban, KwaZulu-Natal', 'Walk');
GO

-- Categories (per event)
INSERT INTO dbo.Category (EventId, CategoryName, Distance, MaxParticipants, EntryFee)
VALUES
((SELECT EventId FROM dbo.Event WHERE EventName = 'Joburg City 10K'), '5km', 5.0, 200, 100.00),
((SELECT EventId FROM dbo.Event WHERE EventName = 'Joburg City 10K'), '10km', 10.0, 300, 150.00),
((SELECT EventId FROM dbo.Event WHERE EventName = 'Cape Coastal Cycle Challenge'), '40km', 40.0, 150, 250.00),
((SELECT EventId FROM dbo.Event WHERE EventName = 'Cape Coastal Cycle Challenge'), '80km', 80.0, 100, 350.00),
((SELECT EventId FROM dbo.Event WHERE EventName = 'Durban Beachfront Fun Walk'), '3km Fun Walk', 3.0, 500, 50.00);
GO

-- Enrolments (sample)
INSERT INTO dbo.Enrolment (ParticipantId, CategoryId, Status)
VALUES
((SELECT UserId FROM dbo.[User] WHERE Email = 'sipho.mokoena@gmail.com'),
 (SELECT CategoryId FROM dbo.Category WHERE CategoryName = '10km' AND EventId = (SELECT EventId FROM dbo.Event WHERE EventName = 'Joburg City 10K')),
 'Confirmed'),
((SELECT UserId FROM dbo.[User] WHERE Email = 'amanda.vanwyk@gmail.com'),
 (SELECT CategoryId FROM dbo.Category WHERE CategoryName = '40km' AND EventId = (SELECT EventId FROM dbo.Event WHERE EventName = 'Cape Coastal Cycle Challenge')),
 'Confirmed'),
((SELECT UserId FROM dbo.[User] WHERE Email = 'sipho.mokoena@gmail.com'),
 (SELECT CategoryId FROM dbo.Category WHERE CategoryName = '3km Fun Walk' AND EventId = (SELECT EventId FROM dbo.Event WHERE EventName = 'Durban Beachfront Fun Walk')),
 'Pending');
GO

-- Sample Result (for the confirmed 10km enrolment)
INSERT INTO dbo.Result (EnrolmentId, FinishTime, Position, CapturedByOrganiserId)
VALUES
((SELECT EnrolmentId FROM dbo.Enrolment
  WHERE ParticipantId = (SELECT UserId FROM dbo.[User] WHERE Email = 'sipho.mokoena@gmail.com')
    AND CategoryId = (SELECT CategoryId FROM dbo.Category WHERE CategoryName = '10km' AND EventId = (SELECT EventId FROM dbo.Event WHERE EventName = 'Joburg City 10K'))),
 '00:48:35', 12,
 (SELECT UserId FROM dbo.[User] WHERE Email = 'thabo.nkosi@raceday.co.za'));
GO

/* =========================================================
   Quick sanity checks (optional - comment out before submitting if not needed)
   ========================================================= */
-- SELECT * FROM dbo.Role;
-- SELECT * FROM dbo.[User];
-- SELECT * FROM dbo.Event;
-- SELECT * FROM dbo.Category;
-- SELECT * FROM dbo.Enrolment;
-- SELECT * FROM dbo.Result;
