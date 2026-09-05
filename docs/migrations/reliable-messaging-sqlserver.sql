-- Stop every writer and take a verified backup before running this migration.
-- Replace the three __...__ tokens. Add one mapping row per distinct legacy MessageType value.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @StoreKey nvarchar(128) = N'__VICIONE_STORE_KEY__';
DECLARE @ContractMap table (
    LegacyMessageType nvarchar(max) PRIMARY KEY,
    ContractIdentity nvarchar(320) NOT NULL
);
INSERT INTO @ContractMap (LegacyMessageType, ContractIdentity)
VALUES (N'__LEGACY_MESSAGE_TYPE__', N'__CONTRACT_IDENTITY__');
-- INSERT INTO @ContractMap (LegacyMessageType, ContractIdentity) VALUES (N'...', N'name@1');

IF @StoreKey LIKE N'__VICIONE[_]%'
    THROW 51000, 'Set an explicit stable ViciOne bus/store key before migration.', 1;
IF EXISTS (
    SELECT 1 FROM dbo.OutboxMessage AS message
    LEFT JOIN @ContractMap AS contract ON contract.LegacyMessageType = message.MessageType
    WHERE contract.LegacyMessageType IS NULL
       OR message.DestinationAddress IS NULL
       OR LTRIM(RTRIM(message.DestinationAddress)) = N''
       OR message.MessageId IS NULL)
    THROW 51001, 'Every retained message needs a destination, message id and explicit contract mapping.', 1;
IF EXISTS (SELECT 1 FROM dbo.OutboxState WHERE BusKey IS NOT NULL AND BusKey <> @StoreKey)
    THROW 51002, 'The configured store key does not match every retained legacy bus key.', 1;

