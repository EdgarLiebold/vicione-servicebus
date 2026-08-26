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

## Second frozen subject: saga persistence semantics

- Technical commit: `f1096d531e00e64f2683d4e41542630df2af4550`
- Technical tree: `89ee8622c1bae270af661c3d098d4b98aaa7b17b`
- Technical parent: `700021c7225b640ff4f54ffaed17e402d5ebd383`

The second subject adds source-mirrored PostgreSQL owners for correlated saga lifecycle, exact
pessimistic and optimistic transaction isolation, transactionless operation, read-only state-machine
events, same-correlation row-lock serialization and required two-level navigation graphs. It also
corrects `SetOptimisticConcurrency()` to use the same isolation-policy owner as the `ConcurrencyMode`
property. No inherited EF test file is removed before the complete 90-obligation assignment reaches
one atomic terminal disposition.

The complete 42-project locked Engineering restore completed with exit code 0. The complete
Engineering Release build completed with zero warnings and zero errors. The unfiltered native
UnitArchitecture solution passed 1,868/1,868 with zero failure and zero skip. The unfiltered native
LocalIntegration solution passed 35/35 against fresh run-scoped PostgreSQL and Azurite fixtures
(`vicione-d545bbb0be4f`), also with zero failure and zero skip. The Entity Framework subsets passed
47/47 UnitArchitecture and 24/24 LocalIntegration.

M04 through M07 in `MUTATION_MANIFEST.md` independently prove the shared optimistic default,
read-only persistence boundary, pessimistic navigation query and PostgreSQL `FOR UPDATE` ownership.
Each isolated mutant compiled, its one named owner failed for the stated cause with MTP exit code 2,
and the detached worktree restored byte-identically to the second frozen subject.

## Third frozen subject: transactional outbox reliability

- Final technical commit: `b95faaeff0be853baa4747df1366f15049d3ec0b`
- Final technical tree: `9598ce52248094920b1c59c646b50bb8f37d7288`
- Technical range: `9e755a6a..b95faaef`
- Component commits: `debec2dbb4e0aa582a893422678e2708860c3049` and
  `b95faaeff0be853baa4747df1366f15049d3ec0b`

The third subject replaces `OBL-R0-PER-0018..0032` with fifteen source-mirrored native
LocalIntegration facts and adds one separately identified rollback-hardening fact. The exact
one-to-one inherited disposition and the independent hardening entry are bound in
`OUTBOX_RELIABILITY_DISPOSITIONS.json`; no theory row carries multiple inherited obligations.

The new owners prove exact consumer/application scope identity, typed database constraint faults and
endpoint recovery, original request-fault identity, one terminal fault after retries, exact saga
OpenTelemetry and scoped endpoint proxies, deterministic delayed response identity, EF-to-Quartz
commit ordering, complete consumer and saga rollback semantics, recovery from a real serialized
transport-send failure, and routing-key persistence. The former `VSB-Fail-Delivery` product hook is
removed; failure injection is exclusively test-owned and uses the normal observer boundary.

The locked Engineering restore completed successfully for the complete graph. The final Engineering
Release build completed with exit code 0, zero warnings and zero errors. The unfiltered serial
UnitArchitecture profile completed with 1,868/1,868 passed, zero failed and zero skipped. The
unfiltered LocalIntegration profile completed with 51/51 passed, zero failed and zero skipped against
fresh run-scoped PostgreSQL and Azurite fixtures (`vicione-e96131c6c3d1`). The focused final
EF-to-Quartz owner also passed 1/1 after its observer was made complete for both publish and send
forms (`vicione-03350e6b178f`).

M08 through M13 independently prove transport-property restoration, the EF-before-Quartz commit
boundary, a real send-failure retry, delayed request identity, originating scope identity and
first-attempt rollback. Every mutant compiled and its one named owner failed for the exact stated
reason. All six target files then matched their frozen baseline hashes, the detached worktree was
clean and it was removed normally.

This is a phase disposition, not permission to delete the inherited EF project. Its physical files
remain untouched until all 90 assigned EF obligations have terminal dispositions and the project can
be retired atomically.
