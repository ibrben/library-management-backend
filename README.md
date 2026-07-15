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

The health endpoint is available at `/health` and verifies PostgreSQL connectivity.

## Run with Docker Compose

Optionally copy `.env.example` to `.env` and replace the development JWT secret,
then start the API and PostgreSQL:

```bash
docker compose up --build
```

The API is exposed at <http://localhost:8080>, Swagger at
<http://localhost:8080/swagger>, and PostgreSQL at `localhost:5432`. EF Core
migrations run automatically when the API starts.

For local development, Compose creates an initial administrator when no
administrator exists:

- Username: `admin`
- Password: `ChangeMe12345`

Override all development credentials through `.env`; never use the defaults in
a shared or production environment. Stop the services with:

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

## Authentication

Log in with either a username or email:

```bash
curl --request POST http://localhost:8080/api/auth/login \
  --header 'Content-Type: application/json' \
  --data '{"usernameOrEmail":"admin","password":"ChangeMe12345"}'
```

Use the returned access token as `Authorization: Bearer <token>`. The following
endpoints are available:

| Method | Endpoint | Access |
| --- | --- | --- |
| `POST` | `/api/auth/login` | Anonymous |
| `POST` | `/api/auth/register` | Administrator only |
| `GET` | `/api/auth/me` | Authenticated users |

Registration supports the roles `Administrator`, `Librarian`, and `EndUser`.
Passwords are stored only as bcrypt hashes.

## Database schema

The initial migration creates `Users`, `Books`, and `BorrowTransactions`, with
unique indexes for usernames, email addresses, and ISBNs, plus restricted foreign
keys that protect transaction history.
