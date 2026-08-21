# R0 convergence 06 — R0-C05-01 to R0-C05-04

Under `DIR-A0071-R0-CONVERGENCE-06`, Development Slice **revision 0003** bound by reslice `0014`.
No candidate commit hash appears here. Evidence bound by record `0015` is untouched.

## R0-C05-01 — active truth

| Was | Now |
|---|---|
| plan: revision 0002, reslice `0008`, `DIR-…-CONVERGENCE-03`, evidence `R0-CONVERGENCE-03/` | revision 0003, reslice `0014`, `DIR-…-CONVERGENCE-06`, evidence `R0-CONVERGENCE-06/`; the old values kept in one paragraph explicitly marked historical |
| status: reslice `0014` mixed with revision 0002 and reslice `0008` | revision 0003 bound by reslice `0014`; the blocker row marked historical |
| architecture deletion precondition 20 | 21 |
| EF Unit deletion precondition 52 | 55 |
| one `.gitattributes` exception, one convergence commit | two exact-path exceptions, each with its own reason; three retained correction commits after `dca20477` |

Verified over all **41** cohorts: matrix count, gate floor and deletion precondition each equal
the TSV projection for their target project. Matrix sum 3667.

## R0-C05-02 — the EF unit obligation is due in C1

`OBL-R0-PER-0119` stays `UnitArchitecture` in the EF Unit project and is due in **C1** with that
cohort and its single writer. The string "planned for C2" occurs nowhere in the effective ledger,
plan or status. C2 keeps only the EF LocalIntegration cohort at 127 entries. No count, profile,
target project, variant or carrier decision changed.

## R0-C05-03 — source provenance

Both new rows now carry `sourceFile`, `sourceProject` and `sourceSymbol` exactly as directed, and
`OBL-R0-PER-0119` enumerates the three `Shared/` implementations plus the historical fixed-name
dependency in `TestDatabase.cs`. **Zero** executable obligations have empty provenance.

## R0-C05-04 — decoded byte equality

The table encodes **only** the delimiter (`|` as `\|`) and line breaks. Ordinary backslashes are
never doubled: the ledger's `host\instance` round-trips unchanged. **Comparison unit: the decoded
cell value**, not raw Markdown bytes. All 21 rows compared: 0 differing cells.

## Invariants

| Invariant | Required | Measured |
|---|---:|---:|
| total entries | 3667 | 3667 |
| `UnitArchitecture` | 2766 | 2766 |
| `LocalIntegration` | 773 | 773 |
| `External` | 128 | 128 |
| target projects | 41 | 41 |
| anchor identities | 3114 | 3114 |
| architecture cohort | 21 | 21 |
| EF Core Unit cohort | 55 | 55 |
| EF Core LocalIntegration cohort | 127 | 127 |
