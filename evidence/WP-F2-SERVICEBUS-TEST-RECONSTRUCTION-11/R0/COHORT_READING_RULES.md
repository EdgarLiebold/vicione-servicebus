# R0 cohort reading rules

Binding for every internal read-only agent of Team 1 in wave R0.
Work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`, baseline commit
`ae73c6da748e3bc3257dffa4971ee8680e086207`, tree `e5897e7632be4f491e01d51221ee59081d4d2aa0`.

## 1. Role

Read-only census and semantic disposition of one assigned cohort. No agent writes,
moves or deletes any product, test, tool or build file. The only writable location is
`evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0/cohorts/<COHORT_ID>/`.

## 2. Completeness (TLP-017)

Sampling is a protocol violation. The cohort is closed only when the set of files read
equals `git ls-files <scope>` exactly, in both directions. Every agent emits
`READ_MANIFEST.tsv` (`path<TAB>sha256`, sorted, `LC_ALL=C`) covering its full scope;
the integrator reconciles it byte-for-byte against the baseline manifest.

## 3. What a ledger row is

A row is one behavioural test obligation, not one method name. Parameterized cases keep
their resolved variant list. For every discovered case record:

- the observable behaviour under test, its preconditions and its externally visible effect;
- fixture inheritance, `SetUp`/`TearDown` effects, observers and shared state actually reached;
- boundaries, negative paths, expected exceptions, timeout, recovery and concurrency semantics;
- the old execution state (`Explicit`, `Ignore`, platform condition, infrastructure need);
- the assertion intent: which fact each assertion establishes.

## 4. Forbidden deletion rationales (§9 of the Lead plan)

None of the following alone justifies a `REMOVED` disposition: test count, coverage,
compilation success, text similarity, empty method body, missing direct assertion,
`Explicit`, `Ignore`, platform condition or missing infrastructure. An unclear obligation is
`QUESTION`, never a silent drop. `BLOCKED` and `QUESTION` are non-terminal.

## 5. Terminal dispositions

`REPLACED_EXECUTING`, `REMOVED_WITH_PRODUCT_CAPABILITY`, `BENCHMARK_ONLY`, `DIAGNOSTIC_ONLY`.
In R0 no disposition can yet be `REPLACED_EXECUTING` (no new test exists); R0 proposes the
target owner, target project and profile and marks the row `PROPOSED_<disposition>`.

## 6. Identity reconciliation

Where the cohort maps to one of the eleven inherited anchors under
`build/verification/expected/`, reconcile per identity: anchor identities not covered by a
ledger row, and ledger rows without an anchor identity, are both listed explicitly and
explained. Differences are reported, never adopted as the new truth.

## 7. Output contract

Per cohort, under its own directory:

- `READ_MANIFEST.tsv` — proof of complete reading
- `LEDGER_DRAFT.jsonl` — one JSON object per obligation, fields:
  `obligationId, sourceProject, sourceFile, sourceSymbol, discoveredCase, behaviorContract,`
  `variants, boundaries, negativeAndFailurePaths, oldExecutionState, targetOwner,`
  `targetProject, targetTests, profile, assertionIntent, disposition, evidenceRun, reviewer, notes`
  (`targetTests` and `evidenceRun` stay empty in R0; `reviewer` stays empty)
- `RECONCILIATION.md` — anchor comparison, deviations, open questions
- `FINDINGS.md` — semantic gaps, suspicious constructs, everything needing a Lead QUESTION

`obligationId` is stable and derived as `OBL-<COHORT_ID>-<4-digit sequence>`.

## 8. Reporting honesty

Numbers name their derivation, scope, commit and hash. An unread file is reported as unread.
A guess is marked as a guess. No agent reports its cohort closed while any file is unread or
any anchor identity is unexplained.
