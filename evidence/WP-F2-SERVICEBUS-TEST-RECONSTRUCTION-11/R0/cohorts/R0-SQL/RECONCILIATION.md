# R0-SQL — anchor reconciliation

Cohort `R0-SQL`, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`, baseline commit
`ae73c6da748e3bc3257dffa4971ee8680e086207`.

## 1. Scope and completeness (TLP-017)

| Scope | tracked files | of which `.cs` |
|---|---:|---:|
| `tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests` | 40 | 37 |
| `src/ViciOne.ServiceBus/SqlTransport` | 149 | 149 |
| `src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql` | 20 | 17 |
| `src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer` | 17 | 14 |
| **Total** | **226** | **217** |

The three non-`.cs` files in the test project are `ViciOne.ServiceBus.SqlTransport.Tests.csproj`,
`docker-compose.yml` and `packages.lock.json`; each provider project adds a `.csproj`, a
`.csproj.DotSettings` and a `packages.lock.json`. The assignment named the `.cs` counts (37 / 17 / 14);
the manifest covers the full tracked set, which is what `git ls-files` returns for those four paths.

Every one of the 226 files was read in full. `READ_MANIFEST.tsv` (`path<TAB>sha256`, `LC_ALL=C` sorted)
was compared against `R0/BASELINE_TRACKED_FILE_MANIFEST.tsv` restricted to the same four path prefixes:
**226 rows, identical path set and identical SHA-256 for every row.** No file in scope is unread.

Files read outside the cohort scope, for context only and **not** in `READ_MANIFEST.tsv`
(they belong to other cohorts): `tests/ViciOne.ServiceBus.TestInfrastructure/{TestRunnerContract,
TestDatabase,DatabaseEndpoint}.cs`, `build/test-infrastructure/compose.yaml`,
`tools/ci/fixtures/{compose_fixture,broker_logs}.py`, `build/verification/expected/sql-transport.txt`.

## 2. Anchor reconciliation — `build/verification/expected/sql-transport.txt`

| Property | Lead plan section 9 | measured at the baseline commit |
|---|---|---|
| Path | `build/verification/expected/sql-transport.txt` | same |
| SHA-256 | `76c7d0dac6b9f9e94420907bfa9c66d48cd5f808a34940885763d3750a12fa76` | `76c7d0dac6b9f9e94420907bfa9c66d48cd5f808a34940885763d3750a12fa76` |
| Identities | 110 | 110 (113 lines minus 3 documenting header lines) |

**Derivation of the census.** The ledger was written from the fully read test sources first. It was
then checked against an independent mechanical census: a script that walks the 37 `.cs` files, resolves
`[TestFixture(typeof(T))]` into one identity per closed fixture, expands `[TestCase(...)]` into one
identity per resolved argument list, and emits `namespace.Class<T>.Method(args)`. That census produced
**110** identities.

**Result of the comparison, in both directions:**

| Direction | count |
|---|---:|
| Anchor identities not covered by a ledger row | **0** |
| Ledger rows derived from an inherited test without an anchor identity | **0** |
| Identities produced by the census that the anchor does not list | **0** |
| Identities the anchor lists that the census does not produce | **0** |

Nothing was adopted from the anchor: the anchor is evidence, the ledger is derived from read semantics
and current product behaviour. There is **no deviation to disposition** on this anchor.

### 2.1 Per-category identity counts

| Test file | identities | ledger rows | owner |
|---|---:|---:|---|
| `Address_Specs.cs` | 20 | 20 | shared hermetic |
| `Configuration_Specs.cs` | 1 | 1 | shared hermetic |
| `RunnerContract_Specs.cs` | 9 | 7 | shared hermetic |
| `PgSql/ChannelName_Specs.cs` | 6 | 3 | PostgreSQL (hermetic tier) |
| `PgSql/Migration_Specs.cs` | 4 | 2 | PostgreSQL |
| `PgSql/MultiHost_Specs.cs` | 4 | 4 | PostgreSQL |
| `PgSql/PortAddress_Specs.cs` | 2 | 2 | PostgreSQL |
| `PgSqlBus_Specs.cs` | 4 | 4 | PostgreSQL |
| `Provision_Specs.cs` | 2 | 2 | PostgreSQL |
| `Fault_Specs.cs` | 3 | 3 | PostgreSQL |
| `BusOutbox_Specs.cs` | 1 | 1 | PostgreSQL |
| `RoutingSlip_Specs.cs` | 1 | 1 | PostgreSQL |
| `SubscriptionType_Specs.cs` | 2 | 2 | PostgreSQL |
| `SqlServer/InstanceName_Specs.cs` | 3 | 3 | SQL Server |
| `SqlServer/PortAddress_Specs.cs` | 3 | 3 | SQL Server |
| `SqlServer/Provision_Specs.cs` | 2 | 2 | SQL Server |
| `SqlServer/ReceiveEndpoint_Specs.cs` | 1 | 1 | SQL Server |
| generic fixtures (11 classes, 21 methods x 2 providers) | 42 | 42 | 21 PostgreSQL + 21 SQL Server |
| **Total** | **110** | **103** | |

Seven fewer rows than identities, all from `[TestCase]` collapsing: `ChannelName_Specs` (6 identities
in 3 rows), `Migration_Specs.Should_parse_connection_string_into_options` (3 in 1) and
`The_runner_contract.Should_refuse_a_port_outside_the_range` (3 in 1). Each of those rows carries the
resolved variant list, as section 3 of the reading rules requires.

### 2.2 Identity-to-obligation map

Columns: obligation, number of anchor identities it carries, owner (`shared` / `pg` / `mssql`),
profile (`U` = `UnitArchitecture`, `L` = `LocalIntegration`), identity (namespace prefix
`ViciOne.ServiceBus.SqlTransport.Tests.` omitted).

| obligation | ids | owner | prof | anchor identity |
|---|---:|---|---|---|
| OBL-R0-SQL-0001 | 1 | shared | U | `Specifying_a_host_address.Should_support_a_virtual_host` |
| OBL-R0-SQL-0002 | 1 | shared | U | `Specifying_a_host_address.Should_support_a_virtual_host_and_scope` |
| OBL-R0-SQL-0003 | 1 | shared | U | `Specifying_a_host_address.Should_support_a_virtual_host_and_scope_option_2` |
| OBL-R0-SQL-0004 | 1 | shared | U | `Specifying_a_host_address.Should_support_a_virtual_host_and_scope_with_queue` |
| OBL-R0-SQL-0005 | 1 | shared | U | `Specifying_a_host_address.Should_support_the_simplest_use_case` |
| OBL-R0-SQL-0006 | 1 | shared | U | `Specifying_a_host_address.Should_support_the_simplest_use_case_option_2` |
| OBL-R0-SQL-0007 | 1 | shared | U | `Specifying_a_host_address.Should_throw_on_invalid_scope` |
| OBL-R0-SQL-0008 | 1 | shared | U | `Specifying_a_host_address.Should_throw_on_invalid_virtual_host` |
| OBL-R0-SQL-0009 | 1 | shared | U | `Specifying_an_endpoint_address.Should_parse_the_instance_name_from_url` |
| OBL-R0-SQL-0010 | 1 | shared | U | `Specifying_an_endpoint_address.Should_support_a_virtual_host` |
| OBL-R0-SQL-0011 | 1 | shared | U | `Specifying_an_endpoint_address.Should_support_a_virtual_host_and_scope` |
| OBL-R0-SQL-0012 | 1 | shared | U | `Specifying_an_endpoint_address.Should_support_a_virtual_host_and_scope_option_2` |
| OBL-R0-SQL-0013 | 1 | shared | U | `Specifying_an_endpoint_address.Should_support_a_virtual_host_and_scope_with_queue` |
| OBL-R0-SQL-0014 | 1 | shared | U | `Specifying_an_endpoint_address.Should_support_a_virtual_host_and_scope_with_topic` |
| OBL-R0-SQL-0015 | 1 | shared | U | `Specifying_an_endpoint_address.Should_support_ipv6_address` |
| OBL-R0-SQL-0016 | 1 | shared | U | `Specifying_an_endpoint_address.Should_support_the_backslash_in_sql_host_names` |
| OBL-R0-SQL-0017 | 1 | shared | U | `Specifying_an_endpoint_address.Should_support_the_simplest_use_case` |
| OBL-R0-SQL-0018 | 1 | shared | U | `Specifying_an_endpoint_address.Should_support_the_simplest_use_case_option_2` |
| OBL-R0-SQL-0019 | 1 | shared | U | `Specifying_an_endpoint_address.Should_throw_on_invalid_scope` |
| OBL-R0-SQL-0020 | 1 | shared | U | `Specifying_an_endpoint_address.Should_throw_on_invalid_virtual_host` |
| OBL-R0-SQL-0021 | 1 | shared | U | `Configuration_Specs.Configuring_an_ipv6_host` |
| OBL-R0-SQL-0022 | 1 | pg | U | `PgSql.ChannelName_Specs.Should_sanitize_schema("string that has 40 characters 1234567890")` |
| OBL-R0-SQL-0023 | 2 | pg | U | `PgSql.ChannelName_Specs.Should_not_sanitize_schema` x2<br>`PgSql.ChannelName_Specs.Should_not_sanitize_schema("string that has 38 characters 12345678")`<br>`PgSql.ChannelName_Specs.Should_not_sanitize_schema("string that has 39 characters 123456789")` |
| OBL-R0-SQL-0024 | 3 | pg | U | `PgSql.ChannelName_Specs.Should_return_default` x3<br>`PgSql.ChannelName_Specs.Should_return_default(" ")`<br>`PgSql.ChannelName_Specs.Should_return_default("")`<br>`PgSql.ChannelName_Specs.Should_return_default(null)` |
| OBL-R0-SQL-0025 | 1 | pg | L | `PgSql.Migration_Specs.Should_work_with_data_source` |
| OBL-R0-SQL-0026 | 3 | pg | U | `PgSql.Migration_Specs.Should_parse_connection_string_into_options` x3<br>`PgSql.Migration_Specs.Should_parse_connection_string_into_options("host=localhost;user id=viciOneServiceBusAdmin;password=2Legit2Quit;database=sample;")`<br>`PgSql.Migration_Specs.Should_parse_connection_string_into_options("host=messaging.postgres.database.azure.com;user id=viciOneServiceBusAdmin@messaging;password=2Legit2Quit;database=sample")`<br>`PgSql.Migration_Specs.Should_parse_connection_string_into_options("host=messaging.server.com;user id=viciOneServiceBusAdmin;password=2Legit2Quit;database=sample")` |
| OBL-R0-SQL-0027 | 1 | pg | U | `PgSql.MultiHost_Specs.Should_allow_multiple_host_names` |
| OBL-R0-SQL-0028 | 1 | pg | U | `PgSql.MultiHost_Specs.Should_allow_multiple_host_names_with_custom_port` |
| OBL-R0-SQL-0029 | 1 | pg | U | `PgSql.MultiHost_Specs.Should_allow_multiple_host_names_with_custom_ports` |
| OBL-R0-SQL-0030 | 1 | pg | U | `PgSql.MultiHost_Specs.Should_allow_single_host_names_with_custom_port` |
| OBL-R0-SQL-0031 | 1 | pg | U | `PgSql.PortAddress_Specs.Should_include_the_port_for_postgres` |
| OBL-R0-SQL-0032 | 1 | pg | U | `PgSql.PortAddress_Specs.Should_not_include_the_port_for_postgres` |
| OBL-R0-SQL-0033 | 1 | pg | L | `Configuring_the_postgresql_bus.Should_support_standard_syntax_with_consumers` |
| OBL-R0-SQL-0034 | 1 | pg | L | `Configuring_the_postgresql_bus.Should_support_standard_syntax_with_consumers_and_topology` |
| OBL-R0-SQL-0035 | 1 | pg | L | `Configuring_the_postgresql_bus.Should_support_the_standard_syntax` |
| OBL-R0-SQL-0036 | 1 | pg | L | `Configuring_the_postgresql_bus.Should_support_the_standard_syntax_with_three_queues` |
| OBL-R0-SQL-0037 | 1 | pg | L | `Provisioning_the_transport_database.Should_create_the_required_schema_tables_and_indices` |
| OBL-R0-SQL-0038 | 1 | pg | L | `Provisioning_the_transport_database.Should_drop_the_database_on_shutdown` |
| OBL-R0-SQL-0039 | 1 | mssql | U | `SqlServer.InstanceName_Specs.Should_include_the_instance_name` |
| OBL-R0-SQL-0040 | 1 | mssql | L | `SqlServer.InstanceName_Specs.Should_include_the_instance_name_and_port_and_start` |
| OBL-R0-SQL-0041 | 1 | mssql | L | `SqlServer.InstanceName_Specs.Should_include_the_instance_name_and_start` |
| OBL-R0-SQL-0042 | 1 | mssql | U | `SqlServer.PortAddress_Specs.Should_include_the_port` |
| OBL-R0-SQL-0043 | 1 | mssql | U | `SqlServer.PortAddress_Specs.Should_not_blow_up_with_local_db` |
| OBL-R0-SQL-0044 | 1 | mssql | U | `SqlServer.PortAddress_Specs.Should_not_include_the_port` |
| OBL-R0-SQL-0045 | 1 | mssql | L | `SqlServer.Provisioning_the_transport_database.Should_create_the_required_schema_tables_and_indices` |
| OBL-R0-SQL-0046 | 1 | mssql | L | `SqlServer.Provisioning_the_transport_database.Should_drop_the_database_on_shutdown` |
| OBL-R0-SQL-0047 | 1 | mssql | L | `SqlServer.Configuring_a_receive_endpoint_without_topology.Should_create_the_queue` |
| OBL-R0-SQL-0048 | 1 | shared | U | `The_runner_contract.Should_describe_a_complete_endpoint` |
| OBL-R0-SQL-0049 | 1 | shared | U | `The_runner_contract.Should_refuse_an_incomplete_endpoint` |
| OBL-R0-SQL-0050 | 3 | shared | U | `The_runner_contract.Should_refuse_a_port_outside_the_range` x3<br>`The_runner_contract.Should_refuse_a_port_outside_the_range(-1)`<br>`The_runner_contract.Should_refuse_a_port_outside_the_range(0)`<br>`The_runner_contract.Should_refuse_a_port_outside_the_range(65536)` |
| OBL-R0-SQL-0051 | 1 | shared | U | `The_runner_contract.Should_not_disclose_the_secret_when_it_is_described` |
| OBL-R0-SQL-0052 | 1 | shared | U | `The_runner_contract.Should_name_every_missing_variable_and_no_secret_when_the_contract_is_incomplete` |
| OBL-R0-SQL-0053 | 1 | shared | U | `The_runner_contract.Should_refuse_a_port_the_runner_published_as_nonsense` |
| OBL-R0-SQL-0054 | 1 | shared | U | `The_runner_contract.Should_refuse_a_configuration_it_does_not_know` |
| OBL-R0-SQL-0055 | 1 | pg | L | `When_routing_via_a_routing_key.Should_support_routing_key` |
| OBL-R0-SQL-0056 | 1 | pg | L | `When_routing_using_a_pattern.Should_support_routing_key` |
| OBL-R0-SQL-0057 | 1 | pg | L | `When_a_consumer_throws_an_exception.Should_dead_letter_skipped_messages` |
| OBL-R0-SQL-0058 | 1 | pg | L | `When_a_consumer_throws_an_exception.Should_publish_fault_and_move_to_the_error_queue` |
| OBL-R0-SQL-0059 | 1 | pg | L | `When_a_consumer_throws_an_exception.Should_use_built_in_redelivery_to_redeliver_faulted_messages` |
| OBL-R0-SQL-0060 | 1 | pg | L | `Using_the_bus_outbox.Should_work_with_the_db_transport` |
| OBL-R0-SQL-0061 | 1 | pg | L | `Using_a_routing_slip_with_a_custom_subscription.Should_be_sent` |
| OBL-R0-SQL-0062 | 1 | pg | L | `Canceling_a_scheduled_message_from_a_consumer<PostgresDatabaseTestConfiguration>.Should_be_supported` |
| OBL-R0-SQL-0063 | 1 | mssql | L | `Canceling_a_scheduled_message_from_a_consumer<SqlServerDatabaseTestConfiguration>.Should_be_supported` |
| OBL-R0-SQL-0064 | 1 | pg | L | `Canceling_a_scheduled_message_from_a_consumer<PostgresDatabaseTestConfiguration>.Should_be_supported_from_outside_the_consumer` |
| OBL-R0-SQL-0065 | 1 | mssql | L | `Canceling_a_scheduled_message_from_a_consumer<SqlServerDatabaseTestConfiguration>.Should_be_supported_from_outside_the_consumer` |
| OBL-R0-SQL-0066 | 1 | pg | L | `Publishing_a_unsubscribed_message_type<PostgresDatabaseTestConfiguration>.Should_leave_neither_a_delivery_nor_a_message_row` |
| OBL-R0-SQL-0067 | 1 | mssql | L | `Publishing_a_unsubscribed_message_type<SqlServerDatabaseTestConfiguration>.Should_leave_neither_a_delivery_nor_a_message_row` |
| OBL-R0-SQL-0068 | 1 | pg | L | `Purging_a_queue_on_startup<PostgresDatabaseTestConfiguration>.Should_remove_what_was_in_the_queue_before_the_start` |
| OBL-R0-SQL-0069 | 1 | mssql | L | `Purging_a_queue_on_startup<SqlServerDatabaseTestConfiguration>.Should_remove_what_was_in_the_queue_before_the_start` |
| OBL-R0-SQL-0070 | 1 | pg | L | `Using_a_job_consumer<PostgresDatabaseTestConfiguration>.Should_cancel_the_job` |
| OBL-R0-SQL-0071 | 1 | mssql | L | `Using_a_job_consumer<SqlServerDatabaseTestConfiguration>.Should_cancel_the_job` |
| OBL-R0-SQL-0072 | 1 | pg | L | `Using_a_job_consumer<PostgresDatabaseTestConfiguration>.Should_cancel_the_job_and_get_the_status` |
| OBL-R0-SQL-0073 | 1 | mssql | L | `Using_a_job_consumer<SqlServerDatabaseTestConfiguration>.Should_cancel_the_job_and_get_the_status` |
| OBL-R0-SQL-0074 | 1 | pg | L | `Using_a_job_consumer<PostgresDatabaseTestConfiguration>.Should_cancel_the_job_and_retry_it` |
| OBL-R0-SQL-0075 | 1 | mssql | L | `Using_a_job_consumer<SqlServerDatabaseTestConfiguration>.Should_cancel_the_job_and_retry_it` |
| OBL-R0-SQL-0076 | 1 | pg | L | `Using_a_job_consumer<PostgresDatabaseTestConfiguration>.Should_cancel_the_job_while_waiting` |
| OBL-R0-SQL-0077 | 1 | mssql | L | `Using_a_job_consumer<SqlServerDatabaseTestConfiguration>.Should_cancel_the_job_while_waiting` |
| OBL-R0-SQL-0078 | 1 | pg | L | `Using_a_job_consumer<PostgresDatabaseTestConfiguration>.Should_complete_the_job` |
| OBL-R0-SQL-0079 | 1 | mssql | L | `Using_a_job_consumer<SqlServerDatabaseTestConfiguration>.Should_complete_the_job` |
| OBL-R0-SQL-0080 | 1 | pg | L | `Using_a_job_consumer<PostgresDatabaseTestConfiguration>.Should_create_a_unique_job_id` |
| OBL-R0-SQL-0081 | 1 | mssql | L | `Using_a_job_consumer<SqlServerDatabaseTestConfiguration>.Should_create_a_unique_job_id` |
| OBL-R0-SQL-0082 | 1 | pg | L | `Using_a_job_consumer<PostgresDatabaseTestConfiguration>.Should_return_not_found` |
| OBL-R0-SQL-0083 | 1 | mssql | L | `Using_a_job_consumer<SqlServerDatabaseTestConfiguration>.Should_return_not_found` |
| OBL-R0-SQL-0084 | 1 | pg | L | `Using_a_slow_consumer<PostgresDatabaseTestConfiguration>.Should_renew_the_lock_and_deliver_the_message_once` |
| OBL-R0-SQL-0085 | 1 | mssql | L | `Using_a_slow_consumer<SqlServerDatabaseTestConfiguration>.Should_renew_the_lock_and_deliver_the_message_once` |
| OBL-R0-SQL-0086 | 1 | pg | L | `Using_delayed_send<PostgresDatabaseTestConfiguration>.Should_be_supported` |
| OBL-R0-SQL-0087 | 1 | mssql | L | `Using_delayed_send<SqlServerDatabaseTestConfiguration>.Should_be_supported` |
| OBL-R0-SQL-0088 | 1 | pg | L | `Using_delayed_publish<PostgresDatabaseTestConfiguration>.Should_be_supported` |
| OBL-R0-SQL-0089 | 1 | mssql | L | `Using_delayed_publish<SqlServerDatabaseTestConfiguration>.Should_be_supported` |
| OBL-R0-SQL-0090 | 1 | pg | L | `Using_json_extension_data<PostgresDatabaseTestConfiguration>.Should_properly_serialize_the_message` |
| OBL-R0-SQL-0091 | 1 | mssql | L | `Using_json_extension_data<SqlServerDatabaseTestConfiguration>.Should_properly_serialize_the_message` |
| OBL-R0-SQL-0092 | 1 | pg | L | `Using_message_delivery_count_limit<PostgresDatabaseTestConfiguration>.Should_carry_the_configured_limit_into_the_queue` |
| OBL-R0-SQL-0093 | 1 | mssql | L | `Using_message_delivery_count_limit<SqlServerDatabaseTestConfiguration>.Should_carry_the_configured_limit_into_the_queue` |
| OBL-R0-SQL-0094 | 1 | pg | L | `Using_message_delivery_count_limit<PostgresDatabaseTestConfiguration>.Should_not_consume_the_message_after_the_limit` |
| OBL-R0-SQL-0095 | 1 | mssql | L | `Using_message_delivery_count_limit<SqlServerDatabaseTestConfiguration>.Should_not_consume_the_message_after_the_limit` |
| OBL-R0-SQL-0096 | 1 | pg | L | `Using_partition_keys<PostgresDatabaseTestConfiguration>.Should_consume_a_lot_of_published_messages` |
| OBL-R0-SQL-0097 | 1 | mssql | L | `Using_partition_keys<SqlServerDatabaseTestConfiguration>.Should_consume_a_lot_of_published_messages` |
| OBL-R0-SQL-0098 | 1 | pg | L | `Using_publish<PostgresDatabaseTestConfiguration>.Should_consume_a_lot_of_published_messages` |
| OBL-R0-SQL-0099 | 1 | mssql | L | `Using_publish<SqlServerDatabaseTestConfiguration>.Should_consume_a_lot_of_published_messages` |
| OBL-R0-SQL-0100 | 1 | pg | L | `Using_the_request_client<PostgresDatabaseTestConfiguration>.Should_properly_return_the_response` |
| OBL-R0-SQL-0101 | 1 | mssql | L | `Using_the_request_client<SqlServerDatabaseTestConfiguration>.Should_properly_return_the_response` |
| OBL-R0-SQL-0102 | 1 | pg | L | `When_the_redelivery_header_is_present<PostgresDatabaseTestConfiguration>.Should_not_exist_on_outgoing_messages` |
| OBL-R0-SQL-0103 | 1 | mssql | L | `When_the_redelivery_header_is_present<SqlServerDatabaseTestConfiguration>.Should_not_exist_on_outgoing_messages` |

## 3. The target shape from Lead plan section 5

Section 5 assigns this cohort "gemeinsamer hermetischer SQL-Owner plus je ein providerbezogener
Integrationowner — PostgreSQL und SQL Server getrennt und real ausfuehrend". The ledger makes that
concrete: every row names one of three owners and one profile.

| target owner | target project | profile | rows |
|---|---|---|---:|
| shared hermetic SQL owner | `tests2/Transports/ViciOne.ServiceBus.SqlTransport.Tests` | `UnitArchitecture` | 30 |
| PostgreSQL integration owner | `tests2/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql.IntegrationTests` | `LocalIntegration` 55 rows, `UnitArchitecture` 10 rows | 65 |
| SQL Server integration owner | `tests2/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer.IntegrationTests` | `LocalIntegration` 36 rows, `UnitArchitecture` 4 rows | 40 |
| **Total** | | | **135** |

### 3.1 The rule that decides the split

An obligation belongs to the **shared hermetic owner** when the behaviour under test is implemented
entirely in `src/ViciOne.ServiceBus/SqlTransport`, is identical for both engines by construction, and
needs no database. That is: address parsing and formatting (`SqlHostAddress`, `SqlEndpointAddress`),
endpoint-name and settings validation (`SqlReceiveEndpointConfiguration.Validate`), and the
test-infrastructure contract itself.

An obligation belongs to a **provider owner** when any one of the following is true, and it then
belongs to that provider only:

1. the type under test lives in the provider project (`PostgresSqlHostSettings`, `NotifyChannel`,
   `SqlServerSqlTransportConnection`, either migrator, either client context);
2. the behaviour is produced by a stored procedure or function that each migrator writes separately —
   which is every fetch, send, publish, lock, unlock, move, purge, dead-letter and maintenance
   behaviour, because none of that logic exists in C# at all;
3. the two engines observably differ (section 4 lists seven such differences).

`UnitArchitecture` inside a provider owner is not a contradiction and is used deliberately for the
14 rows that touch a provider type but open no connection (`NotifyChannel`, connection-string and
Data-Source formatting, multi-host parsing). They must sit with their provider because the constant
they encode is that engine's, but they must not cost a container.

### 3.2 Nothing here is `External`

Requested check: **no row carries the `External` profile, and none should.** Both engines run as
short-lived local containers that the canonical fixture already pins by digest in
`build/test-infrastructure/compose.yaml` (`postgres:16@sha256:9520...74b0b`,
`mcr.microsoft.com/mssql/server:2025-CU8-ubuntu-24.04@sha256:4bab...12e1`). There is no
cloud-only or non-emulatable behaviour anywhere in this cohort. Two connection-string cases mention
`messaging.postgres.database.azure.com` and `(LocalDb)`, but both are pure string parsing and never
connect, so neither implies an external resource.

### 3.3 Infrastructure required, per owner

Named per obligation in the `notes` field of every ledger row; summarised:

**PostgreSQL owner** — engine PostgreSQL 16, image `postgres:16` pinned by digest. Extensions: none;
`gen_random_uuid()` has been core since PostgreSQL 13 and `plpgsql` is installed by default, so no
`pgcrypto` and no `CREATE EXTENSION` is needed. Permissions: the run account must be able to
`CREATE DATABASE`, `CREATE SCHEMA`, `CREATE ROLE`, `CREATE USER`, `GRANT`, `ALTER SCHEMA ... OWNER TO`
and `ALTER DEFAULT PRIVILEGES` — the migrator's `GrantAccess` does all of them. Engine features the
transport depends on: `LISTEN`/`NOTIFY` with a row trigger, `FOR UPDATE ... SKIP LOCKED`, `jsonb`,
`UNLOGGED` tables, and the POSIX regex operator `~`.

**SQL Server owner** — engine SQL Server 2025 CU8 on Linux, image `mcr.microsoft.com/mssql/server`
pinned by digest. Extensions: none. Permissions: the run account must be able to `CREATE DATABASE`,
`CREATE LOGIN`, `CREATE SCHEMA`, `CREATE ROLE`, `CREATE USER`, `EXEC sp_addrolemember` and
`EXEC sp_getapplock` — in practice `sa` or an equivalent sysadmin, which is what the migrator's
`GrantAccess` assumes. Engine features: `MERGE`, `OUTPUT ... INTO`, table hints
`(ROWLOCK, READPAST, UPDLOCK)`, `CREATE OR ALTER`, sequences, and `LIKE`.

**Shared hermetic owner** — no external service of any kind.

## 4. Resolution of the 0-of-17 and 0-of-14 pairing result

The static pairing run in `R0/FIND_UNTESTED_SOURCES.md` reports **0 of 17** paired source files for
`SqlTransport.PostgreSql` and **0 of 14** for `SqlTransport.SqlServer`. Answered from the real code:

**Both provider projects are tested. Neither is untested. The pairing score is an artefact of the
heuristic's file-name matching.**

Measurement. For each of the 31 provider `.cs` files, the number of occurrences of its type name in
the 37 test files:

| provider type | direct references in the test project |
|---|---:|
| `PostgresSqlTransportConnection` | 12 |
| `SqlServerSqlTransportConnection` | 9 |
| `NotifyChannel` | 3 |
| the other 28 types | 0 |

Three of thirty-one types are named directly, and none of the 31 file names has a matching
`*_Specs.cs` counterpart, which is exactly the condition the heuristic scores as unpaired. The
remaining 28 are reached indirectly, and the entry points are counted the same way:

| entry point | call sites in the test project | what it drags in |
|---|---:|---|
| `UsingPostgres(...)` (extension method, 4 overloads) | 21 | `PostgresBusFactoryConfiguratorExtensions` -> `SqlRegistrationBusFactory` -> `UsePostgres` -> `PostgresSqlHostConfigurator` -> `PostgresSqlHostSettings` |
| `UsingSqlServer(...)` | 8 | `SqlServerBusFactoryConfiguratorExtensions` -> `SqlServerSqlHostConfigurator` -> `SqlServerSqlHostSettings` |
| `UsePostgres` / `UseSqlServer` | 1 / 1 | `PostgresHostConfigurationExtensions` / `SqlServerHostConfigurationExtensions` |
| `AddPostgresMigrationHostedService` | 2 | DI registration `ISqlTransportDatabaseMigrator -> PostgresDatabaseMigrator`, executed by `SqlTransportMigrationHostedService` |
| `AddSqlServerMigrationHostedService` | 1 | DI registration -> `SqlServerDatabaseMigrator` |
| `SqlTransportOptions` | 35 | both `CreateBuilder` implementations |

The chain that the heuristic cannot see, and that carries the bulk of the provider code, is:

`PostgresSqlHostSettings.CreateConnectionContextFactory` -> `PostgresConnectionContextFactory`
-> `PostgresDbConnectionContext` (plus its `NotificationAgent` and `MaintenanceAgent`)
-> `PostgresClientContext` -> `SqlStatements` -> `JsonParameter` / `EnumParameter` / `UriTypeHandler`.

Every link is a virtual override or a factory return value; not one is a named construction in a test.
The SQL Server chain is identical in shape. `PostgresDatabaseMigrator` and `SqlServerDatabaseMigrator`
are reached only through a DI registration and a hosted service, which is the second blind spot the
report names.

**Which provider files are genuinely unexercised** (read, not guessed): none. The two
`ISqlServerSqlHostConfigurator` / `IPostgresSqlHostConfigurator` marker interfaces and the two
`UriTypeHandler` classes are exercised only as base types and Dapper type handlers, so a rewrite could
lose them without any inherited test failing — they are covered by the gap obligations rather than by
inherited ones. `SqlServerSqlTransportOptionsExtensions.FormatDataSource` is exercised, but only
transitively through `CreateBuilder`; the `Should_include_the_port` case asserts its output.

Conclusion for the Lead: the 0 % score for these two projects establishes nothing about coverage and
must not be read as "no tests exist". It does correctly identify that no test file is *named after* a
provider file, which the rebuild should fix by giving each provider owner test files named for the
behaviour rather than for the spec suffix.

## 5. Open questions for the Lead

- **Q-1 — sub-second `LockDuration` is accepted by the configurator and rejected by both engines, at
  different points.** `SqlReceiveEndpointConfiguration.Validate` does not check `LockDuration`.
  PostgreSQL then fails at renewal time (`renew_message_lock` raises `Invalid lock duration` below one
  second); SQL Server fails already at fetch time, because `SqlServerClientContext.ReceiveMessages`
  passes `(int)lockDuration.TotalSeconds`, which is `0`, and `FetchMessages` throws. Should the
  configurator refuse it, or should the rebuild pin the two different failures as the contract?
  Ledger row `OBL-R0-SQL-0113`, disposition `QUESTION`.

- **Q-2 — re-declaring a queue without `MaxDeliveryCount` resets a previously configured limit to 10,
  identically on both engines.** Two buses sharing a queue name is normal, so the second one silently
  widens the first one's limit. Intended, or a defect to fix before the rebuild pins it? Ledger row
  `OBL-R0-SQL-0116`, disposition `QUESTION`. Note the neighbouring `auto_delete` column behaves
  *differently* between the engines in the same statement (finding P-5), which suggests neither is
  deliberate.

- **Q-3 — three inherited SQL Server cases start a harness against a host that does not exist in the
  run.** `SqlServer.InstanceName_Specs.Should_include_the_instance_name_and_start` and
  `..._and_port_and_start` call `StartTestHarness()` with `Host = "localhost\instance"` (and port
  3381), which no fixture publishes; the third, `SqlServer.Configuring_a_receive_endpoint_without_
  topology.Should_create_the_queue`, raises `ViciOneServiceBusHostOptions.StartTimeout` to ten seconds
  for what looks like the same reason. Either bus start does not establish transport readiness — in
  which case the `_and_start` suffix measures nothing and the cases are hermetic address tests — or a
  start failure is being tolerated silently. I could not settle this statically and did not start a
  container. The disposition of those three rows depends on the answer.

- **Q-4 — `requeue_message(s)` / `RequeueMessage(s)` and the `queues` / `subscriptions` views have no
  caller anywhere in the repository** (verified by grep over `src`, `tools`, `tests`, `benchmarks`).
  Every provisioning run creates them. Are they an operator-facing surface that must keep working and
  therefore needs obligations of its own, or dead schema whose removal should be recorded? Ledger row
  `OBL-R0-SQL-0124`, disposition `QUESTION`. The answer matters because on PostgreSQL the requeue
  procedures are inert even if called — see finding P-7.

- **Q-5 — the test project carries its own `docker-compose.yml`** with `mcr.microsoft.com/azure-sql-edge`
  and an untagged `postgres`, fixed ports 1433 and 5432 and the literal password `Password12!`. It is
  superseded by the digest-pinned canonical fixture and is referenced by nothing. Confirming it is dead
  is a Lead call, not mine; see finding C-3.
