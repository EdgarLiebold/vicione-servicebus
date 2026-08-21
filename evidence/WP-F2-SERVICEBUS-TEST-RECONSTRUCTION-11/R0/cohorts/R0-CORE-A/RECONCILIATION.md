# R0-CORE-A — anchor reconciliation

Cohort `R0-CORE-A`, Team 1, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`.
Baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207` (verified as `HEAD` before reading; working
tree carried no modification under `tests/` or `src/`).

## 1. Scope and completeness (TLP-017)

| Item | Value | Derivation |
|---|---:|---|
| Files in `tests/ViciOne.ServiceBus.Tests/SagaStateMachineTests/` | 107 | `git ls-files <path> \| wc -l` at the baseline commit |
| Files in `tests/ViciOne.ServiceBus.Tests/Saga/` | 26 | same |
| Files in scope, total | 133 | same |
| Files read completely | 133 | every file in `READ_MANIFEST.tsv` was printed in full and read; no sampling |
| Lines read | 17,551 | `xargs wc -l` over the scope file list |
| Unread files | 0 | — |

`READ_MANIFEST.tsv` holds `path<TAB>sha256` for all 133 files, sorted with `LC_ALL=C`.
Its own SHA-256 is `d2e1700c18782f69277eefc85884e0459d24faef81f35f2fef7cc74c4ec78dc6`.

The scope includes four non-`.cs` / non-compiled files, which were read but carry no test identity:

- `tests/ViciOne.ServiceBus.Tests/Saga/SagaTestContext.cs` — excluded from compilation by
  `<Compile Remove="Saga\SagaTestContext.cs" />` in `tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj`
  (line 14). It is dead code: namespace `ViciOne.ServiceBus.ServiceBus.Tests.Saga`, references
  `Castle.Windsor`, `WindsorIntegration` and `IServiceBus`, none of which exist in this repository.
- `tests/ViciOne.ServiceBus.Tests/Saga/RegisterUserSaga.hbm.xml` and `saga.nhibernate.cfg.xml` — NHibernate
  mapping and configuration for `RegisterUserSaga`; no NHibernate reference exists in the repository.
- `tests/ViciOne.ServiceBus.Tests/Saga/RegisterUserSaga.cs` and `Messages/RegisterUser*.cs`,
  `SendValidationEmail.cs`, `UserValidated.cs`, `UserRegistrationPending.cs`,
  `UserRegistrationComplete.cs`, `UserVerificationEmailSent.cs`, `SendUserVerificationEmail.cs`,
  `CorrelatedMessage.cs` — compiled, but no test in this cohort or anywhere in
  `tests/ViciOne.ServiceBus.Tests` references `RegisterUserSaga`.

## 2. Anchor

| Item | Value |
|---|---|
| Anchor file | `build/verification/expected/core.txt` |
| Anchor SHA-256 measured | `bc2910d3b7611aed036211fc69e67155bdbfe403ed8af5ccadfabc914ca865f5` |
| Anchor SHA-256 in section 9 of the Lead plan | `bc2910d3b7611aed036211fc69e67155bdbfe403ed8af5ccadfabc914ca865f5` |
| Match | yes |
| Total identities in the anchor | 1873 (1876 lines minus 3 documenting header lines) |

Filter applied: identities whose first namespace segment after `ViciOne.ServiceBus.Tests.` is
`SagaStateMachineTests` or `Saga`.

```
grep -v '^#' build/verification/expected/core.txt \
  | grep -E '^ViciOne\.ServiceBus\.Tests\.(SagaStateMachineTests|Saga)\.' | LC_ALL=C sort
```

| Segment | Identities |
|---|---:|
| `SagaStateMachineTests` | 399 |
| `Saga` | 16 |
| **Cohort anchor total** | **415** |

This equals the 399 / 16 / 415 stated in the assignment.

## 3. Census

The census was derived from the source files, not from the anchor. A parser walked each file,
tracking block-scoped and file-scoped namespaces, nested namespaces, class nesting and brace depth,
and recorded every method carrying a `[Test]`, `[TestCase]` or `[TestCaseSource]` attribute as
`<namespace>.<class chain>.<method>`.

| Item | Value |
|---|---:|
| Identities discovered in the 133 read files | 415 |
| Identities carrying `[Test]` | 415 |
| Identities carrying `[TestCase]` or `[TestCaseSource]` | 0 |
| Identities in files excluded from compilation | 0 |

## 4. Reconciliation, both directions

```
comm -23 census_ids.txt anchor_ids.txt   # in census, not in anchor -> empty
comm -13 census_ids.txt anchor_ids.txt   # in anchor, not in census -> empty
```

