using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Creates, removes, and provisions the PostgreSQL database infrastructure required by the SQL transport.</summary>
internal sealed class PostgreSqlDatabaseMigrator :
    ISqlTransportDatabaseMigrator
{
    const string DbExistsSql = "SELECT COUNT(*) FROM pg_database WHERE datname = @Name";
    const string DbCreateSql = """CREATE DATABASE "{0}" """;
    const string SchemaCreateSql = """CREATE SCHEMA IF NOT EXISTS "{0}" """;
    const string GrantConnectSql = """GRANT CONNECT ON DATABASE "{0}" to "{1}";""";
    const string DropSql = """DROP DATABASE "{0}" WITH (force)""";
    const string RoleExistsSql = "SELECT COUNT(*) FROM pg_catalog.pg_roles WHERE rolname = @Name";
    const string CreateRoleSql = """CREATE ROLE "{0}" """;
    const string GrantRoleToPrincipalSql = """GRANT "{0}" TO "{1}";""";
    const string BodyExactColumnTypeSql = """
        SELECT a.atttypid = 'pg_catalog.json'::regtype
        FROM pg_catalog.pg_attribute a
        JOIN pg_catalog.pg_class c ON c.oid = a.attrelid
        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = @Schema AND c.relname = 'message'
            AND a.attname = 'body_exact' AND NOT a.attisdropped
        """;
    const string AddBodyExactColumnSql = """ALTER TABLE "{0}".message ADD COLUMN IF NOT EXISTS body_exact json""";
    const string PrepareBodyExactMigrationSql = """SET LOCAL ROLE "{0}"; SET LOCAL lock_timeout = '5s';""";

    const string GrantRoleSql = """
        GRANT USAGE ON SCHEMA "{1}" TO "{0}";
        ALTER SCHEMA "{1}" OWNER TO "{0}";
        GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA "{1}" TO "{0}";
        GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA "{1}" TO "{0}";
        ALTER DEFAULT PRIVILEGES IN SCHEMA "{1}" GRANT ALL PRIVILEGES ON TABLES TO "{0}";
        ALTER DEFAULT PRIVILEGES IN SCHEMA "{1}" GRANT ALL PRIVILEGES ON SEQUENCES TO "{0}";
        """;

    const string CreateUserSql = """
        CREATE OR REPLACE FUNCTION pg_temp.vicione_create_transport_user(role_name text, user_name text, user_password text)
        RETURNS void
        LANGUAGE plpgsql
        AS $$
        BEGIN
            EXECUTE format('CREATE USER %I WITH PASSWORD %L', user_name, user_password);
            EXECUTE format('GRANT %I TO %I', role_name, user_name);
        END;
        $$;
        SELECT pg_temp.vicione_create_transport_user(@RoleName, @Username, @Password);
        """;

    const string CreateInfrastructureSql = """
        SET ROLE "{1}";

        CREATE OR REPLACE FUNCTION "{0}".create_constraint_if_not_exists (t_name text, c_name text, constraint_sql text)
        RETURNS void AS
        $$
        BEGIN
            IF NOT EXISTS (SELECT constraint_name
                           FROM information_schema.constraint_column_usage
                           WHERE table_name = t_name AND table_schema = '{0}'
                           AND constraint_name = c_name AND constraint_schema = '{0}') THEN
                EXECUTE constraint_sql;
            END IF;
        END;
        $$ LANGUAGE plpgsql;

        CREATE OR REPLACE FUNCTION "{0}".create_index_if_not_exists (i_name text, index_sql text)
        RETURNS void AS
        $$
        BEGIN
            IF NOT EXISTS (SELECT indexname
                          FROM pg_indexes
                          WHERE schemaname = '{0}'
                          AND indexname = i_name) THEN
                EXECUTE index_sql;
            END IF;
        END;
        $$ LANGUAGE plpgsql;

        CREATE OR REPLACE FUNCTION "{0}".add_column_if_not_exists (t_name text, c_name text, alter_sql text)
        RETURNS void AS
        $$
        BEGIN
            IF NOT EXISTS (SELECT column_name
                          FROM information_schema.columns
                          WHERE table_schema = '{0}' AND table_name = t_name AND column_name = c_name) THEN
                EXECUTE alter_sql;
            END IF;
        END;
        $$ LANGUAGE plpgsql;

        CREATE SEQUENCE IF NOT EXISTS "{0}".topology_seq AS bigint;

        CREATE TABLE IF NOT EXISTS "{0}".queue
        (
            id          bigint          not null primary key default nextval('"{0}".topology_seq'),
            updated     timestamptz     not null default (now()),

            name        text            not null,
            type        integer         not null,
            auto_delete integer,
            max_delivery_count integer  not null DEFAULT 10
        );

        SELECT "{0}".add_column_if_not_exists('queue', 'max_delivery_count',
            'ALTER TABLE "{0}".queue ADD COLUMN IF NOT EXISTS max_delivery_count integer not null DEFAULT 10;');

        SELECT "{0}".create_constraint_if_not_exists('queue', 'unique_queue',
            'CREATE UNIQUE INDEX IF NOT EXISTS queue_uqx ON "{0}".queue (type, name) INCLUDE (id);ALTER TABLE "{0}".queue ADD CONSTRAINT unique_queue UNIQUE USING INDEX queue_uqx;');

        SELECT "{0}".create_index_if_not_exists('queue_auto_delete_ndx',
            'CREATE INDEX IF NOT EXISTS queue_auto_delete_ndx ON "{0}".queue (auto_delete) INCLUDE (id);');

        CREATE TABLE IF NOT EXISTS "{0}".topic
        (
            id          bigint      not null primary key default nextval('"{0}".topology_seq'),
            updated     timestamptz not null default (now()),

            name        text        not null
        );

        SELECT "{0}".create_constraint_if_not_exists('topic', 'unique_topic',
                'CREATE UNIQUE INDEX IF NOT EXISTS topic_uqx ON "{0}".topic (name) INCLUDE (id);ALTER TABLE "{0}".topic ADD CONSTRAINT unique_topic UNIQUE USING INDEX topic_uqx;');

        CREATE TABLE IF NOT EXISTS "{0}".topic_subscription
        (
            id              bigint       not null primary key default nextval('"{0}".topology_seq'),
            updated         timestamptz  not null default (now()),

            source_id       bigint       not null references "{0}".topic (id) ON DELETE CASCADE,
            destination_id  bigint       not null references "{0}".topic (id) ON DELETE CASCADE,

            sub_type        integer      not null,
            routing_key     text         not null,
            filter          jsonb        not null
        );

        SELECT "{0}".create_constraint_if_not_exists('topic_subscription', 'unique_topic_subscription',
                'CREATE UNIQUE INDEX IF NOT EXISTS topic_subscription_uqx ON "{0}".topic_subscription (source_id, destination_id, sub_type, routing_key, filter) INCLUDE (id);ALTER TABLE "{0}".topic_subscription ADD CONSTRAINT unique_topic_subscription UNIQUE USING INDEX topic_subscription_uqx;');

        SELECT "{0}".create_index_if_not_exists('topic_subscription_source_id_ndx',
                'CREATE INDEX IF NOT EXISTS topic_subscription_source_id_ndx ON "{0}".topic_subscription (source_id) INCLUDE (id, destination_id);');

        SELECT "{0}".create_index_if_not_exists('topic_subscription_destination_id_ndx',
                'CREATE INDEX IF NOT EXISTS topic_subscription_destination_id_ndx ON "{0}".topic_subscription (destination_id) INCLUDE (id, source_id);');

        CREATE TABLE IF NOT EXISTS "{0}".queue_subscription
        (
            id              bigint       not null primary key default nextval('"{0}".topology_seq'),
            updated         timestamptz  not null default (now()),

            source_id       bigint       not null references "{0}".topic (id) ON DELETE CASCADE,
            destination_id  bigint       not null references "{0}".queue (id) ON DELETE CASCADE,

            sub_type        integer      not null,
            routing_key     text         not null,
            filter          jsonb        not null
        );

        SELECT "{0}".create_constraint_if_not_exists('queue_subscription', 'unique_queue_subscription',
                'CREATE UNIQUE INDEX IF NOT EXISTS queue_subscription_uqx ON "{0}".queue_subscription (source_id, destination_id, sub_type, routing_key, filter);ALTER TABLE "{0}".queue_subscription ADD CONSTRAINT unique_queue_subscription UNIQUE USING INDEX queue_subscription_uqx;');

        SELECT "{0}".create_index_if_not_exists('queue_subscription_source_id_ndx',
                'CREATE INDEX IF NOT EXISTS queue_subscription_source_id_ndx ON "{0}".queue_subscription (source_id) INCLUDE (id, destination_id);');

        SELECT "{0}".create_index_if_not_exists('queue_subscription_destination_id_ndx',
                'CREATE INDEX IF NOT EXISTS queue_subscription_destination_id_ndx ON "{0}".queue_subscription (destination_id) INCLUDE (id, source_id);');

        CREATE TABLE IF NOT EXISTS "{0}".message
        (
            transport_message_id uuid        not null primary key,

            content_type         text,
            message_type         text,
            body                 jsonb,
            body_exact           json,
            binary_body          bytea,

            message_id           uuid,
            correlation_id       uuid,
            conversation_id      uuid,
            request_id           uuid,
            initiator_id         uuid,
            scheduling_token_id  uuid,

            source_address       text,
            destination_address  text,
            response_address     text,
            fault_address        text,

            sent_time            timestamptz NOT NULL DEFAULT (now()),

            headers              jsonb,
            host                 jsonb
        );

        SELECT "{0}".create_index_if_not_exists('message_scheduling_token_id_ndx',
                'CREATE INDEX IF NOT EXISTS message_scheduling_token_id_ndx ON "{0}".message (scheduling_token_id) where message.scheduling_token_id IS NOT NULL;');

        CREATE TABLE IF NOT EXISTS "{0}".message_delivery
        (
            message_delivery_id     bigserial   not null primary key,
            transport_message_id    uuid        not null REFERENCES "{0}".message ON DELETE CASCADE,

            queue_id                bigint      not null,
            priority                smallint    not null,
            enqueue_time            timestamptz not null,
            expiration_time         timestamptz,

            partition_key           text,
            routing_key             text,

            consumer_id             uuid,
            lock_id                 uuid,

            delivery_count          int         not null,
            max_delivery_count      int         not null,
            last_delivered          timestamptz,
            transport_headers       jsonb
        );

        SELECT "{0}".create_index_if_not_exists('message_delivery_fetch_ndx',
                'CREATE INDEX IF NOT EXISTS message_delivery_fetch_ndx on "{0}".message_delivery (queue_id, priority, enqueue_time, message_delivery_id);');

        SELECT "{0}".create_index_if_not_exists('message_delivery_fetch_part_ndx',
                'CREATE INDEX IF NOT EXISTS message_delivery_fetch_part_ndx on "{0}".message_delivery (queue_id, partition_key, priority, enqueue_time, message_delivery_id);');

        SELECT "{0}".create_index_if_not_exists('message_delivery_transport_message_id_ndx',
                'CREATE INDEX IF NOT EXISTS message_delivery_transport_message_id_ndx ON "{0}".message_delivery (transport_message_id);');

        -- Save only the four supported topology signatures. A recreated routine must retain
        -- its existing owner, explicit EXECUTE grants, grant options, and PUBLIC revocation.
        CREATE TEMP TABLE vicione_topology_routine_acl ON COMMIT DROP AS
        SELECT p.oid AS original_oid, p.proname::text AS routine_name,
               pg_catalog.oidvectortypes(p.proargtypes) AS argument_types,
               p.proowner AS owner_oid,
               COALESCE(p.proacl, pg_catalog.acldefault('f', p.proowner)) AS privileges
        FROM pg_catalog.pg_proc p
        WHERE p.oid = ANY (ARRAY[
            to_regprocedure('"{0}".create_queue(text,integer,integer)')::oid,
            to_regprocedure('"{0}".create_topic(text)')::oid,
            to_regprocedure('"{0}".create_topic_subscription(text,text,integer,text,jsonb)')::oid,
            to_regprocedure('"{0}".create_queue_subscription(text,text,integer,text,jsonb)')::oid]);

        DO $topology_return_upgrade$
        DECLARE
            routine_oid oid;
        BEGIN
            FOR routine_oid IN
                SELECT p.oid
                FROM pg_catalog.pg_proc p
                JOIN pg_temp.vicione_topology_routine_acl saved ON saved.original_oid = p.oid
                WHERE saved.routine_name <> 'create_queue'
                    AND p.prorettype = 'pg_catalog.int4'::regtype
            LOOP
                EXECUTE format('DROP FUNCTION %s RESTRICT', routine_oid::regprocedure);
            END LOOP;
        END
        $topology_return_upgrade$;

        DO $cleanup$
        DECLARE
            routine_oid oid;
        BEGIN
            FOR routine_oid IN
                SELECT p.oid
                FROM pg_proc p
                    INNER JOIN pg_namespace n ON n.oid = p.pronamespace
                WHERE n.nspname = '{0}'
                    AND p.proname IN ('create_queue', 'create_queue_v2', 'send_message', 'send_message_v2', 'publish_message', 'publish_message_v2')
            LOOP
                EXECUTE format('DROP FUNCTION %s', routine_oid::regprocedure);
            END LOOP;
        END
        $cleanup$;

        CREATE FUNCTION "{0}".create_queue(queue_name text, auto_delete integer DEFAULT NULL, max_delivery_count integer DEFAULT NULL)
            RETURNS bigint
        AS
        $$
        DECLARE
            v_queue_id bigint;
        BEGIN
            IF queue_name IS NULL OR LENGTH(queue_name) < 1 THEN
                RAISE EXCEPTION 'Queue names must not be null or empty';
            END IF;

            INSERT INTO "{0}".queue (name, type, auto_delete, max_delivery_count) VALUES (queue_name, 1, auto_delete, COALESCE(max_delivery_count, 10))
                ON CONFLICT ON CONSTRAINT unique_queue DO
                UPDATE SET updated = (now()),
                           auto_delete = COALESCE(create_queue.auto_delete, excluded.auto_delete),
                           max_delivery_count = COALESCE(create_queue.max_delivery_count, excluded.max_delivery_count, 10)
                RETURNING queue.id INTO v_queue_id;

            INSERT INTO "{0}".queue (name, type, auto_delete, max_delivery_count) VALUES (queue_name, 2, auto_delete, COALESCE(max_delivery_count, 10))
                ON CONFLICT ON CONSTRAINT unique_queue DO
                UPDATE SET updated = (now()),
                           auto_delete = COALESCE(create_queue.auto_delete, excluded.auto_delete),
                           max_delivery_count = COALESCE(create_queue.max_delivery_count, excluded.max_delivery_count, 10);

            INSERT INTO "{0}".queue (name, type, auto_delete, max_delivery_count) VALUES (queue_name, 3, auto_delete, COALESCE(max_delivery_count, 10))
                ON CONFLICT ON CONSTRAINT unique_queue DO
                UPDATE SET updated = (now()),
                           auto_delete = COALESCE(create_queue.auto_delete, excluded.auto_delete),
                           max_delivery_count = COALESCE(create_queue.max_delivery_count, excluded.max_delivery_count, 10);

            RETURN v_queue_id;

        END;
        $$ LANGUAGE plpgsql;

        CREATE OR REPLACE FUNCTION "{0}".create_topic(topic_name text)
            RETURNS bigint
        AS
        $$
        DECLARE
            v_topic_id bigint;
        BEGIN
            IF topic_name IS NULL OR LENGTH(topic_name) < 1 THEN
                RAISE EXCEPTION 'Topic names must not be null or empty';
            END IF;

            INSERT INTO "{0}".topic (name) VALUES (topic_name)
                ON CONFLICT ON CONSTRAINT unique_topic DO
                UPDATE SET updated = (now())
                RETURNING topic.id INTO v_topic_id;

            RETURN v_topic_id;

        END;
        $$ LANGUAGE plpgsql;

        CREATE OR REPLACE FUNCTION "{0}".create_topic_subscription(source_topic_name text, destination_topic_name text, type integer,
            routing_key text DEFAULT '', filter jsonb DEFAULT '{{}}')
            RETURNS bigint
        AS
        $$
        DECLARE
            v_topic_subscription_id bigint;
            v_source_id bigint;
            v_destination_id bigint;
        BEGIN
            IF source_topic_name IS NULL OR LENGTH(source_topic_name) < 1 THEN
                RAISE EXCEPTION 'Topic names must not be null or empty';
            END IF;
            IF destination_topic_name IS NULL OR LENGTH(destination_topic_name) < 1 THEN
                RAISE EXCEPTION 'Topic names must not be null or empty';
            END IF;

            SELECT INTO v_source_id t.Id FROM "{0}".topic t WHERE t.name = source_topic_name;
            IF v_source_id IS NULL THEN
                RAISE EXCEPTION 'Source topic not found: %', source_topic_name;
            END IF;
            SELECT INTO v_destination_id t.Id FROM "{0}".topic t WHERE t.name = destination_topic_name;
            IF v_destination_id IS NULL THEN
                RAISE EXCEPTION 'Destination topic not found: %', destination_topic_name;
            END IF;

            INSERT INTO "{0}".topic_subscription (source_id, destination_id, sub_type, routing_key, filter)
                VALUES (v_source_id, v_destination_id, type, COALESCE(create_topic_subscription.routing_key, ''), COALESCE(create_topic_subscription.filter, '{{}}'::jsonb))
                ON CONFLICT ON CONSTRAINT unique_topic_subscription DO
                UPDATE SET updated = (now())
                RETURNING topic_subscription.id INTO v_topic_subscription_id;

            RETURN v_topic_subscription_id;

        END;
        $$ LANGUAGE plpgsql;

        CREATE OR REPLACE FUNCTION "{0}".create_queue_subscription(source_topic_name text, destination_queue_name text, type integer,
            routing_key text DEFAULT '', filter jsonb DEFAULT '{{}}')
            RETURNS bigint
        AS
        $$
        DECLARE
            v_queue_subscription_id bigint;
            v_source_id bigint;
            v_destination_id bigint;
        BEGIN
            IF source_topic_name IS NULL OR LENGTH(source_topic_name) < 1 THEN
                RAISE EXCEPTION 'Topic names must not be null or empty';
            END IF;
            IF destination_queue_name IS NULL OR LENGTH(destination_queue_name) < 1 THEN
                RAISE EXCEPTION 'Queue names must not be null or empty';
            END IF;

            SELECT INTO v_source_id t.Id FROM "{0}".topic t WHERE t.name = source_topic_name;
            IF v_source_id IS NULL THEN
                RAISE EXCEPTION 'Source topic not found: %', source_topic_name;
            END IF;
            SELECT INTO v_destination_id q.Id FROM "{0}".queue q WHERE q.name = destination_queue_name AND q.type = 1;
            IF v_destination_id IS NULL THEN
                RAISE EXCEPTION 'Destination queue not found: %', destination_queue_name;
            END IF;

            INSERT INTO "{0}".queue_subscription (source_id, destination_id, sub_type, routing_key, filter)
                VALUES (v_source_id, v_destination_id, type, COALESCE(create_queue_subscription.routing_key, ''), COALESCE(create_queue_subscription.filter, '{{}}'::jsonb))
                ON CONFLICT ON CONSTRAINT unique_queue_subscription DO
                UPDATE SET updated = (now())
                RETURNING queue_subscription.id INTO v_queue_subscription_id;

            RETURN v_queue_subscription_id;

        END;
        $$ LANGUAGE plpgsql;


        CREATE OR REPLACE FUNCTION "{0}".purge_queue(queue_name text)
            RETURNS bigint
        AS
        $$
        DECLARE
            v_transport_message_ids uuid[];
            v_deleted_count bigint;
        BEGIN
            IF queue_name IS NULL OR LENGTH(queue_name) < 1 THEN
                RAISE EXCEPTION 'Queue name must not be null';
            END IF;

            WITH msgs AS (
                DELETE FROM "{0}".message_delivery md
                    USING (SELECT mdx.message_delivery_id
                           FROM "{0}".message_delivery mdx
                               INNER JOIN "{0}".queue q on mdx.queue_id = q.Id
                           WHERE q.name = queue_name
                             AND q.type = 1) mds
                    WHERE md.message_delivery_id = mds.message_delivery_id
                    RETURNING md.transport_message_id)
                SELECT COUNT(*), array_agg(msgs.transport_message_id)
                INTO v_deleted_count, v_transport_message_ids
                FROM msgs;

            DELETE FROM "{0}".message m
                WHERE m.transport_message_id = ANY(v_transport_message_ids)
                    AND NOT EXISTS(SELECT FROM "{0}".message_delivery md WHERE md.transport_message_id = m.transport_message_id);

            RETURN v_deleted_count;
        END;
        $$ LANGUAGE plpgsql;


        CREATE OR REPLACE FUNCTION "{0}".fetch_messages(
            queue_name text
        ,   fetch_consumer_id uuid
        ,   fetch_lock_id uuid
        ,   lock_duration interval
        ,   fetch_count integer DEFAULT 1)
            RETURNS TABLE(
            transport_message_id uuid
        ,   queue_id bigint
        ,   priority smallint
        ,   message_delivery_id bigint
        ,   consumer_id uuid
        ,   lock_id uuid
        ,   enqueue_time timestamp with time zone
        ,   expiration_time timestamp with time zone
        ,   delivery_count integer
        ,   partition_key text
        ,   routing_key text
        ,   transport_headers jsonb
        ,   content_type text
        ,   message_type text
        ,   body jsonb
        ,   binary_body bytea
        ,   message_id uuid
        ,   correlation_id uuid
        ,   conversation_id uuid
        ,   request_id uuid
        ,   initiator_id uuid
        ,   source_address text
        ,   destination_address text
        ,   response_address text
        ,   fault_address text
        ,   sent_time timestamp with time zone
        ,   headers jsonb
        ,   host jsonb)
        AS
        $$
        DECLARE
            v_queue_id bigint;
            v_enqueue_time timestamptz;
            v_now timestamptz;
        BEGIN
            SELECT INTO v_queue_id q.Id FROM "{0}".queue q WHERE q.name = queue_name AND q.type = 1;
            IF v_queue_id IS NULL THEN
                RAISE EXCEPTION 'Queue not found: %', queue_name;
            END IF;

            v_now := (now());
            v_enqueue_time := v_now + lock_duration;

            RETURN QUERY WITH msgs AS (
                SELECT md.*
                FROM "{0}".message_delivery md
                WHERE md.queue_id = v_queue_id
                    AND md.enqueue_time <= v_now
                    AND md.delivery_count < md.max_delivery_count
                ORDER BY md.priority, md.enqueue_time, md.message_delivery_id
                LIMIT fetch_count FOR UPDATE OF md SKIP LOCKED)
                UPDATE "{0}".message_delivery dm
                SET delivery_count = dm.delivery_count + 1,
                    last_delivered = v_now,
                    consumer_id = fetch_consumer_id,
                    lock_id = fetch_lock_id,
                    enqueue_time = v_enqueue_time
                    FROM msgs
                    INNER JOIN "{0}".message m on msgs.transport_message_id = m.transport_message_id
                WHERE dm.message_delivery_id = msgs.message_delivery_id
                RETURNING
                    dm.transport_message_id,
                    dm.queue_id,
                    dm.priority,
                    dm.message_delivery_id,
                    dm.consumer_id,
                    dm.lock_id,
                    dm.enqueue_time,
                    dm.expiration_time,
                    dm.delivery_count,
                    dm.partition_key,
                    dm.routing_key,
                    dm.transport_headers,
                    m.content_type,
                    m.message_type,
                    m.body,
                    m.binary_body,
                    m.message_id,
                    m.correlation_id,
                    m.conversation_id,
                    m.request_id,
                    m.initiator_id,
                    m.source_address,
                    m.destination_address,
                    m.response_address,
                    m.fault_address,
                    m.sent_time,
                    m.headers,
                    m.host;
        END;
        $$ LANGUAGE plpgsql;

        CREATE OR REPLACE FUNCTION "{0}".fetch_messages_partitioned(
            queue_name text
        ,   fetch_consumer_id uuid
        ,   fetch_lock_id uuid
        ,   lock_duration interval
        ,   fetch_count integer DEFAULT 1
        ,   concurrent_count integer DEFAULT 1
        ,   ordered integer DEFAULT 0)
            RETURNS TABLE(
            transport_message_id uuid
        ,   queue_id bigint
        ,   priority smallint
        ,   message_delivery_id bigint
        ,   consumer_id uuid
        ,   lock_id uuid
        ,   enqueue_time timestamp with time zone
        ,   expiration_time timestamp with time zone
        ,   delivery_count integer
        ,   partition_key text
        ,   routing_key text
        ,   transport_headers jsonb
        ,   content_type text
        ,   message_type text
        ,   body jsonb
        ,   binary_body bytea
        ,   message_id uuid
        ,   correlation_id uuid
        ,   conversation_id uuid
        ,   request_id uuid
        ,   initiator_id uuid
        ,   source_address text
        ,   destination_address text
        ,   response_address text
        ,   fault_address text
        ,   sent_time timestamp with time zone
        ,   headers jsonb
        ,   host jsonb)
        AS
        $$
        DECLARE
            v_queue_id bigint;
            v_enqueue_time timestamptz;
            v_now timestamptz;
        BEGIN
            SELECT INTO v_queue_id q.Id FROM "{0}".queue q WHERE q.name = queue_name AND q.type = 1;
            IF v_queue_id IS NULL THEN
                RAISE EXCEPTION 'Queue not found: %', queue_name;
            END IF;

            v_now := (now());
            v_enqueue_time := v_now + lock_duration;

            RETURN QUERY WITH msgs AS (
                SELECT md.*
                FROM "{0}".message_delivery md
                WHERE md.message_delivery_id IN (
                    WITH ready AS (
                        SELECT mdx.message_delivery_id, mdx.enqueue_time, mdx.lock_id, mdx.priority,
                               row_number() over ( partition by mdx.partition_key
                                   order by mdx.priority, mdx.enqueue_time, mdx.message_delivery_id ) as row_normal,
                               row_number() over ( partition by mdx.partition_key
                                   order by mdx.priority, mdx.message_delivery_id,mdx.enqueue_time )  as row_ordered,
                               first_value(CASE WHEN mdx.enqueue_time > v_now THEN mdx.consumer_id END) over (partition by mdx.partition_key
                                   order by mdx.enqueue_time DESC, mdx.message_delivery_id DESC) as consumer_id,
                               sum(CASE WHEN mdx.enqueue_time > v_now AND mdx.consumer_id = fetch_consumer_id AND mdx.lock_id IS NOT NULL THEN 1 END)
                                   over (partition by mdx.partition_key
                                       order by mdx.enqueue_time DESC, mdx.message_delivery_id DESC) as active_count
                        FROM "{0}".message_delivery mdx
                        WHERE mdx.queue_id = v_queue_id
                            AND mdx.delivery_count < mdx.max_delivery_count
                    )
                    SELECT ready.message_delivery_id
                        FROM ready
                        WHERE ( ( ordered = 0 AND ready.row_normal <= concurrent_count) OR ( ordered = 1 AND ready.row_ordered <= concurrent_count ) )
                        AND ready.enqueue_time <= v_now
                        AND (ready.consumer_id IS NULL OR ready.consumer_id = fetch_consumer_id)
                        AND (active_count < concurrent_count OR active_count IS NULL)
                        ORDER BY ready.priority, ready.enqueue_time, ready.message_delivery_id
                    LIMIT fetch_count FOR UPDATE SKIP LOCKED)
                FOR UPDATE OF md SKIP LOCKED)
                UPDATE "{0}".message_delivery dm
                SET delivery_count = dm.delivery_count + 1,
                    last_delivered = v_now,
                    consumer_id = fetch_consumer_id,
                    lock_id = fetch_lock_id,
                    enqueue_time = v_enqueue_time
                    FROM msgs
                    INNER JOIN "{0}".message m on msgs.transport_message_id = m.transport_message_id
                WHERE dm.message_delivery_id = msgs.message_delivery_id
                RETURNING
                    dm.transport_message_id,
                    dm.queue_id,
                    dm.priority,
                    dm.message_delivery_id,
                    dm.consumer_id,
                    dm.lock_id,
                    dm.enqueue_time,
                    dm.expiration_time,
                    dm.delivery_count,
                    dm.partition_key,
                    dm.routing_key,
                    dm.transport_headers,
                    m.content_type,
                    m.message_type,
                    m.body,
                    m.binary_body,
                    m.message_id,
                    m.correlation_id,
                    m.conversation_id,
                    m.request_id,
                    m.initiator_id,
                    m.source_address,
                    m.destination_address,
                    m.response_address,
                    m.fault_address,
                    m.sent_time,
                    m.headers,
                    m.host;
        END;
        $$ LANGUAGE plpgsql;

        CREATE OR REPLACE FUNCTION "{0}".delete_message(message_delivery_id bigint, lock_id uuid)
            RETURNS bigint
            LANGUAGE PLPGSQL
        AS
        $$
        DECLARE
            v_message_delivery_id   bigint;
            v_queue_id              bigint;
            v_transport_message_id  uuid;
        BEGIN
            DELETE FROM "{0}".message_delivery md
                WHERE md.message_delivery_id = delete_message.message_delivery_id
                AND md.lock_id = delete_message.lock_id
                RETURNING md.message_delivery_id, md.transport_message_id, md.queue_id INTO v_message_delivery_id, v_transport_message_id, v_queue_id;

            IF v_transport_message_id IS NOT NULL THEN
                DELETE FROM "{0}".message m
                    WHERE m.transport_message_id = v_transport_message_id
                    AND NOT EXISTS(SELECT FROM "{0}".message_delivery md WHERE md.transport_message_id = v_transport_message_id);

                INSERT INTO "{0}".queue_metric_capture (captured, queue_id, consume_count, error_count, dead_letter_count)
                    VALUES (now(), v_queue_id, 1, 0, 0);

            END IF;

            RETURN v_message_delivery_id;
        END;
        $$;

        CREATE OR REPLACE FUNCTION "{0}".touch_queue(queue_name text)
            RETURNS bigint
            LANGUAGE PLPGSQL
        AS
        $$
        DECLARE
            v_queue_id              bigint;
        BEGIN
            IF queue_name IS NULL OR LENGTH(queue_name) < 1 THEN
                RAISE EXCEPTION 'Queue name must not be null';
            END IF;

            SELECT INTO v_queue_id q.Id FROM "{0}".queue q WHERE q.name = queue_name AND q.type = 1;
            IF v_queue_id IS NULL THEN
                RAISE EXCEPTION 'Queue not found: %', queue_name;
            END IF;

            INSERT INTO "{0}".queue_metric_capture (captured, queue_id, consume_count, error_count, dead_letter_count)
                VALUES (now(), v_queue_id, 0, 0, 0);

            RETURN v_queue_id;
        END;
        $$;

        CREATE OR REPLACE FUNCTION "{0}".delete_scheduled_message(token_id uuid)
            RETURNS TABLE (transport_message_id uuid)
            LANGUAGE PLPGSQL
        AS
        $$
        BEGIN
            RETURN QUERY
            DELETE FROM "{0}".message m
            WHERE m.scheduling_token_id = token_id
                AND EXISTS (
                    SELECT 1
                    FROM "{0}".message_delivery md
                    WHERE md.transport_message_id = m.transport_message_id)
                AND NOT EXISTS (
                    SELECT 1
                    FROM "{0}".message_delivery md
                    WHERE md.transport_message_id = m.transport_message_id
                        AND (md.delivery_count <> 0 OR md.lock_id IS NOT NULL))
            RETURNING m.transport_message_id;
        END;
        $$;

        CREATE OR REPLACE FUNCTION "{0}".renew_message_lock(message_delivery_id bigint, lock_id uuid, duration interval)
            RETURNS bigint
            LANGUAGE PLPGSQL
        AS
        $$
        DECLARE
            v_message_delivery_id   bigint;
            v_queue_id              bigint;
        BEGIN
            IF duration < INTERVAL '1 seconds' THEN
                RAISE EXCEPTION 'Invalid lock duration';
            END IF;

            UPDATE "{0}".message_delivery md
                SET enqueue_time = (now()) + duration
                WHERE md.message_delivery_id = renew_message_lock.message_delivery_id AND md.lock_id = renew_message_lock.lock_id
                RETURNING md.message_delivery_id, md.queue_id INTO v_message_delivery_id, v_queue_id;

            IF v_queue_id IS NOT NULL THEN
                INSERT INTO "{0}".queue_metric_capture (captured, queue_id, consume_count, error_count, dead_letter_count)
                    VALUES (now(), v_queue_id, 0, 0, 0);
            END IF;

            RETURN v_message_delivery_id;
        END;
        $$;

        DROP FUNCTION IF EXISTS "{0}".move_message(bigint, uuid, text, integer, jsonb);

        CREATE OR REPLACE FUNCTION "{0}".move_message(message_delivery_id bigint, lock_id uuid, queue_name text, queue_type integer,
            expiration_time timestamptz, headers jsonb)
            RETURNS bigint
            LANGUAGE PLPGSQL
        AS
        $$
        DECLARE
            v_message_delivery_id   bigint;
            v_queue_id              bigint;
            v_source_queue_id       bigint;
            v_enqueue_time          timestamptz;
        BEGIN
            SELECT INTO v_queue_id q.Id FROM "{0}".queue q WHERE q.name = queue_name AND q.type = queue_type;
            IF v_queue_id IS NULL THEN
                RAISE EXCEPTION 'Queue not found: %', queue_name;
            END IF;

            v_enqueue_time := (now());

            UPDATE "{0}".message_delivery md
                SET enqueue_time = v_enqueue_time, queue_id = v_queue_id, lock_id = NULL, consumer_id = NULL,
                    expiration_time = move_message.expiration_time, transport_headers = headers
                FROM (SELECT mdx.message_delivery_id, queue_id, consumer_id FROM "{0}".message_delivery mdx
                    WHERE mdx.message_delivery_id = move_message.message_delivery_id AND mdx.lock_id = move_message.lock_id FOR UPDATE) mdy
                WHERE mdy.message_delivery_id = md.message_delivery_id
                RETURNING md.message_delivery_id, mdy.queue_id INTO v_message_delivery_id, v_source_queue_id;

            IF v_source_queue_id IS NOT NULL THEN
                INSERT INTO "{0}".queue_metric_capture (captured, queue_id, consume_count, error_count, dead_letter_count)
                    VALUES (now(), v_source_queue_id, 0,
                    CASE WHEN queue_type = 2 THEN 1 ELSE 0 END, CASE WHEN queue_type = 3 THEN 1 ELSE 0 END);
            END IF;

            RETURN v_message_delivery_id;
        END;
        $$;

        CREATE OR REPLACE FUNCTION "{0}".unlock_message(message_delivery_id bigint, lock_id uuid, delay interval, headers jsonb)
            RETURNS bigint
            LANGUAGE PLPGSQL
        AS
        $$
        DECLARE
            v_message_delivery_id   bigint;
            v_enqueue_time          timestamptz;
            v_queue_id              bigint;
        BEGIN
            v_enqueue_time := (now());
            IF delay > INTERVAL '0 seconds' THEN
                v_enqueue_time = v_enqueue_time + delay;
            END IF;

            UPDATE "{0}".message_delivery md
                SET enqueue_time = v_enqueue_time, lock_id = NULL, consumer_id = NULL, transport_headers = headers
                WHERE md.message_delivery_id = unlock_message.message_delivery_id AND md.lock_id = unlock_message.lock_id
                RETURNING md.message_delivery_id, md.queue_id INTO v_message_delivery_id, v_queue_id;

            IF v_queue_id IS NOT NULL THEN
                INSERT INTO "{0}".queue_metric_capture (captured, queue_id, consume_count, error_count, dead_letter_count)
                    VALUES (now(), v_queue_id, 0, 0, 0);
            END IF;

            RETURN v_message_delivery_id;
        END;
        $$;

        CREATE FUNCTION "{0}".send_message(
          entity_name text
        , priority integer DEFAULT NULL
        , transport_message_id uuid DEFAULT gen_random_uuid()
        , body jsonb DEFAULT NULL
        , binary_body bytea DEFAULT NULL
        , content_type text DEFAULT NULL
        , message_type text DEFAULT NULL
        , message_id uuid DEFAULT NULL
        , correlation_id uuid DEFAULT NULL
        , conversation_id uuid DEFAULT NULL
        , request_id uuid DEFAULT NULL
        , initiator_id uuid DEFAULT NULL
        , source_address text DEFAULT NULL
        , destination_address text DEFAULT NULL
        , response_address text DEFAULT NULL
        , fault_address text DEFAULT NULL
        , sent_time timestamptz DEFAULT NULL
        , expiration_time timestamptz DEFAULT NULL
        , headers jsonb DEFAULT NULL
        , host jsonb DEFAULT NULL
        , partition_key text DEFAULT NULL
        , routing_key text DEFAULT NULL
        , delay interval DEFAULT INTERVAL '0 seconds'
        , scheduling_token_id uuid DEFAULT NULL
        )
            RETURNS bigint AS
        $$
        DECLARE
            v_queue_id     bigint;
            v_max_delivery_count int;
            v_enqueue_time timestamptz;
        BEGIN
            if entity_name is null or length(entity_name) < 1 then
                raise exception 'Queue names must not be null or empty';
            end if;

            SELECT INTO v_queue_id,v_max_delivery_count q.id,q.max_delivery_count FROM "{0}".queue q WHERE q.name = entity_name AND q.type = 1;
            if v_queue_id IS NULL THEN
                raise exception 'Queue not found';
            end if;

            v_enqueue_time := (now());
            IF delay > INTERVAL '0 seconds' THEN
                v_enqueue_time = v_enqueue_time + delay;
            END IF;

            INSERT INTO "{0}".message (transport_message_id, body, binary_body, content_type, message_type, message_id, correlation_id, conversation_id, request_id, initiator_id,
                source_address, destination_address, response_address, fault_address, sent_time, headers, host, scheduling_token_id)
            VALUES (transport_message_id, body, binary_body, content_type, message_type, message_id, correlation_id, conversation_id, request_id, initiator_id,
                source_address, destination_address, response_address, fault_address, sent_time, headers, host, scheduling_token_id);
            INSERT INTO "{0}".message_delivery (queue_id, transport_message_id, priority, enqueue_time, expiration_time, delivery_count, max_delivery_count, partition_key, routing_key)
            VALUES (v_queue_id, send_message.transport_message_id, send_message.priority, v_enqueue_time, expiration_time, 0, v_max_delivery_count, send_message.partition_key, send_message.routing_key);

            RETURN 1;

        END;
        $$ LANGUAGE plpgsql;

        CREATE FUNCTION "{0}".publish_message(
          entity_name text
        , priority integer DEFAULT NULL
        , transport_message_id uuid DEFAULT gen_random_uuid()
        , body jsonb DEFAULT NULL
        , binary_body bytea DEFAULT NULL
        , content_type text DEFAULT NULL
        , message_type text DEFAULT NULL
        , message_id uuid DEFAULT NULL
        , correlation_id uuid DEFAULT NULL
        , conversation_id uuid DEFAULT NULL
        , request_id uuid DEFAULT NULL
        , initiator_id uuid DEFAULT NULL
        , source_address text DEFAULT NULL
        , destination_address text DEFAULT NULL
        , response_address text DEFAULT NULL
        , fault_address text DEFAULT NULL
        , sent_time timestamptz DEFAULT NULL
        , expiration_time timestamptz DEFAULT NULL
        , headers jsonb DEFAULT NULL
        , host jsonb DEFAULT NULL
        , partition_key text DEFAULT NULL
        , routing_key text DEFAULT NULL
        , delay interval DEFAULT INTERVAL '0 seconds'
        , scheduling_token_id uuid DEFAULT NULL
        )
            RETURNS bigint AS
        $$
        DECLARE
            v_topic_id      bigint;
            v_enqueue_time  timestamptz;
            v_publish_count bigint;
        BEGIN
            IF entity_name IS NULL OR LENGTH(entity_name) < 1 THEN
                RAISE EXCEPTION 'Topic names must not be null or empty';
            END IF;

            SELECT INTO v_topic_id t.Id FROM "{0}".topic t WHERE t.name = entity_name;
            if v_topic_id IS NULL THEN
                RAISE EXCEPTION 'Topic not found';
            END IF;

            v_enqueue_time := (now());
            IF delay > INTERVAL '0 seconds' THEN
                v_enqueue_time = v_enqueue_time + delay;
            END IF;

            INSERT INTO "{0}".message (transport_message_id, body, binary_body, content_type, message_type, message_id, correlation_id, conversation_id, request_id, initiator_id,
                source_address, destination_address, response_address, fault_address, sent_time, headers, host, scheduling_token_id)
            VALUES (transport_message_id, body, binary_body, content_type, message_type, message_id, correlation_id, conversation_id, request_id, initiator_id,
                source_address, destination_address, response_address, fault_address, sent_time, headers, host, scheduling_token_id);

            WITH delivered AS (
            INSERT INTO "{0}".message_delivery (queue_id, transport_message_id, priority, enqueue_time, expiration_time, delivery_count, max_delivery_count, partition_key, routing_key)
            WITH RECURSIVE fabric AS (
                SELECT source_id, destination_id
                    FROM "{0}".topic t
                    LEFT JOIN "{0}".topic_subscription ts ON t.id = ts.source_id
                    AND CASE
                        WHEN ts.sub_type = 1 THEN true
                        WHEN ts.sub_type = 2 THEN publish_message.routing_key = ts.routing_key
                        WHEN ts.sub_type = 3 THEN publish_message.routing_key ~ ts.routing_key
                        ELSE false END
                    WHERE t.id = v_topic_id

              UNION

                SELECT ts.source_id, ts.destination_id
                    FROM "{0}".topic_subscription ts, fabric
                    WHERE ts.source_id = fabric.destination_id
                    AND CASE
                        WHEN ts.sub_type = 1 THEN true
                        WHEN ts.sub_type = 2 THEN publish_message.routing_key = ts.routing_key
                        WHEN ts.sub_type = 3 THEN publish_message.routing_key ~ ts.routing_key
                        ELSE false END
                )
            SELECT DISTINCT qs.destination_id, publish_message.transport_message_id, publish_message.priority, v_enqueue_time, publish_message.expiration_time, 0, q.max_delivery_count, publish_message.partition_key, publish_message.routing_key
                FROM "{0}".queue_subscription qs, "{0}".queue q, fabric
                WHERE CASE
                    WHEN qs.sub_type = 1 THEN true
                    WHEN qs.sub_type = 2 THEN publish_message.routing_key = qs.routing_key
                    WHEN qs.sub_type = 3 THEN publish_message.routing_key ~ qs.routing_key
                    ELSE false END
                AND qs.destination_id = q.id
                AND (qs.source_id = fabric.destination_id OR qs.source_id = v_topic_id)
            RETURNING message_delivery_id)
            SELECT COUNT(d.message_delivery_id) FROM delivered d INTO v_publish_count;

            IF v_publish_count = 0 THEN
                DELETE FROM "{0}".message WHERE message.transport_message_id = publish_message.transport_message_id;
            END IF;

            RETURN v_publish_count;

        END;
        $$ LANGUAGE plpgsql;

        CREATE OR REPLACE FUNCTION "{0}".notify_msg()
            RETURNS trigger AS
        $$
        DECLARE
            v_payload   json;
        BEGIN
            IF NEW.enqueue_time <= (now()) THEN
                v_payload = json_build_object(
                    'message_delivery_id', NEW.message_delivery_id,
                    'enqueue_time', to_char(NEW.enqueue_time, 'YYYY-MM-DD"T"HH24:MI:SS.MS"Z"')
                );

                PERFORM pg_notify('{2}' || NEW.queue_id, v_payload::text);
            END IF;

            RETURN NEW;
        END;
        $$ LANGUAGE plpgsql VOLATILE;

        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_trigger t
                JOIN pg_class c ON c.oid = t.tgrelid
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE c.relname = 'message_delivery'
                  AND n.nspname = '{0}'
                  AND t.tgname = 'message_delivery_notify_trigger'
            ) THEN
                CREATE TRIGGER message_delivery_notify_trigger AFTER INSERT OR UPDATE ON "{0}".message_delivery
                    FOR EACH ROW EXECUTE FUNCTION "{0}".notify_msg();
            END IF;
        END $$;

        CREATE UNLOGGED TABLE IF NOT EXISTS "{0}".queue_metric_capture (
            queue_metric_id bigint NOT NULL GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            captured timestamptz NOT NULL,
            queue_id bigint NOT NULL,
            consume_count int NOT NULL,
            error_count int NOT NULL,
            dead_letter_count int NOT NULL
        );

        CREATE TABLE IF NOT EXISTS "{0}".queue_metric (
            queue_metric_id bigint NOT NULL GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            start_time timestamptz NOT NULL,
            duration interval NOT NULL,
            queue_id bigint NOT NULL,
            consume_count bigint NOT NULL,
            error_count bigint NOT NULL,
            dead_letter_count bigint NOT NULL
        );

        SELECT "{0}".create_constraint_if_not_exists('queue_metric', 'unique_queue_metric',
                'CREATE UNIQUE INDEX IF NOT EXISTS queue_metric_ndx ON "{0}".queue_metric (start_time, duration, queue_id);ALTER TABLE "{0}".queue_metric ADD CONSTRAINT unique_queue_metric UNIQUE USING INDEX queue_metric_ndx;');

        SELECT "{0}".create_index_if_not_exists('queue_metric_queue_id',
            'CREATE INDEX IF NOT EXISTS queue_metric_queue_id ON "{0}".queue_metric (queue_id, start_time) INCLUDE (duration);');

        CREATE OR REPLACE FUNCTION "{0}".process_metrics(row_limit int DEFAULT 10000)
            RETURNS int
            LANGUAGE PLPGSQL
        AS
        $$
        BEGIN
            LOCK TABLE "{0}".queue_metric IN EXCLUSIVE MODE;
            WITH metrics AS (
                DELETE FROM "{0}".queue_metric_capture
                    WHERE queue_metric_id < COALESCE((SELECT MIN(queue_metric_id) FROM "{0}".queue_metric_capture), 0) + row_limit
                       RETURNING *
                )
                INSERT INTO "{0}".queue_metric (start_time, duration, queue_id, consume_count, error_count, dead_letter_count)
                    SELECT date_trunc('minute', m.captured),
                           interval '1 minute',
                           m.queue_id,
                           sum(m.consume_count),
                           sum(m.error_count),
                           sum(m.dead_letter_count)
                    FROM metrics m
                    GROUP BY date_trunc('minute', m.captured), m.queue_id
                    ON CONFLICT ON CONSTRAINT unique_queue_metric DO
                        UPDATE SET consume_count = queue_metric.consume_count + excluded.consume_count,
                                   error_count = queue_metric.error_count + excluded.error_count,
                                   dead_letter_count = queue_metric.dead_letter_count + excluded.dead_letter_count;

            WITH metrics AS (
                DELETE FROM "{0}".queue_metric
                    WHERE duration = interval '1 minute' AND start_time < (now()) - interval '8 hours'
                       RETURNING *
                )
            INSERT INTO "{0}".queue_metric (start_time, duration, queue_id, consume_count, error_count, dead_letter_count)
                SELECT date_trunc('hour', m.start_time), interval '1 hour', m.queue_id,
                    sum(m.consume_count),
                    sum(m.error_count),
                    sum(m.dead_letter_count)
                    FROM metrics m
                    GROUP BY date_trunc('hour', m.start_time), m.queue_id
                ON CONFLICT ON CONSTRAINT unique_queue_metric DO
                UPDATE SET consume_count = queue_metric.consume_count + excluded.consume_count,
                           error_count = queue_metric.error_count + excluded.error_count,
                           dead_letter_count = queue_metric.dead_letter_count + excluded.dead_letter_count;

            WITH metrics AS (
                DELETE FROM "{0}".queue_metric
                    WHERE duration = interval '1 hour' AND start_time < (now()) - interval '48 hours'
                       RETURNING *
                )
            INSERT INTO "{0}".queue_metric (start_time, duration, queue_id, consume_count, error_count, dead_letter_count)
                SELECT date_trunc('day', m.start_time), interval '1 day', m.queue_id,
                    sum(m.consume_count),
                    sum(m.error_count),
                    sum(m.dead_letter_count)
                    FROM metrics m
                    GROUP BY date_trunc('day', m.start_time), m.queue_id
                ON CONFLICT ON CONSTRAINT unique_queue_metric DO
                UPDATE SET consume_count = queue_metric.consume_count + excluded.consume_count,
                           error_count = queue_metric.error_count + excluded.error_count,
                           dead_letter_count = queue_metric.dead_letter_count + excluded.dead_letter_count;

            DELETE FROM "{0}".queue_metric
                WHERE start_time < (now()) - interval '90 days';

            RETURN 0;
        END;
        $$;

        CREATE OR REPLACE FUNCTION "{0}".remove_orphaned_messages(row_limit int DEFAULT 10000)
                RETURNS int
                LANGUAGE PLPGSQL
        AS
        $$
        DECLARE
            v_removed_count   int;
        BEGIN
            DELETE FROM "{0}".message
                WHERE transport_message_id IN (
                    SELECT m.transport_message_id
                    FROM "{0}".message m
                    WHERE NOT EXISTS (SELECT FROM "{0}".message_delivery md WHERE md.transport_message_id = m.transport_message_id)
                    LIMIT row_limit);

            GET DIAGNOSTICS v_removed_count = ROW_COUNT;

            RETURN v_removed_count;

        END;
        $$;

        CREATE OR REPLACE FUNCTION "{0}".purge_topology()
            RETURNS int
            LANGUAGE PLPGSQL
        AS
        $$
        BEGIN
            WITH expired AS (SELECT q.id, q.name, (now()) - make_interval(secs => q.auto_delete) as expires_at
                             FROM "{0}".queue q
                             WHERE q.type = 1 AND q.auto_delete IS NOT NULL AND (now()) - make_interval(secs => q.auto_delete) > updated),
                 metrics AS (SELECT qm.queue_id, MAX(start_time) as start_time
                             FROM "{0}".queue_metric qm
                                      INNER JOIN expired q2 on q2.id = qm.queue_id
                             WHERE start_time + duration > q2.expires_at
                             GROUP BY qm.queue_id)
            DELETE FROM "{0}".queue qd
                   USING (SELECT qdx.name FROM expired qdx WHERE qdx.id NOT IN (SELECT queue_id FROM metrics)) exp
                   WHERE qd.name = exp.name;

            RETURN 0;
        END;
        $$;

        CREATE OR REPLACE FUNCTION "{0}".requeue_messages(queue_name text, source_queue_type int, target_queue_type int, message_count int,
                                                                delay interval DEFAULT INTERVAL '0 seconds', redelivery_count int DEFAULT 10)
            RETURNS int
            LANGUAGE PLPGSQL
        AS
        $$
        DECLARE
            v_source_queue_id bigint;
            v_target_queue_id bigint;
            v_requeue_count   int;
            v_enqueue_time    timestamptz;
        BEGIN
            IF NOT source_queue_type BETWEEN 1 AND 3 THEN
                RAISE EXCEPTION 'Invalid source queue type: %', source_queue_type;
            END IF;
            IF NOT target_queue_type BETWEEN 1 AND 3 THEN
                RAISE EXCEPTION 'Invalid target queue type: %', target_queue_type;
            END IF;
            IF source_queue_type = target_queue_type THEN
                RAISE EXCEPTION 'Source and target queue type must not be the same';
            END IF;

            SELECT INTO v_source_queue_id q.Id FROM "{0}".queue q WHERE q.name = queue_name AND q.type = source_queue_type;
            IF v_source_queue_id IS NULL THEN
                RAISE EXCEPTION 'Queue not found: %', queue_name;
            END IF;

            SELECT INTO v_target_queue_id q.Id FROM "{0}".queue q WHERE q.name = queue_name AND q.type = target_queue_type;
            IF v_target_queue_id IS NULL THEN
                RAISE EXCEPTION 'Queue not found: %', queue_name;
            END IF;

            v_enqueue_time := (now()) + delay;

            UPDATE "{0}".message_delivery md
            SET enqueue_time      = v_enqueue_time,
                queue_id          = v_target_queue_id,
                max_delivery_count = md.delivery_count + redelivery_count
            FROM (SELECT mdx.message_delivery_id, queue_id
                  FROM "{0}".message_delivery mdx
                  WHERE mdx.queue_id = v_source_queue_id
                    AND mdx.lock_id IS NULL
                    AND mdx.consumer_id IS NULL
                    AND (mdx.expiration_time IS NULL OR mdx.expiration_time > v_enqueue_time)
                    ORDER BY mdx.message_delivery_id FOR UPDATE LIMIT message_count) mdy
            WHERE mdy.message_delivery_id = md.message_delivery_id;
            GET DIAGNOSTICS v_requeue_count = ROW_COUNT;

            RETURN v_requeue_count;
        END;
        $$;

        CREATE OR REPLACE FUNCTION "{0}".dead_letter_messages(queue_name text, message_count int)
            RETURNS int
            LANGUAGE PLPGSQL
        AS
        $$
        DECLARE
            v_source_queue_id bigint;
            v_target_queue_id bigint;
            v_count           int;
            v_current_time    timestamptz;
        BEGIN
            SELECT INTO v_source_queue_id q.Id FROM "{0}".queue q WHERE q.name = queue_name AND q.type = 1;
            IF v_source_queue_id IS NULL THEN
                RAISE EXCEPTION 'Queue not found: %', queue_name;
            END IF;

            SELECT INTO v_target_queue_id q.Id FROM "{0}".queue q WHERE q.name = queue_name AND q.type = 3;
            IF v_target_queue_id IS NULL THEN
                RAISE EXCEPTION 'Dead-Letter Queue not found: %', queue_name;
            END IF;

            v_current_time := (now());

            UPDATE "{0}".message_delivery md
            SET queue_id = v_target_queue_id
            FROM (SELECT mdx.message_delivery_id, queue_id
                  FROM "{0}".message_delivery mdx
                  WHERE mdx.queue_id = v_source_queue_id
                    AND mdx.enqueue_time < v_current_time
                    AND mdx.delivery_count >= mdx.max_delivery_count
                    AND (mdx.expiration_time IS NULL OR mdx.expiration_time > v_current_time)
                    ORDER BY mdx.message_delivery_id FOR UPDATE SKIP LOCKED LIMIT message_count) mdy
            WHERE mdy.message_delivery_id = md.message_delivery_id;
            GET DIAGNOSTICS v_count = ROW_COUNT;

            IF v_count > 0 THEN
                INSERT INTO "{0}".queue_metric_capture (captured, queue_id, consume_count, error_count, dead_letter_count)
                    VALUES (now(), v_source_queue_id, 0, 0, v_count);
            END IF;

            RETURN v_count;
        END;
        $$;

        CREATE OR REPLACE FUNCTION "{0}".requeue_message(
            message_delivery_id bigint,
            target_queue_type int,
            delay interval DEFAULT INTERVAL '0 seconds',
            redelivery_count int DEFAULT 10)
            RETURNS int
            LANGUAGE PLPGSQL
        AS
        $$
        DECLARE
            v_source_queue_id   bigint;
            v_source_queue_name text;
            v_source_queue_type int;
            v_target_queue_id   bigint;
            v_requeue_count     int;
            v_enqueue_time      timestamptz;
        BEGIN
            IF NOT target_queue_type BETWEEN 1 AND 3 THEN
                RAISE EXCEPTION 'Invalid target queue type: %', target_queue_type;
            END IF;

            SELECT INTO v_source_queue_id md.queue_id
            FROM "{0}".message_delivery md
            WHERE md.message_delivery_id = requeue_message.message_delivery_id;
            IF v_source_queue_id IS NULL THEN
                RAISE EXCEPTION 'Message delivery not found: %', requeue_message.message_delivery_id;
            END IF;

            SELECT INTO v_source_queue_name, v_source_queue_type q.name, q.type
            FROM "{0}".queue q
            WHERE q.id = v_source_queue_id;
            IF v_source_queue_name IS NULL THEN
                RAISE EXCEPTION 'Queue not found: %', v_source_queue_id;
            END IF;

            SELECT INTO v_target_queue_id q.Id
            FROM "{0}".queue q
            WHERE q.name = v_source_queue_name
              AND q.type = target_queue_type
              AND q.type != v_source_queue_type;
            IF v_target_queue_id IS NULL THEN
                RAISE EXCEPTION 'Queue type not found: %', target_queue_type;
            END IF;

            v_enqueue_time := (now()) + delay;

            UPDATE "{0}".message_delivery md
            SET enqueue_time       = v_enqueue_time,
                queue_id           = v_target_queue_id,
                max_delivery_count = md.delivery_count + redelivery_count
            FROM (SELECT mdx.message_delivery_id, queue_id
                  FROM "{0}".message_delivery mdx
                  WHERE mdx.queue_id = v_source_queue_id
                    AND mdx.lock_id IS NULL
                    AND mdx.consumer_id IS NULL
                    AND (mdx.expiration_time IS NULL OR mdx.expiration_time > v_enqueue_time)
                    AND mdx.message_delivery_id = requeue_message.message_delivery_id FOR UPDATE) mdy
            WHERE mdy.message_delivery_id = md.message_delivery_id;
            GET DIAGNOSTICS v_requeue_count = ROW_COUNT;

            RETURN v_requeue_count;
        END;
        $$;

        CREATE OR REPLACE VIEW "{0}".queues
        AS
        SELECT x.queue_name,
               MAX(x.queue_auto_delete)                      as queue_auto_delete,
               SUM(x.message_ready)                          as ready,
               SUM(x.message_scheduled)                      as scheduled,
               SUM(x.message_error)                          as errored,
               SUM(x.message_dead_letter)                    as dead_lettered,
               SUM(x.message_locked)                         as locked,
               COALESCE(MAX(x.consume_count), 0)::bigint     as consume_count,
               COALESCE(MAX(x.error_count), 0)::bigint       as error_count,
               COALESCE(MAX(x.dead_letter_count), 0)::bigint as dead_letter_count,
               COALESCE(MAX(x.duration), 0)::int             as count_duration,
               MAX(x.queue_max_delivery_count)               as queue_max_delivery_count

        FROM (SELECT q.name                                               as queue_name,
                     q.auto_delete                                        as queue_auto_delete,
                     qm.consume_count,
                     qm.error_count,
                     qm.dead_letter_count,
                     qm.duration,

                     CASE
                         WHEN q.type = 1
                             AND md.message_delivery_id IS NOT NULL
                             AND md.enqueue_time <= (now()) THEN 1
                         ELSE 0 END                                       as message_ready,
                     CASE
                         WHEN q.type = 1
                             AND md.message_delivery_id IS NOT NULL
                             AND md.lock_id IS NULL
                             AND md.enqueue_time > (now()) THEN 1
                         ELSE 0 END                                       as message_scheduled,
                     CASE
                         WHEN q.type = 1
                             AND md.message_delivery_id IS NOT NULL
                             AND md.lock_id IS NOT NULL
                             AND md.delivery_count >= 1
                             AND md.enqueue_time > (now()) THEN 1
                         ELSE 0 END                                       as message_locked,
                     CASE
                         WHEN q.type = 2
                             AND md.message_delivery_id IS NOT NULL THEN 1
                         ELSE 0 END                                       as message_error,
                     CASE
                         WHEN q.type = 3
                             AND md.message_delivery_id IS NOT NULL THEN 1
                         ELSE 0 END                                       as message_dead_letter,
                     CASE
                         WHEN q.type = 1 THEN q.max_delivery_count
                         ELSE NULL END                                    as queue_max_delivery_count
              FROM "{0}".queue q
                       LEFT JOIN "{0}".message_delivery md ON q.id = md.queue_id
                       LEFT JOIN (SELECT DISTINCT ON (qm.queue_id) qm.queue_id,
                                                                   q2.name                         as queue_name,
                                                                   qm.consume_count                as consume_count,
                                                                   qm.error_count                  as error_count,
                                                                   qm.dead_letter_count            as dead_letter_count,
                                                                   EXTRACT(EPOCH FROM qm.duration) as duration
                                  FROM "{0}".queue_metric qm
                                           INNER JOIN "{0}".queue q2 on qm.queue_id = q2.id
                                  WHERE q2.type = 1
                                    AND qm.start_time >= (now()) - interval '1 minutes'
                                  ORDER BY qm.queue_id, qm.start_time DESC) qm ON qm.queue_id = q.id) x
        GROUP BY x.queue_name;

        CREATE OR REPLACE VIEW "{0}".subscriptions
        AS
            SELECT t.name as topic_name, 'topic' as destination_type, t2.name as destination_name, ts.sub_type as subscription_type, ts.routing_key
            FROM "{0}".topic t
                     JOIN "{0}".topic_subscription ts ON t.id = ts.source_id
                     JOIN "{0}".topic t2 on t2.id = ts.destination_id
            UNION
            SELECT t.name as topic_name, 'queue' as destination_type, q.name as destination_name, qs.sub_type as subscription_type, qs.routing_key
            FROM "{0}".queue_subscription qs
                     LEFT JOIN "{0}".queue q on qs.destination_id = q.id
                     LEFT JOIN "{0}".topic t on qs.source_id = t.id;

        -- CREATE TABLE IF NOT EXISTS does not replace defaults in retained schemas.
        ALTER TABLE "{0}".queue ALTER COLUMN updated SET DEFAULT now();
        ALTER TABLE "{0}".topic ALTER COLUMN updated SET DEFAULT now();
        ALTER TABLE "{0}".topic_subscription ALTER COLUMN updated SET DEFAULT now();
        ALTER TABLE "{0}".queue_subscription ALTER COLUMN updated SET DEFAULT now();
        ALTER TABLE "{0}".message ALTER COLUMN sent_time SET DEFAULT now();

        DO $restore_topology_privileges$
        DECLARE
            saved record;
            privilege record;
            routine_identity text;
            current_oid oid;
            grantee_name text;
            migration_role text := current_user;
            restored_count integer;
        BEGIN
            CREATE TEMP TABLE vicione_topology_privilege_replay ON COMMIT DROP AS
            SELECT saved_acl.original_oid, acl.grantor, acl.grantee, acl.is_grantable, false AS restored
            FROM pg_temp.vicione_topology_routine_acl saved_acl,
                LATERAL pg_catalog.aclexplode(saved_acl.privileges) acl;

            FOR saved IN SELECT * FROM pg_temp.vicione_topology_routine_acl
            LOOP
                routine_identity := format('%I.%I(%s)', '{0}', saved.routine_name, saved.argument_types);
                current_oid := to_regprocedure(routine_identity)::oid;
                IF current_oid <> saved.original_oid THEN
                    EXECUTE format('ALTER FUNCTION %s OWNER TO %I', routine_identity,
                        pg_catalog.pg_get_userbyid(saved.owner_oid));

                    -- Remove newly applied default grants before replaying the saved effective ACL.
                    FOR privilege IN
                        SELECT DISTINCT acl.grantee
                        FROM pg_catalog.pg_proc p,
                            LATERAL pg_catalog.aclexplode(COALESCE(p.proacl, pg_catalog.acldefault('f', p.proowner))) acl
                        WHERE p.oid = current_oid
                    LOOP
                        grantee_name := CASE WHEN privilege.grantee = 0 THEN 'PUBLIC'
                            ELSE format('%I', pg_catalog.pg_get_userbyid(privilege.grantee)) END;
                        EXECUTE format('REVOKE ALL PRIVILEGES ON FUNCTION %s FROM %s', routine_identity, grantee_name);
                    END LOOP;

                    -- Replay grant chains only when the original grantor already has the
                    -- grant option. SET ROLE retains the actual grantor/revocation semantics.
                    WHILE EXISTS (SELECT 1 FROM pg_temp.vicione_topology_privilege_replay
                        WHERE original_oid = saved.original_oid AND NOT restored)
                    LOOP
                        restored_count := 0;
                        FOR privilege IN
                            SELECT replay.* FROM pg_temp.vicione_topology_privilege_replay replay
                            WHERE replay.original_oid = saved.original_oid AND NOT replay.restored
                                AND pg_catalog.has_function_privilege(replay.grantor, current_oid, 'EXECUTE WITH GRANT OPTION')
                        LOOP
                            grantee_name := CASE WHEN privilege.grantee = 0 THEN 'PUBLIC'
                                ELSE format('%I', pg_catalog.pg_get_userbyid(privilege.grantee)) END;
                            EXECUTE format('SET LOCAL ROLE %I', pg_catalog.pg_get_userbyid(privilege.grantor));
                            EXECUTE format('GRANT EXECUTE ON FUNCTION %s TO %s%s', routine_identity, grantee_name,
                                CASE WHEN privilege.is_grantable THEN ' WITH GRANT OPTION' ELSE '' END);
                            EXECUTE format('SET LOCAL ROLE %I', migration_role);
                            UPDATE pg_temp.vicione_topology_privilege_replay
                            SET restored = true
                            WHERE original_oid = saved.original_oid AND grantor = privilege.grantor
                                AND grantee = privilege.grantee;
                            restored_count := restored_count + 1;
                        END LOOP;
                        IF restored_count = 0 THEN
                            RAISE EXCEPTION 'The topology function EXECUTE grant chain could not be restored for %', routine_identity;
                        END IF;
                    END LOOP;

                    -- GRANT may only warn: verify the effective ACL instead of trusting replay bookkeeping.
                    IF EXISTS (
                        WITH expected_acl AS (
                            SELECT acl.grantor, acl.grantee, acl.privilege_type, acl.is_grantable
                            FROM pg_catalog.aclexplode(saved.privileges) acl
                        ), actual_acl AS (
                            SELECT acl.grantor, acl.grantee, acl.privilege_type, acl.is_grantable
                            FROM pg_catalog.pg_proc p,
                                LATERAL pg_catalog.aclexplode(COALESCE(p.proacl, pg_catalog.acldefault('f', p.proowner))) acl
                            WHERE p.oid = current_oid
                        )
                        SELECT 1 FROM (
                            (SELECT * FROM expected_acl EXCEPT SELECT * FROM actual_acl)
                            UNION ALL
                            (SELECT * FROM actual_acl EXCEPT SELECT * FROM expected_acl)
                        ) acl_delta
                    ) THEN
                        RAISE EXCEPTION 'The topology function EXECUTE ACL was not preserved for %', routine_identity;
                    END IF;
                END IF;
            END LOOP;
        END
        $restore_topology_privileges$;

        SET ROLE none;
        """;

    readonly ILogger<PostgreSqlDatabaseMigrator> _logger;

    /// <summary>Initializes the PostgreSQL database migrator.</summary>
    /// <param name="logger">The logger used to report migration failures.</param>
    public PostgreSqlDatabaseMigrator(ILogger<PostgreSqlDatabaseMigrator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Creates the configured PostgreSQL database when it does not exist.</summary>
    /// <param name="options">The database name, connection settings, and migration credentials.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CreateDatabaseAsync(SqlTransportOptions options, CancellationToken cancellationToken = default)
    {
        await CreateDatabaseIfNotExistAsync(options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Forcibly drops the configured PostgreSQL database when it exists.</summary>
    /// <param name="options">The database name, connection settings, and migration credentials.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteDatabaseAsync(SqlTransportOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var database = PostgreSqlIdentifier.Validate(options.Database, nameof(options.Database));

        await using var connection = PostgreSqlTransportConnection.GetSystemDatabaseConnection(options);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var existsCommand = new CommandDefinition(DbExistsSql, new { Name = database }, cancellationToken: cancellationToken);
        var result = await connection.Connection.ExecuteScalarAsync<int>(existsCommand).ConfigureAwait(false);
        if (result == 1)
        {
            var dropCommand = new CommandDefinition(string.Format(DropSql, database), cancellationToken: cancellationToken);
            await connection.Connection.ExecuteScalarAsync<int>(dropCommand).ConfigureAwait(false);

            _logger.LogInformation("Database {Database} deleted", database);
        }
    }

    /// <summary>Creates the PostgreSQL transport types, tables, functions, indexes, and grants in the configured schema.</summary>
    /// <param name="options">The target schema, transport role, and database connection settings.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CreateInfrastructureAsync(SqlTransportOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        var schema = PostgreSqlIdentifier.Validate(options.Schema, nameof(options.Schema));
        var role = PostgreSqlIdentifier.Validate(options.Role, nameof(options.Role));

        await using var connection = PostgreSqlTransportConnection.GetDatabaseConnection(options);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        string notifyChannelPrefix = PostgreSqlNotificationChannel.CreatePrefix(schema);
        await using (var transaction = await connection.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
        {
            var command = new CommandDefinition(
                string.Format(CreateInfrastructureSql, schema, role, notifyChannelPrefix),
                transaction: transaction, cancellationToken: cancellationToken);

            await connection.Connection.ExecuteScalarAsync<int>(command).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        var columnTypeCommand = new CommandDefinition(BodyExactColumnTypeSql, new { Schema = schema }, cancellationToken: cancellationToken);
        bool? columnIsJson = await connection.Connection.ExecuteScalarAsync<bool?>(columnTypeCommand).ConfigureAwait(false);
        if (columnIsJson == false)
            throw new InvalidOperationException($"The PostgreSQL transport schema '{schema}' has a body_exact column that is not json.");

        if (columnIsJson is null)
        {
            await using var transaction = await connection.Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var prepareCommand = new CommandDefinition(string.Format(PrepareBodyExactMigrationSql, role),
                transaction: transaction, cancellationToken: cancellationToken);
            await connection.Connection.ExecuteAsync(prepareCommand).ConfigureAwait(false);

            try
            {
                var addColumnCommand = new CommandDefinition(string.Format(AddBodyExactColumnSql, schema),
                    transaction: transaction, cancellationToken: cancellationToken);
                await connection.Connection.ExecuteAsync(addColumnCommand).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.LockNotAvailable)
            {
                throw new InvalidOperationException(
                    $"The PostgreSQL transport schema '{schema}' could not add body_exact within the bounded lock wait. Retry the migration when the message table is available.",
                    exception);
            }

            columnIsJson = await connection.Connection.ExecuteScalarAsync<bool?>(columnTypeCommand).ConfigureAwait(false);
            if (columnIsJson != true)
                throw new InvalidOperationException($"The PostgreSQL transport schema '{schema}' did not acquire a json body_exact column.");
        }

        _logger.LogDebug("Transport infrastructure in schema {Schema} created or updated", schema);
    }

    async Task CreateDatabaseIfNotExistAsync(SqlTransportOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        var database = PostgreSqlIdentifier.Validate(options.Database, nameof(options.Database));

        await using var connection = PostgreSqlTransportConnection.GetSystemDatabaseConnection(options);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var existsCommand = new CommandDefinition(DbExistsSql, new { Name = database }, cancellationToken: cancellationToken);
        var result = await connection.Connection.ExecuteScalarAsync<int>(existsCommand).ConfigureAwait(false);
        if (result == 1)
            _logger.LogDebug("Database {Database} already exists", database);
        else
        {
            var createCommand = new CommandDefinition(string.Format(DbCreateSql, database), cancellationToken: cancellationToken);
            await connection.Connection.ExecuteScalarAsync<int>(createCommand).ConfigureAwait(false);

            _logger.LogInformation("Database {Database} created", database);
        }
    }

    /// <summary>Creates the configured PostgreSQL schema when necessary and grants it to the transport role.</summary>
    /// <param name="options">The target schema, transport role, and database connection settings.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CreateSchemaIfNotExistAsync(SqlTransportOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        var schema = PostgreSqlIdentifier.Validate(options.Schema, nameof(options.Schema));

        await using var connection = PostgreSqlTransportConnection.GetDatabaseAdminConnection(options);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var createCommand = new CommandDefinition(string.Format(SchemaCreateSql, schema), cancellationToken: cancellationToken);
        await connection.Connection.ExecuteScalarAsync<int>(createCommand).ConfigureAwait(false);

        _logger.LogDebug("Schema {Schema} created or already exists", schema);

        await GrantAccessAsync(connection, options, cancellationToken).ConfigureAwait(false);
    }

    async Task GrantAccessAsync(IPostgreSqlTransportConnection connection, SqlTransportOptions options, CancellationToken cancellationToken)
    {
        var database = PostgreSqlIdentifier.Validate(options.Database, nameof(options.Database));
        var role = PostgreSqlIdentifier.Validate(options.Role, nameof(options.Role));
        var schema = PostgreSqlIdentifier.Validate(options.Schema, nameof(options.Schema));
        var principal = PostgreSqlIdentifier.Validate(
            PostgreSqlTransportConnection.GetAdminMigrationPrincipal(options), nameof(options.AdminUsername));

        var roleExistsCommand = new CommandDefinition(RoleExistsSql, new { Name = role }, cancellationToken: cancellationToken);
        var result = await connection.Connection.ExecuteScalarAsync<int>(roleExistsCommand).ConfigureAwait(false);
        if (result != 1)
        {
            var createRoleCommand = new CommandDefinition(string.Format(CreateRoleSql, role), cancellationToken: cancellationToken);
            await connection.Connection.ExecuteScalarAsync<int>(createRoleCommand).ConfigureAwait(false);

            _logger.LogDebug("Role {Role} created", role);
        }

        if (!string.Equals(role, principal, StringComparison.Ordinal))
        {
            var grantRoleCommand = new CommandDefinition(
                string.Format(GrantRoleToPrincipalSql, role, principal), cancellationToken: cancellationToken);
            await connection.Connection.ExecuteScalarAsync<int>(grantRoleCommand).ConfigureAwait(false);
        }

        var grantSchemaCommand = new CommandDefinition(string.Format(GrantRoleSql, role, schema), cancellationToken: cancellationToken);
        await connection.Connection.ExecuteScalarAsync<int>(grantSchemaCommand).ConfigureAwait(false);

        _logger.LogDebug("Role {Role} granted access to schema {Schema}", role, schema);

        var grantConnectCommand = new CommandDefinition(string.Format(GrantConnectSql, database, role), cancellationToken: cancellationToken);
        await connection.Connection.ExecuteScalarAsync<int>(grantConnectCommand).ConfigureAwait(false);

        _logger.LogDebug("Role {Role} granted connect to database {Database}", role, database);

        ArgumentException.ThrowIfNullOrWhiteSpace(options.Username);
        var userExistsCommand = new CommandDefinition(RoleExistsSql, new { Name = options.Username }, cancellationToken: cancellationToken);
        result = await connection.Connection.ExecuteScalarAsync<int>(userExistsCommand).ConfigureAwait(false);
        if (result != 1)
        {
            var createUserCommand = new CommandDefinition(CreateUserSql,
                new { RoleName = role, options.Username, options.Password }, cancellationToken: cancellationToken);
            await connection.Connection.ExecuteScalarAsync<int>(createUserCommand).ConfigureAwait(false);

            _logger.LogDebug("User role {Username} created", options.Username);
        }
    }
}
