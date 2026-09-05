-- Stop every writer and take a verified backup before running this migration.
-- Replace the three __...__ tokens. Add one mapping row per distinct legacy MessageType value.
BEGIN IMMEDIATE;

CREATE TEMP TABLE _vicione_reliable_settings (
    StoreKey TEXT NOT NULL CHECK (length(StoreKey) BETWEEN 1 AND 128)
);
INSERT INTO _vicione_reliable_settings (StoreKey) VALUES ('__VICIONE_STORE_KEY__');

CREATE TEMP TABLE _vicione_reliable_contract_map (
    LegacyMessageType TEXT PRIMARY KEY,
    ContractIdentity TEXT NOT NULL CHECK (length(ContractIdentity) BETWEEN 3 AND 320)
);
INSERT INTO _vicione_reliable_contract_map (LegacyMessageType, ContractIdentity)
VALUES ('__LEGACY_MESSAGE_TYPE__', '__CONTRACT_IDENTITY__');
-- INSERT INTO _vicione_reliable_contract_map (LegacyMessageType, ContractIdentity) VALUES ('...', 'name@1');

CREATE TEMP TABLE _vicione_reliable_guard (Accepted INTEGER NOT NULL CHECK (Accepted = 1));
INSERT INTO _vicione_reliable_guard
SELECT CASE WHEN
    (SELECT StoreKey FROM _vicione_reliable_settings) NOT LIKE '__VICIONE_%'
    AND NOT EXISTS (
        SELECT 1 FROM OutboxMessage AS message
        LEFT JOIN _vicione_reliable_contract_map AS contract
          ON contract.LegacyMessageType = message.MessageType
        WHERE contract.LegacyMessageType IS NULL
           OR message.DestinationAddress IS NULL
           OR trim(message.DestinationAddress) = ''
           OR message.MessageId IS NULL
    )
    AND NOT EXISTS (
        SELECT 1 FROM OutboxState AS state
        WHERE state.BusKey IS NOT NULL
          AND state.BusKey <> (SELECT StoreKey FROM _vicione_reliable_settings)
    )
THEN 1 ELSE 0 END;

CREATE TABLE IF NOT EXISTS vicione_outbox (
    StoreKey TEXT NOT NULL,
    Id TEXT NOT NULL,
    GenerationToken TEXT NOT NULL,
    ContractIdentity TEXT NOT NULL,
    DestinationAddress TEXT NOT NULL,
    ContentType TEXT NOT NULL,
    Body BLOB NOT NULL,
    Metadata BLOB NULL,
    MessageId TEXT NULL,
    CorrelationId TEXT NULL,
    StorageSize INTEGER NOT NULL,
    Status INTEGER NOT NULL,
    EnqueuedAt TEXT NOT NULL,
    DueAt TEXT NULL,
    DeliveryAttempts INTEGER NOT NULL,
    NextAttemptAt TEXT NULL,
    LeaseToken TEXT NULL,
    LeaseExpiresAt TEXT NULL,
    QuarantinedAt TEXT NULL,
    LastFailureKind INTEGER NOT NULL,
    LastFailureType TEXT NULL,
    LastFailureAt TEXT NULL,
    CONSTRAINT PK_vicione_outbox PRIMARY KEY (StoreKey, Id),
    CONSTRAINT CK_vicione_outbox_store_key CHECK (length(StoreKey) BETWEEN 1 AND 128),
    CONSTRAINT CK_vicione_outbox_failure_type CHECK (LastFailureType IS NULL OR length(LastFailureType) <= 512)
);
CREATE INDEX IF NOT EXISTS IX_vicione_outbox_claim
    ON vicione_outbox (StoreKey, Status, NextAttemptAt, EnqueuedAt);
CREATE INDEX IF NOT EXISTS IX_vicione_outbox_lease
    ON vicione_outbox (StoreKey, LeaseExpiresAt);

CREATE TABLE IF NOT EXISTS vicione_reliable_capacity (
    StoreKey TEXT NOT NULL PRIMARY KEY,
    StoredCount INTEGER NOT NULL,
    StoredBytes INTEGER NOT NULL,
    CONSTRAINT CK_vicione_reliable_capacity_nonnegative
        CHECK (StoredCount >= 0 AND StoredBytes >= 0)
);

CREATE TABLE IF NOT EXISTS vicione_inbox (
    StoreKey TEXT NOT NULL,
    MessageId TEXT NOT NULL,
    ConsumerId TEXT NOT NULL,
    Status INTEGER NOT NULL,
    Attempts INTEGER NOT NULL,
    ReceivedAt TEXT NOT NULL,
    DueAt TEXT NULL,
    LeaseToken TEXT NULL,
    LeaseExpiresAt TEXT NULL,
    FailedAt TEXT NULL,
    QuarantinedAt TEXT NULL,
    CompletedAt TEXT NULL,
    FailureType TEXT NULL,
    CONSTRAINT PK_vicione_inbox PRIMARY KEY (StoreKey, MessageId, ConsumerId),
    CONSTRAINT CK_vicione_inbox_store_key CHECK (length(StoreKey) BETWEEN 1 AND 128),
    CONSTRAINT CK_vicione_inbox_attempts CHECK (Attempts > 0),
    CONSTRAINT CK_vicione_inbox_failure_type CHECK (FailureType IS NULL OR length(FailureType) <= 512)
);
CREATE INDEX IF NOT EXISTS IX_vicione_inbox_due
    ON vicione_inbox (StoreKey, Status, DueAt, ReceivedAt);
