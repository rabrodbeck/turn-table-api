---
title: TurnTable API
emoji: 🎛️
colorFrom: indigo
colorTo: blue
sdk: docker
pinned: false
---

# 🎛️ TurnTable API

TurnTable is a real-time host stand seating manager and server rotation engine designed for high-volume restaurants. It ensures fair seating distribution using a dynamic round-robin rotation queue, enforces table capacities, manages guest waitlists with proportional alert timers, and coordinates server shifts in real-time.

Built using **ASP.NET Core Web API 8.0** and backed by **Supabase (PostgreSQL)**, TurnTable leverages **SignalR (WebSockets)** to instantly synchronize actions across multiple iPads or host stands.

---

## 🚀 Key Features

*   **Round-Robin Server Rotation**: Keeps servers organized by active sections, ensuring fair table distribution.
*   **Dynamic Layout Shifts**: Adjusts the floor layout automatically on server clock-ins and clock-outs (Main Floor ➜ Front/Back ➜ Sections 1/2/3/4).
*   **Active Table Transfers**: Dynamically transfers seated tables to remaining server sections during shift cuts.
*   **Waitlist with Proportional Timers**: Displays wait timers in `hh:mm:ss` format with three-tier warning alerts (Green, Yellow, Red) scaled to the guest's quoted wait time.
*   **Turn Estimator**: Tracks seated dining times in real-time, warning hostesses when tables have exceeded average dining durations.
*   **Real-Time Push Sync**: Broadcasts database updates to all connected frontends instantly using SignalR WebSockets.
*   **End-of-Shift Reset**: Performs a complete system wipe (clearing tables, clocking out servers, archiving waitlists) in a single atomic database transaction.

---

## 🛠️ Technology Stack

*   **Framework**: .NET 8.0 (ASP.NET Core Web API)
*   **Database**: PostgreSQL (hosted on Supabase)
*   **ORM**: Entity Framework Core (EF Core) with `Npgsql`
*   **Real-Time Layer**: ASP.NET Core SignalR (WebSockets)
*   **Testing**: xUnit with `Microsoft.EntityFrameworkCore.InMemory`
*   **Hosting**: Docker-packaged container

---

## 📁 Repository Structure

*   `src/TurnTable.Api/` — The main Web API project.
    *   `Controllers/` — REST API controllers exposing tables, servers, waitlist, and rotation endpoints.
    *   `Hubs/` — SignalR real-time websocket hub.
    *   `Models/` — EF Core domain entities and input/output DTOs.
    *   `Services/` — Business logic implementation (`DbTurnTableStore.cs`).
*   `tests/` — xUnit service-level unit tests.

---

## 🚦 Local Development Setup

### 1. Prerequisites
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   A running PostgreSQL database instance (or Supabase project)

### 2. Secret Configuration
Instead of keeping sensitive database connection strings in `appsettings.json`, TurnTable utilizes .NET User Secrets in development:

```bash
# Initialize secrets in the API directory
dotnet user-secrets init --project src/TurnTable.Api/TurnTable.Api.csproj

# Save your connection string (replace with your database credentials)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=your-host;Port=5432;Database=postgres;Username=postgres;Password=your-password;" --project src/TurnTable.Api/TurnTable.Api.csproj
```

### 3. Run the App
```bash
# Restore dependencies and build
dotnet build

# Run the API
dotnet run --project src/TurnTable.Api/TurnTable.Api.csproj
```
The server will start running on **`http://localhost:5000`**.

---

## 🧪 Running Tests

The test suite runs against an isolated, mocked EF Core in-memory database to verify seating constraints, server rotations, reassignments, and reset triggers.

```bash
dotnet test
```
