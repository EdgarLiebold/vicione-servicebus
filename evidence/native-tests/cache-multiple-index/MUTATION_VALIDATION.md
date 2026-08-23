# Cache index and removal mutation validation

Each mutation was applied alone to the working candidate, rebuilt in `Release`, exercised through
the native xUnit 4 / Microsoft Testing Platform 2 entry point, and immediately reversed with an
explicit patch. No mutation is part of the accepted source tree.

| Mutation | Native test owner | Result |
|---|---|---|
| Replace the atomic value exchange in `BucketNode.TryEvict` with a read | removal validity and visible-cache state | killed: 5 total, 2 failed, exit 2 |
| Clear a removed node's `Next` link before bucket compaction | complete visible-cache traversal after removal | killed: 5 total, 1 failed, exit 2 |
| Remove a pending factory placeholder and report success | pending-removal truthfulness | killed: 5 total, 1 failed, exit 2 |
| Remove the pending-removal row from the embedded Core requirement projection | compiled requirement-projection equality | killed: 1 total, 1 failed, exit 2 |

The first three probes used the complete five-case `MultipleIndexTests` class. The projection probe
used the ordinary `RequirementCoverageProjectionTests` xUnit fact after rebuilding the owning test
assembly. The final unmutated candidate is accepted only by an unfiltered 818-case profile run.
