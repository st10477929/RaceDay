# RaceDay Event Management System

## Project Description

RaceDay is a full-stack web-based event management system built for the South African road running, walking and cycling community. Many local events (park runs, charity cycles, community walks) are still managed through paper-based registration, spreadsheets and disconnected communication. RaceDay replaces this with a single platform where Event Organisers can create and manage events, categories and results, and Participants can browse events, enter categories, and track their own enrolments and performance history.

This repository contains **Part 1: System Planning and Database** — the planning phase completed before any application code is written. It includes the data model, the planned RESTful API, and the SQL database script that will be built on in Parts 2 and 3.

## User Roles

### Organiser
- Create, edit and delete events
- Manage event categories
- Capture participant results (finish time and position)
- View enrolments for events they manage

### Participant
- Create an account and log in
- Browse available events
- Enter an event by selecting a category
- View their own enrolments
- Track their own results and performance history

## Part 1 Deliverables

- **ERD** — `docs/RaceDay_ERD.png`
- **API Endpoint Plan** — `docs/RaceDay_API_Endpoint_Plan.md`
- **SQL Database Script** — `docs/RaceDay_Database.sql`

## Repository Structure

```
RaceDay/
│
├── README.md
│
├── docs/
│   ├── RaceDay_ERD.png
│   ├── RaceDay_API_Endpoint_Plan.md
│   └── RaceDay_Database.sql
│
└── .github/
    └── workflows/
        └── part1-ci.yml
```

The `/docs` folder contains all Part 1 planning artefacts: the entity relationship diagram, the full API endpoint plan, and the SQL script used to create and seed the database. The `.github/workflows` folder contains the GitHub Actions workflow that validates this repository structure on every push.

## Database Setup

To run the database script locally:

1. Install SQL Server Express (if not already installed) and SQL Server Management Studio (SSMS).
2. Open SSMS and connect to your local SQL Server instance.
3. Open a **New Query** window.
4. Open `docs/RaceDay_Database.sql`.
5. Execute the full script (F5).
6. Confirm that the `RaceDayDB` database and all six tables (`Role`, `User`, `Event`, `Category`, `Enrolment`, `Result`) were created successfully.
7. Expand the tables and confirm the seed data (2 Organisers, 2 Participants, 3 Events, categories and enrolments) is present.

The script is idempotent — it drops and recreates all tables, so it can be re-run safely on a clean instance.

## CI/CD

This repository uses a GitHub Actions workflow (`.github/workflows/part1-ci.yml`) that runs on every push and pull request. It validates that the required Part 1 repository structure is in place by checking that:

- The `/docs` folder exists
- `docs/RaceDay_ERD.png` exists
- `docs/RaceDay_API_Endpoint_Plan.md` exists
- `docs/RaceDay_Database.sql` exists
- `README.md` exists

**Successful build screenshot:**

_[Insert screenshot of the green GitHub Actions build here before submitting]_

## Video Demonstration

YouTube Link (Unlisted): _[INSERT YOUR YOUTUBE LINK HERE]_

The video walks through the RaceDay ERD, explains the entities, primary/foreign keys and cardinality, covers the API endpoint plan and role-based access decisions, and runs the SQL script live in SSMS to demonstrate that it builds and seeds the database successfully.
