# Source first-read reconciliation — 2026-09-19

This note reconciles the committed raw Git remainder with existing *current-byte* reading records. It does not replace `../source-read-remainder.txt`, the PO's raw control list, and does not claim A+ acceptance.

## Scope and arithmetic

- Denominator: 4,207 tracked paths currently under `src`; no untracked `src` paths at this checkpoint.
- PO convention: a current path edited at or after `e01a5e5eb3411412229221bd58b170b583ce6caa` (including working-tree changes) counts as read. Its complement is the 100 paths in `../source-read-remainder.txt` (87 C#, 13 non-C#). That convention alone does not independently prove a human/agent reading act.
- Of those 100 raw paths, 50 already had current-byte SHA-256 records in `iteration242/source-admission.tsv`. Three more had current-byte records in `iteration142`, `iteration143`, or `iteration147`. Hence **100 - 50 - 3 = 47** effective paths were still to read when the user challenged the incorrect “0 open” report. Those 47 comprised 34 C# and 13 non-C# paths.
- The Lead then read all 34 C# paths in full and recorded them in `remaining-csharp-source-read.tsv`. A separate, disjoint Sol 5.6 xhigh agent read the 13 non-C# paths in full and recorded them in `noncsharp-source-read.tsv`. A fresh SHA-256 verification over these two ledgers and the prior `source-admission.tsv` records matched **all 100 of 100** raw remainder paths, with zero stale hashes or unproven paths.

The previous “0 open” statement preceded this reconciliation and incorrectly used the later `944d0e235` historical comparison as if it were the PO's `e01a5e5e` start. The subsequent claim of 100 remaining ignored 53 already documented current-byte reads. Neither arithmetic should be used again. At this checkpoint the first-read balance is zero under the stated convention **and** all 100 raw exceptions have current-byte read evidence. This closes only the first-read balance, not per-file A+ dispositions, API review, coverage/CRAP, provider matrix, or remote verification.

## Recheck rule

Before reporting the balance again, recompute the current tracked `src` set and Git complement from `e01a5e5e`, then intersect that complement with per-file reading records only when their SHA-256 equals the current file bytes. Changed or newly added paths must return to the queue unless their current version is newly read and recorded. Preserve the 100-path raw list as a historical checkpoint rather than silently deleting it.
