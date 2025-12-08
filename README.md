# TuringMachinesAPI

Backend API for a Turing Machine sandbox game, providing:

- Player registration, authentication and JWT-based sessions
- Public workshop for sharing levels and machines
- Multiplayer lobbies with real-time collaboration over SignalR
- Discord webhooks for basic notifications
- Tested, container-friendly setup using PostgreSQL and GitHub Actions

This repository is the **server** side only. The game client (UI) lives in a separate project.

Game Project: [TuringSandbox](https://github.com/vascoapolinario/Turing-Sandbox-Game)

Website (Made by Gemini) that uses the API: [https://vapoli.tech/#/TuringSandbox/dashboard](https://vapoli.tech/#/TuringSandbox/dashboard)

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Technology Stack](#technology-stack)
- [Core Features](#core-features)
  - [Players](#players)
  - [Workshop Items](#workshop-items)
  - [Leaderboards](#Leaderboards)
  - [Multiplayer Lobbies](#multiplayer-lobbies)
  - [Health Checks](#health-checks)
  - [Admin Logs](#admin-logs)
- [Real-Time Collaboration (SignalR)](#real-time-collaboration-signalr)
- [Security & Data Handling](#security--data-handling)
- [Validation & Content Filtering](#validation--content-filtering)
- [HTTP API Summary](#http-api-summary)
- [Cache](#cache)
- [Running Locally](#running-locally)
- [Testing and CI](#testing-and-ci)
- [Deployment Notes](#deployment-notes)

---

## Architecture Overview

The API is an ASP.NET Core 8 Web API that talks to a PostgreSQL database via Entity Framework Core.  
The main responsibilities are split across:

- **Controllers**
  - `PlayersController` – authentication and user management
  - `WorkshopItemController` – All workshop related actions
  - `LeaderboardController` – Leaderboard for player submission of levels
  - `LobbyController` – creation and management of multiplayer lobbies
  - `HealthController` – simple health/keep-alive endpoint
  - `AdminLogs` – controller to track actions like creations, deletions and updates
- **Services**
  - `PlayerService` – business logic around players, credentials and JWT handling
  - `WorkshopItemService` – business logic for level/machine workshop items
  - `LobbyService` – lobby lifecycle and rules (joining, leaving, starting, kicking)
  - `AdminLogsService` – service to manage and create admin logs
  - `DiscordWebhookService` – integrates with Discord for notifications (user created, workshop item created, lobby created)
  - `ICryptoService` / `AesCryptoService` – encryption for sensitive data
- **Real-time hub**
  - `LobbyHub` (SignalR) – syncs Turing machine state, chat and collaboration events between clients in a lobby

The project is designed to be used both in **single-player** (just workshop + auth) and **multiplayer** setups (lobbies + SignalR).

---

## Technology Stack

- **Language / Runtime**
  - .NET 8
  - ASP.NET Core Web API
- **Data**
  - Entity Framework Core
  - PostgreSQL
- **Real-time**
  - ASP.NET Core SignalR
- **Auth**
  - JWT bearer tokens
- **Messaging / Integrations**
  - Discord webhooks
- **Validation / Filtering**
  - Custom `ValidationUtils`
  - Profanity filter for user-generated content from [stephenhaunts/ProfanityDetector](https://github.com/stephenhaunts/ProfanityDetector)
- **Testing & CI**
  - xUnit test project
  - Integration tests against PostgreSQL in GitHub Actions
  - Test result publishing via GitHub Actions

---

## Core Features

### Players

The **Players** module handles registration, login and basic identity:

- Register new players with unique usernames
- Store passwords **encrypted** using an AES-based crypto service
- Issue **JWT tokens** for authenticated requests
- Expose a `/players/verify` endpoint to validate the token and fetch basic user info
- `NonSensitivePlayer` DTOs ensure passwords never leave the server
- Only admins are able to see the list of all players or players by id
- The DateTime of AccountCreation and Last Login are saved in the db and acessed through the get requests (By Admins) Or by the player themselves with the verify GET.

Key endpoints:

| Method | Route              | Description                 | Auth |
|--------|--------------------|-----------------------------|------|
| GET    | `/players`         | List all players            | Yes  |
| GET    | `/players/{id}`    | Get player by id            | Yes  |
| POST   | `/players`         | Register new player         | No   |
| POST   | `/players/login`   | Login, return JWT           | No   |
| GET    | `/players/verify`  | Verify token and return user| Yes  |
| DELETE | `/players/{id}`    | Delete a player account     | Yes  |

### Workshop Items

The **Workshop** module is the public content repository for the game:

- Two main item types:
  - **Level** – includes metadata like objective, mode (accept/transform), alphabet, examples, etc.
  - **Machine** – includes alphabet, nodes and connections as JSON
- Server-side validation for:
  - Disallowed content (profanity, URLs, control characters, etc.)
  - JSON structure and reasonable size limits
- Rating system:
  - Users can rate items 1–5
  - Average rating is computed from reviews
- Subscriptions:
  - Users can subscribe/unsubscribe to items
  - API exposes subscriber counts and whether the current user is subscribed

Selected endpoints:

| Method | Route                                  | Description                              | Auth |
|--------|----------------------------------------|------------------------------------------|------|
| GET    | `/workshop`                            | List items (optional name filter)        | Yes  |
| GET    | `/workshop/{id}`                       | Get full item (level/machine)            | Yes  |
| POST   | `/workshop`                            | Create new item from JSON payload        | Yes  |
| POST   | `/workshop/{id}/rate/{rating}`         | Rate item (1–5)                          | Yes  |
| POST   | `/workshop/{id}/subscribe`             | Toggle subscription                      | Yes  |
| GET    | `/workshop/{id}/subscribed`            | Check if current user is subscribed      | Yes  |
| DELETE | `/workshop/{id}`                       | Delete item (author or admin only)       | Yes  |

Internally, workshop data is represented by separate entity types for levels and machines while sharing a common `WorkshopItem` base for metadata.

### Leaderboards

The **Leaderboard** module tracks best solutions for **official levels** (not arbitrary workshop items):

- `LeaderboardLevels` table defines which levels participate in leaderboards:
  - `Name` – official level name used by the client (e.g. matches `Levels.py`)
  - `Category` – e.g. `Tutorial`, `Starter`, `Medium`, `Hard` or `Workshop`
  - `WorkshopItemId` (optional) – link to a workshop item used for multiplayer when applicable
- `LevelSubmissions` store per-player best performance for a given `LeaderboardLevel`:
  - `Time` – completion time (as sent by the client)
  - `NodeCount` – number of states used
  - `ConnectionCount` – number of transitions used

The leaderboard API supports:

- **Global leaderboard** for a level or all tracked levels
- **Per-player leaderboard view** (filtered to the current user)
- Admin-only ability to register new leaderboard levels
- Admin-only ability to delete player submissions

The current server-side implementation trusts the metrics sent by the client and is meant primarily for friendly competition rather than anti-cheat-grade security.

Selected endpoints:

| Method | Route                 | Description                                                       | Auth |
|--------|-----------------------|-------------------------------------------------------------------|------|
| GET    | `/leaderboard`        | Get leaderboard entries (global or per-player)                    | Yes  |
| GET    | `/leaderboard/levels` | Get the list of leaderboard levels                                | Yes  |
| POST   | `/leaderboard`        | Submit a new result for the current user                          | Yes  |
| POST   | `/leaderboard/level`  | Register a new leaderboard level (name, category, workshop link)  | Yes (Admin) |
| DELETE | `/leaderboard`        | Delete a player's submission                                      | Yes (Admin) |

#### `GET /leaderboard`

Query parameters:

- `Player` (bool, optional)  
  - `true` → only entries for the current authenticated user  
  - `false` or omitted → global leaderboard
- `levelName` (string, optional)  
  - If provided, filters to a specific official level name

Examples:

- `GET /leaderboard` – all submissions, sorted by time  
- `GET /leaderboard?levelName=Palindrome` – all submissions for “Palindrome”  
- `GET /leaderboard?Player=true` – current user’s best runs

Response: list of `LevelSubmission` DTOs:

```json
[
  {
    "levelName": "Palindrome",
    "playerName": "Alice",
    "time": 12.34,
    "nodeCount": 6,
    "connectionCount": 10
  }
]
```

### Multiplayer Lobbies

The **Lobby** module powers multiplayer sessions:

- Create password-protected or open lobbies
- Limit lobby size and enforce min/max players
- Host and players tracked via IDs + claims
- Lobbies can be started, joined, left and deleted
- Host/admin can kick players
- Integration with Discord webhooks for lobby announcements
- Integration with SignalR (`LobbyHub`) for real-time collaboration

Selected endpoints:

| Method | Route                                     | Description                                        | Auth |
|--------|-------------------------------------------|----------------------------------------------------|------|
| GET    | `/lobbies`                                | List lobbies (filter by code, include started)     | Yes  |
| GET    | `/lobbies/{code}`                         | Get lobby details by code                          | Yes  |
| POST   | `/lobbies`                                | Create lobby (host = current user)                 | Yes  |
| POST   | `/lobbies/{code}/join`                    | Join lobby (optional password)                     | Yes  |
| POST   | `/lobbies/{code}/leave`                   | Leave lobby                                        | Yes  |
| POST   | `/lobbies/{code}/start`                   | Start lobby (host only, valid player count)        | Yes  |
| DELETE | `/lobbies/{code}`                         | Delete lobby (host or admin)                       | Yes  |
| POST   | `/lobbies/{code}/kick/{targetPlayerName}` | Kick a player (host only)                          | Yes  |

The `LobbyService` enforces all business rules around ownership, permissions and player counts.

### Health Checks

A minimal health endpoint is used both for uptime monitoring and for platforms like Render:

Status page for the service is available at: [Turing Machine Status](https://stats.uptimerobot.com/gJ9feqIKwK)
```csharp
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [HttpHead]
    public IActionResult Get()
    {
        return Ok(new { status = "Healthy" });
    }
}
```
- **Route:** `GET /health`  
- **Response:** simple JSON: `{ "status": "Healthy" }`

If you host your own instance, you can point an uptime monitor at this endpoint and/or expose your own status page.

### Admin Logs

The **Admin Logs** module records important actions performed across the API, so you can see **who did what, to which entity, and when**.

Each log entry stores:

- **Actor** – the player who performed the action (name + role)
- **Action** – a typed action such as `Create`, `Update`, `Delete`, etc. (`ActionType` enum)
- **Target entity** – type + id of the affected entity (`TargetEntityType`, `TargetEntityId`)
  - Examples: `Player`, `Lobby`, `WorkshopLevel`, `WorkshopMachine`, `LeaderboardLevel`, `LeaderboardSubmission`
- **TargetEntityName** – a human-friendly name resolved from the database (e.g. player username, lobby name, workshop item name)
- **DoneAt** – UTC timestamp of when the action occurred

Logs are created server-side via the `AdminLogService` whenever an operation is performed (for example: deleting a player, removing a workshop item, deleting a lobby, etc.). This gives operators of a deployment a simple audit trail for debugging or moderation.

All log endpoints are **admin-only** and guarded by role-based authorization.

Selected endpoints:

| Method | Route                    | Description                                      | Auth      |
|--------|--------------------------|--------------------------------------------------|-----------|
| GET    | `/logs`                  | List all admin logs, newest first               | Yes (Admin) |
| GET    | `/logs/actor/{actorName}` | List logs performed by a specific actor (name) | Yes (Admin) |
| DELETE | `/logs/{id}`             | Delete a single log entry by id                 | Yes (Admin) |
| DELETE | `/logs`                  | Delete all logs or those older than a given timespan | Yes (Admin) |

#### `GET /logs`

Returns a list of admin log DTOs:

```json
[
  {
    "id": 42,
    "actorName": "AdminUser",
    "actorRole": "Admin",
    "action": "Delete",
    "targetEntityType": "WorkshopLevel",
    "targetEntityId": 17,
    "targetEntityName": "Unary Increment Level",
    "doneAt": "2025-11-25T10:23:45Z"
  }
]
```
#### `GET /logs/actor/{actorName}`

Filters the same structure by actor username, making it easy to inspect everything a specific admin account has done.

#### `DELETE /logs/{id}`

Removes a single log entry by id. Intended for cleanup or correcting mistakes in the audit trail when necessary.

#### `DELETE /logs?timeSpan=…`

Deletes logs in bulk. If a `timeSpan` query parameter is provided (e.g. `7.00:00:00` for 7 days), only logs **older** than that span are removed; otherwise, all logs are deleted. This can be used to implement a log retention policy for self-hosted deployments.

---

## Real-Time Collaboration (SignalR)

The `LobbyHub` is a SignalR hub that coordinates real-time state between clients in the same lobby.  
It is responsible for:

- Tracking connections and disconnections  
- Automatically removing players from lobbies when they disconnect  
- Deleting the lobby when the host disconnects  
- Broadcasting collaboration events such as:
  - `EnvironmentSynced` – full Turing machine state sync
  - `NodeProposed` – a user proposes adding a node
  - `ConnectionProposed` – a user proposes a connection
  - `DeleteProposed` – a user proposes deleting nodes or connections
  - `ChatMessageReceived` / `ChatRejected` – in-lobby chat

Payloads are validated and sanitized where appropriate, and message broadcasting is restricted to the specific lobby group via:

- `Groups.AddToGroupAsync(connectionId, lobbyCode)`  
- `Groups.RemoveFromGroupAsync(connectionId, lobbyCode)`

Typical client flow:

1. Authenticate and obtain a JWT token  
2. Join a lobby and then join the SignalR group for the lobby code  
3. Exchange real-time events while building or running Turing machines together

---

## Security & Data Handling

The API is intended to be deployed with the following security properties.

### Encrypted passwords

Player passwords are never stored in clear text.  
The project uses an `ICryptoService` implementation (e.g. `AesCryptoService`) to **encrypt credentials at rest** using a key and salt supplied via configuration:

- Locally: `appsettings.Development.LocalMachine.json` (not committed)  
- Production: environment variables

Example: 
```json
{
  "Crypto": {
    "Key": "REPLACE_WITH_SECURE_KEY",
    "Salt": "REPLACE_WITH_SECURE_SALT"
  }
}
```

### JWT authentication

- Login issues a signed JWT with user id, username and role in the claims.  
- Protected endpoints use `[Authorize]` and extract the `id` claim to enforce per-user logic (ownership, permissions, etc.).  
- Secrets for signing tokens are not checked into source control and should be provided via environment variables, user-secrets, or through local appsettings.

Example:
```json
{
  "Jwt": {
    "Key": "REPLACE_WITH_LONG_RANDOM_BASE64_SECRET",
    "ExpireHours": 1
  }
}
```


### Roles and permissions

- Basic roles: e.g. `User`, `Admin` 
- Certain operations (e.g. deleting workshop items or lobbies) require either **ownership** or **admin** role.

### Configuration

- Secrets (JWT signing key, AES key/salt, Discord webhook URLs, etc.) must be provided via local secrets or environment variables.  
- Default configuration files in the repository are kept free of sensitive values.

---

## Validation & Content Filtering

User-generated content is validated on the server side through `ValidationUtils` and other checks.

### Disallowed content

Input is rejected if it contains:

- URLs and domains (`https://`, `www.`, `.com`, `.net`, `.org`, etc.)  
- Unexpected characters outside a controlled set (letters, digits, space, and a limited set of punctuation)  
- Control characters (except standard newlines)  
- Profanity (via the `ProfanityFilter` library)

### JSON validation

- Methods like `IsValidJson` verify that serialized alphabets, node lists and connection sets are valid JSON and within reasonable size limits.

These checks are applied to:

- Workshop item names and descriptions  
- Level/machine JSON fields (alphabet, nodes, connections, examples)  
- Lobby names
- In-lobby chat messages (chat can be rejected server-side if it contains disallowed content)

This is not a full moderation system, but it reduces obvious spam and harmful content before it is stored or broadcast.

---

## HTTP API Summary

A condensed view of the API surface:

| Area       | Controller               | Base Route     |
|------------|--------------------------|----------------|
| Health     | `HealthController`       | `/health`      |
| Players    | `PlayersController`      | `/players`     |
| Workshop   | `WorkshopItemController` | `/workshop`    |
| Lobbies    | `LobbyController`        | `/lobbies`     |
| Real-time  | `LobbyHub` (SignalR)     | `/hubs/lobby`* |
| Leaderboard | `LeaderboardController` | `/leaderboard` |
| AdminLogs  | `AdminLogsController`    | `/logs`        |

\* The exact hub route depends on the SignalR configuration in `Program.cs` / `Startup`.

For detailed request/response shapes, see the DTO definitions (the `Dtos` folder) and XML comments in the controllers.  
The project can also be wired up with Swagger/Swashbuckle for interactive API docs if desired.

---

## Cache
To drastically reduce database load and improve response times, the API uses IMemoryCache to store frequently accessed data across multiple services.

Before caching, some endpoints could trigger hundreds or thousands of SQL queries per request, especially when iterating workshop items and looking up authors, ratings or subscriptions.~

### What gets cached?

| Cache Key           | Contents                        | Used In                       |
| ------------------- | ------------------------------- | ----------------------------- |
| `WorkshopItems`     | All workshop DTOs               | Workshop APIs                 |
| `LastPlayerGetId`   | Previously requested user id    | User-specific metadata reuse  |
| `Players`           | All player DTOs                 | Name resolution, Playerservice|
| `Leaderboard`       | All LevelSubmission DTOs        | LeaderboardService            |
| `LeaderboardLevels` | All LeaderboardLevel DTOs       | LeaderboardService            |
| `AdminLogs`         | All AdminLog DTOs               | AdminLogService               |

Caches are built at the launch of the API, unless there are no players.

The tests project does not build the cache before running.

---

## Running Locally

### Requirements

- .NET 8 SDK installed  
- PostgreSQL running locally

### Database

1. Create a database (e.g. `TuringDB`).  
2. Set the connection string in `appsettings.Development.json` or via environment variable:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=TuringDB;Username=...;Password=..."
}
```

Run the API: ```dotnet run --project src/Project/TuringMachinesAPI.csproj ```

## Testing and CI
The repository includes an xUnit test project focusing on service-level behavior.

### Test coverage
Currently the project has tests for:
- PlayerServiceTests
- WorkshopItemServiceTests
- LobbyServiceTests
- AdminLogService
- LeaderboardService

### GitHub Actions
Tests are run in GitHub Actions using:
- A PostgreSQL 16 service container
- A dedicated test database (TuringDBTests)
- Database migrations + SQL seed scripts
- dotnet test with TRX results

## Deployment Notes

The API is designed to be hosted on platforms like:
- Render.com (current host)
- Azure App Service
- Any container-friendly environment (Docker, Kubernetes, etc.)

Typical deployment concerns:

- Provide all secrets (JWT key, AES key/salt, Discord webhooks, DB connection string) as environment variables.
- Configure an uptime monitor to call /health.

If you expose the API publicly, pair it with a trusted game client to avoid misuse of endpoints.

If you self-host this API and allow public user-generated content, moderation and legal responsibility remain with the operator of that instance.

