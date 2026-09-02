# WP-F2 ServiceBus test reconstruction — final local validation

## Verdict

PASS. The complete inherited Azure Service Bus and RabbitMQ test obligations are terminally mapped.
Every executing obligation has a native source-owner xUnit 4 / Microsoft Testing Platform 2 carrier;
real-cloud-only obligations remain explicitly external and are not counted as green.

## Frozen subjects

- Mutation technical subject: `b2529849c6042429845245ee60678651aa3e92f5`, tree
  `de59387f7e5a8e0a358c47195bbb95639eecd24f`.
- Final technical subject: `cfec5cf5887efa2ac0aa4d0ac4db220743e8232d`, tree
  `c7bfa85926ca0f151543c19626e942cf809382de`, direct parent `b2529849...`.
- The final child changes only six indentation positions in the surviving SQL runner unit test. It
  changes no product path, provider-profile path or mutation target. The complete Engineering build
  and UnitArchitecture profile were rerun after that correction.
- The nine-commit range from `5be125d1...` changes 226 paths: 30 added, 164 deleted, 31 modified and
  one rename. `git diff --check` passes. `build/verification/expected/core.txt` is unchanged.

## Native execution

- UnitArchitecture: 22 CTRFs, 2,900/2,900 passed, zero failed/skipped/pending/other, floor 2,850.
- LocalIntegration: nine CTRFs, 338/338 passed, zero failed/skipped/pending/other, floor 326.
- SqlServerLocalIntegration: one CTRF, 60/60 passed, zero failed/skipped, floor 60.
- AzureServiceBusLocalIntegration: one CTRF, 24/24 passed, zero failed/skipped, floor 24.
- RabbitMqLocalIntegration: one CTRF, 17/17 passed, zero failed/skipped, floor 17.
- Total native executions across the five disjoint profiles: 3,339, with zero failures and zero
  skips. Every provider fixture is run-scoped, loopback-only, torn down, and records no finding.

The four provider-bearing profiles were executed at mutation technical `b2529849...`. The final
correction changes only a UnitArchitecture SQL runner test and no project in those provider profiles;
their source inputs are byte-identical at final technical `cfec5cf5...`. UnitArchitecture and the
complete Engineering build were rerun at the final technical subject.

## Obligation closure

- Azure Service Bus: 153 total = 58 UnitArchitecture + 49 AzureServiceBusLocalIntegration + 46
  `EXTERNAL_PENDING`. No external row is counted as executing or green.
- RabbitMQ: 354 total = 191 UnitArchitecture + 162 RabbitMqLocalIntegration + one
  `EXTERNAL_PENDING`. No external row is counted as executing or green.
- The inherited Azure Service Bus project, inherited RabbitMQ project and legacy TestInfrastructure
  project are absent. No empty inherited test directory remains.

## Mutation closure

- Azure Service Bus M01–M12: 12/12 independently reconstructed one-cause mutations killed by 12
  causal failing owner cases, zero pass/skip.
- RabbitMQ M01–M15: 15/15 independently reconstructed one-cause mutations killed by 21 causal
  failing owner cases, zero pass/skip.
- `FINAL/verify_mutations.py` reconstructs every mutant from the frozen baseline bytes, checks exact
  occurrence/index, mutant SHA-256, post-restore SHA-256, build evidence, CTRF counts and fixture-log
  hashes. Its final result is 27/27 PASS.

## Build and repository gates

- Locked Engineering restore succeeds.
- Complete Engineering Release build succeeds with zero warnings and zero errors.
- Scoped whitespace verification passes for all 27 added or modified C# paths. A deliberately
  unscoped diagnostic encountered inherited repository-wide formatting debt and is excluded from the
  acceptance evidence; no formatter changed product bytes.
- Verification model passes; CI tool self-tests pass 266/266.
- Identity self-tests pass 148/148. The generated CHANGELIST validates after all 194 Evidence files,
  including ignored raw logs force-added as evidence, are staged. The final SHA-256 inventory binds
  all 193 non-manifest files and is verified before commit.

## Transparent diagnostics

- The first RabbitMQ fixture start encountered a local `.erlang.cookie` permission error. A fresh
  isolated rerun passed 17/17; the initial broker diagnostic is retained separately and is not counted.
- One successful 60/60 SQL Server run could not write its wrapper because its requested directory did
  not yet exist. The complete run was repeated with a valid wrapper, CTRF and fresh fixture; only that
  repeat is counted.
- Redundant first-pass results remain recoverable under ignored `artifacts/superseded-evidence` and
  are not part of this Evidence package. Run-root tokens are not committed.

This is a local immutable review candidate after the Evidence commit is created. Independent review,
architecture binding and any remote push remain separate actions; no remote push is performed here.
