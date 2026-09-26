# Complete product measurement, 2026-09-26

Measured commit: `2464cdc45eabf470664f287b05b713482cc6ea0a`.
Product tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Test tree: `68dbc8859088eac3215b2acd10388f9fcdb8eeb7`.
The documentation successor must not be represented as the measured commit.

## Measurement

| Item | Result |
| --- | --- |
| Profiles | 33: 18 unit, two portability, 13 local integration |
| Product assemblies | 32; none missing |
| Accepted executions | 12,536 passed; zero failed/skipped |
| Lines | 84,968 / 93,762 = 90.6209% |
| Conservative branches | 30,655 / 36,841 = 83.2089% |
| Methods | 26,061; zero CRAP > 30 |
| Methods with some uncovered lines | 4,540; retained in the complete gap inventory |

Cobertura does not identify taken branch arcs across reports. The branch figure
is a conservative observation, not an exact union. The source/PDB denominator
audit accounts for all 4,132 compiled product C# files: 2,775 measured and 1,357
without visible sequence points. No instrumentable source is omitted. That
earlier audit is reused because the product tree is byte-identical; its 32
representative assembly receipts are not the final execution-profile count.

The project defines acceptance through complete measurement, behavior-sensitive
assertions, gap/mutation review, applicable provider gates and no unresolved
critical test gap, as recorded in the September 19 research/plan. It defines no
percentage that alone means A+. Microsoft defaults of 80% lines and 70% branches
are analysis flags, not A+ grades. CRAP > 30 is the explicit risk threshold.

## Final behavioral packet

| Requirement | Concrete evidence |
| --- | --- |
| Preserve the original consumer failure when failure-state persistence or logging fails | `FailureStateWriteFailure_PreservesConsumerFailureAndRollsBackEveryEffectAsync`: two SQLite cases; original exception, empty tracker, no persisted business/inbox/outbox effects, one failure-write attempt; return-retry counterchange fails both. |
| Preserve terminal winners committed after rollback | `TerminalWinnerAfterRollback_PreservesEveryFieldAndSuppressesRetryAsync`: Consumed, Quarantined and Abandoned; every stored field and external progress counters asserted; missing terminal guard fails all three. |
| Cancellation must not become retry, quarantine or delivery | `ConsumerCancellation_PreservesTheOriginalTokenWithoutRetryQuarantineOrOutboxDeliveryAsync`: attempt limits one and three, actual prepared outgoing message, original exception/token and no outbox delivery; omitted cancellation catch fails both. |
| Reject invalid schedules before outgoing effects | `InvalidSchedule_RejectsSubmissionBeforeSerializingOrPublishingAsync`: four cases assert configuration key and zero serializer/publish calls; removed validation fails all four. |
| Preserve valid schedule boundaries and coordination fields | `ValidSchedule_PublishesOneCompleteCoordinationCommandAsync`: three positive cases assert the complete command and independent schedule copy; positive controls remain green under the negative-case counterchange. |

All counterchanges were removed and original product hashes verified. The final
EF subset passed 10/10; the combined InMemory/job/requirement subset passed 17/17.
Full Core and EF profiles then passed 6,466 and 310 cases in the final collection.
The internal read-only reviewer reports no critical open finding from its prior
reviews of this packet. This is not an external independent acceptance.

Limits: InMemory cancellation uses an explicit operation token. Job tests prove
the consumer's publish/serialization boundary, not native transport delivery or
the serializer implementation. SQL tests do not establish additional Windows,
Entra or Azure SQL identity behavior. No speculative tests were added solely to
increase the reported percentages.

## Gates and failed attempts

- Unit/Architecture initially passed 10,836/10,837. Thirteen local projects had
  not been restored in the isolated checkout, so their generated NuGet/MSBuild
  imports were absent. After successful restore, all 445 Architecture cases
  passed. This is a correction/recheck, not one entirely green initial run.
- The focused formatter check passed without rewriting files. Identity passed
  with zero findings and byte-identical license. CHANGELIST passed with 16,474
  entries in the clean checkout. Main's 79 untracked TestResults and 34 review
  files remain outside the delivery; the generator's initial main-checkout
  mismatch is explained by these 113 extra candidates.
- The initial Azure profile passed 29/30; cleanup canceled within the shared
  setup/operation budget. Unchanged isolated and full rechecks passed 1/1 and
  30/30. The isolated HTTP trace lost no events. The supplementary full trace
  lost 38,762 events and cannot prove complete causality. The original timeout
  cause remains unproven; no timeout or assertion was weakened.
- The first SQL profile passed 75/75, but the lead changed three documentation
  files during execution and the receipt's final clean-tree check rejected it.
  The exact patch was saved outside the repo and reversed. A new frozen profile
  passed 75/75 and issued the accepted receipt. Rejected runs are preserved and
  excluded from the aggregate.
- Earlier Engineering/package/API, artifact-identity and vulnerability results
  retain their original commit/run identity. The package gate covered 31
  packages, 18 journeys, four isolated consumers and 30 API baselines. Reuse
  depends on unchanged product/build/package inputs; these are not fresh runs
  of the final documentation commit and do not prove the all-repository API task.

## Reproducible evidence

Raw artifacts are local under the main repository's ignored `artifacts/` tree.
The accepted manifest preserves the original manifest and explicitly lists the
two rejected attempts. Every accepted receipt binds DLL/PDB, settings, runner,
report and log hashes; the aggregator validates these before merging.

| Artifact | SHA-256 |
| --- | --- |
| `coverage-profile-2464cdc45-all.json` | `12b91b6ad56f48ceea3536ba5a71c6514724da6140acfd5da801843f20870d58` |
| `t11-exact-count-audit.json` | `bf400eea8ab86ad3be9996194cbcdffe2415f41e8e53bf883083dfbff37f705e` |
| `t11-all-methods.json` | `5b25c24398c3fdeed53690c8beb0da8f9c813e035a96514626a802668c7b3a3d` |
| `t11-method-gaps.json` | `b2872d7e6d416d8b79b2afd733c29101f266a42e5614d706085528136c93bcf2` |
| `t7-source-denominator.json` | `8e67768ba234e92e05d29f6e62cb719d61b65b3a35dd6db13f6ace7fd57d6d54` |
| `t11-source-identity.json` | `1140e53f578ed628954da6f8e5fda8d1b3d2d4a3121f981b91b47515aba4d26b` |
| `t11-architecture-restored.log` | `1d6c28ca8cbc278768d5a22294613fcadeea62cb4481e62e764d9b0f81ac1315` |
| `t11-unit-architecture-initial.log` | `fbcd317fb35cb40409766b266087acce5c25d5b91def111cdf678a055d05f680` |

The separate internal read-only numerical/integrity counterreview passed. It
verified all receipt/report/log/binary hashes and reconstructed the assembly,
source, line, conservative-branch and method totals independently from raw XML.
The identity JSON and architecture log do not themselves embed a commit/tree;
their checkout attribution relies on the observed command sequence. The first
SQL run's documentation drift is recorded run history supported by the saved
patch and tool trace, not a standalone cryptographic proof of cause.

The generated CHANGELIST for this documentation successor contains 16,475
entries: one new report beyond the measured candidate's 16,474. This metadata
change does not alter the product or test trees.

The explicitly requested follow-up remains: Roslyn inventory of
the complete API and associated XML comments across all repositories, followed
by contract, consistency and documentation assessment.
