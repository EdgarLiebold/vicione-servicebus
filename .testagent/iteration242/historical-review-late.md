# Historical source-review reconciliation: iterations 121–241

This is a read-only reconciliation of older ServiceBus evidence against current `src` bytes, not a new source review or an A+ verdict. The available iteration directories in this range are 138–154, 181–223, 228 and 233; the other numbers have no local iteration directory. `review/**` and `TestResults/**` were not listed or read, and no .NET process was run.

## Countable, file-specific historical record

Exactly **one additional current-byte path** has an unambiguous historical full-read and disposition record. Iteration 148 is a one-file owner packet: its tracked `source-admission.tsv` names the sole file and SHA-256; `research.md` describes that file's validation, provider resolution and forwarding behavior and explicitly reports no reproduced defect; `status.md` records the personal full read and local admission without product or test changes. All three evidence files entered Git in commit `8cbdc3ab5531f00554529bf9041ec89a5f43b664`. The committed source blob at that commit equals the current source blob, and the current SHA-256 is the admission SHA-256. The exact row is in `historical-review-late.tsv`.

The existing paired-TSV checker still reports 341 because this historical record has not been rewritten into a `source-admission.tsv`/`source-dispositions.tsv` pair in iteration 242. It supports a logical lower bound of **342 distinct current-byte detailed records** when kept separate from the existing 341; it does not itself claim A+ acceptance, complete API mapping, whole-fork tests, or release readiness.

## Valuable evidence not promoted to strict per-file disposition

- Historical `source-admission.tsv` files in iterations 139–154, 228 and 233 contain 311 rows over 283 distinct paths. At this check, 263 distinct paths had matching current-byte hashes; 218 of those were outside the existing iteration-242 disposition set. These are full-read attestations, not individually reasoned disposition rows. The singleton above is included in those counts, not additional to them.
- Iterations 214–219 provide 53 filename/SHA-256 lines in final source snapshots and packet-level admission language. Forty-seven snapshots still match current bytes, and 45 of those paths are outside the current strict pair set. The aggregate outcome cannot safely be converted into 45 file-specific reasons. Six snapshot hashes are stale. These are reconciliation candidates, **not counted** here.
- Iteration 220 prints ten source hashes but expressly qualifies its prior local admission after discovering that the required complete owning-test-project read had not preceded test editing. Iteration 221 explicitly says “not admitted”; iteration 222 says “no source or test admission yet”; iteration 223 and the 228/233 ledgers are source-only or hash checkpoints. None is promoted by this reconciliation.
- For other 181–219 packets, source inventories, commit changes, tests, coverage and “no unresolved finding” statements are useful context, but they do not supply an exact per-file manual-read **and** disposition record. Historical silence is **unknown**, not evidence that any file was unread.

Method: compare tracked historical evidence and Git commits to current `src` file SHA-256 values; treat exact bytes, explicit reading and explicit disposition as separate requirements. No generic edit or commit history was treated as A+ review evidence.
