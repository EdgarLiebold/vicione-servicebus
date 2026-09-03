# V5 durable sender review disposition

Date: 2026-09-03

| Review obligation | Disposition |
|---|---|
| Stable durable-send public SPI | Integrated into Abstractions with bounded immutable contracts |
| Restart-safe admission boundary | Integrated through `IDurableSendStore<TBus>`; sender returns after store completion |
| Unknown stable contract identity | V5.1 RT-002 integrated before any store call |
| Exact idempotence and loud id conflict | Integrated and tested in both in-memory and EF owners |
| Atomic retained count plus logical content bytes | Integrated; V5.1 RT-004 terminology and independent concurrent EF predicates proved |
| Claim/lease/retry/quarantine/operator lifecycle | Integrated with deterministic `TimeProvider` ownership and hard bounds |
| Consumer-completion races and generation fencing | Integrated for early, late, timeout, ambiguous failure, discard/re-admission, and stale capability |
| Transport failure taxonomy | Integrated using the existing V4 provider-classifier SPI; unknown remains fail-forward quarantine |
| InMemory completion capability | Integrated only as nonserializable process-local context after logical receive completion |
| EF persistent store | Integrated with serializable ledger, compare-and-set lease, generation, server aggregates, and bounded model |
| EF commit-durability preflight | V5.1 RT-003 integrated fail closed before the first query; host validator remains replaceable |
| Health and telemetry | Integrated as bounded, low-cardinality, payload-free observers |
| Donor regression project | Not copied; replaced by native source-owner xUnit 4/MTP v2 tests and passive requirement projection |
| Real SQL Server/PostgreSQL crash and HA proof | Correctly retained as release-environment evidence; not falsely claimed by this local package |
| Provider-specific broker durable-acceptance adapters | Not part of package 2's InMemory/EF boundary; later V5 reconciliation retains this separate obligation |
| Bounded body writer, diagnostics/analyzers, final API review | Assigned to the remaining V5 packages; not conflated with Durable Sender storage semantics |

No legacy compatibility alias, second catalog owner, second test verdict path, CLR-type persistence, serialized completion
handle, payload-bearing quarantine evidence, or guessed provider durability was introduced.
