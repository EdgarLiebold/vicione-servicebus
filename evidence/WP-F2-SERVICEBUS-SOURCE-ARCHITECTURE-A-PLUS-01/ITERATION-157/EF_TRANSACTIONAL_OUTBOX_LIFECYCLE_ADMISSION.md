# Iteration 157 — EF transactional-outbox lifecycle admission

## Result

This packet personally reads all four classic EF transactional-outbox lifecycle files (353 lines)
and both owning test files (755 lines). The retained implementation already serializes session
mutation, couples business changes and ordered outbox intent in one `DbContext`, signals externally
committed batches exactly once and fails loudly when an active session is disposed. No functional
product correction was required.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 4 / 353 | `162cd6d2a9e34b00fc1061f2afa30836bcc80408de6a1853688d52ca3874c309` | `a417cb59fea1f1a672b71ab32bb83b8adf717126498badc3cb81bc5b628895af` |
| Tests | 2 / 755 | `629d36746a768472cfd447797c2827a0440273f6d9f0c30c03234d9a5490bddc` | `a558c852979f8a4cb615a2be647d9f8814ec3e0949428a0e79250f3ac5f78680` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 156 source/test chains with the corresponding manifest
hash. Cumulative personal source admission is 276/4,116 current C# files.

## Proof

Fourteen new tests cover every constructor dependency, stable lazy scoped endpoints, provider
resolution, same-context business/outbox commit, business-only commit, abort and uncommitted
disposal isolation, external-save completion and DbContext-first disposal, 32-way concurrent
writes, successive batches, tracker-accepted state rollover, preservation of foreign
transactional and inbox intent, cancellation, post-disposal rejection, ambient consume-context
endpoints and both coordinator input/busy boundaries. Requirement projection entries bind every
new method. Assertions observe persisted records, tracker state, reference identity, notification
counts, diagnostics and disposal behavior; none is assertion-free, trivial-only or
self-referential.

Six compiled single-cause mutants were killed and restored: remove the `SavedChanges` hook, remove
the persisted bus key, remove the delivery signal, detach foreign outbox messages, suppress the
coordinator busy failure and create a fresh outbox state for every staged message.

Final instrumentation covers 100% of executable lines and branches in the transactional scoped
context, consume-context subclass and write coordinator. The interface has no executable code.
Maximum owner-method CRAP is 14 and none exceeds 30. Unit sorted-name SHA-256 is
`2d62dd9089f9120760883bd31b565c410ccb81427c43a9d855c5be2783cb45ff`; Cobertura SHA-256 is
`493c43004223fe74bdc6cf9e52945731e51e1f818be41ad5a66096e92820ab66`.

| Gate | Result |
| --- | --- |
| Focused owners | 23/23 passed |
| Full EF unit | 214/214 passed |
| Strict product/unit/local builds | 0 warnings, 0 errors |
| Product/unit/local format | Exit 0; no changes |
| Core | 4,799/4,799 passed with suite parallelism disabled |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race in
one batching integration test; the complete serialized gate is green. Raw artifacts remain under
`/private/tmp/vsb-iteration157-*` and
`/private/tmp/vicione-servicebus-iteration-157-final.cobertura.xml`; protected trees were not read
or modified. Intended tag:
`servicebus-a-plus-iteration-157-ef-transactional-outbox-lifecycle-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open.
