# ActiveMQ native closure correction — validation

## Frozen subject

- Correction baseline: `72e788b25979adf5d9f0f3d72fbaf54dbc5cce23`.
- Product/test correction: `3057025b5c02c1ce034a9633baf87cb24a24ba92`.
- Frozen technical commit: `c2a0cfeda3466029eddd75033fe2220ffbb9a612`.
- Frozen technical tree: `9b9d032a8bd80ee02f9376ab3fdf443cdfbadeeb`.
- Frozen technical parent: `3057025b5c02c1ce034a9633baf87cb24a24ba92`.
- `TECHNICAL_DIFF.txt` binds exactly 40 paths from the correction baseline to the frozen technical
  commit. This evidence directory contains no product, test, build, package, solution or workflow
  mutation.

## Corrected product contracts

The public DI/options path now requires all three connection coordinates together: host, typed
protocol (`OpenWire` or `Amqp`) and a non-zero explicit port. It does not infer port 61616, and it
cannot project AMQP as an OpenWire URI. Empty options still mean that no options-owned host is
configured. Unit tests bind both protocol projections, both missing values, zero port and the real
Microsoft-DI resolution path.

Connection and session disposal execute every cleanup stage exactly once. A sole failure is rethrown
as the same exception object; multiple failures are aggregated in stable stage order. Auto-delete
cleanup similarly attempts each unique consumer queue, topic and queue in stable order, retains every
failure, and uses the current connection after recovery. Manual destination deletion is installed
only for the OpenWire provider that supports it; AMQP brokers retain their own auto-delete lifetime.

Scheduling is provider-owned before delivery. Apache.NMS.AMQP projects `NMSDeliveryTime` to the AMQP
delivery annotation used by Artemis; ActiveMQ Classic receives `AMQ_SCHEDULED_DELAY` over both
OpenWire and AMQP. The Future-Scheduling carrier first proves an exact broker-side scheduled item,
then exact delivery, then an empty scheduled state. Quartz and shared-subscription carriers retain
receive-completion barriers, stop the buses, and finally bind exact broker enqueue/dequeue/rest-state
statistics. A second provider send is therefore observable after drain rather than escaping behind a
first-delivery continuation.

ActiveMQ LocalIntegration source uses no `Task.Delay` or `Thread.Sleep`. The architecture gate parses
the actual C# syntax trees and fails on invocations, not prose. Broker lifecycle/recovery tests use
runner acknowledgements and exact management state instead of wall-clock polling. Artemis is a
required, pinned local broker gate; the stale contrary image rationale was removed.

## Positive execution

- Locked Engineering restore: exit 0; raw log and binary log bound.
- Engineering Release build: exit 0, 0 warnings, 0 errors; raw log and binary log bound.
- ActiveMQ UnitArchitecture: 122/122, 0 failed, 0 skipped.
- ActiveMQ LocalIntegration: 95/95, 0 failed, 0 skipped, using run identity
  `vicione-45e3ddee6dfc` with ActiveMQ Classic, Artemis and ActiveMQ outage control.
- Unfiltered serial UnitArchitecture: 2185/2185 across 18 CTRF reports, 0 failed, 0 skipped,
  0 pending and 0 other.
- Unfiltered serial LocalIntegration: 244/244 across seven CTRF reports, 0 failed, 0 skipped,
  0 pending and 0 other. Run identity `vicione-19a2b33ee70b` binds PostgreSQL, Azurite, LocalStack,
  ActiveMQ Classic and Artemis, dynamic loopback endpoints, ActiveMQ outage control, broker logs,
  empty fixture findings and guarded teardown.

The full positive commands in `FINAL_RESULTS.json` are the commands that produced these logs and
CTRFs. MTP owns the process verdict and xUnit owns discovery/results. No Python policy validator,
private receipt mechanism, VSTest separator or NUnit runner participates.

## One-cause attacks

`MUTATION_MANIFEST.json` binds baseline bytes, the one exact replacement, mutant bytes and restored
bytes for M18-M29. `MUTATION_EXECUTION.tsv` binds every build/test command, exit code and raw result.
All compiled mutants built with 0 warnings and 0 errors; all twelve behavioral/gate runs exited 2 for
the expected reason, with zero skips.

- M18-M20 independently kill wrong protocol projection, missing protocol and missing/zero port.
- M21-M24 independently kill swallowed connection/session failures, lost sole-exception identity and
  short-circuiting auto-delete cleanup.
- M25 keeps the OpenWire positive control green while the invalid AMQP manual-delete path fails.
- M26 removes only Artemis `NMSDeliveryTime`: the complete 95-case ActiveMQ LocalIntegration cohort
  has exactly one failure, the Artemis provider-state assertion; 94 controls pass.
- M27 removes only the Classic scheduling header: OpenWire and Classic AMQP fail their provider-state
  assertion while the Artemis control passes.
- M28 adds only one second provider send: the terminal Artemis shared-subscription carrier fails its
  duplicate assertion after receive completion and bus stop.
- M29 adds one real `Task.Delay(TimeSpan.Zero)` invocation: the Roslyn architecture gate fails on the
  exact source path.

Each provider mutation has a fresh run-scoped broker fixture with endpoint projection, broker logs
and empty teardown findings. The temporary mutation worktree is restored to the exact technical file
hash after every attack and is clean at the end.

Broker and one console log that contain producer-emitted trailing spaces are stored as deterministic
gzip streams. Decompression yields the unmodified raw bytes; compression avoids rewriting those
foreign-process bytes merely to satisfy a text-diff whitespace convention.

## Closure and scope

The original terminal mapping is unchanged: 113 unique selected R0 obligations cover all 179
historical execution identities with 40 UnitArchitecture and 73 LocalIntegration owners. Its SHA-256
is `05e3a90bd09a8303eb5ca0ec68c06b1251655a348bc230af1bcbe5317dddc66a`.
The already-retired inherited ActiveMQ project remains absent. This correction adds or strengthens
native carriers; it does not revive legacy tests or turn pending work green.

ActiveMQ has no deferred cloud/account boundary. Classic OpenWire, Classic AMQP and Artemis are
locally executable against fresh digest-pinned fixtures. Repository GitHub workflows remain globally
manual/disabled under the PO's temporary decision; their future command shape stays statically
validated and is not claimed as an executed GitHub run.

Final acceptance still requires independent static read-only review of this exact technical/evidence
freeze. Reviewers must not run .NET, MSBuild, Docker or network processes and must not alter bytes.
