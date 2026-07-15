# Library Management Backend

Initial .NET 8 backend scaffold for the Library Management System. It follows a
three-tier dependency direction and deliberately contains no business logic yet.

## Project structure

```text
src/
  LibraryManagement.Api/          Presentation layer
  LibraryManagement.Business/     Business layer
  LibraryManagement.DataAccess/   Data access layer
tests/
  LibraryManagement.Business.UnitTests/
```

Dependencies flow from `Api` to `Business` to `DataAccess`. A layer must not
reference a layer above it.

## Run locally

Install a .NET 8-compatible SDK, then run:

```bash
dotnet restore
dotnet test
dotnet run --project src/LibraryManagement.Api
```

The infrastructure health endpoint is available at `/health`.

## Run with Docker Compose

Optionally copy `.env.example` to `.env` and replace the development JWT secret,
then start the API and PostgreSQL:

```bash
docker compose up --build
```

The API is exposed at <http://localhost:8080> and PostgreSQL at `localhost:5432`.
The default credentials are for local development only. Stop the services with:

```bash
docker compose down
```

Use `docker compose down --volumes` only when you intentionally want to remove
the local database data.

## Tests

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Add unit-test projects alongside the layer they exercise as the system grows.
