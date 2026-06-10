# Retail.IdentityService

This folder contains the Retail.IdentityService service.

- reference/db/sql_schema.sql (canonical DB schema)
- reference/openapi/openapi.yaml (service API contract)
- tools/docker/Dockerfile and docker-compose for local dev
- src/ contains code projects

## Runtime configuration

Non-development deployments must provide:

- `ConnectionStrings__Default`
- `JWT_SIGNING_KEY` with at least 32 bytes of key material

Local development defaults remain in `src/RMS.Identity.Service.Api/appsettings.Development.json`.
