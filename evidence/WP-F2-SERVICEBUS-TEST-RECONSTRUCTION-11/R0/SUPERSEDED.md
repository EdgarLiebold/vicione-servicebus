# Superseded R0 artefacts

Lead directive `DIR-A0071-R0-CORRECTION-01` did not approve the checkpoint at commit
`0e58fa5c8fb4f15a2b812176130d76aa69ea1d4f`. Four artefacts of that checkpoint are superseded by the
corrected versions under `R0-CORRECTION-01/`:

| Superseded path | Replaced by |
|---|---|
| `R0/SOURCE_OWNER_MAP.json` | `R0-CORRECTION-01/SOURCE_OWNER_MAP.json` |
| `R0/COMBINED_SEMANTIC_LEDGER.jsonl` | `R0-CORRECTION-01/COMBINED_SEMANTIC_LEDGER.jsonl` |
| `R0/OBLIGATION_SET_FROZEN.tsv` | `R0-CORRECTION-01/OBLIGATION_SET_FROZEN.tsv` |
| `R0/R0_FROZEN_RESULT.md` | `R0-CORRECTION-01/R0_FROZEN_RESULT.md` |

**They stay in the tree with their bound bytes.** Exchange record `0002-progress.json` cites all four
with SHA-256 values, and the orchestration validator requires every cited path to remain byte-exact
in the working tree — not merely retrievable from the commit that carried it. Superseded means
"no longer the effective artefact", never "removed".

## A conflict this creates, for Lead disposition

Directive item 4 requires `git diff --check ae73c6da748e3bc3257dffa4971ee8680e086207..HEAD` to be
clean. `R0/OBLIGATION_SET_FROZEN.tsv` represents its empty final field as a trailing tab in **543
rows**, so any range containing the commit that added it reports whitespace errors. Record 0002 binds
exactly those bytes. **The two requirements cannot both hold**, and the team may resolve neither on
its own: rewriting the file breaks a bound record, removing it breaks the validator.

Measured, not argued:

- `R0/OBLIGATION_SET_FROZEN.tsv` on disk hashes to `cd8d0cf22fbb4b63…`, identical to the value record
  0002 binds, and contains 543 rows ending in a tab.
- `git diff --check 0e58fa5c8fb4f15a2b812176130d76aa69ea1d4f..HEAD` — the range this correction owns —
  is clean.
- `R0-CORRECTION-01/OBLIGATION_SET_FROZEN.tsv` uses the canonical token `NONE`, has no empty final
  field and no trailing whitespace anywhere.

Three ways out, for the Lead to choose: accept the correction range as the scope of the cleanliness
proof; release the byte binding of record 0002 for these four paths so they can leave `HEAD`; or
re-record the evidence of record 0002 against the corrected artefacts.

An earlier version of this correction removed the four files from `HEAD` to satisfy item 4. That was
wrong and is reverted: it made the orchestration preflight fail on record 0002, which is the concrete
proof that the immutability rule is enforced against the working tree.
