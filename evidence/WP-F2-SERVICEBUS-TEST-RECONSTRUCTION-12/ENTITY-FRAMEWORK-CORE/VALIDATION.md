# Entity Framework Core persistence validation

## Frozen subject

- Technical commit: `8ea356043316861f0ac983de5fa8470410e2f9ca`
- Technical tree: `3d645d11219a32a05280f70310a72f5dc8f9afaa`
- Technical parent: `d7ea0047b3d9b1dc77ca9f968a22c7a8df7274ab`
- Branch: `test/servicebus-xunit4-mtp2-a-plus-v2`

The technical commit is the stationary subject. All .NET and MSBuild commands were started by the
Lead Architect with SDK `/usr/local/share/dotnet/dotnet` version `10.0.302`, an isolated
`DOTNET_CLI_HOME`, `DOTNET_MULTILEVEL_LOOKUP=0`, `MSBUILDDISABLENODEREUSE=1`, and execution outside
the filesystem sandbox. No Developer AI or reviewer started a .NET process.

## Positive validation

1. `dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode --disable-build-servers`
   completed with exit code 0 for the complete 42-project Engineering graph.
2. `dotnet build ViciOne.ServiceBus.Engineering.slnx -c Release --no-restore
   --disable-build-servers --no-incremental --property:TreatWarningsAsErrors=true
   --verbosity:minimal` completed with exit code 0, zero warnings and zero errors.
3. The unfiltered native xUnit 4 / Microsoft Testing Platform 2 UnitArchitecture solution completed
   with exit code 0: 1,867 total, 1,867 passed, zero failed, zero skipped.
4. The unfiltered LocalIntegration solution ran through the canonical fixture owner against
   run-scoped PostgreSQL and Azurite resources (`vicione-2c378f34ec4c`) and completed with exit code
   0: 28 total, 28 passed, zero failed, zero skipped.
5. The source-owner focused results are 46/46 EF UnitArchitecture, 17/17 EF LocalIntegration and 4/4
   Core outbox notification, all without failure or skip.
6. `git diff --check` passed; every embedded Requirement JSON parsed successfully; active README,
   build documentation, workflow contract, architecture test and implementation plan all bind the
   UnitArchitecture floor 1,867 and LocalIntegration floor 28.

The required profiles were not filtered. The minimum counts were declared before the final runs and
are lower-bound guards, not inferred completeness claims.

## Product result

The retained provider matrix is SQLite, PostgreSQL and SQL Server/Azure SQL. MySQL and Oracle
adapter APIs are removed. Provider selection and configuration are explicit and frozen. EF-model
specific SQL is cached by model identity through a weak owner and quotes model-resolved identifiers.
Saga load/query and transaction policy use one runtime strategy, custom query projection has one
fail-closed owner, and insertion-race recovery accepts only a failure entry for the exact saga
candidate followed by the exact now-existing identity.

Outbox envelope creation is separate from scoped write coordination. Concurrent writes share one
state owner, committed batches roll to a fresh state, public execution strategies own retry
classification, and no English exception-message heuristic remains. Inbox delivery count describes
outer deliveries rather than internal retry attempts. `BusOutboxNotification` uses the injected
`TimeProvider`, retains a signal arriving before waiter registration, and rejects concurrent waiter
ownership.

The local provider tests use generated provider-safe database names from the canonical run identity
and xUnit test identity. No fixed database name, embedded credential, sleep, wall-clock timing
window, random scheduler assumption or skipped result is used.

## Mutation conclusion

The three final defects are independently protected by exact, reproducible one-cause mutations in
`MUTATION_MANIFEST.md`. Every mutant builds and its focused native owner fails for the intended
reason with MTP exit code 2. Restoring the technical commit restores all three product files to the
recorded baseline SHA-256 values.

The broader inherited Activity-listener no-throw normalization is deliberately recorded in
`TODO.md` as one path-complete observability slice. It is not partially patched in EF and is not
claimed as closed by this evidence.
