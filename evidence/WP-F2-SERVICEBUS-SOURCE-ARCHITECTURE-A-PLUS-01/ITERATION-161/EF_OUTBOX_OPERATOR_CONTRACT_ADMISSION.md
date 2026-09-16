# Iteration 161 — EF outbox operator-contract admission

## Result

This packet personally reads the public EF outbox operator contract and its complete implementation
(2 files, 191 lines), plus the final concrete operator test file (400 lines). No product correction
was required: the retained API is narrowly scoped to one bus/DbContext identity, follows the
repository's asynchronous naming convention and implements bounded deterministic inspection,
owner-isolated requeue and atomic discard semantics. The test contract now directly proves every
constructor dependency, identifier/page boundary, cancellation propagation, state projection,
failure reset, ownership boundary, unavailable-state outcome, concurrent winner and rollback guard.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 2 / 191 | `07c02d126947cdd08ce7c8b6362324b38cf3a495a0f76b073d1e47a32e16eba3` | `ddb7c06daafc8781e70c0f92204f9cafee140fbe5471be2e957388f699f14845` |
| Tests | 1 / 400 | `bc6c5ac66613cad2e3af085493066b32abdc21b4dbcc19e0f2ec2af4855400b0` | `60c12644beb9d2ea7543a17aa926515aa1289cc06b49f850dc7ccbd09696646e` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 160 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `EntityFrameworkOutboxOperations.cs` | `473f91d2fc11bbdfc2562902abb74cc453496afd3841ba0e94588d603bf8f773` |
| Source | `IEntityFrameworkOutboxOperations.cs` | `31d0ae4ff44b3d52884a5f3787e7c7f6d0b37a015bc24cdf18e77cc4abd951f6` |
| Test | `EntityFrameworkOutboxOperationsTests.cs` | `ba9a54e488e4b624c8ab3f5cd8b812bfc1e242cb23bb316ef7b791d0f55b9eea` |

Cumulative personal source admission is 288/4,116 current C# files.

## Proof

Five new focused cases prove null collaborators, empty mutation identifiers, the declared default
and exact maximum page size, pre-canceled caller tokens and transaction rollback when a claimed
discard row disappears. The rollback probe uses a scoped SQLite trigger to remove the claimed row
inside the operator transaction; the implementation throws its explicit fail-closed exception and
rollback restores the quarantined state. Five requirement-projection entries bind the new methods.
Together with the five retained cases, assertions observe exact state, ownership, ordering,
metadata, notification, exception and concurrency outcomes; none is assertion-free, trivial-only
or self-referential.

Six compiled single-cause mutants were killed and restored: reject the exact maximum page size,
bypass the empty requeue identifier guard, bypass the empty discard identifier guard, accept a null
notification, ignore the caller token in the quarantine query and remove the bus-key ownership
filter. Product sources are byte-identical to their admitted hashes after restoration.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-161-final.cobertura.xml`,
SHA-256 `8b9efafa9e8859f017433e2efd87a99c750d09b3ca80f05fe67c71422e37f5ee`.
Every implementation class/state-machine entry has 100% line coverage. All branches are covered
except the non-SQLite ordering arm of `GetQuarantinedAsync`; provider-specific ordering is retained
for external-provider acceptance rather than simulated in the SQLite unit suite. Maximum measured
method/state-machine complexity is 10; with 100% line coverage, maximum CRAP is 10 and none exceeds
30. The interface contains no executable lines. Unit sorted-display-name SHA-256 is
`dc3e7acd970a1a6584c89a2794c069925995ce1fdf6cd9f79adb9c57b8cd4290`.

| Gate | Result |
| --- | --- |
| Focused operator contract | 10/10 passed |
| Full EF unit | 239/239 passed |
| Strict Release product/unit/local builds | 0 warnings, 0 errors |
| Product/unit/local format | Exit 0 |
| Core Release | 4,799/4,799 passed with suite parallelism disabled |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-161-ef-outbox-operator-contract-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open.
