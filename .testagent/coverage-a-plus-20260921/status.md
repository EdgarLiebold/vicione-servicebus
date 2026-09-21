# ServiceBus A+ coverage campaign — status

## Baseline complete

- 9,682/9,682 Unit/Architecture tests passed.
- 526/526 tests across all 13 local-provider projects passed; 0 failed, 0 skipped.
- Four canonical fixture runs exited 0 with empty `fixture-findings.json` arrays.
- 29 fresh, parseable Cobertura reports cover 32/32 loadable product assemblies.
- Current aggregate: 86.9862% lines, 76.5356–82.9676% branches, 198 methods
  with CRAP > 30.

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

The next work is the product-wide A+ coverage and CRAP campaign. The quantitative
baseline above remains the starting measurement; this closure does not claim
that the A+ coverage target has already been reached.
