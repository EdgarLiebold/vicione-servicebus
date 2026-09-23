# RabbitMQ durable destination rejection

The `RabbitMqDurableSendAcceptanceTests.UnsafeDestinationOptions_RejectBeforePublishingToExistingQuorumQueueAsync`
theory exercises seven different unsafe destinations through the registered
`IDurableSendDispatcher<IBus>` against an actual RabbitMQ broker: queue
declaration, non-durable and auto-delete topology, non-fanout exchange,
alternate exchange, additional binding, and direct reply-to. It predeclares
an existing durable quorum queue and its binding. Each attempt must throw the
specific durable-acceptance `ConfigurationException`, leave that queue empty,
and preserve all of that queue's original bindings. The binding baseline is
taken immediately before dispatch and compared without depending on API order.
The production path calls
`ValidateDestination` before acquiring a send endpoint. The test does not
measure broker-wide exchange topology, and direct reply-to is not the existing
queue's publish destination.

The first adversarial review passed the rejection contract but pointed out
that checking only the presence of one binding could not prove the queue's
whole binding set was unchanged. The next review identified a possible false
failure from taking the baseline before bus startup and depending on API order.
Both findings were corrected. The final build and test run were archived;
the build has zero warnings and errors, and the test run passed under
CodeCoverage.

| Evidence | Result | SHA-256 |
| --- | --- | --- |
| `artifacts/coverage-rabbitmq-durable-address-20260923-final/build.log` | Final Release build, 0 warnings/errors | `1936bd2d9bc5b76f12b04beb468d6fdfc9f92c018301086179c6d261c06008ac` |
| `artifacts/rabbitmq-durable-address-unit-gate-20260923.log` | Unit/Architecture 10,226/10,226, 0 failed/skipped; run before the final local-integration-only assertion refinement | `a3d1017cd499b21dfe4b392d3b6f355cb6e1cdcf9455ed8911d4d0693b8dd4ae` |
| `artifacts/coverage-rabbitmq-durable-address-20260923-final/test.log` | Final Microsoft CodeCoverage and RabbitMQ local integration 38/38, 0 failed/skipped | `747c4b445f2a6d4caa08f637064aa3a9610c7b0fa8c3ffb406360b062fb15268` |
| `artifacts/coverage-rabbitmq-durable-address-20260923-final/coverage.cobertura.xml` | `ValidateDestination` 17/17 lines, 19/22 reported branches, complexity 22, CRAP 22 | `2a18a9cfb99748da9ebcd784d4332bb154c56a83caf3fa16b20228981206f4cf` |
| `artifacts/run-output/vicione-1eb54d1ab942/fixture-findings.json` | Empty findings | `1daae58a4dc650141e8b5fc0ee4d680cff1c93d17aeaafff7e618b9d7b9accb4` |

The previous complete 36-report profile at product/test commit `6a3689d33`
measured `ValidateDestination` at 11/17 lines and CRAP 43.28. The focused
report above measures the new tests; it does not replace the complete profile.
The product-wide A+ goal remains open.

The final read-only adversarial review returned PASS for the bounded behavior
claim, the corrected assertions, and the archived build, test, coverage, and
fixture evidence.
