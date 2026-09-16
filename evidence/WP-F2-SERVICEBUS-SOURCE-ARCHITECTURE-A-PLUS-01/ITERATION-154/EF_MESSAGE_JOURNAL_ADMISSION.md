# Iteration 154 — EF MessageJournal admission

## Result

This packet personally reads all six EF MessageJournal/configuration product files (353 lines) and
both owning unit/local test files (759 lines). The public relational store failed every valid append
on SQLite because SQLite could not translate the `DateTimeOffset` retention predicate. Persisting
`ObservedAt` as UTC ticks preserves the documented UTC instant and makes comparison, ordering and
indexing provider-neutral.

| Admission | Files / lines | Manifest | Chain |
| --- | ---: | --- | --- |
| Source | 6 / 353 | `082905d0ccebedeef83678eb51b151a02bea1803be5fcac4cf1928c6c4aaa64f` | `f340b292a610ff819e31da23b712bf532d35e2c1ba3f8139580e192574e05044` |
| Tests | 2 / 759 | `a1a174c922a402e6f0563c59846fc1c3782a2183f7d2f765da4835bb41651b7c` | `63c0429166f8fe2f9bffce02a0c092498c5e96635ec5bafa876229f3c3e69884` |

Chains extend Iteration 153. Cumulative personal source admission is 259/4,116 files.

## Proof

Six new SQLite cases cover provider-executable append, independent retention, capacity eviction,
null/oversize/cancellation rejection, duplicate-key rollback, earliest timestamp and both cache-key
overloads. Four compiled single-cause mutants—removed UTC conversion, inverted retention,
capacity off-by-one and inverted size guard—each fail their designated test and were restored.
The additions contain no assertion-free, trivial-only or self-referential test.

Final instrumentation covers 132/133 lines (99.2481%) and 16/16 branches across 18 methods; maximum
CRAP is 4 and none exceeds 30. Unit sorted-name SHA-256 is
`a5fd8ac3c5d9db12e896f1c07c09f818a2d7ee1606878104740cfbf1e1394253`; Cobertura SHA-256 is
`0d80602aebb673121cf5fbbca84b94951e8c9e509a03b3d0a89d3d275d7b8cc6`.

| Gate | Result |
| --- | --- |
| Full EF unit | 185/185 passed |
| Strict product/unit/local builds | 0 warnings, 0 errors |
| Product/unit/local format | Exit 0 |
| Core | 4,799/4,799 passed |
| Local requirement projection | 1/1 passed |
| PostgreSQL MessageJournal | 5/5 blocked before product behavior by missing `UnitArchitecture` settings |

No credentials were invented. Raw artifacts remain under `/private/tmp/vsb-iteration154-*`;
protected trees were not read or modified. Intended tag:
`servicebus-a-plus-iteration-154-ef-message-journal-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open.
