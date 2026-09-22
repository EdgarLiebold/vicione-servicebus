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
