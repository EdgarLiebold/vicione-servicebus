# Message-contract analyzer mutation validation

## Subject

- technical commit: `b6d142473dbbd68316ce5dc166efbc52c1eb3e99`
- technical tree: `d28fa2930f25f0b5317063d5b04bc14ce4a9ad5b`
- execution: disposable detached worktree; each mutation was restored before the next mutation
- positive UnitArchitecture result: 368 passed, 0 failed, 0 skipped

## Results

| Mutation | Expected owner | Observed result |
|---|---|---|
| Remove the `custom-extension-method` scenario | Catalog integrity | Build passed; the catalog test failed with 47 instead of 48 scenarios. |
| Change the expected missing member from `CustomerId` to `CustomerId_BROKEN` | Analyzer behavior | Build passed; both direct-argument and local-variable cases failed on the exact diagnostic message. |
| Change the expected fix for `CustomerId` from `default(string)` to `default(int)` | CodeFix behavior | Build passed; both source forms failed on the exact added initializer. |
| Disable the local-variable form for `root-customer-id-missing` | Source-form closure | Build passed; the catalog test failed with 90 instead of 91 analyzer cases. |

## Acceptance

The native xUnit/MTP suite rejects an omitted semantic scenario, an incorrect diagnostic, an
incorrect CodeFix result, and the loss of a required source form. Each mutant failed at its intended
test boundary. The technical checkout remains unchanged; no mutant or generated mutation output is
part of the accepted tree.
