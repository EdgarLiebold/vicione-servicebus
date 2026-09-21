# Prior work closure — 2026-09-21

## Scope closed

- Completed the inherited source-review fixes and their regression tests.
- Preserved the thirteen sibling package boundaries, moved capability-owned
  implementations out of Abstractions, removed the unnecessary SignalR to
  Initializers reference, and updated the package/API documentation.
- Closed the EF saga model-cache and factory-lifetime findings, raw EF outbox
  identity replay, PostgreSQL renewal timing, direct Amazon SQS admission limits,
  mandatory JSON-depth ordering, provider-runner category execution, and the
  RabbitMQ acceptance/topology findings.
- Updated `CHANGELOG.md` with the product defects repaired during complete source
  reading.

## Final gates

| Gate | Result |
|---|---:|
| Release Unit/Architecture build | 0 warnings, 0 errors |
| Complete Unit/Architecture profile | 9,769 passed, 0 failed, 0 skipped |
| Six-fixture local provider matrix | 402 passed, 0 failed, 0 skipped |
| Standalone SQL Server profile | 69 passed, 0 failed, 0 skipped |
| Final RabbitMQ Unit | 324 passed, 0 failed, 0 skipped |
| Final real RabbitMQ profile | 31 passed, 0 failed, 0 skipped |
| Developer Journeys | 18 passed |
| Isolated package consumers | 4 passed |
| Runtime package API comparison | 30 assemblies matched |

Fixture runs `vicione-285cc34ba452`, `vicione-0a7a93d9e1d6`, and
`vicione-5e9fd08c2ca0` each recorded an empty findings collection.

## Bounded RabbitMQ guarantee

Durable acceptance uses an existing durable quorum queue, a persistent mandatory
publish, publisher confirmation, and matching queue declarations immediately
before and after the publish. Queue proof does not create a binding. AMQP 0-9-1
does not expose an immutable queue generation, so concurrent privileged deletion
or redeclaration during or after broker acceptance is outside the product
guarantee. Concurrent failed sends preserve their individual broker causes and
invalidate the cache even while another sender rebuilds topology. A post-confirm
failure can follow delivery; retry can therefore duplicate, consistent with
at-least-once delivery. The final adversarial re-review reported no remaining
concrete cleanup-ownership or error-masking defect.

## Remaining campaign

This closes the inherited change set. It does not close the separate A+ target.
The current baseline remains 86.9862% line coverage, 76.5356–82.9676% branch
coverage, and 198 methods with CRAP above 30.
