# Analyzer mutation validation

## Subject

- technical commit: `bd43857706d9e58b7a21443b4537d636dc435b9b`
- technical tree: `e29c54e12fd0ce64249eb975fbe37cdc0dc8328d`
- execution: disposable detached worktree; every mutation was restored before the next one
- positive UnitArchitecture floor: 250

## Results

| Mutation | Expected owner | Observed result |
|---|---|---|
| Remove `AsyncMethodAnalyzer` syntax-node registration | Analyzer behavior tests | Build passed; exactly the four unobserved messaging-operation cases failed and the exclusion case remained green. |
| Replace the selected consumer-context token with `CancellationToken.None` | CodeFix behavior tests | Build passed; all three token-insertion cases failed on exact fixed source and the unsupported case remained green. |
| Remove the `[Fact]` marker from the Analyzer projection test | MTP minimum-test policy | All remaining 249 cases passed; MTP returned exit code 9 because the accepted floor is 250. |
| Replace one projected Analyzer method with a nonexistent identity | Requirement projection | The projection rejected the missing method, the orphaned real test, and the unproved requirement variant. |
| Remove the Roslyn fixture compilation-error guard | Roslyn infrastructure test | Build passed; the invalid-fixture test failed because no exception was raised. |
| Accept an array for a dictionary contract property | Message-contract analyzer test | Build passed; exactly the incompatible-dictionary behavior case failed while both neighboring cases remained green. |

## Acceptance

The cohort rejects disabled analyzer execution, behaviorally incorrect fixes, a silently omitted
test, false requirement mappings, non-binding Roslyn fixtures, and relaxed contract compatibility.
The first attempt at the minimum-count mutation was discarded because earlier mutant binaries were
still present; the accepted result above followed a complete serial rebuild of the disposable Unit
solution and is isolated. No mutation, generated output, or disposable worktree remains in the
technical checkout.
