# RaceDay – API Endpoint Plan

**PROG6122 Part 1 — Section A**
Baarabile Faith Ndlalane
ST10477929

---

## Authentication

| Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates a new user account as organiser or participant | None (public) | `{ fullName, email, password, role }` | 201 Created – user object (no password) <br> 409 Conflict – email already exists |
| POST | `/api/auth/login` | Authenticates a user and returns a JWT | None (public) | `{ email, password }` | 200 OK – `{ token, user }` <br> 401 Unauthorized – invalid credentials |

---

## User Profile

| Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/users/me` | Returns the logged-in user's profile | Any (logged in) | None | 200 OK – user object <br> 401 Unauthorized |
| PUT | `/api/users/me` | Updates the logged-in user's profile details | Any (logged in) | `{ fullName, phoneNumber }` | 200 OK – updated user <br> 400 Bad Request |

---

## Events

| Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | Lists all upcoming events | None (public) | None | 200 OK – array of events |
| GET | `/api/events/{id}` | Returns details of a single event | None (public) | None | 200 OK – event object <br> 404 Not Found |
| POST | `/api/events` | Creates a new event | Organiser | `{ eventName, description, eventDate, location, eventType }` | 201 Created – event object <br> 400 Bad Request |
| PUT | `/api/events/{id}` | Updates an event | Organiser | `{ eventName, description, eventDate, location, eventType }` | 200 OK – updated event <br> 403 Forbidden (not owner) <br> 404 Not Found |
| DELETE | `/api/events/{id}` | Deletes an event | Organiser | None | 204 No Content <br> 403 Forbidden <br> 404 Not Found |

---

## Categories

| Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Lists categories for a given event | None (public) | None | 200 OK – array of categories |
| POST | `/api/events/{eventId}/categories` | Adds a category to an event | Organiser | `{ categoryName, distance, maxParticipants, entryFee }` | 201 Created – category object <br> 404 Not Found (event) |
| PUT | `/api/categories/{id}` | Updates a category | Organiser | `{ categoryName, distance, maxParticipants, entryFee }` | 200 OK – updated category <br> 403 Forbidden <br> 404 Not Found |
| DELETE | `/api/categories/{id}` | Deletes a category | Organiser | None | 204 No Content <br> 403 Forbidden <br> 404 Not Found |

---

## Results

| Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/categories/{id}/results` | Captures a result for a participant's enrolment | Organiser | `{ finishTime, position }` | 201 Created – result object <br> 404 Not Found (enrolment) <br> 409 Conflict (result already exists) |
| GET | `/api/results/{id}` | Updates a captured result | Organiser | `{ finishTime, position }` | 200 OK – updated result <br> 403 Forbidden <br> 404 Not Found |
| GET | `/api/enrolments/{id}` | Lists the logged-in participant's personal results | Participant | None | 200 OK – array of results |
| DELETE | `/api/events/{id}/results` | Returns the full results/leaderboard for an event | None (public) | None | 200 OK – array of results |

> ⚠️ Note: the HTTP methods above for "Updates a captured result" and "Returns the full results/leaderboard" don't match typical REST conventions (an update is usually PUT, and a public leaderboard fetch is usually GET, not DELETE). Recommend reviewing before final submission.

---

## Event Enrolment

| Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/categories/{id}/enrolment` | Enrols the logged-in participant into a category | Participant | None | 201 Created – enrolment object <br> 404 Not Found (category) <br> 409 Conflict (already enrolled) |
| GET | `/api/enrolments/me` | Lists the logged-in participant's own enrolments | Participant | None | 200 OK – array of enrolments <br> 403 Forbidden |
| GET | `/api/events/{id}/enrolment` | Lists all enrolments (all categories) | Organiser | None | 200 OK – array of enrolments <br> 403 Forbidden |
| DELETE | `/api/enrolment/{id}` | Cancels the logged-in participant's own enrolment | Participant | None | 204 No Content <br> 403 Forbidden (not owner) <br> 404 Not Found |
