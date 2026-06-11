# RMS Identity Service

RMS Identity Service is the identity, authentication, company registration, and company membership service for the Retail Management System. It owns global user identities, login and refresh-token sessions, email verification, company metadata, company membership roles, platform-admin company status changes, idempotent write handling, and the email verification outbox.

This README is intended to give future maintainers enough project context to understand the service goals, assumptions, architecture, implementation shape, local setup, and validation workflow.

## Objective

The service exists to provide a clean identity boundary for the broader Retail Management System.

Its main objectives are:

- Create and maintain global user identities.
- Authenticate users and issue user-level JWT access tokens plus refresh tokens.
- Keep company registration separate from public signup.
- Represent company access through explicit `CompanyUser` memberships.
- Enforce company-scoped authorization from database state, not only token claims.
- Support safe retries for mutating API requests through idempotency keys.
- Request and process email verification through an outbox-based workflow.
- Provide platform-admin controls for company verification status.

The service does not own retail operational domains such as stores, products, inventory, billing, cashier permissions, or reporting. Those capabilities are expected to live in other services or later modules.

## Core Assumptions

These assumptions are reflected in the code, schema, tests, and API contract.

- A `UserAccount` is a global identity and can belong to zero, one, or many companies.
- `UserAccount.CompanyID` is deprecated. Company membership lives in `CompanyUser`.
- A company can have multiple users, and a user can be a member of multiple companies.
- JWT access tokens are user-level tokens. They contain the user UUID in `sub`; they do not encode company access.
- Company access is checked against current database membership on each company-scoped request.
- Company roles are `OWNER`, `ADMIN`, and `MEMBER`.
- Operational roles in `Role` and `UserRole` are separate from company membership roles.
- Company registration creates a company in `pending_verification` status.
- The authenticated user who registers a company becomes an active `OWNER` member of that company.
- A company must retain at least one active `OWNER`.
- Mutating business endpoints are idempotent unless explicitly excluded.
- Email verification is token-based and uses hashed tokens at rest.
- Refresh tokens are opaque values stored as hashes at rest.
- The canonical schema is `reference/db/sql_schema.sql`.
- The OpenAPI contract is `reference/openapi/openapi.yaml`; endpoint docs in `docs/api` should stay aligned with it.
- Database integrity is primarily application-enforced. The canonical schema intentionally does not declare foreign keys.
- Non-development deployments must provide secrets and database connection settings through configuration or environment variables.

## Technology Stack

- .NET 8
- ASP.NET Core controllers
- MySQL 8 compatible database
- MySqlConnector
- BCrypt.Net-Next for password hashing
- HMAC-SHA256 JWT signing
- xUnit for tests
- Swashbuckle for Swagger in Development
- Docker Compose for local MySQL

## Repository Layout

```text
.
|-- docs/
|   `-- api/                         Human-readable API notes
|-- reference/
|   |-- db/sql_schema.sql             Canonical MySQL schema
|   `-- openapi/openapi.yaml          OpenAPI contract
|-- src/
|   |-- RMS.Identity.Service.Api/     HTTP API, controllers, middleware, request models
|   |-- RMS.Identity.Service.Application/
|   |                                  Command handlers and application validation
|   |-- RMS.Identity.Service.Domain/  Contracts, entities, repository interfaces
|   |-- RMS.Identity.Service.Infrastructure/
|   |                                  MySQL persistence, transactions, security, email, outbox
|   |-- RMS.Identity.Service.Infrastructure.Abstractions/
|   |                                  CQRS abstractions
|   `-- RMS.Identity.Service.Tests/   Unit and integration tests
|-- tools/docker/                     Docker assets
|-- docker-compose.yaml               Local MySQL container
`-- README.md
```

## Architecture

The service uses a layered architecture with clear dependency direction:

```text
API
  -> Application
      -> Domain contracts and interfaces
  -> Infrastructure
      -> Application
      -> Domain interfaces
```

### API Layer

Project: `src/RMS.Identity.Service.Api`

Responsibilities:

- Configure ASP.NET Core.
- Register controllers, JSON options, Swagger, middleware, filters, and request validators.
- Parse HTTP request models.
- Resolve the authenticated user from bearer tokens.
- Run authorization checks.
- Start transaction scopes for endpoints that call transaction-bound repositories.
- Convert command responses to HTTP response DTOs.

Important components:

- `Program.cs`
- `ApiExceptionHandlingMiddleware`
- `IdempotencyMiddleware`
- `JwtAccessTokenUserResolver`
- `CompanyAccessAuthorizer`
- `PlatformAdminAuthorizer`
- Endpoint folders under `Endpoint/`

