# Iteration 159 — EF inbox-cleanup portability

## Result

This packet personally reads the complete EF inbox-cleanup worker (176 lines) and its new owning
test file (256 lines). Real SQLite execution exposed that the retained
`OrderBy(...).Take(...).ExecuteDeleteAsync()` expression could not translate, so cleanup faulted
instead of removing expired duplicate-detection rows. The corrected worker selects a bounded,
oldest-first ID window using the provider-compatible ordering already established by the EF
outbox API, applies the exact UTC cutoff in memory and deletes only those selected IDs inside the
same transaction and ownership lock. Construction now also rejects a missing lock provider and an
undefined isolation level before the hosted worker starts.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 1 / 176 | `c4d712e98af40634ff9dddb2db143ea1a2f29bda62ab84d7408806194e60ccae` | `b184f2a37f952cfaf7fef2cb6227cafeaf5aba2fcb2eccf81dbdb77871e4bad7` |
| Tests | 1 / 256 | `5bc9fa015672cf5da13e9f7764eca36ab4e2ce505829c09ca5d4f808722d6a2b` | `8c30fa40d884fa2bda93b518f4f9ab638e40430a7e77864b0d00154f67532da1` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` record with a terminal
newline. Chain hashes extend the corrected Iteration 158 source/test chains. Source content hash is
`560beb718d6d1a25bcc79c1d1d9784551f306caf5d06ff19f9e55eb94d6e2bce`; test content hash is
`1eacfdd671cf454e91aeb584f6cea457f0086760d0adac3a1e74fb081f7d3132`.
Cumulative personal source admission is 278/4,116 current C# files.

## Proof

Five focused cases cover every constructor dependency, real SQLite cleanup, oldest-first batch
limits across successive calls, strict cutoff semantics, retention of current and undelivered
rows, unavailable ownership locks, caller cancellation and the hosted start/work/stop loop.
Requirement-projection entries bind all five methods. Assertions observe exact counts and durable
row identities; none is assertion-free, trivial-only or self-referential.

Six compiled single-cause mutants were killed and restored: accept a missing lock provider, accept
an undefined isolation level, invert lock ownership, exceed the configured batch limit, invert the
retention cutoff and remove the SQLite canonical-text ordering branch. The final mutant reproduced
the original provider exception for `DateTimeOffset` ordering.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-159-final.cobertura.xml`,
SHA-256 `f1595786a53d6dbcd73c60e17c0ad6633b1621db28dde9c804ef285c5b087d81`.
The hand-written class has 100% line and branch coverage. The hosted-loop state machine has 78.95%
line and 100% branch coverage; the cleanup-attempt state machine has 73.81% line and 70% branch
coverage. Residual lines are the non-SQLite ordering arm, operational-error logging, defensive
rollback and rollback-failure preservation. The SQLite success, no-lock, empty, repeated-batch,
cancellation and lifecycle contracts are executed. Maximum owner-method CRAP is 11.80 and none
exceeds 30. Unit sorted-name SHA-256 is
`a2eb923a7009d6a99ba0cc74ae9fe4aaeb4f0888932dc15413eb1ad9fe0293bb`.

| Gate | Result |
| --- | --- |
| Focused owner | 5/5 passed |
| Full EF unit | 230/230 passed |
| Strict product/unit/local builds | 0 warnings, 0 errors |
| Product/unit/local format | Exit 0 |
| Core | 4,799/4,799 passed with suite parallelism disabled |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-159-ef-inbox-cleanup-portability-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open.
