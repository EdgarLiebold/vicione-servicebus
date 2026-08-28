# Transactional bus capability split — validation

## Frozen subject

- Technical commit: `1e8d0b4c6ad2e4b10cf90f8cfef9c3d16fc70407`
- Technical tree: `66344039fd960905a1e1e69f7cd7dd883c503289`
- Technical parent: `afb8edd119e83834c6838ed151eec3a345ef7d74`
- The compressed `TECHNICAL_DIFF.patch.gz` is the byte-level parent-to-technical delta.

## Product result

The ambiguous `ITransactionalBus` contract is retired without a compatibility shim. Its two unrelated
behaviors now have different names, lifetimes and ownership rules:

- `IAmbientTransactionBus` follows `Transaction.Current`. It dispatches immediately outside an ambient
  transaction, enlists pending actions while one exists, executes the captured FIFO during preparation,
  discards on rollback/in-doubt, honors caller cancellation before enlistment and thereafter uses the
  transaction lifetime. It is explicitly best-effort and is not a durable atomic outbox.
- `IBufferedBus` is scoped and dispatches only through `FlushAsync`. Each flush owns one FIFO snapshot;
  cancellation before an action restores the complete unattempted tail, dispatch failure preserves the
  original exception and restores only the later tail, actions added during a flush remain behind that
  restored tail, and concurrent flushes are single-drain.
- Publish and send share the same deferral boundary for every overload. The concrete publish endpoint
  bypasses the deferred endpoint provider internally, preventing double buffering.
- Consumer scopes route both publish and send through the selected capability, for bus-specific and
  global consume contexts.
- Entity Framework Bus Outbox and either lightweight capability are rejected in both registration
  orders because they cannot both own `IScopedBusContextProvider<IBus>` truthfully.
- The three inherited NUnit transaction fixtures are removed after their eleven R0 obligations are
  bound to executing native carriers. No empty legacy test directory remains.

## Positive execution

All commands below executed at the frozen technical bytes. Their complete arguments and raw paths are in
`FINAL_RESULTS.json`.

| Boundary | Result |
|---|---:|
| Engineering locked restore | exit 0 |
| Engineering Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered Unit/Architecture profile | 2267/2267 passed; 0 failed; 0 skipped; 19 assemblies |
| Focused transaction namespace | 35/35 passed; 0 failed; 0 skipped |
| Focused EF ownership orders | 4/4 passed; 0 failed; 0 skipped |
| Unfiltered LocalIntegration profile | 244/244 passed; 0 failed; 0 skipped; 7 assemblies |

The LocalIntegration run used run identity `vicione-68ff634d8cda` and freshly allocated loopback endpoints
for PostgreSQL, Azurite, LocalStack, ActiveMQ Classic and Artemis. `positive/local-fixture/fixture-findings.json`
contains an empty findings array and binds all five broker-log digests. The copied fixture evidence omits
the run-root ownership token; no credentials are stored in this evidence package.
Textual MSBuild, MTP-wrapper and broker logs are stored as deterministic `gzip -n -9` streams; decompression
recreates the exact raw bytes used by the verifier.

## Requirements and floors

- `.testagent/transactional-bus-obligation-map.tsv`: exactly eleven unique obligations
  `OBL-R0-CORE-C-0430..0440`, all `REPLACED_EXECUTING` in `UnitArchitecture`.
- `CoreRequirements.json`: 35 unique transaction requirement variants and 35 source-mirrored native Facts.
- `EntityFrameworkRequirements.json`: one Theory carrier with four predeclared registration-order cases.
- `EntityFrameworkLocalIntegrationRequirements.json`: three executing PostgreSQL-backed deferred-bus variants.
- The Unit/Architecture fail-closed floor is 2267 in the workflow, architecture gate, README, build guide
  and active plan. The LocalIntegration floor remains 244.

## Mutation closure

`MUTATION_MANIFEST.json` contains 17 exact replacement recipes. Each recipe binds target, baseline SHA-256,
occurrence count/index, replacement bytes, mutant SHA-256, build project, exact owner, raw result and
post-restore SHA-256. All 17 mutants:

1. compiled in Release with exit 0 and a non-empty binlog;
2. failed only after entering their designated native test owner (test exit 2);
3. reported zero skipped/pending/other tests;
4. restored to the exact technical SHA-256 before the next mutation.

M15 intentionally runs four predeclared Theory cases: two fail when capability-first registration would
otherwise escape the product guard, while the two outbox-first cases remain green because their independent
guard is still present. The mutation run is therefore causal rather than an all-cases sabotage.

Two calibration attempts are not part of the 17 claims and have no retained result artifact: an initial
compile-invalid accessibility change was discarded, and the FIFO mutation was first pointed at an owner that
did not exercise three actions. M07 is the unchanged, buildable FIFO mutation rerun against the actual
three-action failure/tail owner; `mutations/M07-results/M07.json` is only that final red run.

## Structured compilation diagnosis

The product did not require a workaround. Solution-level MTP orchestration inside the filesystem sandbox
failed before discovery while creating its named pipe with `SocketException (13): Permission denied`.
The byte-identical command and isolated CLI environment passed outside the sandbox. Engineering build and
MTP solution runs are therefore executed outside the sandbox with an isolated `DOTNET_CLI_HOME` and MSBuild
node reuse disabled. No product behavior, assertion, floor or test was weakened to accommodate infrastructure.

## Verdict

The technical candidate and its executing native replacement closure are A+ at this freeze. Independent
read-only code and test/evidence reviews remain separate acceptance gates and are not asserted by this file.
