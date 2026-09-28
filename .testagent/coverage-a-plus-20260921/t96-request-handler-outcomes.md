# T96 — request handler outcome lifecycle

Exact test commit: `0d6e59d4a`.

## Selection and product contracts

The Roslyn `find-untested-sources` pass identified 4,290 source files and
1,573 test files across the repository, but its static pairing can miss
runtime and indirect tests. Direct review found strong existing tests for
request outcome forwarding, bus composition, and successful request-handler
registration. The remaining request-handler gap was outcome behavior after
handler invocation.

`Consumer_AwaitsHandlerThenExactResponseAcrossEverySignatureAsync` checks
both message and context handler delegates with zero through three injected
dependencies. It proves the exact request/context and ordered dependencies,
no early response while the handler is pending, exact response identity,
one response call, and no early consumer completion while response delivery
is pending.

Three more theories check all four consumer arities: a null handler response
causes no send; a handler failure preserves its exact exception and sends
nothing; a response failure preserves its exact exception after one attempt.
The message-only method adapters forward the same task into those consumer
branches and are checked by the eight pending-success rows.

## Verification

- Focused class: 20/20 passed, no failures or skips.
- Complete Core project on exact commit `0d6e59d4a`: 6,991/6,991 passed,
  no failures or skips.
- An isolated counterprobe removed `await` from the two-dependency response
  path. The pending tests and response-failure test failed. A second
  counterprobe forced a send of a null response in the three-dependency path;
  the null-response test failed. Both source changes were restored with no
  product Git diff before the complete Core run.
- Read-only Red Team: PASS with no concrete P1/P2 finding. Its review checked
  the negative-path coverage boundary across message and context delegates.
- Four requirement variants are projected in `CoreRequirements.json`.

## Measurement boundary

No product source changed. T85 remains the latest complete 33-profile
Line/Branch/CRAP checkpoint; global A+ remains open. T96 is the eleventh
focused packet since T85. The next complete measurement is scheduled after
the following coherent packet under the agreed 12–20 packet cadence.
