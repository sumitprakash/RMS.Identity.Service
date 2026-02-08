# User Signup API — `POST /api/v1/signup`

This document describes the **public user signup API** for the **RMS Identity Service**, including behavior, validation rules, lifecycle, and acceptance criteria.

This documentation is **authoritative** and must stay in sync with `openapi.yaml`.

---

## 1. Purpose

The signup API creates a **platform-level user** (global identity) who is **not yet attached to any company**.

This is **Step-0** of the RMS onboarding flow:

```
Signup (User created)
   ↓
Login
   ↓
Create Company
   ↓
User becomes COMPANY_ADMIN
```

---

## 2. Endpoint Summary

| Attribute | Value |
|---------|------|
| Method | `POST` |
| Path | `/api/v1/signup` |
| Auth | ❌ No authentication required |
| Idempotent | ✅ Yes (via `Idempotency-Key`) |
| OpenAPI Ref | `paths./api/v1/signup.post` |

---

## 3. Request

### Headers

| Header | Required | Description |
|------|----------|-------------|
| `Idempotency-Key` | Optional | UUID used to safely retry the request |

### Request Body

```json
{
  "username": "alice@example.com",
  "password": "P@ssw0rd!",
  "displayName": "Alice Example"
}
```

#### Field rules

| Field | Rules |
|------|------|
| `username` | Required, email format, globally unique |
| `password` | Required, minimum 8 characters |
| `displayName` | Optional |

---

## 4. Successful Response

### `201 Created`

```json
{
  "userUuid": "d6f0d5c2-7c1a-4f2b-b8ef-1d9d1a7e9b33",
  "username": "alice@example.com",
  "displayName": "Alice Example",
  "status": "pending",
  "createdAt": "2026-02-08T08:00:00Z"
}
```

### Meaning

- User is created successfully
- Email verification is required
- User is not associated with any company

---

## 5. Error Responses

### `400 Bad Request`

```json
{
  "code": "invalid_input",
  "message": "username and password are required"
}
```

### `409 Conflict`

```json
{
  "code": "user_exists",
  "message": "username already exists"
}
```

### `429 Too Many Requests`

Rate limit exceeded.

---

## 6. Behavioral Contract

On successful signup, the system MUST:

1. Create a new row in `UserAccount` with `CompanyID = NULL`
2. Hash and store the password securely
3. Generate an email verification token
4. Store hashed verification token in `EmailVerification`
5. Commit the DB transaction
6. After commit:
   - Record an audit entry
   - Send verification email
7. Return `201 Created`

---

## 7. Idempotency Rules

If `Idempotency-Key` is provided:

- First request processes normally
- Response is stored
- Subsequent requests with the same key return the same response
- No duplicate users are created

---

## 8. Security Guarantees

- Passwords are never returned
- Passwords are stored only as hashes
- Tokens are stored hashed
- No roles or company context allowed in signup

---

## 9. Email Verification Flow

1. Signup generates verification token
2. Email sent with verification link
3. Client calls `POST /api/v1/users/verify-email`
4. Server validates token and marks email as verified

---

## 10. Acceptance Criteria

### AC-01 — Successful Signup
- Response is `201`
- User exists with `CompanyID = NULL`
- EmailVerified = false

### AC-02 — Idempotent Retry
- Same `Idempotency-Key` returns same response
- No duplicate users

### AC-03 — Duplicate Username
- Returns `409`
- No new user created

### AC-04 — Transaction Safety
- On DB failure, no user or side effects occur

### AC-05 — Email Failure Safety
- User remains created
- Failure is logged and retried

---

## 11. Relation to OpenAPI

Mapped to:
- `paths./api/v1/signup.post`
- `components.schemas.SignupRequest`
- `components.schemas.UserResponse`
- `components.schemas.ErrorResponse`
- `components.parameters.IdempotencyKey`

---

## 12. Out of Scope

- Company creation
- Login / JWT issuance
- Role assignment
- Password reset

---

## 13. Versioning

- API Version: v1
- Document Version: 1.0
- Status: Stable
