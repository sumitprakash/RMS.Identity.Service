-- Apply once to databases created before the review-fix branch.
ALTER TABLE UserAccount
  ADD COLUMN PhoneNumber VARCHAR(32) NULL AFTER DisplayName,
  ADD COLUMN PasswordSetupRequired TINYINT(1) NOT NULL DEFAULT 0 AFTER PhoneNumber;

ALTER TABLE RefreshToken
  ADD KEY ix_refresh_expiry (ExpiresAt);

ALTER TABLE EmailVerification
  ADD KEY ix_ev_created (CreatedAt);

ALTER TABLE IdempotencyKey
  ADD KEY ix_idempotency_created (CreatedAt);

ALTER TABLE Outbox
  ADD KEY ix_outbox_status_created (Status, CreatedAt);
