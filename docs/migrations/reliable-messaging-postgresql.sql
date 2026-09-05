-- Stop every writer and take a verified backup before running this migration.
-- Replace the three __...__ tokens. Add one mapping row per distinct legacy MessageType value.
BEGIN;

CREATE TEMP TABLE vicione_reliable_settings (
    store_key varchar(128) PRIMARY KEY
) ON COMMIT DROP;
INSERT INTO vicione_reliable_settings (store_key) VALUES ('__VICIONE_STORE_KEY__');

CREATE TEMP TABLE vicione_reliable_contract_map (
    legacy_message_type text PRIMARY KEY,
    contract_identity varchar(320) NOT NULL
) ON COMMIT DROP;
INSERT INTO vicione_reliable_contract_map (legacy_message_type, contract_identity)
VALUES ('__LEGACY_MESSAGE_TYPE__', '__CONTRACT_IDENTITY__');
-- INSERT INTO vicione_reliable_contract_map (legacy_message_type, contract_identity) VALUES ('...', 'name@1');

DO $guard$
BEGIN
    IF EXISTS (SELECT 1 FROM vicione_reliable_settings WHERE store_key LIKE '__VICIONE_%') THEN
        RAISE EXCEPTION 'Set an explicit stable ViciOne bus/store key before migration';
    END IF;
    IF EXISTS (
        SELECT 1 FROM "OutboxMessage" AS message
        LEFT JOIN vicione_reliable_contract_map AS contract
          ON contract.legacy_message_type = message."MessageType"
        WHERE contract.legacy_message_type IS NULL
           OR message."DestinationAddress" IS NULL
           OR btrim(message."DestinationAddress") = ''
           OR message."MessageId" IS NULL
    ) THEN
        RAISE EXCEPTION 'Every retained message needs a destination, message id and explicit contract mapping';
    END IF;
    IF EXISTS (
        SELECT 1 FROM "OutboxState" AS state, vicione_reliable_settings AS settings
        WHERE state."BusKey" IS NOT NULL AND state."BusKey" <> settings.store_key
    ) THEN
        RAISE EXCEPTION 'The configured store key does not match every retained legacy bus key';
    END IF;
END
$guard$;

CREATE TABLE IF NOT EXISTS vicione_outbox (
    "StoreKey" varchar(128) NOT NULL,
    "Id" uuid NOT NULL,
    "GenerationToken" uuid NOT NULL,
    "ContractIdentity" varchar(320) NOT NULL,
    "DestinationAddress" varchar(2048) NOT NULL,
    "ContentType" varchar(256) NOT NULL,
    "Body" bytea NOT NULL,
    "Metadata" bytea NULL,
    "MessageId" uuid NULL,
    "CorrelationId" uuid NULL,
    "StorageSize" bigint NOT NULL,
    "Status" integer NOT NULL,
    "EnqueuedAt" timestamp with time zone NOT NULL,
    "DueAt" timestamp with time zone NULL,
    "DeliveryAttempts" integer NOT NULL,
    "NextAttemptAt" timestamp with time zone NULL,
    "LeaseToken" uuid NULL,
    "LeaseExpiresAt" timestamp with time zone NULL,
    "QuarantinedAt" timestamp with time zone NULL,
    "LastFailureKind" integer NOT NULL,
    "LastFailureType" varchar(512) NULL,
    "LastFailureAt" timestamp with time zone NULL,
    CONSTRAINT "PK_vicione_outbox" PRIMARY KEY ("StoreKey", "Id"),
    CONSTRAINT "CK_vicione_outbox_storage" CHECK ("StorageSize" >= 0)
);
CREATE INDEX IF NOT EXISTS "IX_vicione_outbox_claim"
    ON vicione_outbox ("StoreKey", "Status", "NextAttemptAt", "EnqueuedAt");
CREATE INDEX IF NOT EXISTS "IX_vicione_outbox_lease"
    ON vicione_outbox ("StoreKey", "LeaseExpiresAt");

CREATE TABLE IF NOT EXISTS vicione_reliable_capacity (
    "StoreKey" varchar(128) PRIMARY KEY,
    "StoredCount" integer NOT NULL CHECK ("StoredCount" >= 0),
    "StoredBytes" bigint NOT NULL CHECK ("StoredBytes" >= 0)
);

CREATE TABLE IF NOT EXISTS vicione_inbox (
    "StoreKey" varchar(128) NOT NULL,
    "MessageId" uuid NOT NULL,
    "ConsumerId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "Attempts" integer NOT NULL CHECK ("Attempts" > 0),
    "ReceivedAt" timestamp with time zone NOT NULL,
    "DueAt" timestamp with time zone NULL,
    "LeaseToken" uuid NULL,
    "LeaseExpiresAt" timestamp with time zone NULL,
    "FailedAt" timestamp with time zone NULL,
    "QuarantinedAt" timestamp with time zone NULL,
    "CompletedAt" timestamp with time zone NULL,
    "FailureType" varchar(512) NULL,
    CONSTRAINT "PK_vicione_inbox" PRIMARY KEY ("StoreKey", "MessageId", "ConsumerId")
);
CREATE INDEX IF NOT EXISTS "IX_vicione_inbox_due"
    ON vicione_inbox ("StoreKey", "Status", "DueAt", "ReceivedAt");
