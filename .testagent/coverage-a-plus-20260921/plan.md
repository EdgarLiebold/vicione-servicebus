# ServiceBus A+ coverage campaign — plan

## Acceptance mapping

| Requirement | Planned evidence |
| --- | --- |
| `A+ für Line Coverage, Branch Coverage und CRAP` | Fresh 32-assembly, 29-report Unit/Architecture + local-provider aggregate after every completed phase; eliminate or explicitly dispose every CRAP > 30 method and retain the honest Cobertura branch interval. |
| `nur hochwertige Tests, die echtes Produktverhalten, Fehlerfälle, Grenzen und Regressionen hart prüfen` | Each added test must name and assert an observable contract plus a negative, boundary, failure, or regression discriminator. Pseudo-mutation review must show which product mutations the assertions kill. |
| Microsoft skills are mandatory | This campaign uses `code-testing-agent` for this plan, `find-untested-sources` for the static pairing inventory, `coverage-analysis` for ranked CRAP, `run-tests` for MTP commands, and performs `test-gap-analysis` plus `assertion-quality` before completion. |
| adversarial red-team reviews | Read-only review after each coherent phase, followed by a green narrow run and a final product-wide rerun on the exact measured bytes. |

## Sequential implementation phases

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
   are complete. Receive-endpoint validation and the SQL Server transient-error taxonomy are also
   below the CRAP gate with exact boundary contracts. Continue with receiver-loop behavior and
   provider-specific PostgreSQL runtime and SQL Server migration hotspots.
4. **Amazon SQS/SNS.** Cover subscription reconciliation, topology diagnostics,
   failure classification, receive validation, and persisted transport metadata.
5. **Azure Service Bus and Event Hubs.** Cover exception classification,
   context property projections, entity validation/equality, producer failure
   and routing semantics using deterministic fakes or existing emulators.
6. **Core, Abstractions, Job Service, and remaining packages.** Work through the
   remaining ranked CRAP list by observable product contract. Skip trivial code
   only when a written risk disposition shows that a test would have no product
   value; a skipped method may not leave the requested CRAP gate red.
7. **Final gates.** Run assertion-quality and verified pseudo-mutations, full
   Unit/Architecture and all 13 provider projects, rebuild the 29-report
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
