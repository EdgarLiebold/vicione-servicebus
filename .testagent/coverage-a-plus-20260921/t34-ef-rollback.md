# T34: preserve EF Saga failure during failed rollback

Base: `ad9160ffb70a1bf0a928f3c9abc72745203ebe5a`.
Full T34 measurement at `a019ca1b2` is complete; see the
[product-wide report](product-wide-profile-a019ca1b2.md).

`FailedRollback_PreservesPrimaryErrorAndReleasesOwnedContextAsync` exercises both
public LoadAsync and FindAsync on the optimistic Saga repository with real
SQLite transactions. Faults are enabled only after two distinguishable Saga
rows are seeded. Supported EF Core 10.0.12 interceptors fail the repository
SELECT and then reject rollback before the provider executes it.

Assertions require the identical primary exception, one started transaction,
the same context/transaction/transaction ID at failed SELECT and rollback, the
distinct cancelable caller token at SELECT, an uncancelable rollback token,
no commit and exactly one disposal of the factory-owned context. Subsequent
Find and both Load calls verify exact IDs and payloads and disposal of all fresh
contexts. The failed SELECT made no writes; this does not prove reversal of
previous writes or a real provider outage.

Microsoft code-testing-agent focused workflow and run-tests syntax apply.
Production factory and neighboring factory tests were completely read. The
local artifacts/t34-ef-rollback-plan.md contains the reviewed plan and installed
API verification. Read-only implementation review found no concrete blocker;
the requirement manifest maps both cases to REQ-VSB-EF-SAGA-FACTORY.

## Focused evidence

- MAIN: 2/2 passed, no skips, exit 0. Verify-only format exit 0.
- Secondary failure allowed to escape: both fail the exact exception-identity
  assertion, exit 2.
- Initial missing-rollback mutation failed compilation (CS8321, unused local
  function), exit 1. It is retained as a failed attempt, not test evidence.
- Revised mutation suppresses only the provider rollback: both fail the
  missing rollback observation, exit 2.
- Release omitted in both execution wrappers: both fail disposal count 0
  instead of 1, exit 2.
- All product mutations manually restored; isolated src diff is clean.
  Final isolated control: 2/2 passed, no skips, exit 0.

| Local file | SHA-256 |
| --- | --- |
| MAIN artifacts/t34-rollback-first.log | d6958407e24fa2cd843428e72cf02b3c0f24e1726077f3aa62b422335ef7e74e |
| MAIN artifacts/t34-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| EntityFrameworkSagaRollbackFailureTests.cs | e82bb899fc49f571f26d7f310d476e49729d513204db0aa4a5c52e57619c1330 |
| Isolated artifacts/t34-secondary-mutant.log | 9873fc22cac622581d241d4b941a13a51ecfe5e2249e94fe79475af65fe88a70 |
| Isolated artifacts/t34-missing-rollback-mutant.log | f78e337ae82a3430a36dfb31a9b550a4d5109d42b3572cafefbeb2f768cc2aeb |
| Isolated artifacts/t34-missing-rollback-valid-mutant.log | 0e82860d9cdd582614db3152d0c9c42427122897c43c151495a988c1325c4586 |
| Isolated artifacts/t34-release-mutant.log | ee496d6c5c49cbbd15d038171252c115446bee5e6323227d82b671cd82049975 |
| Isolated artifacts/t34-restored.log | 606077c084bacd7adf72272feb85151f6a479ab774c8b5c7c11995381481b771 |

Raw logs remain local. Isolated path: /private/tmp/servicebus-reply-investigation.
All 33 fresh exact-commit profiles and four fixture groups passed, 12,695 test
executions. RollbackAsync has 5/5 lines and CRAP 1. Independent aggregate review
confirmed 487 hashes, all counts and the physical line delta. This report closes
the packet's measurement and review. Neither the focused
result nor the complete aggregate constitutes product-wide A+ acceptance.
