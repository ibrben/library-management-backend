# Library Management Backend

.NET 8 backend for library inventory, authentication, borrowing, returns, and
transaction history. It follows a three-tier dependency direction.

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

The API is exposed at <http://localhost:8080> and Swagger at
<http://localhost:8080/swagger>. PostgreSQL remains private to the Compose network
to avoid host-port conflicts and unnecessary database exposure. EF Core migrations
run automatically in the local Compose environment.

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

### Sample data for local development

Docker Compose enables idempotent sample-data seeding by default. On startup the
API applies migrations, creates the bootstrap administrator, and inserts any
missing sample librarian, member, and catalog books. Existing records are left
unchanged, so restarting the stack does not duplicate data.

| Role | Username | Password |
| --- | --- | --- |
| Administrator | `admin` | `ChangeMe12345` |
| Librarian | `librarian` | `Librarian12345` |
| End User | `member` | `Member123456` |

Ten books across software engineering, fiction, history, and classics are also
created with stable ISBNs. Copy `.env.example` to `.env` to customize credentials,
or set `SEED_DATA_ENABLED=false` to disable the librarian, member, and book seed.
The bootstrap administrator remains controlled separately by the `ADMIN_*`
variables.

For a completely fresh local dataset, intentionally remove the Compose volume:

```bash
docker compose down --volumes
docker compose up --build
```

Sample seeding is disabled in the base application configuration and is enabled
explicitly by `compose.yaml`, preventing accidental sample accounts in other
deployment environments.

To inspect the local database without publishing PostgreSQL, use:

```bash
docker compose exec postgres psql -U library -d library_management
```

## Production configuration

`compose.yaml` is a local-development stack, not a production deployment manifest.
Production deployments must provide `ConnectionStrings__DefaultConnection` and a
unique `Jwt__Secret` of at least 32 characters through a secret manager. They
should also override `Jwt__Issuer`, `Jwt__Audience`, and `AllowedHosts` for the
deployed domains.

The safe base defaults keep sample seeding, bootstrap-account creation, automatic
migrations, Swagger, and application-level HTTPS redirects disabled. Apply EF
migrations as a separate deployment step before starting multiple API replicas.
TLS should normally terminate at the ingress or reverse proxy; enable
`Http__UseHttpsRedirection=true` only when the application receives and understands
the original HTTPS scheme. Never set `SeedData__Enabled=true` outside the
`Development` environment—the application rejects that configuration at startup.

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

## Book inventory

Book reads and searches are public. Creating, updating, and deleting books requires
an `Administrator` or `Librarian` token.

| Method | Endpoint | Access |
| --- | --- | --- |
| `GET` | `/api/books/{id}` | Public |
| `GET` | `/api/books` | Public |
| `GET` | `/api/books/search` | Public |
| `POST` | `/api/books` | Administrator or Librarian |
| `PUT` | `/api/books/{id}` | Administrator or Librarian |
| `DELETE` | `/api/books/{id}` | Administrator or Librarian |

List and search accept `isbn`, `title`, `author`, `category`, `availability`,
`page`, `pageSize`, `sortBy`, and `sortOrder`. ISBN values are unique, and books
with an active borrowing cannot be deleted.

## Borrowing and transaction history

All borrowing endpoints require a bearer token. Administrators and librarians can
borrow or return on behalf of any user. End users can borrow and return only their
own books and can only view their own history. Global history is restricted to
administrators.

| Method | Endpoint | Access |
| --- | --- | --- |
| `POST` | `/api/borrowings` | Any user; `userId` override for Administrator/Librarian |
| `POST` | `/api/borrowings/{transactionId}/return` | Owner, Administrator, or Librarian |
| `GET` | `/api/borrowings/{transactionId}` | Owner, Administrator, or Librarian |
| `GET` | `/api/borrowings/mine` | Authenticated user's history |
| `GET` | `/api/borrowings` | Administrator global history |

History supports `status` (`Borrowed` or `Returned`), `page`, and `pageSize`.
Global history additionally supports `userId`. Borrowing checks and updates book
availability, records the borrower and optional future `dueDate`, and returning
records the UTC return date and makes the book available again.
