# Company Registration API - `POST /api/v1/companies`

This document describes authenticated company registration for the RMS Identity Service. It must stay in sync with `reference/openapi/openapi.yaml`.

## 1. Purpose

Company registration creates a company for an already signed-up and logged-in user.

On success, the API creates:

1. A `Company`.
2. A `CompanyUser` membership linking the authenticated user to the company as `OWNER`.

The mapping table between users and companies is `CompanyUser`.

The frontend should call `GET /api/v1/me/companies` after login. If the returned `companies` array is empty, show the company registration screen.

## 2. Endpoint Summary

| Attribute | Value |
| --- | --- |
| Method | `POST` |
| Path | `/api/v1/companies` |
| Auth | Bearer access token required |
| Idempotent | Yes, via `Idempotency-Key` |
| OpenAPI Ref | `paths./api/v1/companies.post` |

## 3. List My Companies

| Attribute | Value |
| --- | --- |
| Method | `GET` |
| Path | `/api/v1/me/companies` |
| Auth | Bearer access token required |
| OpenAPI Ref | `paths./api/v1/me/companies.get` |

Successful response:

```json
{
  "companies": [
    {
      "companyUuid": "9b05b13f-0e59-47e6-a249-13de95c3564e",
      "legalName": "Example Retail Pvt Ltd",
      "tradeName": "Example Retail",
      "gstin": "29ABCDE1234F1Z5",
      "status": "pending_verification",
      "companyRole": "OWNER",
      "membershipStatus": "active",
      "createdAt": "2026-02-08T08:00:00Z"
    }
  ]
}
```

An empty `companies` array means the user does not currently belong to any company.

## 4. Register Company Request

### Headers

| Header | Required | Description |
| --- | --- | --- |
| `Authorization` | Yes | `Bearer <accessToken>` |
| `Idempotency-Key` | Yes | UUID used to safely retry the request |

### Request Body

```json
{
  "legalName": "Example Retail Pvt Ltd",
  "tradeName": "Example Retail",
  "gstin": "29ABCDE1234F1Z5",
  "contactEmailAddress": "accounts@example.com",
  "contactPhoneNumber": "+919876543211",
  "addressLine1": "1 Main Road",
  "addressLine2": "Near Market",
  "city": "Bengaluru",
  "state": "Karnataka",
  "postalCode": "560001",
  "country": "IN"
}
```

### Field Rules

| Field | Rules |
| --- | --- |
| `legalName` | Required |
| `tradeName` | Optional |
| `gstin` | Required, valid GSTIN, globally unique |
| `contactEmailAddress` | Required, valid email |
| `contactPhoneNumber` | Required, valid phone number |
| `addressLine1` | Required |
| `addressLine2` | Optional |
| `city` | Required |
| `state` | Required |
| `postalCode` | Required |
| `country` | Required, default client value should be `IN` |

## 5. Register Company Successful Response

### `201 Created`

```json
{
  "companyUuid": "9b05b13f-0e59-47e6-a249-13de95c3564e",
  "legalName": "Example Retail Pvt Ltd",
  "tradeName": "Example Retail",
  "gstin": "29ABCDE1234F1Z5",
  "status": "pending_verification",
  "createdAt": "2026-02-08T08:00:00Z"
}
```

### Meaning

- Company is created with `pending_verification` status.
- `CompanyUser` membership is created with `CompanyRole = OWNER` and `MembershipStatus = active`.
- The authenticated user can register more than one company.

## 6. Error Responses

### `400 Bad Request`

```json
{
  "code": "VALIDATION_ERROR",
  "message": "GSTIN must be a valid GSTIN."
}
```

### `401 Unauthorized`

```json
{
  "code": "UNAUTHORIZED",
  "message": "Authorization bearer token is required."
}
```

### `409 Conflict`

Duplicate GSTIN:

```json
{
  "code": "COMPANY_EXISTS",
  "message": "Company GSTIN already exists."
}
```

## 7. Behavioral Contract

On successful company registration, the system must complete the following in one database transaction:

1. Validate the bearer access token.
2. Resolve the authenticated user from the token `sub` claim.
3. Validate company request fields.
4. Normalize GSTIN to uppercase.
5. Check duplicate company GSTIN.
6. Create `Company` with `CompanyStatus = pending_verification`.
7. Create `CompanyUser` with `CompanyRole = OWNER` and `MembershipStatus = active`.
8. Return `201 Created`.

If membership creation fails after company creation, the transaction must roll back and the company must not remain persisted.

## 8. Company Membership

`CompanyUser` is the user-company mapping table:

| Column | Meaning |
| --- | --- |
| `CompanyID` | Internal company ID |
| `UserID` | Internal user ID |
| `CompanyRole` | `OWNER`, `ADMIN`, or `MEMBER` |
| `MembershipStatus` | `active`, `invited`, or `suspended` |

## 9. Out of Scope

- GSTIN ownership verification workflow.
- Admin-created users and invitations.
- Operational/job permissions such as cashier, inventory, billing, and reporting.
- Company approval/rejection implementation beyond initial `pending_verification` status.

## 10. Relation to OpenAPI

Mapped to:

- `paths./api/v1/companies.post`
- `paths./api/v1/me/companies.get`
- `components.schemas.RegisterCompanyRequest`
- `components.schemas.RegisterCompanyResponse`
- `components.schemas.MyCompaniesResponse`
- `components.schemas.MyCompanyResponse`
- `components.schemas.CompanyRole`
- `components.schemas.CompanyStatus`
- `components.schemas.ErrorResponse`
- `components.parameters.IdempotencyKey`

## 11. Versioning

- API Version: v1
- Document Version: 1.0
- Status: Initial company registration document
