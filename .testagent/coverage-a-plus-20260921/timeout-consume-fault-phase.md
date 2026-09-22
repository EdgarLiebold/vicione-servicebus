# Core timeout consumer fault ownership

## Product corrections

The last complete 36-report profile at exact source/test commit `f7d924f94`
already measured the old asynchronous `TimeoutConsumeContext` fault path at
12/12 lines, 10/10 reported branches, and CRAP 10. Manual contract review and
hard behavior tests still found that it could:

- classify an unrelated cancellation as the configured timeout after that
  timeout had elapsed;
- use the owning context's cancellation state instead of the passed message
  context, or use the timeout wrapper's canceled token as delivery authority;
- publish a timeout fault through a canceled wrapper, preventing real fault
  publication;
- start side effects for a pre-canceled caller and defer required-argument
  validation;
- return before fault generation or receive notification reached their exact
  completion outcome, and accept null tasks from either collaborator.

The public method now validates required inputs synchronously and returns the
caller's exact canceled token before starting side effects. Its asynchronous
core distinguishes the exact wrapper and scopes that carry that same wrapper
from foreign timeout contexts. The wrapper's own chain takes delivery
cancellation and fault publication from the original consume context; foreign
contexts remain authoritative for both. Receive notification always retains
the context passed to the method. Only an `OperationCanceledException` that
carries the canceled configured timeout token is reclassified, and generation
must finish before receive notification begins.

## Fail-first and adversarial evidence

The first valid 17-case run against the unchanged product passed 7 and failed
10. It reproduced wrong context and message-token authority, unrelated
cancellation reclassification, missing pre-cancel behavior, deferred argument
validation, and null generation and notification tasks. An earlier fixture
attempt failed because its typed `DispatchProxy` did not implement the untyped
advanced context operations; it was corrected before any product conclusion
was drawn and is not counted as fail-first product evidence.

Three adversarial review rounds then expanded the matrix:

1. A direct convenience call exposed the timeout wrapper as its own message
   context. The added regression test failed alone, 17/18, until delivery
   cancellation fell back to the original consume context.
2. A real `ConsumeContextScope<T>` retained the wrapper's timeout token. Its
   new regression test failed alone, 24/25, until exact payload identity made
   scopes around the same wrapper part of the owned chain.
3. A scope around another same-generic-type timeout context proved that foreign
   wrapper payloads do not borrow cancellation or publication authority from
   the owner under test.

The first complete Unit/Architecture gate then found two existing integration
regressions: 10,094/10,096 passed. Both real pipeline timeout tests published no
fault because the canceled timeout wrapper was also used for generation. Fault
generation for the owned wrapper chain was moved to the original delivery
context, while receive notification kept the passed wrapper or scope. The two
integration cases and their class then passed 5/5; the corrected complete gate
passed 10,096/10,096.

The final suite has 25 attributed methods and 26 cases. It separates owner,
message, timeout, caller, generation, and receive cancellation tokens and
checks exact context, exception, token, order, side-effect, and completion
identity. Held operations use deterministic `TaskCompletionSource` instances
with asynchronous continuations. There are no sleeps, random values, network
calls, or implementation-replacing mocks. All 25 methods grade A (90–100 band)
under the Microsoft `grade-tests` rubric and its .NET extension. The JSON
projection has the same 25 unique requirement/variant pairs and methods; its
compiled projection test passes 1/1.

The mandatory Microsoft `code-testing-agent` guided the workflow,
`run-tests` the .NET 10 Microsoft.Testing.Platform commands,
`coverage-analysis` and `crap-score` the measurement, and
`test-gap-analysis`, `assertion-quality`, and `grade-tests` the test-quality
review.

## Gates and focused measurement

- Final Release Unit/Architecture build: zero warnings and zero errors.
- Corrected complete Unit/Architecture gate: 10,096/10,096 passed, zero
  failures and skips.
- Current-byte Core Unit: 6,329/6,329 passed, zero failures and skips.
- Focused current-byte suite: 26/26 passed with Microsoft CodeCoverage and
  canonical settings. Its accepted pre-commit report is
  `artifacts/coverage-timeout-consume-20260922-final/coverage-integration-corrected.cobertura.xml`,
  SHA-256 `2227898aff46c73e92d0eba5e2021b56ed8ed5bce2deb718cf81ce74834ec2b1`.
- A clean detached checkout of exact code/test commit `2c61c180a` passed locked
  restore, a zero-warning Release Core-test build, 26/26 focused coverage, and
  the complete Core suite 6,329/6,329. Its retained report is
  `artifacts/coverage-timeout-consume-20260922-final/exact-2c61c180a.cobertura.xml`,
  SHA-256 `5463c72caa980f18a4a3ad4a8b7e2c379cf60789037b525d5c9f99cb6f10148b`.

| Final focused method | Lines | Reported branches | CRAP |
| --- | ---: | ---: | ---: |
| constructor | 7/7 | 100% | 2 |
| `get_CancellationToken` | 1/1 | 100% | 1 |
| `get_Message` | 1/1 | 100% | 1 |
| `NotifyFaultedAsync<T>` validation/dispatch | 6/6 | 100% | 2 |
| `NotifyFaultedCoreAsync.MoveNext` | 25/25 | 100% | 24 |
| `NotifyConsumedAsync` convenience path | 1/1 | 100% | 1 |
| `NotifyFaultedAsync` convenience path | 1/1 | 100% | 1 |

The family therefore has 42/42 lines, full reported branch coverage, and no
CRAP score above 30. Final read-only adversarial review returned PASS for the
direct owned context, a real scope around it, a foreign same-type timeout
context, generation identity, and receive identity.

These are focused Core results. Product source changed after the complete
profile at `f7d924f94`; that profile remains a prior baseline. Global A+
remains open and requires a fresh complete multi-project profile after further
coherent slices.
