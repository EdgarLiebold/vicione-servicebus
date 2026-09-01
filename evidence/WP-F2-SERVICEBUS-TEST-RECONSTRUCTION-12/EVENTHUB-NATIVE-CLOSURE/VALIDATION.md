# Event Hubs native closure validation

Frozen Technical subject: `b6d7e930337cbc0b4db3a0259246bda524288736`, tree
`f8465a78f404ece92ade53d179992609bf5960b5`, relative to the accepted predecessor
`ce1f03c94a343505c1c97bbbb7eda2700f8182fa`.

The inherited Event Hubs test root is absent. Its 22 paths were removed from that root; the lock file
was deliberately carried into the source-mirrored replacement project. The native replacement owns
22 executable Event Hubs behaviors and one compiled requirement-projection case. The 25 selected R0
obligations close as 22 `REPLACED_EXECUTING` and three honest Azure-service-only `EXTERNAL_PENDING`
items: workload identity/RBAC, service-owned rebalance, and service retention.

The positive frozen runs are:

- Engineering solution Release build: exit 0, 0 warnings, 0 errors; the raw log and 4.7 MiB binlog
  are bound.
- Unit/Architecture: 21 CTRF modules, 2858/2858 passed, zero failed/skipped/pending/other.
- LocalIntegration: 10 CTRF modules, 398/398 passed, zero failed/skipped/pending/other. Run
  `vicione-5b171e100f31` used run-scoped loopback ports and all seven declared providers.
- Event Hubs focused: 23/23 passed against the pinned official Event Hubs emulator 2.2.1 and Azurite.
- CI tooling: 262/262 Python tests passed outside the macOS sandbox. This is the established
  diagnostic solution for the process-tree tests that require `ps`; sandbox PermissionError is not a
  product or test failure.
- Identity tooling: 148/148 passed after the complete Evidence inventory and generated CHANGELIST
  were present. Textual raw logs are bound losslessly as deterministic gzip streams.

Compose readiness is intentionally staged one provider at a time inside one run-scoped Compose
project. After readiness all providers remain active together for the complete test command. M13
reverts only this ordering and makes both fail-closed fixture-state tests red. This avoids the
reproduced SQL Server `EAGAIN` cold-start failure on the bounded local host without weakening the
provider matrix.

M01-M14 cover rider restart/reuse, checkpoint confirmation, receive metadata, constructed-client
options, batch item identity, partition key, both faulted saga activity arities, partition lifecycle
callbacks, bound multi-bus DI, sequential fixture readiness, and the ConsumerConvention immutable
snapshot that was found by the first full-profile CTRF rerun. Every target is restored to its frozen
baseline SHA. M14 is deliberately timing-free: an enumerable acquired before registration must not
observe a convention registered afterwards, while a later snapshot must observe it.

`TECHNICAL_DIFF.patch.gz` is the exact binary Git diff from the accepted predecessor through the
final Technical commit. `SHA256SUMS` binds every evidence file except itself.
