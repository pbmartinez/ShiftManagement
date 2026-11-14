# ShiftManagement

Manage collaborators and their work shifts:
- create, edit and delete collaborators and shifts
- a collaborator can't have overlapping shifts
- import shifts from Excel and export them to Excel
- shifts that fall on a public holiday in the collaborator's country are highlighted. Holidays come from the public [Nager.Date](https://date.nager.at) API.

**Stack:** .NET 10 (ASP.NET Core, EF Core, SQL Server), Angular 22 + Angular Material.

## Run it

### Docker (one command)
Run this from the repository root:

```bash
docker compose up        # or: docker-compose up
```

| What | URL |
|---|---|
| App | http://localhost:8080 |
| Swagger UI | http://localhost:8080/swagger |
| OpenAPI document | http://localhost:8080/openapi/v1.json |

- The first run builds the image, which takes a few minutes.
- SQL Server runs in its own container, and the API waits until it is healthy.
- The database schema is created on start.
- `docker compose down` stops everything. `docker compose down -v` also deletes the data.
- After changing code, use `docker compose up --build`.

### Visual Studio / dotnet
1. Have SQL Server running locally. The connection string is in `src/Api/appsettings.json`.
2. Set **Api** as the startup project and press F5. SpaProxy starts the Angular dev server.

### Tests
```bash
dotnet test src/Tests/Tests.csproj
```
Docker must be running: the integration tests start a disposable SQL Server with Testcontainers.

## How it works

```
Angular page ──HTTP──► Controller ──► EntityWriter ──► Repository ──► EF Core ──► SQL Server
                                        │
                                        ├─ data annotations (DTO rules)
                                        └─ business validators (e.g. no overlapping shifts)
                                           run in one Serializable transaction with the save
```

**Writes.** Every write goes through `EntityWriter`. It checks the DTO's data annotations, then runs every `IBusinessValidator<T>`. The validators and the save happen inside one transaction. Errors come back as a `400 ValidationProblem` keyed by field, so the UI shows each one under its input.

**Excel import.** The rows are read, each collaborator email is resolved to a collaborator, and the whole batch is saved through the same writer. It's all-or-nothing: if any row fails, nothing is saved and every row's error is reported.

**Holidays:**
- `GET api/shift` groups the shifts by (country, year) and asks `IHolidayDates` once per group.
- The answer is cached in memory for 30 days, keyed by country and year.
- Calls to Nager are retried by Polly (count and backoff come from `appsettings.json`).
- If Nager is down, the list still loads, just without highlights, and the failure is not cached.
- The shift dialog checks the chosen date live through `GET api/holiday/{country}/{year}`.

## Architecture

Clean-architecture layers, with dependencies pointing inward:

```
Api ─► Application ─► Domain
 └────► Infrastructure ─┘
```

| Project | Contains |
|---|---|
| **Domain** | Entities, DTOs (with their validation rules), repository interfaces, AutoMapper profiles |
| **Application** | Use cases (`EntityWriter`, `ShiftImportService`), business validators, interfaces for external services (`IHolidayDates`, `ILoadShifts`, `IExportShifts`) |
| **Infrastructure** | EF Core (`AppDbContext`, repositories, migrations), Excel with ClosedXML, the Nager.Date client |
| **Api** | Controllers, DI setup, Swagger. It also serves the built Angular app, so the UI and the API share one origin. |
| **AngularApp** | Standalone components, signals, reactive forms, Angular Material |
| **Tests** | NUnit unit and integration tests |

## Design patterns
- **Repository + Unit of Work.** The generic `Repository<TDto, TEntity>` hides EF Core and works with DTOs. `AppDbContext` is the unit of work and runs the transactions.
- **Generic base controller (Template Method).** `ApiBaseController<T>` provides CRUD and PATCH. `ShiftController` overrides only what differs, such as adding holidays to `Get`.
- **DTO + Mapper.** The API never exposes entities. AutoMapper maps entities to DTOs and also translates query expressions.
- **Strategy.** Business rules are pluggable `IBusinessValidator<T>` classes, for example `ShiftOverlapValidator`. To add a rule, register another validator.
- **Adapter.** `IHolidayDates`, `ILoadShifts` and `IExportShifts` keep external APIs and file formats out of the application layer.
- **Cache-aside + Retry.** Nager answers go through `IMemoryCache` first, and HTTP calls go through a Polly retry pipeline.
- **Options pattern.** `HolidaySettings` and the retry options are bound from configuration.

## Design decisions
- **Serializable transaction for the overlap rule.** Two requests at the same time can't both pass the check and then both save.
- **Filtering, pagination and Excel export happen in the browser.** The data sets are small, so this needs no extra endpoints, and the export matches exactly what is shown (filtered and sorted).
- **Country is an ISO code chosen from Nager's country list**, which is also fetched and cached. This makes it always a code the holiday API understands.
- **The holiday lookup fails soft.** An outside service being down should not break the shift list.
- **One container for the API and the UI.** There's no CORS, no reverse proxy, and one URL.
- **Migrations run on startup**, so `docker compose up` needs no manual database step.

## Tests
The tests aim for quality over quantity. They cover the rules that matter and the places where the layers meet.
- **Unit:**
  - shift overlap and time rules
  - `EntityWriter`: validation order, a single Serializable transaction, nothing saved on errors
  - the Excel import resolving emails
  - the holiday cache: one HTTP call per country and year, and failures aren't cached
- **Integration (real SQL Server in Testcontainers):**
  - the API end to end through `WebApplicationFactory`: CRUD, PATCH, validation errors, import, export, holiday flags
  - repository queries
  - Excel export, then importing the same file back
  - AutoMapper configuration
- The external holiday API is replaced by a stub in the integration tests, so they don't depend on the internet.

## Left out (and why)
- **Authentication and authorization.** They weren't required; the app is open by design.
- **Server-side pagination and filtering.** Not needed at this data size. It would be the next step if the data grew.
- **Custom holidays.** `GetCustomHolidaysAsync` is kept as an extension point but returns nothing.
- **HTTPS inside the container.** TLS would normally be handled by a reverse proxy or the hosting platform.
- **A distributed cache (e.g. Redis).** The app runs as a single instance, so an in-memory cache is enough.
- **Frontend unit tests.** The time went into backend unit and integration tests, which cover the business rules.
- **Secrets management.** The SQL password in `docker-compose.yml` is for local demo use only.
- **Known gaps.** Each has a test marked `Ignore` in `ShiftApiTests`:
  - PATCH needs `AddNewtonsoftJson()` to bind the request body
  - a POST without an `id` is rejected
  - an unknown `CollaboratorId` returns 500 instead of 400
