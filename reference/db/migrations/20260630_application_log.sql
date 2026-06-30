-- Apply once to databases created before application log persistence was added.
CREATE TABLE IF NOT EXISTS ApplicationLog (
  ApplicationLogID BIGINT AUTO_INCREMENT PRIMARY KEY,
  CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  LogLevel VARCHAR(32) NOT NULL,
  Category VARCHAR(255) NOT NULL,
  EventID INT NOT NULL DEFAULT 0,
  CorrelationTraceID VARCHAR(128) NULL,
  Message TEXT NOT NULL,
  Exception LONGTEXT NULL,
  KEY ix_applicationlog_created (CreatedAt),
  KEY ix_applicationlog_level_created (LogLevel, CreatedAt),
  KEY ix_applicationlog_correlation (CorrelationTraceID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
