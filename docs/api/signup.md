# User Signup API - `POST /api/v1/signup`

This document describes the public user signup API for the RMS Identity Service. It must stay in sync with `reference/openapi/openapi.yaml`.

## 1. Purpose

Signup creates only a global `UserAccount`.

Company registration is a separate authenticated flow:

1. User signs up.
2. User logs in and receives a user-level access token after the account is allowed to authenticate.
3. Client calls `GET /api/v1/current-user/companies`.
4. User registers a company with `POST /api/v1/companies`.

Email verification or other account activation flows are outside the scope of this company registration change.

## 2. Endpoint Summary

| Attribute | Value |
| --- | --- |
| Method | `POST` |
| Path | `/api/v1/signup` |
| Auth | No authentication required |
| Idempotent | Yes, via `Idempotency-Key` |
| OpenAPI Ref | `paths./api/v1/signup.post` |

## 3. Request

### Headers

| Header | Required | Description |
| --- | --- | --- |
| `Idempotency-Key` | Yes | UUID used to safely retry the request |

### Request Body

```json
{
  "emailAddress": "owner@example.com",
  "password": "StrongPass@123",
  "firstName": "First",
  "middleName": "Middle",
  "lastName": "Last",
  "phoneNumber": "+919876543210"
}
```

### Field Rules

| Field | Rules |
| --- | --- |
| `emailAddress` | Required, valid email, globally unique |
| `password` | Required, minimum 8 characters |
| `firstName` | Required |
| `middleName` | Optional |
| `lastName` | Required |
| `phoneNumber` | Required, valid phone number |

## 4. Successful Response

### `201 Created`

```json
{
  "userUuid": "d6f0d5c2-7c1a-4f2b-b8ef-1d9d1a7e9b33",
  "emailAddress": "owner@example.com",
  "status": "pending",
  "createdAt": "2026-02-08T08:00:00Z"
}
```

### Meaning

- User is created as a global identity.
- The validated phone number is stored on the user account.
- `UserAccount.CompanyID` remains `NULL`; it is not the source of company membership.
- No company or `CompanyUser` membership is created during signup.
- User remains `pending` until a future account activation flow updates it.

## 5. Error Responses

### `400 Bad Request`

Validation failures return:

```json
{
  "code": "VALIDATION_ERROR",
  "message": "Request validation failed."
}
```

Validator-specific example:

```json
{
  "code": "VALIDATION_ERROR",
  "message": "Email address must be a valid email address."
}
```

### `409 Conflict`

Duplicate user email:

```json
{
  "code": "USER_EXISTS",
  "message": "Email address already exists."
}
```

## 6. Behavioral Contract

On successful signup, the system must complete the following in one database transaction:

1. Validate the request before the controller processes it.
2. Normalize user email.
3. Check duplicate user email.
4. Create `UserAccount` with `CompanyID = NULL`.
5. Persist the normalized phone number.
6. Record the signup audit entry.
7. Return `201 Created`.

## 7. Idempotency Rules

For every request, `Idempotency-Key` is required:

- First request processes normally.
- Response is stored.
- Subsequent requests with the same key and same payload return the stored response.
- Reusing the same key with a different payload returns `409 IDEMPOTENCY_KEY_REUSED`.
- No duplicate user rows are created.

## 8. Security Guarantees

- Passwords are never returned.
- Passwords are stored only as hashes.
- Signup does not accept company details.
- Signup does not accept roles or permissions.

## 9. Acceptance Criteria

### AC-01 - Successful Signup

- Response is `201`.
- User exists with `CompanyID = NULL`.
- `EmailVerified = false`.
- No `Company` row is created.
- No `CompanyUser` row is created.

### AC-02 - Idempotent Retry

- Same `Idempotency-Key` and same payload returns the same response.
- No duplicate user row is created.

### AC-03 - Duplicate Username

- Returns `409 USER_EXISTS`.
- No new user is committed.

## 10. Relation to OpenAPI

Mapped to:

- `paths./api/v1/signup.post`
- `components.schemas.SignupRequest`
- `components.schemas.SignupResponse`
- `components.schemas.ErrorResponse`
- `components.parameters.IdempotencyKey`

## 11. Related Flow

Company registration is documented in `docs/api/companies.md`.

## 12. Versioning

- API Version: v1
- Document Version: 3.0
- Status: Updated for separate user signup and company registration