IF OBJECT_ID(N'dbo.vicione_outbox', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.vicione_outbox (
        StoreKey nvarchar(128) NOT NULL,
        Id uniqueidentifier NOT NULL,
        GenerationToken uniqueidentifier NOT NULL,
        ContractIdentity nvarchar(320) NOT NULL,
        DestinationAddress nvarchar(2048) NOT NULL,
        ContentType nvarchar(256) NOT NULL,
        Body varbinary(max) NOT NULL,
        Metadata varbinary(max) NULL,
        MessageId uniqueidentifier NULL,
        CorrelationId uniqueidentifier NULL,
        StorageSize bigint NOT NULL,
        Status int NOT NULL,
        EnqueuedAt datetime2 NOT NULL,
        DueAt datetime2 NULL,
        DeliveryAttempts int NOT NULL,
        NextAttemptAt datetime2 NULL,
        LeaseToken uniqueidentifier NULL,
        LeaseExpiresAt datetime2 NULL,
        QuarantinedAt datetime2 NULL,
        LastFailureKind int NOT NULL,
        LastFailureType nvarchar(512) NULL,
        LastFailureAt datetime2 NULL,
        CONSTRAINT PK_vicione_outbox PRIMARY KEY (StoreKey, Id),
        CONSTRAINT CK_vicione_outbox_storage CHECK (StorageSize >= 0)
    );
    CREATE INDEX IX_vicione_outbox_claim
        ON dbo.vicione_outbox (StoreKey, Status, NextAttemptAt, EnqueuedAt);
    CREATE INDEX IX_vicione_outbox_lease
        ON dbo.vicione_outbox (StoreKey, LeaseExpiresAt);
END;

IF OBJECT_ID(N'dbo.vicione_reliable_capacity', N'U') IS NULL
    CREATE TABLE dbo.vicione_reliable_capacity (
        StoreKey nvarchar(128) NOT NULL PRIMARY KEY,
        StoredCount int NOT NULL CHECK (StoredCount >= 0),
        StoredBytes bigint NOT NULL CHECK (StoredBytes >= 0)
    );

IF OBJECT_ID(N'dbo.vicione_inbox', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.vicione_inbox (
        StoreKey nvarchar(128) NOT NULL,
        MessageId uniqueidentifier NOT NULL,
        ConsumerId uniqueidentifier NOT NULL,
        Status int NOT NULL,
        Attempts int NOT NULL CHECK (Attempts > 0),
        ReceivedAt datetime2 NOT NULL,
        DueAt datetime2 NULL,
        LeaseToken uniqueidentifier NULL,
        LeaseExpiresAt datetime2 NULL,
        FailedAt datetime2 NULL,
        QuarantinedAt datetime2 NULL,
        CompletedAt datetime2 NULL,
        FailureType nvarchar(512) NULL,
        CONSTRAINT PK_vicione_inbox PRIMARY KEY (StoreKey, MessageId, ConsumerId)
    );
    CREATE INDEX IX_vicione_inbox_due ON dbo.vicione_inbox (StoreKey, Status, DueAt, ReceivedAt);
    CREATE INDEX IX_vicione_inbox_lease ON dbo.vicione_inbox (StoreKey, LeaseExpiresAt);
END;

IF OBJECT_ID(N'dbo.vicione_recurring_schedule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.vicione_recurring_schedule (
        StoreKey nvarchar(128) NOT NULL,
        ScheduleId uniqueidentifier NOT NULL,
        CronExpression nvarchar(128) NOT NULL,
        TimeZoneId nvarchar(128) NOT NULL,
        NextDueAt datetime2 NOT NULL,
        IsPaused bit NOT NULL,
        CreatedAt datetime2 NOT NULL,
        CONSTRAINT PK_vicione_recurring_schedule PRIMARY KEY (StoreKey, ScheduleId)
    );
    CREATE INDEX IX_vicione_recurring_schedule_due
        ON dbo.vicione_recurring_schedule (StoreKey, IsPaused, NextDueAt);
END;

INSERT INTO dbo.vicione_outbox (
    StoreKey, Id, GenerationToken, ContractIdentity, DestinationAddress, ContentType,
    Body, Metadata, MessageId, CorrelationId, StorageSize, Status, EnqueuedAt, DueAt,
    DeliveryAttempts, NextAttemptAt, LeaseToken, LeaseExpiresAt, QuarantinedAt,
    LastFailureKind, LastFailureType, LastFailureAt)
SELECT
    @StoreKey,
    message.MessageId,
    message.MessageId,
    contract.ContractIdentity,
    message.DestinationAddress,
    message.ContentType,
    CONVERT(varbinary(max), message.Body),
    0x,
    message.MessageId,
    message.CorrelationId,
    DATALENGTH(CONVERT(varbinary(max), message.Body)),
    CASE WHEN state.Status = 3 THEN 2 WHEN state.Status = 1 THEN 1 ELSE 0 END,
    message.SentTime,
    message.EnqueueTime,
    ISNULL(state.DeliveryAttempts, 0),
    COALESCE(state.NextDeliveryTime, message.EnqueueTime),
    NULL, NULL,
    CASE WHEN state.Status = 3 THEN state.LastFailureTime ELSE NULL END,
    CASE state.LastFailureKind WHEN 1 THEN 1 WHEN 2 THEN 2 WHEN 3 THEN 5 WHEN 4 THEN 4 WHEN 5 THEN 3 ELSE 0 END,
    CASE WHEN state.Status = 3 THEN LEFT(state.LastFailure, 512) ELSE NULL END,
    state.LastFailureTime
FROM dbo.OutboxMessage AS message
JOIN @ContractMap AS contract ON contract.LegacyMessageType = message.MessageType
LEFT JOIN dbo.OutboxState AS state ON state.OutboxId = message.OutboxId
WHERE ISNULL(state.Status, 0) <> 2
  AND NOT EXISTS (
      SELECT 1 FROM dbo.vicione_outbox AS current
      WHERE current.StoreKey = @StoreKey AND current.Id = message.MessageId);

INSERT INTO dbo.vicione_inbox (
    StoreKey, MessageId, ConsumerId, Status, Attempts, ReceivedAt, DueAt,
    LeaseToken, LeaseExpiresAt, FailedAt, QuarantinedAt, CompletedAt, FailureType)
SELECT
    @StoreKey,
    inbox.MessageId,
    inbox.ConsumerId,
    CASE WHEN inbox.Consumed IS NULL THEN 0 ELSE 1 END,
    CASE WHEN inbox.ReceiveCount < 1 THEN 1 ELSE inbox.ReceiveCount END,
    inbox.Received,
    NULL, NULL, NULL, NULL, NULL,
    inbox.Consumed,
    NULL
FROM dbo.InboxState AS inbox
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.vicione_inbox AS current
    WHERE current.StoreKey = @StoreKey
      AND current.MessageId = inbox.MessageId
      AND current.ConsumerId = inbox.ConsumerId);

MERGE dbo.vicione_reliable_capacity WITH (HOLDLOCK) AS target
USING (
    SELECT @StoreKey AS StoreKey, COUNT_BIG(*) AS StoredCount, ISNULL(SUM(StorageSize), 0) AS StoredBytes
    FROM dbo.vicione_outbox WHERE StoreKey = @StoreKey
) AS source
ON target.StoreKey = source.StoreKey
WHEN MATCHED THEN UPDATE SET
    StoredCount = CONVERT(int, source.StoredCount), StoredBytes = source.StoredBytes
WHEN NOT MATCHED THEN INSERT (StoreKey, StoredCount, StoredBytes)
    VALUES (source.StoreKey, CONVERT(int, source.StoredCount), source.StoredBytes);

COMMIT TRANSACTION;
