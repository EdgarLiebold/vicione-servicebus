# ServiceBus A+ coverage campaign — plan

## Acceptance mapping

| Requirement | Planned evidence |
| --- | --- |
| `A+ für Line Coverage, Branch Coverage und CRAP` | Fresh 32-assembly, 33-receipt product aggregate (18 product Unit, 13 local provider, no-AVX2 and scalar fallback), plus separate green support-test gates; eliminate or explicitly dispose every CRAP > 30 method and retain the conservative Cobertura branch lower bound. |
| `nur hochwertige Tests, die echtes Produktverhalten, Fehlerfälle, Grenzen und Regressionen hart prüfen` | Each added test must name and assert an observable contract plus a negative, boundary, failure, or regression discriminator. Pseudo-mutation review must show which product mutations the assertions kill. |
| Microsoft skills are mandatory | This campaign uses `code-testing-agent` for this plan, `find-untested-sources` for the static pairing inventory, `coverage-analysis` for ranked CRAP, `run-tests` for MTP commands, and performs `test-gap-analysis` plus `assertion-quality` before completion. |
| adversarial red-team reviews | Read-only review after each coherent phase, followed by a green narrow run and a final product-wide rerun on the exact measured bytes. |

## Current continuation

The phase list below preserves the campaign's initial implementation plan and
its then-current progress labels. It is not the current open-work ledger.
The [current continuation](../plan.md#current-continuation-2026-09-26) and
[status checkpoint](../status.md) supersede those labels. The complete
`98ac8bb78` measurement and the final `2464cdc45` measurement are separate;
only the latter can verify the fourteen subsequently added contract cases.
All final provider receipts, the architecture recheck, identity and aggregate
are complete. The passed final evidence review and publication are recorded in the
current checkpoint; the historical labels below remain unchanged.

## Initial sequential implementation phases (historical)

1. **RabbitMQ transport primitives and topology — complete.** Add exact AMQP header,
   transport-property, move-header, topology-probe, declaration equality/hash,
   send-setting, and persisted send-context contracts. Run the RabbitMQ unit
   project, empirically test selected mutations, then collect its coverage.
2. **ActiveMQ topology and headers — complete.** Exercise primitive-map conversion through actual
   NMS marshalling and real OpenWire/AMQP/Artemis brokers, persisted transport properties, runtime
   destination metadata, diagnostics, and every declaration comparer with field-level negative
   controls.
3. **PostgreSQL and generic SQL topology — in progress.** The declaration identity, complete
   topology diagnostic, publish scan, public input, invalid-contract, namespace-boundary, host
   parsing, address-symbol/area, validation, atomic replacement, and effective Npgsql-target slices
   are complete. Receive-endpoint validation, the SQL Server transient-error taxonomy, and
   receiver-loop shutdown and retention are covered with exact boundary contracts. Continue with
   provider-specific PostgreSQL runtime and SQL Server migration hotspots.
4. **Amazon SQS/SNS — in progress.** Naming and scoped publish topology have hard collision,
   failure, boundary, and routing regressions plus adversarial review. Topology diagnostics,
   declaration identity, broker metadata conflicts, duplicate AWS names across entity subclasses,
   and logical subscription identity are covered. Continue with subscription reconciliation,
   failure classification, receive validation, and persisted transport metadata.
5. **Azure Service Bus and Event Hubs.** Cover exception classification,
   context property projections, entity validation/equality, producer failure
   and routing semantics using deterministic fakes or existing emulators.
6. **Core, Abstractions, Job Service, and remaining packages.** Work through the
   remaining ranked CRAP list by observable product contract. Skip trivial code
   only when a written risk disposition shows that a test would have no product
   value; a skipped method may not leave the requested CRAP gate red.
7. **Final gates.** Run assertion-quality and verified pseudo-mutations, full
   Unit/Architecture and all 13 provider projects, rebuild the 33-receipt product
   aggregate, obtain adversarial read-only review, update changelog/evidence,
   and push only when repository authorization permits.

## Phase-one test oracles

- A complete AMQP delivery exposes exact synthesized and decoded headers while
  excluding null/blank values; direct lookups remain case-insensitive and use
  UTC transport timestamps.
- Persisted transport properties include each explicitly present RabbitMQ
  property, retain zero priority, omit absent values, and return null when empty.
- Topology probing emits one correctly named scope per declaration/binding and
  keeps null broker arguments as key-only diagnostics.
- Name comparers and entity comparers treat reference/null/type/value changes
  distinctly; equal argument maps remain order-independent and hash-compatible.
- Move headers distinguish overwrite, conditional add, removal, UTF-8 decoding,
  null filtering, and unsupported typed retrieval.
- Send settings preserve queue/exchange bindings, delayed/alternate/single-active
  settings, direct-reply-to emptiness, and exact diagnostic strings.