CREATE INDEX IF NOT EXISTS "IX_vicione_inbox_lease"
    ON vicione_inbox ("StoreKey", "LeaseExpiresAt");

CREATE TABLE IF NOT EXISTS vicione_recurring_schedule (
    "StoreKey" varchar(128) NOT NULL,
    "ScheduleId" uuid NOT NULL,
    "CronExpression" varchar(128) NOT NULL,
    "TimeZoneId" varchar(128) NOT NULL,
    "NextDueAt" timestamp with time zone NOT NULL,
    "IsPaused" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_vicione_recurring_schedule" PRIMARY KEY ("StoreKey", "ScheduleId")
);
CREATE INDEX IF NOT EXISTS "IX_vicione_recurring_schedule_due"
    ON vicione_recurring_schedule ("StoreKey", "IsPaused", "NextDueAt");

INSERT INTO vicione_outbox (
    "StoreKey", "Id", "GenerationToken", "ContractIdentity", "DestinationAddress", "ContentType",
    "Body", "Metadata", "MessageId", "CorrelationId", "StorageSize", "Status", "EnqueuedAt", "DueAt",
    "DeliveryAttempts", "NextAttemptAt", "LeaseToken", "LeaseExpiresAt", "QuarantinedAt",
    "LastFailureKind", "LastFailureType", "LastFailureAt")
SELECT
    settings.store_key,
    message."MessageId",
    message."MessageId",
    contract.contract_identity,
    message."DestinationAddress",
    message."ContentType",
    convert_to(message."Body", 'UTF8'),
    ''::bytea,
    message."MessageId",
    message."CorrelationId",
    octet_length(convert_to(message."Body", 'UTF8')),
    CASE WHEN state."Status" = 3 THEN 2 WHEN state."Status" = 1 THEN 1 ELSE 0 END,
    message."SentTime",
    message."EnqueueTime",
    COALESCE(state."DeliveryAttempts", 0),
    COALESCE(state."NextDeliveryTime", message."EnqueueTime"),
    NULL, NULL,
    CASE WHEN state."Status" = 3 THEN state."LastFailureTime" ELSE NULL END,
    CASE state."LastFailureKind" WHEN 1 THEN 1 WHEN 2 THEN 2 WHEN 3 THEN 5 WHEN 4 THEN 4 WHEN 5 THEN 3 ELSE 0 END,
    CASE WHEN state."Status" = 3 THEN left(state."LastFailure", 512) ELSE NULL END,
    state."LastFailureTime"
FROM "OutboxMessage" AS message
CROSS JOIN vicione_reliable_settings AS settings
JOIN vicione_reliable_contract_map AS contract
  ON contract.legacy_message_type = message."MessageType"
LEFT JOIN "OutboxState" AS state ON state."OutboxId" = message."OutboxId"
WHERE COALESCE(state."Status", 0) <> 2
ON CONFLICT ("StoreKey", "Id") DO NOTHING;

INSERT INTO vicione_inbox (
    "StoreKey", "MessageId", "ConsumerId", "Status", "Attempts", "ReceivedAt", "DueAt",
    "LeaseToken", "LeaseExpiresAt", "FailedAt", "QuarantinedAt", "CompletedAt", "FailureType")
SELECT
    settings.store_key,
    inbox."MessageId",
    inbox."ConsumerId",
    CASE WHEN inbox."Consumed" IS NULL THEN 0 ELSE 1 END,
    GREATEST(inbox."ReceiveCount", 1),
    inbox."Received",
    NULL, NULL, NULL, NULL, NULL,
    inbox."Consumed",
    NULL
FROM "InboxState" AS inbox
CROSS JOIN vicione_reliable_settings AS settings
ON CONFLICT ("StoreKey", "MessageId", "ConsumerId") DO NOTHING;

INSERT INTO vicione_reliable_capacity ("StoreKey", "StoredCount", "StoredBytes")
SELECT settings.store_key, count(outbox."Id")::integer, COALESCE(sum(outbox."StorageSize"), 0)
FROM vicione_reliable_settings AS settings
LEFT JOIN vicione_outbox AS outbox ON outbox."StoreKey" = settings.store_key
GROUP BY settings.store_key
ON CONFLICT ("StoreKey") DO UPDATE SET
    "StoredCount" = EXCLUDED."StoredCount",
    "StoredBytes" = EXCLUDED."StoredBytes";

COMMIT;
