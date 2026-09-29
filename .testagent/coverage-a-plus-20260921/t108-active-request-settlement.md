# T108 — ActiveRequest settlement ownership

Implementation commit: `06f28cb78`.

T97 `3cb94a285` remains the latest product-wide Line/Branch/CRAP checkpoint.
This packet makes no new global grade claim.

`ActiveRequest` previously called `EndRequestAsync` for every completion and
used a non-atomic completed flag during disposal. A second completion could
decrement the active count and release a second request permit; completion
after disposal or racing disposal could do the same. The correction gives
completion and disposal one atomic settlement claim. Only the winner updates
the owning algorithm's active count, pending result capacity and permit.

The first Red Team review found a second ownership path: the public
`ActiveRequest` constructor let external package callers invent an unowned
lease, which also released never-acquired capacity. A fourth red-first test
failed on that public constructor. It is now internal, so external callers
must use `RequestRateAlgorithm.BeginRequestAsync`. Friend assemblies retain
internal access; the API test claims only the external boundary. This is an
intentional source and binary API removal in the unreleased package.

Three manually authored xUnit tests first failed on the unchanged product:
duplicate and post-disposal completion were accepted, and concurrent
completion/disposal drove `ActiveRequestCount` to −1. On the corrected source
they pass 3/3. They check exception behavior together with owner counts,
permit blocking, and healthy successor requests. The race repeats 32 times
and permits either valid winner.

The fourth public-constructor test failed 1/4 against the first correction,
then passed after the API boundary was closed.

Microsoft test-gap and assertion-quality review: a reintroduced unconditional
completion is killed by the duplicate-completion test; a missing disposal
guard is killed by the post-disposal test; the original non-atomic flag was
killed by the concurrent test. The fourth test checks public API shape and
proves that `BeginRequestAsync` still produces a functioning owned lease.
No test is assertion-free or designed only to touch a line.

The focused test run passes 4/4. The complete Abstractions project passes
958/958, with zero failures and zero skips. The full 33-profile product
measurement stays at the grouped-work cadence.

The existing `verify_developer_journeys.sh --update-public-api-contract`
gate passes from fresh packages: 18 developer journeys, 31 packages, four
isolated package consumers, and 30 runtime assembly APIs. The generated
`docs/api/packed-public-api.txt` diff removes exactly the unsafe public
constructor. Final Abstractions build has zero warnings/errors, and its
complete test run passes 958/958 after the final test/requirement naming.

Final independent read-only Red Team review: **PASS**, with no remaining
concrete P1/P2. It verified the narrowed external-caller API claim, exact
single-claim settlement, and the one-line generated API removal. The
concurrency test samples interleavings; the atomic compare/exchange in the
product code establishes the tested single-owner rule for every interleaving.
