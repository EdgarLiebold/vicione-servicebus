# Event Hubs batch activity-route contract

The last complete product-wide profile at `44d9e3254` reported
`EventHubProducerBatchSender.SetActivityRoute` at CRAP 72.75, with 3/12 lines covered. The
method records a partition route on an active diagnostic activity only when every prepared
event uses the same route and the activity requests all data. Its product behavior was
previously unasserted in the Event Hubs batch-sender tests.

Three new test methods add four cases to the existing source-owned batch-sender cohort:

- `UniformRoute_TagsOnlyTheActualPartitionDimensionAsync` checks both a uniform partition ID
  and a uniform partition key. Each case asserts the exact route tag, the absence of the other
  tag, one two-event provider batch, and batch disposal.
- `MixedRoutes_DoNotTagTheActivityWithTheFirstRouteAsync` verifies two distinct provider routes
  and their separate batches while rejecting both misleading activity route tags.
- `UnrecordedActivity_DoesNotGainRouteTagsAsync` verifies that a successful routed send leaves
  an activity without requested data untagged.

The focused xUnit/MTP run passed 11/11 batch-sender cases, with zero skips. Microsoft
CodeCoverage measured `SetActivityRoute` at 12/12 lines, 100% reported branches, complexity
12, and CRAP 12.00. The focused report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/eventhub-route-tags-focused.cobertura.xml`,
SHA-256 `95dbdbd1c7439a5090be7aa4977b94c228c48ceb8ab971a97a99a4df113dc973`.
The full real Event Hubs emulator suite passed 57/57 with Microsoft CodeCoverage, zero skips,
and empty Azurite/Event Hubs fixture findings, run `vicione-3168d01beb56`. Its report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/eventhub-route-tags-full.cobertura.xml`,
SHA-256 `ffd0c345eb87b881276542def5d1064b20d62b229ccd2ca46a1d1ae252690d24`.
The full report confirms the same 12/12 lines, 100% branches, and CRAP 12.00.

The Microsoft `code-testing-agent` skill guided the focused test design and `run-tests`
guided .NET 10 MTP syntax. The final read-only adversarial review returned PASS after checking
positive and negative tag assertions, Activity setup, actual provider routes, disposal, and
the three requirement-projection entries. No product source was changed. The last complete
product-wide profile remains `44d9e3254`; global A+ remains open.

## Exact source/test commit

Commit `bc51b63c377eeb15d5debaee21e4d785f16ce7f8` was checked in a clean detached
worktree with no tracked diff. Locked restore of the Event Hubs test project and its Release
build passed with zero warnings and errors. The first full emulator attempt passed 56/57:
the existing `ConsumerRetry_ExecutesOneOriginalAndThreeImmediateRetriesWithExactMetadataAsync`
timed out waiting for consumer completion after the producer had sent. Fixture findings were
empty; this failed attempt is retained as `attempt1-failed.cobertura.xml` and
`attempt1-fixture-findings.json` with its broker logs beside the passing exact reports. This
56/57 result and the test name were observed in the MTP console output; no test-result file was
produced for that attempt, and it is not counted as green.

A second full fixture run on the same commit passed 57/57 with Microsoft CodeCoverage and zero
skips in the MTP console; Azurite/Event Hubs fixture findings were empty, run
`vicione-38403ebd86af`. Its report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/exact-bc51b63c3/eventhub-route-exact-bc51b63c3-retry2.cobertura.xml`,
SHA-256 `b6d17ebdd3117719ddcddd9bb06d0e45d21652b5f64305bae9d9c56c20939be6`.
`SetActivityRoute` remains at 12/12 lines, all reported branches, complexity 12, and CRAP 12.
The exact fixture findings, broker logs, and endpoint projection are retained next to the
report; the broker log hashes match the findings file. This run also has no test-result file.

The final same-commit fixture run added xUnit TRX test-result output, passed 57/57, and had empty
fixture findings, run `vicione-8e28552b6a45`. The retained
`artifacts/coverage-a-plus-20260922-8abfe1e8a/exact-bc51b63c3/trx-run/eventhub-route-exact-bc51b63c3.trx`
has SHA-256 `7dd0aec10d092466579d402bc1c5f9a8d502c48146d7aa1b4c33e9db7342b875` and
machine-readable counters: total, executed, and passed are 57; failed, error, and notExecuted
are zero. The same run's Microsoft CodeCoverage report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/exact-bc51b63c3/trx-run/eventhub-route-exact-bc51b63c3-trx.cobertura.xml`,
SHA-256 `3c04ba246f70d8cd00e7f1ed98662267d3d21a527229473ee133529aac4fd810`, and confirms
`SetActivityRoute` at 12/12 lines, all branches, and CRAP 12. Its fixture findings, broker logs,
and endpoint projection are retained in `trx-run/`; log hashes match the findings file. No
product source changed in this slice.
