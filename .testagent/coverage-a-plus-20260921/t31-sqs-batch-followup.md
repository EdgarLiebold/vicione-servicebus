# T31: SQS batch identity — complete measurement recorded

Base HEAD: `82f587ad903552522b4a6966e856e6574388f8d0`.
The focused results below were followed by the complete exact-commit measurement
at `6a0aca7096ad93db552fcb31fb099961033001f0`.
See [complete results and remaining gaps](product-wide-profile-6a0aca709.md).

## Contracts and tests

| Contract | Evidence |
| --- | --- |
| Reordered mixed responses preserve each caller's success or exact error code/text | `AmazonSqsBatchIdentityTests.MixedReorderedResponse_PreservesEveryCallersOwnOutcomeAsync` |
| Duplicate success, duplicate failure and overlapping outcomes reject the entire batch before publishing partial results | `AmazonSqsBatchIdentityTests.DuplicateResponseIdentity_FaultsEveryCallerBeforeAnyPartialCompletionAsync` (three rows) |

Tests exercise real SendBatcher admission, batching and response handling through
an SDK client override. They require one request containing all four original
messages with distinct IDs, the exact queue and the owner token (distinct from
the caller token). Each malformed response starts with complete ID accounting
and adds a duplicate, preventing the completeness guard from masking a missing
duplicate guard. Every caller must receive the same exact malformed-response
exception; a failed Task.WhenAll alone would not prove this.

Read-only adversarial plan and implementation review found no semantic blocker.
The reviewer identified an unbounded disposal wait; both finalizers now bound
DisposeAsync and honor test cancellation. One intermediate build failed
xUnit1051 because those new waits initially omitted the test token; corrected
without suppressions, followed by a clean passing run. Batch collection uses
a one-minute fallback timeout and requires one four-entry request, not a claimed
deterministic scheduler gate. No AWS network behavior is established.

## Completed validation

- Reviewed MAIN class: 4 passed, 0 failed, 0 skipped, exit 0.
- Verify-only whitespace formatting: exit 0, no changes.
- Isolated wrong-caller mutation routes each failure to `(EntryId + 2) % Count`:
  mixed-response test fails on the swapped SecondCode/FourthCode diagnostic;
  three malformed-response controls pass (exit 2).
- Mapping restored; `git diff --exit-code -- src` clean.
- Separate mutation removes only successful-ID duplicate rejection while still
  collecting IDs: DuplicateSuccess fails because a caller succeeds; three
  controls pass (exit 2).
- Duplicate guard restored; isolated final control passes 4/4, exit 0;
  `git diff --exit-code -- src` clean. No product changes remain.

## Local evidence fingerprints

MAIN is the repository; isolated runs use `/private/tmp/servicebus-reply-investigation`.
Raw logs are local ignored artifacts, not published by these hashes.

| File | SHA-256 |
| --- | --- |
| MAIN artifacts/t31-sqs-reviewed-final.log | 6b2b2a7306204f7158904f79c6b4d85efe3c1172615ca70bc983fac2087495b9 |
| MAIN artifacts/t31-sqs-format-final.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| AmazonSqsBatchIdentityTests.cs | 142db170e8d540fa00cce8d7ed9997b0267136fdc90bcaea9ed1d8de3d742287 |
| Isolated artifacts/t31-sqs-mapping-mutant.log | 35dc9cd7baa434a380177fd6d36641e0447b21d018982f081d539bd35619ec38 |
| Isolated artifacts/t31-sqs-duplicate-mutant.log | fd4ea045322474dcc6a1b46de46fcab671639f773b22f68bb4677753347b4809 |
| Isolated artifacts/t31-sqs-restored.log | 744b390a32529c647fe9cdc030e79b69d311cc4614c0f03de3d36a56b81c188a |

## Completed measurement and remaining work

All 33 fresh profiles passed at the exact commit, including full SQS300/300 and
requirement projections. All four fixture groups were cleaned successfully.
Independent accounting review verified 487 hashes and the five-closed/two-new
line-gap delta. ApplyResponse reaches20/20 lines,16/18 branches,CRAP18; its two
optional-collection branches remain open. The documentation successor is ready
for authorized push with the test commit. The complete A+ program remains open.
