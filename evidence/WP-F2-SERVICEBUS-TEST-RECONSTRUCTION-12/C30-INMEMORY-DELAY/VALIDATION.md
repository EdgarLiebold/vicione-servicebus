# C30 validation

## Requirement closure

| Requirement | Evidence |
|---|---|
| Preserve every useful inherited behavior | All five R0 identities have an exact terminal replacement in `INHERITED_BEHAVIOR_DISPOSITION.json`. |
| Treat inherited tests as a minimum | Eighteen native cases cover standard and manual time, ordered/equal/subsequent/absolute deadlines, cancellation, disposal, resources, range validation, API shape and real scheduled publish. |
| Fix defects at their owner | Product code removes the invalid comparer, unbounded channel, reader task, timer churn, delayed cancellation cleanup and asynchronous `Advance` ambiguity. |
| Mirror source ownership | Tests live under `tests2/ViciOne.ServiceBus.Tests/InMemoryTransport`, matching `src/ViciOne.ServiceBus/InMemoryTransport`; the integration assurance also binds `Transports/Fabric/MessageQueue`. |
| Prove sensitivity | `MUTATION_VALIDATION.md` records thirteen product attacks and one projection attack. |

## Final results

| Gate | Result |
|---|---|
| Core product Release build | exit 0; 0 warnings; 0 errors |
| Focused C30 native executable | 18 total; 18 passed; 0 failed; 0 skipped |
| Unfiltered Core native executable | 802 total; 802 passed; 0 failed; 0 skipped |
| Unit solution Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture profile | 1406 total; 1406 passed; 0 failed; 0 skipped |
| LocalIntegration Release build/profile | build 0 warnings/errors; 3 total; 3 passed; 0 failed; 0 skipped |
| Remaining inherited Core project after deletion | exit 0; 0 warnings; 0 errors |
| Complete Engineering solution Release build | exit 0; 0 warnings; 0 errors |
| Bounded product and test `dotnet format ... whitespace --verify-no-changes` | exit 0 |
| JSON, hash restoration, no-mutation and diff checks | exit 0 |

The first sandboxed LocalIntegration attempt was not a product failure: local named-pipe creation was
denied by the sandbox. The identical build and test commands completed successfully when granted the
required local IPC permission.

## Final disposition

PASS. C30 is terminal in the current working tree. No commit, tag or push is implied.
