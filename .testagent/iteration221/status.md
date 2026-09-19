# Iteration 221 status

Local technical gates passed; not admitted. Three disjoint packets cover ten lead-read source files and 32
new requirement variants. The strict Core Release build is warning/error-clean. The three
new focused classes pass 21/21, 7/7 and 16/16 compiled cases, and the requirement
projection passes 1/1. Following a deliberately corrected obsolete expired-response
integration oracle, the unfiltered Core regression passes 6,217/6,217. One preceding
full run had a single Catch integration case fail at repository removal; that test then
passed in isolation and in two subsequent full runs. Its timing sensitivity is an open observation,
not a proven product defect. Sixteen compiled causal single-cause mutants were killed,
the owner coverage and final source hashes were checked, but the governance admission
gate below remains open. The tracked Core test project currently contains 699
files (221,677 lines at HEAD `756fbb52029f8c1829100fa9184f83c8b8ce4944`).

Product regression resolved under `PO-2026-08-24-01`: `CompleteRequestActivity` and
`FaultRequestActivity` forward completed/faulted outcomes even when the original request
expired, while `RequestStateMessagePipe` gives these outcomes one second minimum TTL.
The old integration assertion incorrectly expected no send in that case. Its new oracle
checks one forwarded outcome, original request routing, exact one-second TTL and final
state; the existing test serializer/recorder now exposes the necessary overload/metadata.
The distinct generic-forwarding discard rule remains unchanged. This recorder proves the
state-machine/send-pipe boundary, not a physical transport or either real serializer;
the latter are separate acceptance scopes.

EF validation on the guarded final baseline: the EntityFrameworkCore unit and LocalIntegration test projects each built
in Release with zero warnings/errors; the EF unit suite passed 249/249 and the local
requirement projection passed 1/1. The PostgreSQL-dependent unfiltered provider suite
was **not** run: no LocalIntegration profile/credentials are configured and the default
localhost endpoint is unreachable. Do not claim real durable-provider acceptance.

Final lead-review finding corrected: `CancelRequestTimeoutActivity` had allowed a
pre-canceled zero-timeout call to read/clear the request ID and continue. The
existing pre-cancel case relies on the scheduler rejecting a canceled token and does
not catch this branch. Its focused oracle now covers the branch and a guard executes
before the ID read; the guard-removal mutation is documented below.

Follow-up: the zero-timeout oracle went red without the guard (1/1) and green with it
(1/1); the guarded source, test file and both baseline Sagas DLLs were hash-restored.
An additional guard-removal mutant was attempted but its Sagas build stalled for over
three minutes under concurrent external .NET load and was terminated by exact PID;
it did **not** compile/test and was **not** counted. On a separate retry, the identical
mutant compiled with zero warnings/errors and the focused case failed 1/1 for the
expected missing `OperationCanceledException` (exit 2). The guard source was inverse-
patched to SHA-256 `b13417f7f8644340ffcd8d9993627b8a0db344fc92cdb2f46839102de79127f0`;
both Sagas DLLs match the strictly built guarded baseline SHA-256
`447a8970bc7c71f818928b3ea5b117ac3a8ea84698941e386bd6013d3c480365`,
and the focused case is again 1/1 green. Total: 16/16 compiled causal kills including
the follow-up guard mutation; the abandoned build attempts are not counted.

Mutation progress: packet A's five separate compiled single-cause mutants were each
killed by a named focused test and reverted byte-exactly. The final Sagas output and
Core-test copy of its DLL both match the backed-up baseline SHA-256, and A's 21 cases
were green again. A zero-discovery filter attempt was discarded, not counted.
Packet B's five separate mutants have also compiled and failed their targeted tests
causally (typed publish token, fault covariance, typed respond cancellation, typed
send destination, and missing await on faulted publish). Its four source hashes and
baseline Sagas/Core DLLs were restored; the B focus suite passed 7/7 again.
Packet C's five single-cause mutants compiled cleanly and were killed by one discovered,
named, causally failing test each (address precedence, pre-cancel request, replacement
token identity, pre-cancel unschedule and own-token skip). An initial gated run for the
token mutant hung after asserting and was canceled, **not** counted; a separate bounded
test provided the kill. All three C sources are byte-identical to baseline; both Sagas
DLL copies have the baseline SHA-256
`2b2750d9b0f39674af8329110a73ab683775c435a01b37ff79511d1d42d1e3e8`,
and C's focus suite passes 16/16 after restoration. Total mutation evidence: 15/15
separately compiled causal kills in the initial three packets with no surviving mutant.

