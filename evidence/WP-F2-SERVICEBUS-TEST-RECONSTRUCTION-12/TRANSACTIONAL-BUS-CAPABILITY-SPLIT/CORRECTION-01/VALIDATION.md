# Transactional bus capability split — correction validation

## Frozen subject

- Technical commit: `3c1bfbafeb326047a86a2ec6573f6f516e2bd230`
- Technical tree: `6a0eb15d26288f269feb73ee35dc5ff44d654970`
- Technical parent: `477c600e84104f25936761dbb67f85fa42421c41`
- Architecture correction scope: `17a0791b563a99e81c3f589a5d63214e59330d99`, tree
  `151861503f841e55e2dfd1f4a93dc3c95e879161`
- `TECHNICAL_CORRECTION.patch.gz` is the byte-level parent-to-technical delta.

## Closed review findings

The correction closes all three independent findings against the earlier freeze:

1. `BufferedBus.FlushAsync` rejects a recursive flush from an action currently drained by the same
   instance before waiting on the serialization semaphore. The operation-local frame remains active in
   inherited execution contexts only while that action is active; a captured child can flush the retained
   tail after the action ends. External concurrent flushes keep the original serialized behavior.
2. Generic MultiBus registrations reject Ambient+Buffered ownership in both orders. The reflected generic
   registration boundary rethrows the original `ConfigurationException` with preserved identity instead of
   leaking `TargetInvocationException`.
3. The public-surface carrier closes the exact nine-type implementation set and verifies every concrete
   transaction adapter remains internal with its intended sealed/abstract shape.

The requirements add three variants. Their xUnit execution expands to four new cases because the typed-bus
mixed-capability carrier is a two-row Theory. Therefore the repository floor rises from 2267 to 2271.

## Positive execution

All commands in `FINAL_RESULTS.json` executed at the frozen technical bytes:

| Boundary | Result |
|---|---:|
| Engineering locked restore | exit 0 |
| Engineering Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered Unit/Architecture | 2271/2271; 19 assemblies; 0 failed/skipped |
| Focused transaction namespace | 39/39; 0 failed/skipped |
| Unfiltered LocalIntegration | 244/244; 7 assemblies; 0 failed/skipped |

LocalIntegration used fresh run-scoped PostgreSQL, Azurite, LocalStack, ActiveMQ Classic and Artemis
resources under `vicione-ff320fbbc2ff`. The package binds dynamic loopback endpoints, five broker logs,
two ActiveMQ outage/restore cycles and an empty fixture-findings receipt. It excludes the ownership token
and generated credentials.

## Mutation closure

The immutable parent evidence already binds M01-M17. This correction adds six buildable, byte-exact
mutants M18-M23:

- M18 removes effective reentrancy detection and deadlocks until the bounded test timeout.
- M19 strands the captured child in an active frame after the owning action has ended.
- M20 and M21 remove both compatibility and scoped-owner guards from one generic registration direction;
  exactly the corresponding Theory row fails while the other remains green.
- M22 makes one concrete adapter public; the exact nine-type public-surface oracle fails.
- M23 removes original-exception projection across reflection; the exact exception-type/identity oracle sees
  `TargetInvocationException` instead of the original `ConfigurationException`.

Every M18-M23 Release build exits 0 with zero warnings and errors. Every focused MTP run exits 2 for the
listed causal assertion, with zero skip/pending/other cases. Each target restores to the exact technical
SHA-256 before the next mutation. `MUTATION_MANIFEST.json`, the unified patches, build binlogs, compressed
raw logs and CTRFs bind each execution independently. Cumulative closure is 23/23 killed mutants.

## Structured compilation diagnosis

The product and tests were not adapted to the filesystem sandbox. Solution-level .NET/MTP orchestration
requires a local IPC endpoint that the sandbox denies before discovery. The byte-identical commands pass
outside the sandbox with isolated `DOTNET_CLI_HOME` and disabled build-server reuse; this is the stable
diagnostic recipe recorded in the repository.

## Verdict

The correction is a local evidence candidate, not self-acceptance. Two independent read-only reviews of
the exact technical and evidence commits remain mandatory before remote publication and architecture
acceptance.
