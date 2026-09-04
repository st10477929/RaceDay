
-- Switches to the master database so that the RaceDayDB database
-- can be safely created or removed.
USE master;
GO

-- Checks whether RaceDayDB already exists before attempting to create it.
IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    -- Changes the database to single-user mode and immediately
    -- terminates existing connections to allow the database to be dropped.
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

    -- Deletes the existing RaceDayDB database so the script can
    -- recreate a clean and consistent database environment.
    DROP DATABASE RaceDayDB;
END
GO

-- Creates a new database called RaceDayDB.
CREATE DATABASE RaceDayDB;
GO

-- Selects RaceDayDB as the active database for the remaining commands.
USE RaceDayDB;
GO

-- Creates the Role table to store the different types of users
-- who can access the RaceDay system.
CREATE TABLE dbo.Role (
    RoleId      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    NVARCHAR(50) NOT NULL 
);

-- Creates the User table to store organiser and participant information.
CREATE TABLE dbo.[User] (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    RoleId          INT NOT NULL,
    FullName        NVARCHAR(100) NOT NULL,
    Email           NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255) NOT NULL,
    PhoneNumber     NVARCHAR(20) NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),

    -- Links each user to a valid role stored in the Role table.
    CONSTRAINT FK_User_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role(RoleId)
);

-- Creates the Event table to store information about upcoming
-- running, cycling and walking events.
CREATE TABLE dbo.Event (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId     INT NOT NULL,
    EventName       NVARCHAR(150) NOT NULL,
    Description     NVARCHAR(MAX) NULL,
    EventDate       DATE NOT NULL,
    Location        NVARCHAR(150) NOT NULL,
    EventType       NVARCHAR(20) NOT NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),

    -- Connects each event to the organiser responsible for the event.
    CONSTRAINT FK_Event_Organiser FOREIGN KEY (OrganiserId) REFERENCES dbo.[User](UserId)
);

-- Creates the Category table to define the different race categories
-- available within each event.
CREATE TABLE dbo.Category (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT NOT NULL,
    CategoryName    NVARCHAR(50) NOT NULL,
    Distance        DECIMAL(5,2) NOT NULL,
    MaxParticipants INT NOT NULL DEFAULT 100,
    EntryFee        DECIMAL(10,2) NOT NULL DEFAULT 0,

    -- Ensures that every category belongs to an existing event.
    CONSTRAINT FK_Category_Event FOREIGN KEY (EventId) REFERENCES dbo.Event(EventId)
);

-- Creates the Enrolment table to record participants who register
-- for specific event categories.
CREATE TABLE dbo.Enrolment (
    EnrolmentId     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId   INT NOT NULL,
    CategoryId      INT NOT NULL,
    EnrolmentDate   DATETIME NOT NULL DEFAULT GETDATE(),
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Confirmed',

    -- Links an enrolment to the participant who registered.
    CONSTRAINT FK_Enrolment_Participant FOREIGN KEY (ParticipantId) REFERENCES dbo.[User](UserId),

    -- Links the enrolment to the selected race category.
    CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Category(CategoryId),

    -- Prevents the same participant from registering for the same
    -- category more than once.
    CONSTRAINT UQ_Enrolment_ParticipantCategory UNIQUE (ParticipantId, CategoryId)
);

-- Creates the Result table to store participants' finishing times
-- and positions after completing a race.
CREATE TABLE dbo.Result (
    ResultId                INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId             INT NOT NULL,
    FinishTime              TIME NULL,
    Position                INT NOT NULL,
    CapturedByOrganiserId   INT NOT NULL,
    CapturedAt              DATETIME NOT NULL DEFAULT GETDATE(),

    -- Connects each result to the participant's enrolment.
    CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentId) REFERENCES dbo.Enrolment(EnrolmentId),

    -- Records which organiser captured the participant's result.
    CONSTRAINT FK_Result_Organiser FOREIGN KEY (CapturedByOrganiserId) REFERENCES dbo.[User](UserId)
);
GO

-- Inserts the two available user roles into the Role table.
INSERT INTO dbo.Role (RoleName) VALUES ('Organiser'), ('Participant');
GO

-- Inserts sample organiser accounts into the User table.
INSERT INTO dbo.[User] (RoleId, FullName, Email, PasswordHash, PhoneNumber)
VALUES
((SELECT RoleId FROM dbo.Role WHERE RoleName = 'Organiser'), 'Thabo Nkosi', 'thabo.nkosi@raceday.co.za', 'HashedPassword123!', '0821234567'),
((SELECT RoleId FROM dbo.Role WHERE RoleName = 'Organiser'), 'Lindiwe Dlamini', 'lindiwe.dlamini@raceday.co.za', 'HashedPassword123!', '0827654321');
GO