Coverage after the added guarded-ID and C branch-matrix tests: an instrumented,
unfiltered Core run passed 6,217/6,217, artifact
`/private/tmp/vsb-iteration221-core-final2.cobertura.xml` SHA-256
`4e7783c2224b00a47155b62f04ffe504e5f5f29c9ac1cbf35a5419d9b5a79bf2`.
Those ten owner sources cover 477/477 instrumented lines and 121/122 branches;
the sole missing branch is generated condition #2 of the nullable token comparison in
`FaultedUnscheduleActivity.cs:97`. Existing cases cover no prior token, matching
incoming token, different token and absent incoming token. Read-only IL inspection
shows that the last branch is an unreachable generated second `HasValue` check:
the preceding source guard established the same unchanged local as non-null.
Even a previous `Guid.Empty` with absent incoming token follows a different, covered
edge. No production or test edit is justified to target this generated edge.
The maximum calculated owner-method CRAP is 10.000
(`CancelRequestTimeoutActivity` async state machine, 100% line covered). The Core
run's whole-instrumented-fork totals are 55,057/64,054 lines and 18,916/24,119 branches;
these do **not** represent all provider test assemblies or final fork acceptance.

Final independent read-only packet audits found residual work, so this is a bounded
working checkpoint rather than an A+ admission. `CompleteRequestActivity` and
`FaultRequestActivity` propagate cancellation but do not reject a pre-canceled
context before reading the outcome/resolving an endpoint; an endpoint that ignores
its token could still send and continue. The 16-task forwarding isolation test
does not guarantee an overlapping interleaving, and the faulted-schedule test's
`cancelStarted.Task` wait is unbounded under a cancel-skipping regression. Packet B's
continuation concurrency oracle checks aggregate calls but not per-context identity.
Several tests named `exact-api-*` check only subsets of signatures/nullability and
cannot be cited as proof of the entire public surface or XML documentation. The
same-saga schedule/unschedule token race needs either an upstream serialization
proof or a dedicated interleaving oracle; existing cases use independent sagas.
These observations are not automatically proven product defects. Address the concrete
pre-cancellation gap and strengthen bounded/overlap assertions in a follow-up once
the §4.3 gate is satisfied; retain them as open findings until then.

Open governance finding: §4.3 of `AI_WORKING_AGREEMENT.md` requires each responsible role
to read the entire tracked owning test project, effective shared build/package settings,
fixtures and associated CI **before** test design or editing and before lead acceptance.
Neither the agents nor the lead have yet completed that full personal semantic reading.
The pre-edit order deviation cannot be repaired retroactively; record it explicitly.
No build, parser, hash manifest or sampled test inspection substitutes for the reading.
Even after technical verification, do not claim A+ lead admission until the complete
hash-bound reading ledger, language-appropriate full-parser results and exact Git/read
file-set equality have been established, with a governance decision on the deviation.

The goal remains active and local diagnostics and bounded implementation continue. The
remote push is independent of the technical work and is not an iteration stop condition.

## Source hash snapshot after mutation restoration

- `b13417f7f8644340ffcd8d9993627b8a0db344fc92cdb2f46839102de79127f0` — `CancelRequestTimeoutActivity.cs`
- `b471c9daf8b7db27e6260aa0a4c356bec8bbb47e80d1d9824903fc0d815a404f` — `CompleteRequestActivity.cs`
- `8fcc9717c9580872abe1ea8c33e4e4403a628ab41a70007e0cd4bbce321c0d78` — `FaultRequestActivity.cs`
- `4a12a6b7414bc10b1f464c568223ab46dbae4aed705da2e3a4279834a64ecac1` — `FaultedPublishActivity.cs`
- `6c39945320077f0dc7000d660aeceefbc321f3dd4770775ada9419b2ea6b179d` — `FaultedRequestActivity.cs`
- `053cd7bbc331df7ba1090cd2107729a76f46bbaba021ce9f0bcd5adf1c769cad` — `FaultedRespondActivity.cs`
- `3b7792c3c8210ed93542fbb3dd36f69ac669dea99d3014111fcc52efa4f8a9d7` — `FaultedScheduleActivity.cs`
- `a972cffb93549a4eb0c2352f6b8375f4584e9e1c63f472e5ebcd45f800fabca0` — `FaultedSendActivity.cs`
- `f22d92b2d3a41e47d8754472799462e2cffb4111c9df25589adb4d8441eefa55` — `FaultedUnscheduleActivity.cs`
- `8f7799ca6f1551cbbe5ec239383475a87fd81aa3f0c1ff7df2174ab9ec5de0cc` — `PublishActivity.cs`
