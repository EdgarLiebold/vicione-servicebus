# Reliable-messaging database schema deployment

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

Generate an ordinary EF Core migration in the application project for its selected database
provider. Inspect the generated schema and apply that migration before starting any writer. Use a
stable bus persistence identity and keep it unchanged for the lifetime of retained records.

The repository does not ship a pre-generated provider migration: the application owns its
`DbContext`, provider, optional schema name, and migration history. Service registration does not
create or update tables. Deploy later schema changes through the application's normal EF Core
migration and backup process, with writers stopped when a change requires it.

For a first deployment, apply the application's initial schema migration to an empty database.
This repository provides no MassTransit database import. Subsequent schema upgrades remain
application-owned EF Core migrations.
