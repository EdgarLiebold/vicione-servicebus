# Superseded R0 artefacts

Lead directive `DIR-A0071-R0-CORRECTION-01` did not approve the checkpoint at commit
`0e58fa5c8fb4f15a2b812176130d76aa69ea1d4f`. Four artefacts of that checkpoint are superseded and are
removed from `HEAD` in the correction commit:

| Superseded path | Replaced by |
|---|---|
| `R0/SOURCE_OWNER_MAP.json` | `R0-CORRECTION-01/SOURCE_OWNER_MAP.json` |
| `R0/COMBINED_SEMANTIC_LEDGER.jsonl` | `R0-CORRECTION-01/COMBINED_SEMANTIC_LEDGER.jsonl` |
| `R0/OBLIGATION_SET_FROZEN.tsv` | `R0-CORRECTION-01/OBLIGATION_SET_FROZEN.tsv` |
| `R0/R0_FROZEN_RESULT.md` | `R0-CORRECTION-01/R0_FROZEN_RESULT.md` |

**Their bytes are not lost and their bound hashes stay verifiable.** Exchange record
`0002-progress.json` cites these four paths with SHA-256 values; each is retrievable unchanged with
`git show 0e58fa5c8fb4f15a2b812176130d76aa69ea1d4f:<path>`, and the hash of that blob still equals
the value in the record.

Removing them from `HEAD` rather than rewriting them in place is what the immutability rule requires:
a correction never rewrites the earlier evidence carrier. It is also what makes
`git diff --check ae73c6da748e3bc3257dffa4971ee8680e086207..HEAD` clean, because the directive
forbids the trailing-tab representation of the empty final field that the superseded frozen list used
in 543 rows.

Every other artefact of `R0/` is unchanged and stays bound at its original path.