CREATE INDEX IF NOT EXISTS IX_vicione_inbox_lease
    ON vicione_inbox (StoreKey, LeaseExpiresAt);

CREATE TABLE IF NOT EXISTS vicione_recurring_schedule (
    StoreKey TEXT NOT NULL,
    ScheduleId TEXT NOT NULL,
    CronExpression TEXT NOT NULL,
    TimeZoneId TEXT NOT NULL,
    NextDueAt TEXT NOT NULL,
    IsPaused INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    CONSTRAINT PK_vicione_recurring_schedule PRIMARY KEY (StoreKey, ScheduleId),
    CONSTRAINT CK_vicione_recurring_schedule_store_key CHECK (length(StoreKey) BETWEEN 1 AND 128),
    CONSTRAINT CK_vicione_recurring_schedule_cron CHECK (length(CronExpression) BETWEEN 1 AND 128),
    CONSTRAINT CK_vicione_recurring_schedule_timezone CHECK (length(TimeZoneId) BETWEEN 1 AND 128)
);
CREATE INDEX IF NOT EXISTS IX_vicione_recurring_schedule_due
    ON vicione_recurring_schedule (StoreKey, IsPaused, NextDueAt);

INSERT INTO vicione_outbox (
    StoreKey, Id, GenerationToken, ContractIdentity, DestinationAddress, ContentType,
    Body, Metadata, MessageId, CorrelationId, StorageSize, Status, EnqueuedAt, DueAt,
    DeliveryAttempts, NextAttemptAt, LeaseToken, LeaseExpiresAt, QuarantinedAt,
    LastFailureKind, LastFailureType, LastFailureAt)
SELECT
    settings.StoreKey,
    message.MessageId,
    message.MessageId,
    contract.ContractIdentity,
    message.DestinationAddress,
    message.ContentType,
    CAST(message.Body AS BLOB),
    X'',
    message.MessageId,
    message.CorrelationId,
    length(CAST(message.Body AS BLOB)),
    CASE WHEN state.Status = 3 THEN 2 WHEN state.Status = 1 THEN 1 ELSE 0 END,
    message.SentTime,
    message.EnqueueTime,
    COALESCE(state.DeliveryAttempts, 0),
    COALESCE(state.NextDeliveryTime, message.EnqueueTime),
    NULL,
    NULL,
    CASE WHEN state.Status = 3 THEN state.LastFailureTime ELSE NULL END,
    CASE state.LastFailureKind WHEN 1 THEN 1 WHEN 2 THEN 2 WHEN 3 THEN 5 WHEN 4 THEN 4 WHEN 5 THEN 3 ELSE 0 END,
    CASE WHEN state.Status = 3 THEN substr(state.LastFailure, 1, 512) ELSE NULL END,
    state.LastFailureTime
FROM OutboxMessage AS message
CROSS JOIN _vicione_reliable_settings AS settings
JOIN _vicione_reliable_contract_map AS contract
  ON contract.LegacyMessageType = message.MessageType
LEFT JOIN OutboxState AS state
  ON state.OutboxId = message.OutboxId
WHERE COALESCE(state.Status, 0) <> 2
  AND NOT EXISTS (
      SELECT 1 FROM vicione_outbox AS current
      WHERE current.StoreKey = settings.StoreKey AND current.Id = message.MessageId
  );

INSERT INTO vicione_inbox (
    StoreKey, MessageId, ConsumerId, Status, Attempts, ReceivedAt, DueAt,
    LeaseToken, LeaseExpiresAt, FailedAt, QuarantinedAt, CompletedAt, FailureType)
SELECT
    settings.StoreKey,
    inbox.MessageId,
    inbox.ConsumerId,
    CASE WHEN inbox.Consumed IS NULL THEN 0 ELSE 1 END,
    CASE WHEN inbox.ReceiveCount < 1 THEN 1 ELSE inbox.ReceiveCount END,
    inbox.Received,
    NULL, NULL, NULL, NULL, NULL,
    inbox.Consumed,
    NULL
FROM InboxState AS inbox
CROSS JOIN _vicione_reliable_settings AS settings
WHERE NOT EXISTS (
    SELECT 1 FROM vicione_inbox AS current
    WHERE current.StoreKey = settings.StoreKey
      AND current.MessageId = inbox.MessageId
      AND current.ConsumerId = inbox.ConsumerId
);

INSERT INTO vicione_reliable_capacity (StoreKey, StoredCount, StoredBytes)
SELECT settings.StoreKey, count(outbox.Id), COALESCE(sum(outbox.StorageSize), 0)
FROM _vicione_reliable_settings AS settings
LEFT JOIN vicione_outbox AS outbox ON outbox.StoreKey = settings.StoreKey
GROUP BY settings.StoreKey
ON CONFLICT (StoreKey) DO UPDATE SET
    StoredCount = excluded.StoredCount,
    StoredBytes = excluded.StoredBytes;

DROP TABLE _vicione_reliable_guard;
DROP TABLE _vicione_reliable_contract_map;
DROP TABLE _vicione_reliable_settings;
COMMIT;
