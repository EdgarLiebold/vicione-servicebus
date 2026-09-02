# V4 typed Entity Framework bus-outbox reliability validation

## Bound inputs

- Integration baseline commit: `01fb3d52453df99cd8c6784e781ca80ceabd2a2b`
- Integration baseline tree: `0f0e7997efa89e54fcfcbe819d30a1d9ad92b7f3`
- Protected review aggregate SHA-256:
  `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`
- V4 bundle SHA-256:
  `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`
- V4 bundle head: `f8050928715e536b60c42d800d1cbb81c085818f`
- Semantic donor commit: `a621bf20872f28bf28f22170adba7a14b7af15a1`
- The PO-owned `review/**` tree remained unchanged and untracked.

## Integrated behavior

The Entity Framework bus outbox is now typed by both bus and `DbContext`. Each registration owns a
stable, bounded bus key, an independent notification channel and a scoped transactional session.
Multiple `DbContext` registrations fail closed unless exactly one is the explicit default; typed
operations can list, requeue and discard only the quarantine records owned by their bus.

Runtime settings are validated and frozen when registration completes. Commit persists ordinary
business changes even when no message was staged, signals only after durable persistence, and rolls a
completed batch to a fresh outbox identity. Abort detaches uncommitted outbox state and refuses to
pretend that an already persisted session was rolled back.

Delivery now has explicit pending, retry-scheduled, quarantined and delivered states. Failure
classification is extensible but isolates a defective classifier. Unknown failures remain explicitly
unclassified, permanent and invariant failures are terminally quarantined, retry attempts and
exponential delay are bounded, corrupt persisted metadata cannot hot-loop, and administrative requeue
resets every failure field before it signals delivery.

Provider SQL selects only the current bus, orders by creation time plus outbox identity, and uses
provider-native ownership semantics. PostgreSQL outbox work uses `ReadCommitted`: the prior
`RepeatableRead` plus `FOR UPDATE SKIP LOCKED` combination produced real serialization failures under
concurrent delivery. Explicit application overrides made after provider selection remain authoritative.

## Defects reproduced and corrected

The real 12-producer by 12-message PostgreSQL carrier initially stopped at 70 and then 120 of 144
deliveries. Durable timeout diagnostics exposed two independent state-machine defects and one provider
isolation defect:

1. PostgreSQL `RepeatableRead` caused `could not serialize access due to concurrent update` under the
   skip-locked worker pattern.
2. `ResetFailure` changed a successfully delivered state back to pending, allowing empty earliest rows
   to monopolize the ordered selector.
3. Completing an empty final delivery window persisted `Delivered` but reported zero progress, so the
   agent could sleep while later work remained.

The fixed carrier delivers all 144 messages exactly once. Its timeout failure now reports recorded
count, remaining messages and every persisted state/status/lock/attempt field instead of returning a
low-information timeout.

## Executing evidence

- Analyzer-active Release build of `ViciOne.ServiceBus.Tests.Unit.slnx`: 0 warnings, 0 errors.
- Canonical serialized Unit/Architecture profile: 3,072/3,072 passed, 0 failed, 0 skipped.
- Complete EF unit assembly: 102/102 passed, 0 failed, 0 skipped.
- Focused reliability state owner: 12/12 passed.
- Complete Abstractions owner after the request-rate concurrency hardening: 246/246 passed.
- General LocalIntegration profile: 338/338 passed, run `vicione-37cc138f1b59`.
- SQL Server LocalIntegration profile: 60/60 passed, run `vicione-becdf9431940`.
- Azure Service Bus emulator profile: 24/24 passed, run `vicione-01e70c96b08d`.
- RabbitMQ LocalIntegration profile: 17/17 passed, run `vicione-956e05d2e332`.
- The focused real PostgreSQL retry-due-time test and the 144-message concurrent delivery test pass.
- Every requirements JSON file parses, its compiled projection passes, and `git diff --check` passes.

Two noncanonical fully parallel Unit solution diagnostics exposed unrelated timing sensitivity:
ActiveMQ listener publication failed once and then passed three isolated repetitions; diagnostics and
Quartz timeout carriers failed once and passed immediately in isolation. The existing in-memory job
identity carrier could race its 30-second job-slot wait against its own 30-second test budget, so that
test now configures a one-second slot wait without changing its identity assertions and passes three
consecutive focused runs. The repository's canonical `--max-parallel-test-modules 1` command then passed
all 3,072 cases. The changed EF assembly was green in every complete run; the diagnostic failures are
recorded rather than hidden or misattributed to the EF package.

