# T113 — one-time setup caller cancellation

Implementation commit: `aaa6aa9b4`.

`PipeExtensions.OneTimeSetupAsync<T>` checked its caller's cancellation token
only before joining a shared setup. A caller canceled while the callback was
pending kept waiting for that callback. The public method now applies its
token to its own wait over the shared setup task. The callback, single-flight
state, cache, failure and retry ownership remain shared.

The four-case `OneTimeSetup_CancelingOneCallerLeavesSharedWorkAndOtherCallersIntactAsync`
matrix covers canceled leader and follower, each with a succeeding or faulting
shared callback. It holds the callback pending while canceling one caller,
requires exact cancellation-token identity and a still-pending other caller,
then verifies one callback invocation, shared final outcome, cached success
or original failure plus healthy retry. On unchanged product all four cases
failed with a timeout instead of cancellation. After correction the focused
`PipeExtensionsTests` passed 10/10 and the neighboring OneTime cohort passed
15/15. The exact-commit complete Abstractions project passed 966/966 with
zero skips after a zero-warning/error build.

Assertion and pseudo-mutation review found meaningful exception identity,
callback count, pending-state, cache and recovery oracles. Omitting the new
wait, using a different token, canceling the shared task or swallowing its
fault would fail the matrix. Independent read-only adversarial review is
**PASS**, with no concrete P1/P2. An exactly simultaneous setup completion
and cancellation can be won by either event; the regression intentionally
controls their order and makes no race-priority claim. The Abstractions
requirements JSON has 648 unique tuples and `git diff --check` passes.

Microsoft code-testing-agent, test-gap-analysis, assertion-quality, run-tests,
coverage-analysis and find-untested-sources guidance was applied. The T107
Roslyn pairing and T97 33-profile aggregate were reused for selection; this
packet did not rerun the global Line/Branch/CRAP measurement. T97
`3cb94a285` remains the last valid aggregate: 86,639/93,963 lines
(92.20544%), 31,312/36,927 conservative branches (84.79432%) and zero
methods above CRAP 30. Global A+ remains open.