The API uses camelCase JSON and ignores null response properties. Request bodies that map to closed OpenAPI schemas use `JsonUnmappedMemberHandling.Disallow` to reject unexpected JSON fields.

### Application Layer

Project: `src/RMS.Identity.Service.Application`

Responsibilities:

- Implement business use cases as command handlers.
- Normalize and validate command values.
- Enforce application invariants.
- Coordinate domain repositories and services.

Representative handlers:

- `SignUpCommandHandler`
- `VerifyEmailCommandHandler`
- `LoginCommandHandler`
- `RefreshCommandHandler`
- `RegisterCompanyCommandHandler`
- `CreateCompanyUserCommandHandler`
- `GetCurrentUserCompaniesCommandHandler`
- `UpdateCompanyUserCommandHandler`
- `UpdateCompanyStatusCommandHandler`

The application layer depends on abstractions for persistence, hashing, token generation, and notification-related writes. It does not directly create MySQL connections.

### Domain Layer

Project: `src/RMS.Identity.Service.Domain`

Responsibilities:

- Define command request and response contracts.
- Define entity records used by the application and infrastructure.
- Define repository, persistence, notification, and security interfaces.

The domain layer is intentionally lightweight. It holds service contracts and data shapes rather than a rich domain model.

### Infrastructure Layer

Project: `src/RMS.Identity.Service.Infrastructure`

Responsibilities:

- Implement MySQL repositories.
- Implement transaction scope management.
- Implement password hashing, text hashing, token generation, email sending, and outbox processing.
- Bind and validate infrastructure options.

Important components:

- `MySqlConnectionFactory`
- `MySqlDatabaseTransactionExecutor`
- `DatabaseTransactionAccessor`
- `AuthenticationMySqlRepository`
- `UserAccountMySqlRepository`
- `CompanyMySqlRepository`
- `CompanyUserMySqlRepository`
- `IdempotencyMySqlRepository`
- `OutboxMySqlRepository`
- `JwtAuthTokenGenerator`
- `BcryptPasswordHasher`
- `EmailVerificationRequestedOutboxProcessor`
- `EmailVerificationOutboxWorker`

### CQRS Abstractions

Project: `src/RMS.Identity.Service.Infrastructure.Abstractions`

This project defines the small command/query abstractions used by the application layer, such as `ICommand<TResponse>` and `ICommandHandler<TCommand, TResponse>`.

## HTTP Surface

The OpenAPI contract is the source of truth for request and response shapes. Main endpoint groups are:

| Area | Endpoint | Purpose |
| --- | --- | --- |
| Signup | `POST /api/v1/signup` | Create a global user identity |
| Email verification | `POST /api/v1/users/verify-email` | Verify a user email token |
| Auth | `POST /api/v1/auth/login` | Issue access and refresh tokens |
| Auth | `POST /api/v1/auth/refresh` | Rotate refresh token and issue a new access token |
| Current user companies | `GET /api/v1/current-user/companies` | List non-suspended company memberships for the authenticated user |
| Companies | `POST /api/v1/companies` | Register a company and owner membership |
| Companies | `GET /api/v1/companies/{companyUuid}` | Get company metadata |
| Companies | `PATCH /api/v1/companies/{companyUuid}` | Update company metadata |
| Company users | `GET /api/v1/companies/{companyUuid}/users` | List company users |
| Company users | `POST /api/v1/companies/{companyUuid}/users` | Create a company-scoped user membership |
| Company users | `GET /api/v1/companies/{companyUuid}/users/{userUuid}` | Get a company-scoped user |
| Company users | `PATCH /api/v1/companies/{companyUuid}/users/{userUuid}` | Update company role or membership status |
| Company users | `DELETE /api/v1/companies/{companyUuid}/users/{userUuid}` | Suspend a company user membership |
| Admin companies | `PATCH /api/v1/admin/companies/{companyUuid}/status` | Platform-admin company status transition |

## Main Workflows

### Signup and Verification

1. Client calls `POST /api/v1/signup` with an `Idempotency-Key`.
2. The system validates the body, normalizes the email, hashes the password, and creates `UserAccount`.
3. The system creates an email verification token record with a SHA-256 hash of the token.
4. The system writes an `email_verification_requested` outbox message.
5. The outbox worker sends email or auto-verifies through the configured endpoint mode.
6. Client calls `POST /api/v1/users/verify-email` with the token.
7. The token is consumed and the user email is marked verified.

### Login and Refresh

1. Client calls `POST /api/v1/auth/login`.
2. The system normalizes username, verifies the BCrypt password hash, and checks user state.
3. Login is rejected if the user is deleted, inactive, locked, or email is not verified.
4. The system generates a user-level JWT access token and opaque refresh token.
5. Refresh token hash is persisted.
6. Client calls `POST /api/v1/auth/refresh` to rotate refresh tokens.
7. Refresh token reuse after rotation is rejected because the old token is revoked.

