# Test-harness activity detail rendering

Test commit: `d64e6e2f8`. Product source is unchanged.

The existing `ActivityListener_RendersTheTraceOnceAndDisposesIdempotentlyAsync`
test now observes the rendered detail row for a complete saga transition,
a saga with only one state tag, a processor with and without a request ID,
and an activity with an unrelated tag. It checks saga precedence when a
processor tag is also present, prevents partial state text and unrelated tag
leakage, and retains the idempotent disposal assertion. `await using` closes
the globally registered listener even if an assertion fails early.

Read-only adversarial review found three P2 test gaps: early-failure listener
leakage, a weak partial-state assertion, and an unchecked fallback row. All
were corrected; final review found no P1/P2 issue. A deliberate product
mutation removing the saga state transition made the exact test fail at the
rendered row. Product source was restored to its original SHA-256
`be1a0eb3992932f0414a7bf99203e070e32ceeac6ae8c80fadd6d136f66b857b`;
the product assembly was rebuilt and the targeted test passed again.

The Core test project built at the test commit with zero warnings and errors
from an existing restored SDK artifact. Its full Microsoft CodeCoverage run
passed 6,438/6,438, with no failures or skips. Report:
`artifacts/coverage-activity-details-d64e6e2f8/coverage.cobertura.xml`,
SHA-256 `ac7055d39afb0dcd7e4bd0b7976dfcb63eca126f909008ac02e2ac91ea023798`.
In that Core report, the Testing assembly has 94.6805% line and 79.2219%
branch coverage. `TestActivityListener.FormatDetailsColumn` moved from
3/10 covered lines, 20% branches, CRAP 157.2 to 10/10 covered lines,
75% branches, CRAP 20. The remaining branch outcomes include nullable
activity/type cases outside this real ActivitySource rendering path.

The global A+ goal remains open. This is a verified Core slice using cached
dependencies; it is neither a fresh isolated build nor a whole-product
coverage profile with provider integration reports.
