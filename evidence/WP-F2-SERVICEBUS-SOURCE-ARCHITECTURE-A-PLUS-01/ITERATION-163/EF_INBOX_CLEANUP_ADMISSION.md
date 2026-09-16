# Iteration 163 — EF inbox cleanup admission

## Result

This packet personally reads the complete EF inbox cleanup source (181 lines) and its complete
owning test file (316 lines). The retained change extracts failure-path rollback into an internal
helper which always uses a non-cancelable rollback token and suppresses a secondary rollback fault.
The primary cleanup failure therefore remains authoritative even when caller cancellation or a
broken provider transaction prevents rollback. This does not expand the public API.

The lock-result contract is now explicit for all scalar outcomes: only numeric `1` grants cleanup
ownership. `null`, `DBNull` and every other numeric value return no progress and preserve all inbox
rows. The existing strict retention boundary and configured batch limit remain unchanged.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 1 / 181 | `154db252d2e509de7bf2c12cf05bc81ff41689b9c9aaf87b60733d36606327d9` | `2dd485289fe2f46b5c73c8c0680bae4b9e8e5715b16e9ab2edd293e1429cdd30` |
| Tests | 1 / 316 | `d56b048e6005e3ce89c984d4f1d40062ee2195b2abb01f70725701bb86cc7500` | `b659ba21e008af37ae1a74e712a65acd5f3d8610bfbe50e3bee97acb15db3244` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 162 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `InboxCleanupService.cs` | `073288c25cf1c2acebed89cf420cfa761b7fe432dc41c71d09ca30232fdc5651` |
| Test | `InboxCleanupServiceTests.cs` | `0d11bd550d5a11ffb9c9e101bf7922fc3836fffd961134228a0cb41c9de35bc5` |

Cumulative personal source admission is 290/4,116 current C# files.

## Proof

The focused suite contains nine discovered cases. The added three-row theory proves fail-closed
behavior for `DBNull`, no scalar row and a non-owner numeric value. The rollback case proves null
validation, successful rollback and suppression of a secondary rollback failure. Two requirement
projection records bind the new variants to compiled metadata.

Six compiled single-cause mutants were killed and restored: admit `DBNull`, admit a non-`1` lock
result, remove transaction null validation, rethrow a secondary rollback fault, make the retention
boundary inclusive and remove the batch limit. Each was built successfully before the responsible
focused test rejected its changed behavior.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-163-final.cobertura.xml`,
SHA-256 `5f73b40b9210047a377107daa44c19ed46d7d63633eeddae6e5561af8e9112bf`.
The source reports 83/90 executable lines and 29/32 branches covered. Lock acquisition and the
extracted rollback state machine each report 100% line and branch coverage. Across nine reported
methods/state machines, maximum CRAP is 10.20 and none exceeds 30. Unit sorted-display-name SHA-256
is `6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`.

| Gate | Result |
| --- | --- |
| Focused inbox cleanup | 9/9 passed |
| Full EF unit | 246/246 passed |
| Strict Release product/EF/unit/local builds | 0 warnings, 0 errors |
| Product/test format | Exit 0 |
| Core Release | 4,799/4,799 passed with suite parallelism disabled |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
The broader EF-runner coverage still includes non-owning assemblies, so its zero-hit dependency
methods are not treated as local inbox-cleanup gaps; whole-fork coverage must ultimately merge the
owning assembly runs. Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not
enumerated, read or modified. Intended tag:
`servicebus-a-plus-iteration-163-ef-inbox-cleanup-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open.