| Direction | Count | Detail |
|---|---:|---|
| Anchor identities matched by a ledger row | 415 / 415 | — |
| Anchor identities **not** covered by a ledger row | 0 | — |
| Ledger rows **without** an anchor identity | 0 | — |

**No deviation of any kind was found, so nothing was adopted or proposed as a change to the anchor.**

Every ledger row carries its anchor identity verbatim in `sourceSymbol`, and
`notes` records `anchor=build/verification/expected/core.txt`.

## 5. Ledger shape

415 rows, `OBL-R0-CORE-A-0001` … `OBL-R0-CORE-A-0415`, ordered by identity.

One row per behavioural obligation. In this cohort the mapping to identities is 1:1 because there is
no parameterized case: every one of the 415 identities is a plain `[Test]` method, so `variants` is
empty for every row. Where several identities together describe one observed sequence — for example the
seven `Observing_events_with_substates` cases, which assert the successive entries of one recorded
state-change list — the shared contract is stated in `behaviorContract` on each row and each row's
`assertionIntent` names the single fact that row establishes. That keeps the anchor mapping exact
while making the grouping visible.

Two contract fields carry deliberately redundant grouping information in `notes`:

- `environment=` — `hermetic-none` (336), `hermetic-inmemory` (72), `hermetic-harness` (7)
- `sagaSemantics=` — the saga semantics tags used by the Lead's risk view

`profile` is `UnitArchitecture` for all 415 rows. `targetProject` is
`ViciOne.ServiceBus.Tests` for 406 rows and `ViciOne.ServiceBus.StateMachineVisualizer.Tests` for 9.

## 6. Dispositions proposed

| Disposition | Rows |
|---|---:|
| `PROPOSED_REPLACED_EXECUTING` | 411 |
| `QUESTION` | 4 |

No row is `REMOVED_WITH_PRODUCT_CAPABILITY`, `BENCHMARK_ONLY` or `DIAGNOSTIC_ONLY`. No row was dropped
for test count, coverage, empty body, missing assertion or missing infrastructure.

The four `QUESTION` rows are listed in `FINDINGS.md` §1.

## 7. Open questions for the Lead

1. **Target project for the nine visualizer obligations.** `When_visualizing_a_state_machine` (3 + 3),
   `When_visualizing_a_state_machine_again` (1) and the two `Telephone_Sample.Visualize.Draw` cases
   assert the output of `StateMachineGraphvizGenerator` / `StateMachineMermaidGenerator`, which live in
   `src/ViciOne.ServiceBus.StateMachineVisualizer`, over a `StateMachineGraph` produced by
   `GetGraph()` in `src/ViciOne.ServiceBus`. Section 5 of the plan gives each test project exactly one
   source owner. R0 proposes `ViciOne.ServiceBus.StateMachineVisualizer.Tests` and asks the Lead to
   confirm, and to decide whether the `GetGraph()` shape itself needs a separate obligation in
   `ViciOne.ServiceBus.Tests`.
2. **Two obligations depend on `TestConsumeContext`.**
   `Saga.Locator.Using_a_property_saga_query_expression` (2 identities) and
   `CorrelationExpression_Specs` (1) construct
   `src/ViciOne.ServiceBus.TestFramework/TestConsumeContext.cs`, whose 294 lines contain 22
   `NotImplementedException` throws. Section 10 of the plan names unimplemented `TestConsumeContext`
   paths as explicitly not to be carried into shared infrastructure. The rebuild therefore needs a
   decision: a minimal, complete consume-context fake owned by `ViciOne.ServiceBus.Tests`, or a
   different way to reach `PropertyExpressionSagaQueryFactory` and
   `EventCorrelationExpressionConverter`.
3. **`When_testing_concurrency_with_the_choir` depends on the fast-food-adjacent choir domain in the
   packable TestFramework** (`src/ViciOne.ServiceBus.TestFramework/Sagas/ChoirTest.cs`) and shares that
   type with `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests/Choir_Specs.cs`, which is
   outside this cohort. Per section 10 this domain saga is behaviour-specific and belongs in the owning
   test project, but two owners currently use it. The Lead must decide whether the concurrency saga is
   duplicated per owner or becomes a named shared capability.
4. **Three fixtures mutate process-wide product state and never reset it** (see `FINDINGS.md` §2.1).
   The rebuild cannot keep this; the Lead should decide whether the correlation-by-topology contract is
   rebuilt against a scoped topology or whether a non-parallel, resetting collection is acceptable.
