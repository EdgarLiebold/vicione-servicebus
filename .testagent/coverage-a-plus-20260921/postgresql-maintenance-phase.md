# PostgreSQL scheduled maintenance behavior test

## Product contract

`ScheduledMaintenance_RemovesOrphansAndPreservesADeliveredMessageAsync` starts
the real PostgreSQL transport against an isolated fixture database. Before the
maintenance bus starts, it proves that two messages have no delivery and that
a third message sent through the product has one delivery. After the scheduled
maintenance pass, both orphan IDs must be absent and the delivered message
must still have its delivery. The test never invokes the cleanup SQL function
itself. Removing the product's cleanup call, performing only one cleanup pass,
or deleting the live message would make these assertions fail. The configured
batch size of one encourages repeated passes; the test does not claim to prove
the exact per-pass batch limit.

## Validation

- The PostgreSQL provider project built in Release with zero warnings and
  errors. The new test passed 1/1 with Microsoft CodeCoverage under canonical
  PostgreSQL fixture `vicione-0bc9a7558590`, with empty findings.
- The complete provider suite passed 79/79, with zero failures or skips, under
  fixture `vicione-8dfb31cc8da2`, also with empty findings. This includes the
  compiled Requirements projection test for the new mapping.
- The target generated `MaintenanceAgent` retry callback moved from 0/8
  covered lines and CRAP 156 in the last complete product profile to 8/8
  covered lines and CRAP 12 in the full provider report. This is a targeted
  post-profile result; the 36-report product-wide aggregate at `4488b29fe`
  predates this added test and is not a current global A+ claim.
- The focused report SHA-256 is
  `215d7b576ca8ba2ef490e3c5bdd787fef44c7fa73cde6ee55164865cf5e9567c`;
  the full provider report SHA-256 is
  `e3d34fd8e039489ac28f514222ae50a37bdfb9f7725842cdd0ebcf668e647c45`.
  Both reports are under
  `artifacts/coverage-a-plus-20260922-4488b29fe/postgres-maintenance-phase/`.
- A first filter attempt selected zero tests and its report was quarantined in
  `failed-filter-attempt/`. It was corrected to the xUnit-v3 MTP wildcard
  method filter before either passing run. It contributes no coverage claim.
- Independent read-only adversarial review returned PASS. It confirmed the
  fixture isolation, source-to-database causality, initial and final state
  assertions, bounded wait, and Requirements mapping. No concrete defect
  remains. Global A+ remains open.

## Exact source/test commit

Commit `eb04d4283c95ae77ccd81d35265dde834b93f04e` was checked in a
separate clean detached worktree with no tracked diff. Its locked restore
succeeded, Release build had zero warnings and errors, and the complete
PostgreSQL provider suite passed 79/79 with zero skips against the canonical
fixture. The fixture run identity was `vicione-2bb3508578aa`; findings were
empty and the copied broker log SHA-256 matches its finding record. The
temporary worktree was removed after the build, test log, fixture findings,
broker log, and endpoint projection were copied to
`artifacts/coverage-a-plus-20260922-4488b29fe/exact-eb04d4283/`.
Before restore and after the test, `git rev-parse HEAD` in that worktree
returned the full commit above and `git status --porcelain --untracked-files=no`
returned no lines. Those two command outputs are retained in this turn's tool
transcript, not in a separate provenance file under the artifact directory.
The exact test log SHA-256 is
`85949ce800939249478d31a3c7f8a14aff29c2a68194bd867aad3b4b8095c961`;
the broker log SHA-256 is
`957ff79b16d00a8c16c8f11194be0d06483dbf42e9f50663fac6f0567019bd43`.
The exact-commit run did not collect coverage; the two targeted coverage
reports above came from the same source/test bytes before committing.
Final independent read-only evidence review returned PASS using the retained
logs and this turn's pre/post HEAD and clean-status tool outputs. A standalone
archive of the artifact directory should retain the turn transcript to preserve
that worktree provenance.
