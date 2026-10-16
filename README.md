# RaceDay API

![CI](https://github.com/st10477929/RaceDay/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![Database](https://img.shields.io/badge/SQL%20Server-LocalDB-CC2927)

A RESTful API for managing running, walking and cycling events. Organisers create events and categories and capture results. Participants enrol and view their results.

Built for **PROG6212 – POE Part 2** with ASP.NET Core Web API (C#), Entity Framework Core (code-first) and SQL Server.

| | |
|---|---|
| **Student** | Baarabile Faith Ndlalane (ST10477929) |
| **Video demo** | https://youtu.be/txvRwZWfWS4?feature=shared |
| **Repository** | https://github.com/st10477929/RaceDay |

---

## Table of contents

1. [Overview](#1-overview)
2. [Tech stack](#2-tech-stack)
3. [Getting started](#3-getting-started)
4. [Using the API in Swagger](#4-using-the-api-in-swagger)
5. [Roles and security](#5-roles-and-security)
6. [Endpoints](#6-endpoints)
7. [Database](#7-database)
8. [Unit tests](#8-unit-tests)
9. [CI/CD](#9-cicd)
10. [Project structure](#10-project-structure)
11. [Changes from the Part 1 plan](#11-changes-from-the-part-1-plan)

---

## 1. Overview

RaceDay supports two kinds of user, **Organiser** and **Participant**, and a public area that anyone can view.

- **Organisers** create events, define categories (age or distance) for their events, view enrolments and capture results.
- **Participants** register, enrol in categories, cancel enrolments and view their own results.
- **Anyone** can view events, categories and event leaderboards.

## 2. Tech stack

| Area | Technology |
|---|---|
| Framework | ASP.NET Core Web API, .NET 8 |
| Data access | Entity Framework Core (code-first) |
| Database | SQL Server (LocalDB by default) |
| Authentication | Server-side session (cookie), BCrypt password hashing |
| Documentation | Swagger UI (Swashbuckle, XML doc comments) |
| Testing | xUnit with EF Core InMemory |
| CI | GitHub Actions |

## 3. Getting started

### Prerequisites

- Visual Studio 2022 with the **ASP.NET and web development** workload
- .NET 8 SDK (`dotnet --version` should start with `8.`)
- SQL Server LocalDB (installed with Visual Studio), SQL Server Express or Developer

### Setup and run

1. Clone the repository and open `RaceDay.sln` in Visual Studio.
2. Check the connection string in `RaceDay.Api/appsettings.json`:

   ```json
   "ConnectionStrings": {
     "RaceDayDb": "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```

   If you use SQL Server Express, change the server to `.\\SQLEXPRESS`.
3. Right-click **RaceDay.Api** in Solution Explorer and choose **Set as Startup Project**.
4. Choose the **https** profile and press **F5** (or run `dotnet run --project RaceDay.Api`).
5. Swagger UI opens at `https://localhost:7080/swagger`.

### Database creation

The database is created automatically the first time the API starts. If migrations exist they are applied, otherwise the schema is created directly. The `Role` table is seeded with `Organiser` and `Participant`.

To use migrations instead, run in the Package Manager Console:

```
Add-Migration InitialCreate -Project RaceDay.Api -StartupProject RaceDay.Api
Update-Database -Project RaceDay.Api -StartupProject RaceDay.Api
```

> The sample users in the Part 1 SQL script contain placeholder passwords, not real hashes. Create users through `POST /api/auth/register` instead.

## 4. Using the API in Swagger

Authentication is **session based**, not JWT.

1. **Register** with `POST /api/auth/register`. Create one Organiser and one Participant.

   ```json
   {
     "fullName": "Olivia Organiser",
     "email": "organiser@raceday.com",
     "password": "Password123",
     "role": "Organiser",
     "phoneNumber": "0821234567"
   }
   ```

   Rules: password of at least 8 characters, role exactly `Organiser` or `Participant`, valid email, and a valid phone number or none at all.
2. **Log in** with `POST /api/auth/login`. The server stores the user and role in the session and sets a cookie. Swagger sends the cookie automatically on every request after that.
3. **Call protected endpoints** with "Try it out".
4. **Switch role** with `POST /api/auth/logout`, then log in as the other user.

## 5. Roles and security

| Role | Permissions |
|---|---|
| **Organiser** | Create, update and delete **their own** events. Manage categories for their events. View enrolments for their events. Capture and update results for their events. View and update own profile. |
| **Participant** | Enrol in a category. View and cancel own enrolments. View own results. View and update own profile. |
| **Public** | View events, categories and leaderboards. Register and log in. |

Rules enforced on the server by a `[RequireRole]` authorisation filter:

| Situation | Response |
|---|---|
| No session | `401 Unauthorized` |
| Logged in with the wrong role | `403 Forbidden` |
| Organiser modifies another organiser's data | `403 Forbidden` |
| Invalid input or invalid role at registration | `400 Bad Request` |
| Email already registered | `409 Conflict` |

Passwords are hashed with BCrypt and are never returned by the API.

## 6. Endpoints

### Auth

| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register as Organiser or Participant |
| POST | `/api/auth/login` | Public | Log in and start a session |
| POST | `/api/auth/logout` | Public | Clear the session |

### Users

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/api/users/me` | Any logged in user | View own profile |
| PUT | `/api/users/me` | Any logged in user | Update own profile |

### Events

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/api/events` | Public | List events (`?upcomingOnly=true` optional) |
| GET | `/api/events/{id}` | Public | Event details |
| POST | `/api/events` | Organiser | Create an event |
| PUT | `/api/events/{id}` | Organiser (owner) | Update an event |
| DELETE | `/api/events/{id}` | Organiser (owner) | Delete an event |

### Categories

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Public | List categories of an event |
| POST | `/api/events/{eventId}/categories` | Organiser (owner) | Add a category |
| PUT | `/api/categories/{id}` | Organiser (owner) | Update a category |
| DELETE | `/api/categories/{id}` | Organiser (owner) | Delete a category |

### Enrolments

| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | `/api/categories/{categoryId}/enrolments` | Participant | Enrol in a category |
| GET | `/api/enrolments/me` | Participant | View own enrolments |
| GET | `/api/events/{eventId}/enrolments` | Organiser (owner) | View all enrolments for an event |
| DELETE | `/api/enrolments/{id}` | Participant (owner) | Cancel own enrolment |

### Results

| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | `/api/enrolments/{enrolmentId}/results` | Organiser (owner) | Capture finish time and position |
| PUT | `/api/results/{id}` | Organiser (owner) | Update a result |
| GET | `/api/results/me` | Participant | View own results |
| GET | `/api/events/{eventId}/results` | Public | Event leaderboard |

## 7. Database

Code-first EF Core model matching the Part 1 ERD and SQL script.

| Table | Notes |
|---|---|
| `Role` | Seeded with `Organiser` and `Participant`. `RoleName` is unique. |
| `User` | `Email` is unique. Linked to a role. |
| `Event` | Owned by an Organiser. |
| `Category` | Belongs to an event. |
| `Enrolment` | Unique on `(ParticipantId, CategoryId)`. |
| `Result` | One per enrolment (`EnrolmentId` is unique). |

Deletes do not cascade. Deleting an event or category that already has enrolments returns `409 Conflict`.

## 8. Unit tests

Tests use xUnit with the EF Core InMemory provider (`RaceDay.Tests`). Each area has success and failure scenarios:

- Registration and login
- Event management
- Role enforcement (401, 403 and `[RequireRole]` on every protected endpoint)
- Categories
- Enrolments
- Results
- Profile

Run them in Visual Studio with **Test → Run All Tests**, or from the command line:

```
dotnet test RaceDay.sln
```

## 9. CI/CD

The workflow in `.github/workflows/ci.yml` runs on every push and pull request. It restores, builds and runs all unit tests with .NET 8.

Screenshot of a successful run:

![Green build](docs/ci-green-build.png)

## 10. Project structure

```
RaceDay/
├── RaceDay.sln
├── README.md
├── .gitignore
├── .github/workflows/ci.yml
├── RaceDay.Api/
│   ├── Controllers/   Auth, Users, Events, Categories, Enrolments, Results
│   ├── Models/        EF Core entities
│   ├── Data/          RaceDayDbContext
│   ├── Dtos/          Request and response models
│   ├── Auth/          [RequireRole] session filter and role names
│   ├── Services/      Password hashing
│   └── Program.cs     Session, EF Core and Swagger set-up
└── RaceDay.Tests/     xUnit tests
```

Part 1 documents (ERD, SQL script and the endpoint plan) are kept in the repository root.

## 11. Changes from the Part 1 plan

- Login uses a **server-side session** instead of returning a JWT, as the Part 2 brief requires. `POST /api/auth/logout` was added.
- The result routes were corrected: update is `PUT`, the leaderboard is `GET`, and results are captured per enrolment.
- Enrolment routes were standardised to the plural `/api/enrolments`.
- `GET /api/events` returns all events by default, because results are captured after an event has run. Use `?upcomingOnly=true` for upcoming events only.
- Deleting an event or category with enrolments returns `409 Conflict`.
