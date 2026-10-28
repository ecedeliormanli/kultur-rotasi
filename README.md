# Kültür Rotası

> Cultural route planner for Türkiye. Discover museums and archaeological sites on a map, run spatial queries on them, and plan your own cultural route.

[![CI](https://github.com/ecedeliormanli/kultur-rotasi/actions/workflows/ci.yml/badge.svg)](https://github.com/ecedeliormanli/kultur-rotasi/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![Angular](https://img.shields.io/badge/Angular-22-DD0031)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2025-CC2927)
![License](https://img.shields.io/badge/license-MIT-green)

> **Status:** early development. This is a personal portfolio project built to learn ASP.NET Core and GIS (spatial data, OGC services).
> It is not affiliated with the Republic of Türkiye Ministry of Culture and Tourism or any public institution.

## Planned features

- Interactive map (Leaflet) of museums, archaeological sites and monuments
- Spatial analysis: nearest places, buffer search (within X km), point-in-polygon (places in a province)
- Thematic map of place density per province and a heatmap
- GeoJSON and KML import and export
- WMS and WFS layers served by GeoServer
- Bilingual Turkish and English UI and content
- JWT authentication with `User` and `Admin` roles, OWASP Top 10 hardening
- "Suggest a place" workflow with admin approval and live notifications (SignalR)
- Favourites and personal routes

## Tech stack

| Layer | Technology |
| --- | --- |
| Backend | ASP.NET Core 10 Web API, C# |
| Data | SQL Server 2025 (spatial `geography`), EF Core 10, NetTopologySuite |
| GIS | GeoServer (WMS / WFS), Leaflet, GeoJSON, KML |
| Testing | xUnit, WebApplicationFactory, Testcontainers |
| Frontend | Angular 22 (standalone components, signals) |
| DevOps | Docker Compose, GitHub Actions |

## Architecture

The backend follows a lightweight Clean Architecture. Dependencies point inwards and are enforced by unit tests (`LayerDependencyTests`).

```
Api  ──►  Application  ──►  Domain
              ▲
Infrastructure ┘
```

| Project | Responsibility |
| --- | --- |
| `KulturRotasi.Domain` | Entities and domain rules. No dependencies on other layers. |
| `KulturRotasi.Application` | Use cases (commands and queries), DTOs, validation, interfaces. |
| `KulturRotasi.Infrastructure` | EF Core, migrations, identity, external services. |
| `KulturRotasi.Api` | Controllers, middleware, dependency injection setup, OpenAPI. |

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js 24 (see `frontend/.nvmrc`)
- Docker

### Run locally

```bash
git clone https://github.com/ecedeliormanli/kultur-rotasi.git
cd kultur-rotasi

# 1. Database (SQL Server in Docker)
cp .env.example .env          # if you change the password, also change it in appsettings.Development.json
docker compose up -d

# 2. API  → http://localhost:5140/swagger
#    In Development the API applies migrations and seeds 81 provinces and 40 places on startup.
cd backend
dotnet run --project src/KulturRotasi.Api

# 3. Frontend  → http://localhost:4200
cd ../frontend
npm install
npm start
```

### Tests

```bash
cd backend && dotnet test      # integration tests start their own SQL Server with Testcontainers (Docker required)
cd frontend && npm run test:ci
```

### Database migrations

```bash
cd backend
dotnet tool restore            # installs dotnet-ef from dotnet-tools.json
dotnet ef migrations add <Name> -p src/KulturRotasi.Infrastructure -s src/KulturRotasi.Api -o Persistence/Migrations
```

## Project structure

```
kultur-rotasi/
├─ backend/        open this folder in Rider (KulturRotasi.slnx)
│  ├─ src/        Domain · Application · Infrastructure · Api
│  └─ tests/      UnitTests · IntegrationTests
├─ frontend/       open this folder in WebStorm/Rider (Angular app)
├─ docs/          ER diagram (UML class diagram and BPMN process coming)
├─ docker-compose.yml
└─ .github/workflows/ci.yml
```

## Roadmap

- [x] M0 Repository skeleton, Docker Compose, CI
- [x] M1 Domain model, EF Core, spatial data, seed data, ER diagram
- [ ] M2 Spatial API: filtering, nearest, buffer, point-in-polygon, GeoJSON
- [ ] M3 Angular + Leaflet map, Turkish and English UI
- [ ] M4 Thematic map, heatmap, GeoJSON and KML import and export
- [ ] M5 GeoServer WMS and WFS layers
- [ ] M6 Authentication, roles, OWASP hardening, suggestion workflow, BPMN
- [ ] M7 Favourites, routes, SignalR notifications, Redis cache
- [ ] M8 Structured logging, health checks, Azure deployment

## Data sources

- **Province boundaries:** [geoBoundaries](https://www.geoboundaries.org/) (gbOpen, TUR ADM1, 2021), derived from OpenStreetMap. © OpenStreetMap contributors, CC BY-SA 2.0.
- **Places:** a small, hand-curated sample written for this project. Coordinates are approximate.

## License

[MIT](LICENSE)
