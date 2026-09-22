# RabbitMQ connection-context ownership and failure handling

## Product findings and corrections

The RabbitMQ connection factory did not consistently preserve ownership across
startup, publication, shutdown subscription, and cleanup. A stopped supervisor
could still refresh settings, the supervisor token was not carried through
every real client connection route, and a connection that closed while its
shutdown handler was being installed could still be published as active. A
subscription failure could also leave an unpublished context alive.

The corrected factory now:

- rejects a pre-stopped supervisor before settings refresh or connection work;
- passes the exact supervisor token through refresh and both host and resolver
  client connection routes;
- registers the connection owner before asynchronous connection creation;
- publishes only a connection that remains open after shutdown subscription;
- disposes every context that fails before publication;
- preserves configuration, cancellation, client, and transport failures even
  when logging or cleanup also fails;
- contains both expected and unexpected shutdown-handler removal failures;
- invalidates topology and lifetime state on broker shutdown while assigning
  context cancellation only to the shutdown reasons owned by the broker; and
- completes lifetime disposal even if diagnostic logging fails.

## Hard-test and adversarial process

Twenty-three xUnit facts execute 38 cases against observable connection,
supervisor, client-adapter, shutdown, publication, and disposal behavior. The
tests use controlled completion sources for the publication races and exercise
the default RabbitMQ adapter through both host and resolver routes. Assertions
distinguish the exact cancellation token, primary exception, publication
state, handler lifecycle, disposal count, topology invalidation, and owner
registration order.

The first read-only adversarial review found five surviving families: a
stop-versus-registration leak, a shutdown-subscription race, logger failure
replacing the real failure, missing proof through the default client adapter,
and a polling-based test race. The second review found that an unexpected
handler-removal exception could still escape its asynchronous callback and
that a subscribe-then-check mutation remained viable. Both findings were
reproduced and closed. Final review returned PASS with no concrete remaining
product mutant or test-quality defect.

The complete parallel gate exposed two independent test-harness races. The
RabbitMQ stop/connect case now waits for the existing disposal signal before
asserting its exact count. A SignalR cancellation test now synchronizes on
actual remote consumption before cancelling the caller token. The same gate
then exposed an existing Saga assertion that observed consumption before the
repository's subsequent delete completed; it now waits through the repository
removal contract before independently checking absence. Read-only adversarial
review accepted all three changes as synchronization fixes with unchanged
product expectations.

## Verification and measurement

- Exact source/test commit: `c9926b8c4`.
- Focused RabbitMQ connection suite: 38/38 passed, zero failures and skips.
- Complete RabbitMQ Unit project: 378/378 passed, zero failures and skips.
- Complete SignalR Unit project: 97/97 passed, zero failures and skips.
- Complete Core Unit project: 6,343/6,343 passed, zero failures and skips.
- Focused Saga regression: 1/1 passed.
- Complete Unit/Architecture gate: 10,148/10,148 passed, zero failures and
  skips.
- The exact detached checkout passed locked Engineering restore and the Release
  Unit solution build with zero warnings and zero errors.
- Exact focused report:
  `artifacts/coverage-rabbitmq-connection-exact-c9926b8c4/focused.cobertura.xml`,
  SHA-256 `9ac4aaefb59fb6f5aa5ec7e46c6b0e5d617525384758fbf2c4d93c570a993b7d`.

| Exact-commit target method | Lines | Reported branches | Complexity / CRAP |
| --- | ---: | ---: | ---: |
| `CreateConnectionAsync.MoveNext` | 31/31 | 20/20 | 20 |
| `CreateAndPublishConnectionAsync.MoveNext` | 30/30 | 10/10 | 10 |
| `CreateSharedConnectionAsync.MoveNext` | 9/9 | 6/6 | 6 |
| shutdown handler | 6/6 | 6/6 | 6 |
| shutdown-handler removal | 12/12 | 2/2 | 2 |
| `DisposeUnpublishedConnectionAsync.MoveNext` | 9/9 | 2/2 | 2 |
| `TransportLifetime.DisposeSubjectAsync.MoveNext` | 12/12 | 2/2 | 2 |

Every reported method and compiler state in `ConnectionContextFactory.cs` has
full line and branch coverage, with maximum CRAP 20. The table includes the
changed `TransportLifetime` disposal path; it does not claim that every older
method in `TransportLifetime` is covered.

The mandatory Microsoft `code-testing-agent`, `run-tests`,
`coverage-analysis`, `crap-score`, `test-gap-analysis`, `assertion-quality`,
and `grade-tests` workflows governed test design, execution, measurement,
mutation-style review, assertion review, and the quality gate. This focused
result does not replace the product-wide profile; global A+ remains open.
