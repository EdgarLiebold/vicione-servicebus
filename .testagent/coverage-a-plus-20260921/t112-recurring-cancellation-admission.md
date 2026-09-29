# T112 — recurring command admission under cancellation

Implementation commit: `9d403c961`.

Both recurring schedulers could hand a command to a transport even when the
caller's token was already canceled, if the endpoint ignored that token. The
endpoint-backed scheduler could also send after an endpoint provider returned
successfully despite cancellation during resolution. Publish scheduling could
reach topology resolution before checking a pre-canceled token.

`RecurringCancellationAdmissionTests` exercises public scheduler entry points
with recording endpoint, provider and topology boundaries. Its 24 cases cover
both command transports, explicit send and publish destinations, three pipe
shapes, cancel/pause/resume controls, and cancellation during pending endpoint
resolution. Assertions check exact cancellation-token identity, no topology
lookup or endpoint resolution after pre-cancellation, zero commands, exact
command contract/payload/destination, and a healthy successor after each
failure. The unchanged product failed all 24 original cases. The first green
matrix passed 24/24; adversarial review found two surviving mutants. The
strengthened topology and provider counterprobes close both. Final independent
read-only re-review is **PASS**, with no concrete P1/P2.

The zero-warning/error build passed. The final focused class passed 24/24,
neighboring Recurring tests passed 275/275, and the complete Core project
passed 7,099/7,099 on the exact implementation commit, with no skips. The
requirements JSON has 4,394 unique tuples, and `git diff --check` passes.

Microsoft code-testing-agent, test-gap-analysis, assertion-quality, run-tests,
coverage-analysis and find-untested-sources guidance was applied. The T107
Roslyn pairing and T97 33-profile coverage checkpoint were reused for
selection; no new product-wide Line/Branch/CRAP measurement was run. T97
`3cb94a285` remains the last valid aggregate: 86,639/93,963 lines
(92.20544%), 31,312/36,927 conservative branches (84.79432%) and zero
methods above CRAP 30. Global A+ remains open.
