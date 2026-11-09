# RaceDay – PROG6212 POE Part 3 (MVC web app, Azure Blob Storage, Docker)

**Student:** Baarabile Faith Ndlalane (ST10477929)
**Video demo (unlisted YouTube):** _<paste link here>_

[![CI](https://github.com/<your-username>/<your-repo>/actions/workflows/ci.yml/badge.svg)](https://github.com/<your-username>/<your-repo>/actions)

RaceDay lets Organisers publish running, walking and cycling events and lets Participants enter them.
The **ASP.NET Core MVC** front end (`RaceDay.Web`) contains no database code: every operation goes through the **Part 2 REST API** (`RaceDay.Api`).
Event banners and profile pictures are uploaded to the API, which stores them in **Azure Blob Storage**.

## Features
| Role | Pages |
|---|---|
| Public | Upcoming events with filters, event details with categories and results, register, log in |
| Participant | Enter an event (pick category), My Events with colour-coded status and cancel, My Results (time and position e.g. "47th out of 312 finishers"), Profile with picture upload |
| Organiser | Dashboard (events, enrolments per event, upcoming dates), create/edit/delete events with banner upload, categories, enrolments, results entry |

Session-based authentication: the MVC app keeps the user's ID and role in its session and forwards the API's session cookie on every call. Unauthenticated users are redirected to Log in; wrong-role users to Access Denied.

## Project structure
```
RaceDay.sln
RaceDay.Api/      Part 2 API + Blob Storage upload endpoints
RaceDay.Tests/    xUnit tests
RaceDay.Web/      ASP.NET Core MVC (Controllers, Views, Services/ApiClient, Filters, Dockerfile)
docker-compose.yml
.github/workflows/ci.yml
```

## Run locally
1. SQL Server LocalDB, .NET 8 SDK.
2. Configure Azure Blob Storage for the API (never commit the key):
   ```
   cd RaceDay.Api
   dotnet user-secrets init
   dotnet user-secrets set "AzureBlob:ConnectionString" "<storage account connection string>"
   ```
   In the storage account enable "Allow Blob anonymous access" (the container `raceday-images` is created automatically with public blob read).
3. Add the new columns once: run `schema-update.sql` against `RaceDayDB` (or drop the database and let the API recreate it).
4. Start the API: `dotnet run --project RaceDay.Api --launch-profile http` (http://localhost:5080).
5. Start the web app: `dotnet run --project RaceDay.Web`, then register an Organiser and a Participant.

## Run with Docker (single command)
Start the API so the container can reach it:
```
dotnet run --project RaceDay.Api --urls http://0.0.0.0:5080
```
Then, from the repository root:
```
docker compose up --build
```
or without compose:
```
docker build -t raceday-web RaceDay.Web
docker run --rm -p 8080:8080 -e ApiBaseUrl=http://host.docker.internal:5080/ raceday-web
```
Open http://localhost:8080. The containerised app behaves the same as running locally; `ApiBaseUrl` tells it where the API is (use the deployed API URL if the API is hosted in Azure).

## CI/CD
`.github/workflows/ci.yml` restores, builds the whole solution (API, tests and MVC), runs the tests, checks the repository structure and builds the Docker image on every push and pull request.

_Screenshot of a green build:_ ![Green build](docs/ci-green-build.png)

