-- =========================================================
-- RMS.Identity.Service — schema_v1_identity.sql
-- Canonical schema for Identity & Company Service
-- UUIDs stored as BINARY(16)
-- Internal PKs are BIGINT AUTO_INCREMENT
-- No foreign keys (application-enforced integrity)
-- =========================================================

SET NAMES utf8mb4;
SET time_zone = '+00:00';

-- =========================================================
-- COMPANY
-- =========================================================
CREATE TABLE Company (
  CompanyID BIGINT AUTO_INCREMENT PRIMARY KEY,
  CompanyUUID BINARY(16) NOT NULL,
  CompanyCode VARCHAR(64),
  LegalName VARCHAR(255) NOT NULL,
  TradeName VARCHAR(255) NULL,
  CompanyGSTIN VARCHAR(32) NOT NULL,
  ContactEmailAddress VARCHAR(150) NOT NULL,
  ContactPhoneNumber VARCHAR(32) NOT NULL,
  AddressLine1 VARCHAR(255) NOT NULL,
  AddressLine2 VARCHAR(255) NULL,
  City VARCHAR(128) NOT NULL,
  State VARCHAR(128) NOT NULL,
  PostalCode VARCHAR(20) NOT NULL,
  Country VARCHAR(2) NOT NULL DEFAULT 'IN',
  CompanyStatus ENUM('pending_verification','verified','rejected','suspended') NOT NULL DEFAULT 'pending_verification',
  IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CreatedBy BIGINT NULL,
  UpdatedAt TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  UpdatedBy BIGINT NULL,
  UNIQUE KEY ux_company_uuid (CompanyUUID),
  UNIQUE KEY ux_company_code (CompanyCode),
  UNIQUE KEY ux_company_gstin (CompanyGSTIN)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================================================
-- USER (Global identity; CompanyID deprecated)
-- =========================================================
CREATE TABLE UserAccount (
  UserID BIGINT AUTO_INCREMENT PRIMARY KEY,
  UserUUID BINARY(16) NOT NULL,
  CompanyID BIGINT NULL,                 -- Deprecated: membership lives in CompanyUser
  Username VARCHAR(150) NOT NULL,        -- email
  PasswordHash VARCHAR(512) NOT NULL,
  DisplayName VARCHAR(255),
  EmailVerified TINYINT(1) NOT NULL DEFAULT 0,
  IsActive TINYINT(1) NOT NULL DEFAULT 1,
  IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
  LastLoginAt TIMESTAMP NULL,
  FailedLoginCount INT NOT NULL DEFAULT 0,
  LockedUntil TIMESTAMP NULL,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CreatedBy BIGINT NULL,
  UpdatedAt TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  UpdatedBy BIGINT NULL,
  UNIQUE KEY ux_user_uuid (UserUUID),
  UNIQUE KEY ux_username_global (Username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================================================
-- COMPANY USER MEMBERSHIP
-- =========================================================
CREATE TABLE CompanyUser (
  CompanyUserID BIGINT AUTO_INCREMENT PRIMARY KEY,
  CompanyID BIGINT NOT NULL,
  UserID BIGINT NOT NULL,
  CompanyRole ENUM('OWNER','ADMIN','MEMBER') NOT NULL,
  MembershipStatus ENUM('active','invited','suspended') NOT NULL DEFAULT 'active',
  InvitedBy BIGINT NULL,
  JoinedAt TIMESTAMP NULL,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CreatedBy BIGINT NULL,
  UpdatedAt TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  UpdatedBy BIGINT NULL,
  UNIQUE KEY ux_company_user (CompanyID, UserID),
  KEY ix_companyuser_company (CompanyID),
  KEY ix_companyuser_user (UserID),
  KEY ix_companyuser_role (CompanyRole)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================================================
-- ROLE (Operational roles; not used for company membership)
-- =========================================================
CREATE TABLE Role (
  RoleID BIGINT AUTO_INCREMENT PRIMARY KEY,
  RoleUUID BINARY(16) NOT NULL,
  Name VARCHAR(64) NOT NULL,
  Description VARCHAR(255) NULL,
  IsSystemRole TINYINT(1) NOT NULL DEFAULT 0,
  IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY ux_role_name (Name),
  UNIQUE KEY ux_role_uuid (RoleUUID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================================================
-- USER ROLE (Operational RBAC; not used for company membership)
-- =========================================================
CREATE TABLE UserRole (
  UserRoleID BIGINT AUTO_INCREMENT PRIMARY KEY,
  UserID BIGINT NOT NULL,
  RoleID BIGINT NOT NULL,
  AssignedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  AssignedBy BIGINT NULL,
  UNIQUE KEY ux_user_role (UserID, RoleID),
  KEY ix_userrole_user (UserID),
  KEY ix_userrole_role (RoleID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================================================
-- REFRESH TOKENS (hashed)
-- =========================================================
CREATE TABLE RefreshToken (
  RefreshTokenID BIGINT AUTO_INCREMENT PRIMARY KEY,
  UserID BIGINT NOT NULL,
  TokenHash VARCHAR(512) NOT NULL,
  ExpiresAt TIMESTAMP NOT NULL,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  RevokedAt TIMESTAMP NULL DEFAULT NULL,
  ReplacedByTokenHash VARCHAR(512) NULL,
  UNIQUE KEY ux_refresh_token_hash (TokenHash),
  KEY ix_refresh_userid (UserID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================================================
-- EMAIL VERIFICATION & PASSWORD RESET TOKENS
-- =========================================================
CREATE TABLE EmailVerification (
  EmailVerificationID BIGINT AUTO_INCREMENT PRIMARY KEY,
  UserID BIGINT NOT NULL,
  TokenHash CHAR(64) NOT NULL,   -- sha256(token)
  Purpose ENUM('email_verification','password_reset') NOT NULL,
  ExpiresAt TIMESTAMP NOT NULL,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  Consumed TINYINT(1) NOT NULL DEFAULT 0,
  KEY ix_ev_userid (UserID),
  KEY ix_ev_tokenhash (TokenHash)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================================================
-- IDEMPOTENCY KEYS (safe retries)
-- =========================================================
CREATE TABLE IdempotencyKey (
  IdempotencyKeyID BIGINT AUTO_INCREMENT PRIMARY KEY,
  KeyValue CHAR(36) NOT NULL,
  Method VARCHAR(10) NOT NULL,
  Route VARCHAR(255) NOT NULL,
  RequestHash CHAR(64) NULL,
  ResponseCode INT NULL,
  ResponseBody JSON NULL,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY ux_idempotency_key (KeyValue),
  KEY ix_idempotency_route (Route)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================================================
-- OUTBOX (future-proof events & email delivery)
-- =========================================================
CREATE TABLE Outbox (
  OutboxID BIGINT AUTO_INCREMENT PRIMARY KEY,
  EventType VARCHAR(128) NOT NULL,
  AggregateType VARCHAR(64) NULL,
  AggregateUUID BINARY(16) NULL,
  Payload JSON NOT NULL,
  Status ENUM('pending','processing','published','failed') NOT NULL DEFAULT 'pending',
  Retries INT NOT NULL DEFAULT 0,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  AvailableAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  KEY ix_outbox_status (Status),
  KEY ix_outbox_available (AvailableAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================================================
-- API KEYS (server-to-server / integrations)
-- =========================================================
CREATE TABLE ApiKey (
  ApiKeyID BIGINT AUTO_INCREMENT PRIMARY KEY,
  ApiKeyUUID BINARY(16) NOT NULL,
  CompanyID BIGINT NOT NULL,
  StoreID BIGINT NULL,
  KeyHash VARCHAR(512) NOT NULL,
  Description VARCHAR(255) NULL,
  IsActive TINYINT(1) NOT NULL DEFAULT 1,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CreatedBy BIGINT NULL,
  UNIQUE KEY ux_apikey_uuid (ApiKeyUUID),
  KEY ix_apikey_company (CompanyID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================================================
-- COMPANY SETTINGS (legacy table name)
-- =========================================================
CREATE TABLE TenantSetting (
  TenantSettingID BIGINT AUTO_INCREMENT PRIMARY KEY,
  CompanyID BIGINT NOT NULL,
  SettingKey VARCHAR(128) NOT NULL,
  SettingValue JSON,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY ux_tenant_setting (CompanyID, SettingKey),
  KEY ix_tenant_company (CompanyID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================================================
-- AUDIT LOG (Identity scope)
-- =========================================================
CREATE TABLE AuditLog (
  AuditID BIGINT AUTO_INCREMENT PRIMARY KEY,
  TableName VARCHAR(128) NOT NULL,
  RecordId VARCHAR(64) NOT NULL,
  Action VARCHAR(32) NOT NULL,
  ActorUserID BIGINT NULL,
  Payload JSON,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  KEY ix_audit_tablename (TableName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =========================================================
-- END OF SCHEMA
-- =========================================================