The macOS workspace sandbox denies .NET/MSBuild/MTP IPC. Authoritative builds and tests therefore use
the established external execution profile with isolated `DOTNET_CLI_HOME`, explicit `DOTNET_ROOT`,
disabled multilevel lookup and node reuse, and the existing package cache. This is an execution
environment boundary, not a product workaround. `--disable-build-servers` is intentionally limited to
build commands because forwarding it through `dotnet test --solution` prevents MTP discovery.

## Assertion and mutation review

The permanent tests assert exact status transitions, persisted retry times and attempts, exception and
cancellation identity, bus and `DbContext` ownership, deterministic order, bounded page size, complete
failure reset, durable-before-signal order, exact provider SQL and real concurrent cardinality. Fake time
owns retry scheduling; no success verdict depends on sleep, polling, a quiet window or a timeout.

One isolation mutation initially survived because the foreign-bus record used a random GUID and could
fall behind the page limit. The test now gives that record the deterministic smallest key, so removing
the bus predicate is causally red. Deleting the secondary order happened to preserve SQLite's primary-key
order; the accepted mutation reverses the explicit tie-break and proves the contractual direction. An
ambiguous text replacement during an early progress mutation changed the wrong return site, failed to
compile and was rejected rather than counted.

Seventeen independent, buildable one-cause mutations were killed. Every product target was restored
byte-exactly before the final positive runs.

| ID | Single changed cause | Causal native owner and result |
| --- | --- | --- |
| M01 | Read mutable delivery limits after registration | frozen-registration owner observed the post-registration value |
| M02 | Restore PostgreSQL `RepeatableRead` | provider owner rejected the exact isolation level |
| M03 | Let failure reset overwrite delivered status | empty-final-window owner observed pending instead of delivered |
| M04 | Classify an unknown exception as transient | built-in classification owner rejected the guessed kind |
| M05 | Delay terminal quarantine by one attempt | retry-budget owner observed retry-scheduled instead of quarantined |
| M06 | Increment persisted attempts by two | unknown-failure owner observed attempt two instead of one |
| M07 | Halve every retry delay | exponential-delay rows rejected the exact duration |
| M08 | Propagate a classifier defect | classifier-isolation owner received the exact injected exception |
| M09 | Collapse every typed bus identity to `shared` | identity owner rejected equal bus keys |
| M10 | Remove the bus predicate from quarantine listing | deterministic foreign record entered the bounded page |
| M11 | Retain `LastFailure` during requeue | requeue owner rejected the persisted stale reason |
| M12 | Reverse the quarantine identity tie-break | ordered-page owner observed the exact reversed order |
| M13 | Remove PostgreSQL's outbox-id SQL tie-break | exact SQL owner rejected the provider statement |
| M14 | Skip commit when no outbox state is staged | business-only commit owner found no persisted row |
| M15 | Select the non-default registration | both registration-order rows rejected the selected context |
| M16 | Label corrupt metadata as a transport failure | corrupt-metadata owner rejected `Permanent` |
| M17 | Label a missing destination as a transport failure | missing-destination owner rejected `Permanent` |

Baseline SHA-256 values after restoration:

- configurator: `1f99da130373768601a54dae8d7a8b3431bc47e648fdd70e816c804a4e6d359d`
- configuration extensions: `f56c7e1852242b92facf144ab3d60c6e92c92ba2f3a5e2b60d28dd97a24c9245`
- delivery service: `db4741da5aff3ade58106644e45daf6af0931292cf0ade041a757ec90c6e9cf5`
- bus identity: `f215d19425fa93c47ed56cb882c48161007c6e50ca1d0ffc5b3ea3a0316e0a4b`
- operations: `1f3c95f7653384a9a63df59c042c3e9efa5b466dc229ad164e6a07dae243000f`
- scoped context: `cd35102fc38f5a4b40971d21e9f81c03e4e941895eb1de0ce9d2004f3bb379c5`
- scoped provider: `637b9ff72af9954c89fa330d88ae7918e23a5ff3370858589ccd7ad3ccf2fe19`
- PostgreSQL formatter: `e4e1678b76e93c31f81264827c0742c6e0d6c487b77c04be96243e2b29240e22`

This local package is the fifth of twelve semantic V4 reviewer packages. It raises V4 integration
progress to 5/12 (41.7%). Remote publication is not included or implied.
