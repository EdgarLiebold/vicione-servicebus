# Runtime scheduled-send contract slice

## Scope

- Exact source/test commit: `a136466c1` (the source/test tree was clean after the test commit).
- Product source was unchanged. The new tests exercise the four public runtime-typed
  `MessageScheduler.ScheduleSendAsync` overloads and the converter path that dispatches to the
  provider's generic contract.
- Six attributed test methods execute seven cases. Their six entries were added to the Core
  requirement projection.

## Product behavior proved

- An explicit interface contract reaches the provider as that interface, while an inferred
  runtime contract reaches it as the concrete payload type. Both preserve the original payload.
- All four success paths preserve the destination, due time, nondefault cancellation token, and
  scheduled handle identity. The two pipe paths run the caller's untyped pipe on the exact context
  passed to the provider and preserve its correlation or request metadata.
- A mismatched explicit contract fails before the provider or pipe runs, with and without a pipe.
- Twelve null-argument cases cover the required parameters of all four runtime overloads and
  prove that no provider dispatch occurs.

## Verification

- The final focused Microsoft CodeCoverage run passed 7/7, with zero failures and skips. It used
  `tools/ci/coverage.settings.xml`; its Cobertura report is
  `artifacts/coverage-a-plus-20260923-ac363722c/core-scheduling/focused-final2.cobertura.xml`
  (SHA-256 `82afda06cc1a1247fda7dd304879903343b713480ec7365b448b733fbbbfbab3`).
- The complete current-byte Unit/Architecture solution passed 10,163/10,163 with zero failures and
  skips. The Core requirement projection test passed as part of that gate. A separate complete Core
  CodeCoverage run before the final assertion strengthening passed 6,350/6,350.
- The focused run compiled the test project in Release without warnings or errors. The exact test
  source SHA-256 is `ea67539fe4f604c6392f4d63dc21ff3b99d8b073d32750e83a3c95e2274b42ae`;
  the requirement projection SHA-256 is
  `d4296a9bd1afd4ef02827a23e393316c1112841c514ffa1b671ac487669f4040`.
- Read-only adversarial review first identified forwarding and token-identity mutants. The tests
  were strengthened and the final review returned PASS with no concrete remaining mutant for the
  claimed variants.

## Focused coverage and CRAP

| Runtime path | Previous product-wide CRAP | Final focused Lines | Final reported Branches | Final focused CRAP |
| --- | ---: | ---: | ---: | ---: |
| Public inferred type, no pipe | below 30 | 6/6 | 4/4 | 4 |
| Public explicit type, no pipe | 42 | 7/7 | 6/6 | 6 |
| Public inferred type, pipe | 42 | 8/8 | 6/6 | 6 |
| Public explicit type, pipe | 72 | 9/9 | 8/8 | 8 |
| Converter send state machine, no pipe | 42 | 6/8 | 4/6 | 6.56 |
| Converter send state machine, pipe | 72 | 7/10 | 5/8 | 9.73 |

The converter's uncovered branches are internal defensive null checks that are unreachable through
the validated public entry. The tests cover its two distinct type-match outcomes and the successful
generic dispatch. The next coherent Core scheduling slice is the initialized-message path: two
`MessageScheduler` state machines remain at 0/9 lines and CRAP 42 in this focused report.

The last 36-report product-wide baseline remains `ac363722c`: 89.4386% line coverage, a
conservative 81.1603–88.0158% branch interval, and 83 methods above CRAP 30. These focused
results do not replace that aggregate. Global A+ remains open.