-- Inserts sample participant accounts into the User table.
INSERT INTO dbo.[User] (RoleId, FullName, Email, PasswordHash, PhoneNumber)
VALUES
((SELECT RoleId FROM dbo.Role WHERE RoleName = 'Participant'), 'Sipho Mokoena', 'sipho.mokoena@gmail.com', 'HashedPassword123!', '0731112222'),
((SELECT RoleId FROM dbo.Role WHERE RoleName = 'Participant'), 'Amanda van Wyk', 'amanda.vanwyk@gmail.com', 'HashedPassword123!', '0739998888');
GO

-- Inserts sample events and assigns each event to an organiser.
INSERT INTO dbo.Event (OrganiserId, EventName, Description, EventDate, Location, EventType)
VALUES
((SELECT UserId FROM dbo.[User] WHERE Email = 'thabo.nkosi@raceday.co.za'), 'Joburg City 10K', 'Annual road race through Johannesburg CBD.', '2026-11-08', 'Johannesburg, Gauteng', 'Run'),
((SELECT UserId FROM dbo.[User] WHERE Email = 'thabo.nkosi@raceday.co.za'), 'Cape Coastal Cycle Challenge', 'Scenic coastal cycling event.', '2026-12-05', 'Cape Town, Western Cape', 'Cycle'),
((SELECT UserId FROM dbo.[User] WHERE Email = 'lindiwe.dlamini@raceday.co.za'), 'Durban Beachfront Fun Walk', 'Family-friendly fun walk along the beachfront.', '2026-10-18', 'Durban, KwaZulu-Natal', 'Walk');
GO

-- Adds different participation categories to each event,
-- including the race distance, participant limit and entry fee.
INSERT INTO dbo.Category (EventId, CategoryName, Distance, MaxParticipants, EntryFee)
VALUES
((SELECT EventId FROM dbo.Event WHERE EventName = 'Joburg City 10K'), '5km', 5.0, 200, 100.00),
((SELECT EventId FROM dbo.Event WHERE EventName = 'Joburg City 10K'), '10km', 10.0, 300, 150.00),
((SELECT EventId FROM dbo.Event WHERE EventName = 'Cape Coastal Cycle Challenge'), '40km', 40.0, 150, 250.00),
((SELECT EventId FROM dbo.Event WHERE EventName = 'Cape Coastal Cycle Challenge'), '80km', 80.0, 100, 350.00),
((SELECT EventId FROM dbo.Event WHERE EventName = 'Durban Beachfront Fun Walk'), '3km Fun Walk', 3.0, 500, 50.00);
GO

-- Creates sample enrolments by connecting participants
-- to the categories they have selected.
INSERT INTO dbo.Enrolment (ParticipantId, CategoryId, Status)
VALUES
((SELECT UserId FROM dbo.[User] WHERE Email = 'sipho.mokoena@gmail.com'),
 (SELECT CategoryId FROM dbo.Category WHERE CategoryName = '10km' AND EventId = (SELECT EventId FROM dbo.Event WHERE EventName = 'Joburg City 10K')),
 'Confirmed'),

((SELECT UserId FROM dbo.[User] WHERE Email = 'amanda.vanwyk@gmail.com'),
 (SELECT CategoryId FROM dbo.Category WHERE CategoryName = '40km' AND EventId = (SELECT EventId FROM dbo.Event WHERE EventName = 'Cape Coastal Cycle Challenge')),
 'Confirmed'),

-- This enrolment demonstrates a participant registration that
-- has not yet been confirmed.
((SELECT UserId FROM dbo.[User] WHERE Email = 'sipho.mokoena@gmail.com'),
 (SELECT CategoryId FROM dbo.Category WHERE CategoryName = '3km Fun Walk' AND EventId = (SELECT EventId FROM dbo.Event WHERE EventName = 'Durban Beachfront Fun Walk')),
 'Pending');
GO

-- Inserts a sample race result for a participant who completed
-- the Joburg City 10K event.
INSERT INTO dbo.Result (EnrolmentId, FinishTime, Position, CapturedByOrganiserId)
VALUES
((SELECT EnrolmentId FROM dbo.Enrolment
  WHERE ParticipantId = (SELECT UserId FROM dbo.[User] WHERE Email = 'sipho.mokoena@gmail.com')
    AND CategoryId = (SELECT CategoryId FROM dbo.Category WHERE CategoryName = '10km' AND EventId = (SELECT EventId FROM dbo.Event WHERE EventName = 'Joburg City 10K'))),

 -- Stores the participant's finishing time as 48 minutes and 35 seconds.
 '00:48:35',

 -- Stores the participant's finishing position in the race.
 12,

 -- Identifies the organiser who captured the participant's result.
 (SELECT UserId FROM dbo.[User] WHERE Email = 'thabo.nkosi@raceday.co.za'));
GO