# Core timeout activity fault ownership

## Product corrections

The last complete 36-report profile at exact source/test commit `f7d924f94`
measured `TimeoutActivityContextProxy.NotifyFaultedAsync` at CRAP 110 with
0/11 lines covered. Review of the method found three independent contract
errors:

- once the configured timeout elapsed, any `OperationCanceledException` could
  be reclassified as that timeout even when it carried an unrelated token;
- fault-publication suppression used the owning activity's cancellation token
  instead of the message context passed to the method;
- a caller token canceled before invocation did not prevent fault side effects,
  and required arguments were first observed only after the async method had
  returned a task.

The method now validates its required inputs synchronously and returns the
caller's exact canceled token before starting side effects. Its asynchronous
core snapshots the passed message context token and follows the same
fault-publication contract as `BaseConsumeContext`. It reclassifies an
operation as the configured timeout only when the exception carries the
configured timeout token and that token is canceled. Fault generation must
finish before receive notification begins; null tasks are rejected with a
specific diagnostic, and faulted or canceled tasks preserve their exact
exception or cancellation token.

## Fail-first and hard behavior evidence

The first eight-case run against the unchanged implementation passed five and
failed three. It reproduced unrelated cancellation being labeled as timeout,
pre-canceled caller work producing side effects, and deferred argument
validation. After the first correction, the adversarial review found that the
owner token could still override the message context token. A 19-case rerun
against that intermediate implementation passed 16 and failed three:

- canceled message context plus matching cancellation still generated a fault;
- canceled message context plus an ordinary failure still generated a fault;
- canceled activity owner plus an active message context suppressed a fault.

The final suite has 22 test methods and 23 cases. It separates owner, message,
timeout, caller, generation, and receive tokens. It verifies exact context,
exception and token identity; side-effect absence; generation-before-notify
ordering; completion ownership; null task diagnostics; late caller
cancellation; every generation and receive task outcome; and the positive
timeout conversion. Held tasks use deterministic
`TaskCompletionSource` instances with asynchronous continuations. There are no
sleeps, random values, network calls, or implementation-replacing mocks.

All 22 methods grade A (90–100 band) under the Microsoft `grade-tests` rubric
and its .NET analysis extension. Every method is 23 lines or fewer, has a
distinct product outcome oracle, and has a unique `[RequirementCoverage]`
variant. The requirements projection contains the same 22 unique variants and
22 unique method names; its compiled projection test passes 1/1.

The mandatory Microsoft `code-testing-agent` guided the source/test workflow,
`run-tests` the .NET 10 Microsoft.Testing.Platform commands,
`coverage-analysis` and `crap-score` the quantitative assessment, and
`test-gap-analysis`, `assertion-quality`, and `grade-tests` the adversarial
quality review.

## Gates and focused measurement

- Release Unit/Architecture build: zero warnings and zero errors.
- Complete Unit/Architecture gate: 10,070/10,070 passed, zero failures and
  skips.
- Current-byte Core Unit with Microsoft CodeCoverage and the canonical
  settings: 6,303/6,303 passed, zero failures and skips. Raw report:
  `artifacts/coverage-timeout-activity-20260922-final/core-full.cobertura.xml`,
  SHA-256 `e0eac96c4d3d0b64ccfd1e7a4c314ad19c08662778646ee76b3eb550279664a4`.
- A clean detached checkout of exact code/test commit `5d94930e9` passed locked
  restore, a zero-warning Release Core-test build, focused coverage 23/23, and
  the complete Core suite 6,303/6,303. Its retained focused report is
  `artifacts/coverage-timeout-activity-20260922-final/exact-5d94930e9.cobertura.xml`,
  SHA-256 `87fae6aecedbaa85dc632c123fe3971e8662ee1f87538c86a8850eb8b06dd5dd`.

| Final focused method | Lines | Reported branches | CRAP |
| --- | ---: | ---: | ---: |
| constructor | 6/6 | 100% | 2 |
| `get_CancellationToken` | 1/1 | 100% | 1 |
| `NotifyFaultedAsync` validation/dispatch | 6/6 | 100% | 2 |
| `NotifyFaultedCoreAsync.MoveNext` | 17/17 | 100% | 16 |

The method family therefore has 30/30 lines, full reported branch coverage,
and no CRAP score above 30 in both the focused and complete Core reports. The
final read-only adversarial review returned PASS after two earlier FAIL rounds
identified token-authority and late-cancellation gaps. It found no remaining
concrete race, ordering, null-task, or fault-classification defect in this
slice.

These are focused Core results. Product source changed after the complete
profile at `f7d924f94`, so that profile remains a prior baseline rather than a
current-byte product-wide claim. Global A+ remains open and requires another
complete multi-project profile after further coherent slices.
