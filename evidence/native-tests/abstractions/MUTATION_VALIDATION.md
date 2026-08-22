# Abstractions mutation validation

## Subject

- technical commit: `61f6b133f1ef0a7178488813d6aa95212fefbeef`
- technical tree: `d51d6ac4c585762a34960e1d6276f658281aab8c`
- execution: disposable detached worktree; every mutation was restored before the next one
- positive floors: `UnitArchitecture` 213; `LocalIntegration` 3

## Results

| Mutation | Expected owner | Observed result |
|---|---|---|
| Invert `ExceptionSpecification` filter semantics | Abstractions exception-filter tests | Build passed; three behavior tests failed. |
| Advance `NewIdGenerator` sequence by two | Abstractions NewId tests | Build passed; five ordering/generator tests failed. |
| Remove the requirement attribute from the known-guid test | Requirement projection | Build passed; the projection rejected the missing compiled requirement variant. |
| Remove one formatter `InlineData` row | MTP minimum-test policy | All remaining 212 cases passed; MTP returned exit code 9 because the accepted floor is 213. |
| Replace a projected formatter method with a nonexistent method | Requirement projection | The projection reported the missing compiled method and the now-unprojected real method. |
| Return five instead of six hostname-derived worker-id bytes | LocalIntegration worker-id test | Build passed; exactly the six-byte contract test failed, with actual length 5. |

## Acceptance

The cohort rejects changed product behavior, sequence drift, missing coverage metadata, a silently
removed Theory case, a false projection identity, and host-dependent contract drift. No mutation or
generated artifact remains in the technical checkout.
