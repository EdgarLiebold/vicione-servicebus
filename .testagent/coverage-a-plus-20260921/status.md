# ServiceBus A+ coverage campaign — status

## Current quantitative baseline

- 36 fresh, parseable Cobertura reports cover 32/32 loadable product assemblies at commit
  `e0cf987c845154fea81ec27b63592a910aceac37`.
- Aggregate: 79,569/90,165 lines = 88.2482%.
- Branch interval: 79.2182–86.0351%.
- 142 methods exceed CRAP 30.

This 36-report baseline is retained for comparison with a complete profile. The latest partial
19-report rerun (including fresh SQS coverage and one Event Hubs local integration project) observes
32/32 assemblies but omits other provider integration projects: 76,327/92,836 lines = 82.2170%,
27,689–29,493/36,566 branches = 75.7233–80.6569%, and 237 methods above CRAP 30. The two
profiles have different test scope and cannot be used as a before/after coverage comparison.

## Prior iteration closure

The inherited source-review and package-structure change set is closed on the
current worktree. The final Release build completed with zero warnings and zero
errors, and the complete Unit/Architecture profile passed 9,769/9,769 with zero
failures and zero skips.

Provider evidence retained for this closure:

- the six-fixture local matrix passed 402/402 with empty fixture findings, run
  `vicione-285cc34ba452`;
- the standalone SQL Server profile passed 69/69 with empty fixture findings,
  run `vicione-0a7a93d9e1d6`;
- the final RabbitMQ profile passed 31/31 with empty fixture findings, run
  `vicione-5e9fd08c2ca0`;
- RabbitMQ Unit passed 324/324 after the final pre/post quorum-proof and
  concurrent cleanup regressions;
- the package gate rebuilt 31 packages and passed all 18 Developer Journeys,
  four isolated package consumers, and the 30-assembly runtime API comparison.

The final RabbitMQ contract requires an existing durable quorum queue before
publish and rechecks it after a persistent, mandatory, publisher-confirmed
publish without changing routing. Concurrent privileged queue deletion or
redeclaration during or after broker acceptance is an explicit operational
boundary. Concurrent failed sends preserve their original broker causes while a
new topology generation is running. A failed post-confirm check can retain an
already delivered intent, so retry remains at-least-once and can duplicate.

## Active phase

The RabbitMQ phase is complete in the inherited closure. The ActiveMQ phase is complete in the
current change set: 9,788/9,788 Unit/Architecture tests, three targeted real-broker cases, focused
coverage/CRAP, Microsoft test-quality assessment, and final adversarial review are green. Details are
in `active-mq-phase.md`.

The first generic SQL topology slice is also complete: 147/147 SQL tests and 9,797/9,797 complete
Unit/Architecture tests pass, its six selected baseline hotspots are below CRAP 30, and the final
adversarial re-review returned PASS. Details are in `sql-topology-phase.md`.

The SQL host-configuration slice is complete: 190/190 SQL tests and 9,840/9,840 complete
Unit/Architecture tests pass. URI credentials, mutable validation, PostgreSQL host parsing, atomic
replacement, effective data-source projection, inline-port precedence, and multi-host overrides
have hard behavior regressions. Every selected host hotspot is below CRAP 30, and two final
adversarial reviews returned PASS. Details are in `sql-host-phase.md`.

The receive-validation and SQL Server taxonomy CRAP slice is complete: 191/191 SQL tests, 15/15
filtered provider taxonomy cases, and 9,841/9,841 complete Unit/Architecture tests pass. The two
baseline methods fell from CRAP 34 and 38 to at most 18 and 2. The final adversarial review returned
PASS. Details are in `sql-validation-taxonomy-phase.md`.

The next SQL slice covers the remaining receiver loop, PostgreSQL runtime, and SQL Server migration
hotspots from the exact 142-method global baseline. A fresh product-wide aggregate will follow after
coherent phases; the focused reports do not claim that the requested global A+ target is reached.

The inherited SQL receiver-loop and retention work is closed in commit `b90d5e744`. The current
Amazon SQS naming and scoped-topology slice passed 165/165 SQS tests and final read-only adversarial
review; its 18 new tests are graded A under the Microsoft rubric. The final complete
Unit/Architecture gate passed 9,881/9,881 with no failures or skips. See
`amazon-sqs-naming-phase.md`. The subsequent SQS topology-declaration slice has 14 focused tests
and passed 179/179 SQS tests with Microsoft CodeCoverage. Six selected comparer/diagnostic CRAP
hotspots are now below 30, and its final read-only adversarial review returned PASS. The complete
Unit/Architecture rerun passed 9,895/9,895 without failures or skips. See
`amazon-sqs-topology-phase.md`. Global A+ remains open.