### Company Registration

1. Authenticated user calls `POST /api/v1/companies` with an `Idempotency-Key`.
2. JWT resolves the actor user UUID.
3. The command checks the user is active.
4. GSTIN and contact fields are normalized and validated.
5. Company GSTIN uniqueness is checked.
6. `Company` is created with `pending_verification`.
7. `CompanyUser` creates active `OWNER` membership for the actor.
8. Transaction commit makes company and membership visible together.

### Company Access

Company-scoped endpoints resolve the current user from the bearer token, then check database membership.

Access requires:

- User account is active and not deleted.
- Membership exists for the requested company.
- Membership status is `active`.
- Company status is accessible to users, currently `pending_verification` or `verified`.
- Role-sensitive endpoints require `OWNER`, `ADMIN`, or platform admin as appropriate.

### Idempotent Writes

`IdempotencyMiddleware` applies to mutating HTTP methods:

- `POST`
- `PUT`
- `PATCH`
- `DELETE`

Exclusions:

- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/users/verify-email`

Idempotency behavior:

- `Idempotency-Key` is required for covered endpoints.
- The request hash includes path, query string, and body.
- A repeated key with the same request returns the stored response.
- A repeated key with a different request returns `409 IDEMPOTENCY_KEY_REUSED`.
- Bodyless responses such as `204 No Content` are replayed without writing a literal JSON `null` response body.

## Data Model

Canonical schema: `reference/db/sql_schema.sql`

Key tables:

| Table | Purpose |
| --- | --- |
| `UserAccount` | Global user identity, password hash, email verification state, login state |
| `Company` | Company metadata, GSTIN, company verification status |
| `CompanyUser` | User-to-company membership with company role and membership status |
| `Role` | Operational roles, separate from company membership roles |
| `UserRole` | User-to-operational-role assignments |
| `RefreshToken` | Hashed refresh tokens and rotation metadata |
| `EmailVerification` | Hashed email verification and password reset tokens |
| `IdempotencyKey` | Request hash and stored response for safe retries |
| `Outbox` | Pending, processing, published, or failed integration messages |
| `AuditLog` | Identity-scope audit entries |
| `ApiKey` | Placeholder for server-to-server or integration keys |
| `TenantSetting` | Legacy-named company settings table |

UUIDs are stored as `BINARY(16)` in MySQL and exposed as standard GUID strings through the API.

## Transactions and Persistence

The project has two persistence styles:

- Business repositories that require an ambient transaction through `IDatabaseTransactionAccessor`.
- Specialized repositories that manage their own connection and transaction internally, such as auth session operations and outbox processing.

For transaction-bound repository calls, endpoints or middleware must use `IDatabaseTransactionExecutor`.

Common examples:

- Idempotent mutating endpoints are wrapped by `IdempotencyService`, which opens the transaction.
- Email verification explicitly opens a transaction in the controller.
- Company read and membership endpoints open a transaction where command handlers load user or company state through transaction-bound repositories.
- Login and refresh use `AuthenticationMySqlRepository`, which owns its connection and transaction boundaries internally.

## Security Model

- Passwords are hashed with BCrypt.
- Refresh tokens are stored only as hashes.
- Email verification tokens are stored only as SHA-256 hashes.
- JWT access tokens are HMAC-SHA256 signed.
- `Jwt:SigningKey` must be at least 32 bytes.
- Production should provide the JWT signing key through `JWT_SIGNING_KEY`.
- Development may use the local signing key from `appsettings.Development.json`.
- Company access is not trusted from JWT claims. It is checked against the database.
- Platform-admin authorization checks `UserRole` membership for `PLATFORM_ADMIN`.
- Request DTOs reject unexpected JSON fields where the OpenAPI schema is closed.

## Configuration

### Required Non-Development Settings

Non-development deployments must provide:

- `ConnectionStrings__Default`
- `JWT_SIGNING_KEY` with at least 32 bytes of key material

Base `appsettings.json` intentionally does not contain deployable secrets.

### Development Defaults

Local development defaults are in:

```text
src/RMS.Identity.Service.Api/appsettings.Development.json
src/RMS.Identity.Service.Api/Properties/launchSettings.json
docker-compose.yaml
```

Development config includes a local MySQL connection string and development JWT signing key. These values are for local use only.

### JWT Key Override Behavior

`Jwt:SigningKeyEnvVar` defaults to `JWT_SIGNING_KEY`.

The infrastructure options binding keeps a configured `Jwt:SigningKey` when the referenced environment variable is absent. If `JWT_SIGNING_KEY` is present and non-empty, it overrides the configured key.

### Email Delivery

Email delivery is controlled by `EmailDelivery`.

Important options:

- `Enabled`
- `AutoVerifyByEndpoint`
- `VerifyEmailEndpointUrl`
- `VerificationUrlTemplate`
- `SmtpHost`
- `SmtpPort`
- `SmtpUsername`
- `SmtpPassword`
- `SmtpPasswordEnvVar`
- `PollIntervalSeconds`
- `BatchSize`
- `MaxRetries`
- `RetryDelaySeconds`
- `ProcessingTimeoutSeconds`

Environment overrides:

- `RMS_IDENTITY_AUTO_VERIFY_EMAIL_BY_ENDPOINT`
- `RMS_IDENTITY_VERIFY_EMAIL_ENDPOINT_URL`

## Local Development

### Prerequisites

- .NET 8 SDK
- Docker Desktop or a compatible Docker runtime
- MySQL client is optional; tests can connect through the application using MySqlConnector

### Start MySQL

```powershell
docker compose up -d
```

The compose file starts a MySQL container named `rms.identity.db` and maps port `3306`.

The schema is mounted at container initialization:

```text
reference/db/sql_schema.sql -> /docker-entrypoint-initdb.d/01_schema.sql
```

If the Docker volume already exists with an old schema, MySQL will not automatically re-run initialization scripts. Create a fresh database or recreate the volume before running DB-backed tests against a stale schema.

### Run the API

```powershell
dotnet run --project src\RMS.Identity.Service.Api\RMS.Identity.Service.Api.csproj
```

Development Swagger is available when `ASPNETCORE_ENVIRONMENT=Development`.

### Build

```powershell
dotnet build src\RMS.Identity.Service.sln -v minimal
```

### Run Tests

All tests:

```powershell
dotnet test src\RMS.Identity.Service.Tests\RMS.Identity.Service.Tests.csproj -v minimal
```

DB-backed endpoint tests use:

1. `RMS_IDENTITY_TEST_CONNECTION_STRING`
2. `ConnectionStrings__Default`
3. fallback local connection string

Example with an isolated canonical test database:

```powershell
$env:RMS_IDENTITY_TEST_CONNECTION_STRING='Server=127.0.0.1;Port=3306;Database=rms_identity_codex_test;User ID=rms_user;Password=12345678;'
dotnet test src\RMS.Identity.Service.Tests\RMS.Identity.Service.Tests.csproj -v minimal
```

Non-DB tests only:

```powershell
dotnet test src\RMS.Identity.Service.Tests\RMS.Identity.Service.Tests.csproj --filter "FullyQualifiedName!~SignUpEndpointTests&FullyQualifiedName!~CompanyEndpointTests" -v minimal
```

## Testing Strategy

The test project contains:

- Request model binder tests.
- Request validation filter tests.
- Application command handler tests with fakes.
- Authorization helper tests.
- Idempotency middleware and service tests.
- Outbox processor tests.
- DB-backed endpoint integration tests.

Endpoint integration tests require a reachable MySQL database with the canonical schema. `TestDatabaseWebApplicationFactory` validates database availability and company-schema shape before DB-backed tests run.

## API Contract and Documentation

Keep these files aligned when endpoint behavior changes:

- `reference/openapi/openapi.yaml`
- `docs/api/signup.md`
- `docs/api/companies.md`
- `README.md`

The OpenAPI file is the machine-readable contract. The docs files explain specific flows and expected behavior.

## Operational Notes

- Use `ConnectionStrings__Default` for production database configuration.
- Use `JWT_SIGNING_KEY` for production JWT signing key material.
- Keep production secrets out of committed JSON files.
- Use a canonical schema database for integration tests.
- Mutating endpoint handlers should either run under idempotency middleware or explicitly open a transaction.
- Any repository that calls `IDatabaseTransactionAccessor.GetCurrent()` requires an active `IDatabaseTransactionExecutor` scope.
- Do not add company membership or permissions to JWT claims unless the authorization model is deliberately changed.
- Do not treat `UserAccount.CompanyID` as the membership source.

## Known Boundaries and Out of Scope

Currently out of scope for this service:

- GSTIN ownership verification.
- Store management.
- Inventory management.
- Billing and invoices.
- Retail operational permissions beyond placeholder operational roles.
- Password reset endpoint implementation, despite schema support for password reset token purpose.
- Full API key issuance and validation workflow.

## Current Validation Baseline

At the time this README was written, the following passed on the current codebase:

```text
dotnet build src\RMS.Identity.Service.sln -v minimal
dotnet test src\RMS.Identity.Service.Tests\RMS.Identity.Service.Tests.csproj -v minimal
```

The full test run used an isolated MySQL database with the canonical schema and reported `90/90` passing tests.
