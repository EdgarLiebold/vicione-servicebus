# Reliable-messaging database deployment

The Entity Framework Core provider stores the outbox, inbox, recurring schedules, and retained
capacity counters in the application database. Database schema deployment is an application-owned
operation and never occurs as a service-registration side effect.

## New databases

Map the reliable records in the application context:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddViciOneReliableMessaging();
}
```

Generate and apply an ordinary EF Core migration from that model before starting any writer. Use a
stable bus persistence identity and keep it unchanged for the lifetime of the retained records.

## Databases containing earlier outbox tables

Provider-specific scripts are available for:

- [SQLite](reliable-messaging-sqlite.sql)
- [PostgreSQL](reliable-messaging-postgresql.sql)
- [SQL Server](reliable-messaging-sqlserver.sql)

Each script creates the unified outbox, inbox, schedule, and capacity schema and transforms retained
outgoing rows. The scripts intentionally require an explicit store key and a complete mapping from
stored message type values to stable ViciOne message-contract identities.

## Safe execution

1. Stop every process that can write to the old or new tables.
2. Take a database backup and verify that it can be restored.
3. Copy the script for the selected provider into the application's deployment repository.
4. Replace every `__VICIONE_...__` token and add one contract mapping for every distinct retained
   message type.
5. Execute the script with an account allowed to create tables, indexes, and constraints.
6. Verify row counts, retained byte totals, contract identities, destinations, and message IDs.
7. Deploy the application with the same store key and contract catalog.
8. Start one instance, require startup validation and reliable-messaging health to succeed, then
   increase the instance count.

The scripts fail before committing when placeholders remain, a contract mapping is missing, a
destination or message ID is absent, or retained rows belong to another store key. Do not bypass
these guards; correct the source data or mapping first.
