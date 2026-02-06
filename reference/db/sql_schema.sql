-- RMS.Identity.Service schema_v1.sql
-- Identity & Tenant service canonical schema (aligned to RMS Master Context)
-- UUIDs stored as BINARY(16). Internal PKs are BIGINT AUTO_INCREMENT.
-- Referential integrity is enforced in application layer (no FKs by policy).

CREATE TABLE Company (
  CompanyID BIGINT AUTO_INCREMENT PRIMARY KEY,
  CompanyUUID BINARY(16) NOT NULL,
  CompanyCode VARCHAR(64),
  CompanyName VARCHAR(255) NOT NULL,
  CompanyGSTIN VARCHAR(32),
  IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CreatedBy BIGINT NULL,
  UpdatedAt TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  UpdatedBy BIGINT NULL,
  UNIQUE KEY ux_company_uuid (CompanyUUID),
  UNIQUE KEY ux_company_code (CompanyCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE UserAccount (
  UserID BIGINT AUTO_INCREMENT PRIMARY KEY,
  UserUUID BINARY(16) NOT NULL,
  CompanyID BIGINT NOT NULL,
  Username VARCHAR(150) NOT NULL,
  PasswordHash VARCHAR(512) NOT NULL,
  DisplayName VARCHAR(255),
  Roles JSON NOT NULL, -- array of role strings e.g. ["COMPANY_ADMIN","CASHIER"]
  IsActive TINYINT(1) NOT NULL DEFAULT 1,
  IsDeleted TINYINT(1) NOT NULL DEFAULT 0,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CreatedBy BIGINT NULL,
  UpdatedAt TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  UpdatedBy BIGINT NULL,
  UNIQUE KEY ux_user_uuid (UserUUID),
  UNIQUE KEY ux_company_username (CompanyID, Username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

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

CREATE TABLE TenantSetting (
  TenantSettingID BIGINT AUTO_INCREMENT PRIMARY KEY,
  CompanyID BIGINT NOT NULL,
  SettingKey VARCHAR(128) NOT NULL,
  SettingValue JSON,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY ux_tenant_setting (CompanyID, SettingKey),
  KEY ix_tenant_company (CompanyID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Lightweight AuditLog for identity-related changes (optional)
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

-- Notes:
-- 1) CompanyID references are application-enforced; no DB foreign-key constraints by project decision.
-- 2) Use UTF8MB4 charset for international text.
-- 3) Store UUIDs as BINARY(16) using client & EF Core ValueConverter.