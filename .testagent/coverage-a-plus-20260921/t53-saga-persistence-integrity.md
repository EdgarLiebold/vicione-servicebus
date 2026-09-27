# T53 — persisted saga integrity

Status: implementation, review, counterprobes and restored controls complete;
frozen full-product measurement and publication pending.
Baseline: completed/pushed T52, `322dcc16d`.

## Requirements and acceptance map

| User requirement | Planned evidence |
| --- | --- |
| "schneide größere pakete" | One Azure Table persistence packet combining native corruption, schema evolution, storage boundaries and serialized corruption before focused validation. |
| "nur hochwertige Tests, die echtes Produktverhalten, Fehlerfälle, Grenzen und Regressionen hart prüfen" | The journeys below prove persisted state and ETag integrity, no premature effects and successful recovery; no duplicate happy-path matrix. |
| "die Microsoft Testskillss sind verpflichtend" | code-testing-agent inline Research → Plan → Implement, one find-untested-sources Roslyn pass, test-gap-analysis, assertion-quality, run-tests and final coverage-analysis. |
| "wie immer mit adversal red team reviews" | Read-only review, material isolated counterprobes, immediate hash-verified restoration and final controls before measurement. |

| Family | Planned test | Distinguishing oracle |
| --- | --- | --- |
| Native storage corruption | `CorruptedRow_FaultsBeforeConsumerAndRecoversAfterRepairAsync` | Wrong native int/Guid/nullable-bool representations fail public load and actual consumption; no handler or success event, original row and neighbor survive; repaired row subsequently processes once. |
| Serialized corruption | `CorruptedRow_FaultsBeforeConsumerAndRecoversAfterRepairAsync` | Malformed decimal/enum/object JSON fails real materialization, retains storage and permits repaired recovery. |
| Schema evolution | `LegacyRow_PreservesDefaultsAndExplicitEmptyValuesAcrossUpdateAsync` | Missing properties retain defaults; false/zero/empty string/empty bytes remain values; serialized null differs from absence, including after repository update/reload. |
| Exact limits | `StorageBoundary_RejectsWritesWithoutEffectsAndAcceptsExactLimitAsync` | UTF-16 BMP/surrogate, binary and UTC date limits accepted exactly, oversize/earlier values rejected before SDK writes; insert and update recovery on the same repository, unchanged neighbor. |

## Bounded discovery and existing evidence

One byte-identical Roslyn mirror at `/private/tmp/servicebus-t53-pairing` covers
22 Azure Table Saga/Infrastructure sources, seven associated unit/integration
tests and three project files. Exact hashes/output are retained in
`artifacts/t53-pairing-inputs.json`, `artifacts/t53-pairing.json` and
`artifacts/t53-pairing.log`: 15 paired, seven unpaired sources. Unpaired names
include validators, property type converter, key formatter/provider and client
provider types; suggested test paths are preserved in the output, not treated
as a command to create redundant tests.

Existing AzureTableEntityConverterTests already provides a comprehensive native
and serialized roundtrip, reserved-name isolation, malformed duration/URI/version,
decimal type/null and reference type guards, overlimit strings/binary and an old
DateTime guard. Existing integration tests cover successful conversion, lifecycle,
read-only events and ETag concurrency. T53 adds storage consequences and recovery.
Azure Table omits null native properties: an in-memory dictionary containing null
will not be represented as a real storage state. Outgoing nullable-null paths
bypassed by FromEntity are not artificially forced for coverage.

## Verification still required

Combined focused build/run, requirement projection, adversarial implementation
review, assertion/gap review, isolated mutations, restored controls, verify-only
formatting, exact-commit full33 including providers, independent audit, changelog,
canonical CHANGELIST and authorized remote push. No product defect or A+ clearance
is claimed from preparation alone.

## Combined implementation and first controls

Two new local integration files implement three theory methods and 13 cases,
with all three requirement variants bound. The corruption method combines the
native and serialized families through public load and a real state-machine
consume pipeline. Limits exercise both insert and update, local rejection before
HTTP writes, exact accepted values and recovery using the same repository context.
Legacy rows exercise absent defaults versus explicit false/zero/empty values and
serialized null, then update/reload with an explicit new reference value.

Initial build succeeds with zero warnings/errors. First focused run passes13/14:
the Count case fails recovery. A bounded diagnostic run passes5/6 and proves the
reused TableEntity still persists the repaired Count as Int64 instead of Int32.
The repair now builds a fresh entity for the same key with the original ETag;
stored type/value and public load are independently asserted before resending.
This is a test-fixture correction, not a claimed product fix. Logs are in GATE:
`artifacts/t53-focused.log`, `t53-repair.log`, and `t53-table-build-final.log`.

Read-only review identifies one oracle weakness: serialized failures previously
accepted any exception and used its type as the transport expectation. The test
now independently requires JsonException (including parser subclasses). The
reviewer confirms that fresh repair preserves all original corruption assertions
and finds no further concrete static blocker. A combined wildcard-filter attempt
selects zero tests (exit5); it is not evidence of a pass. Explicit class filters
then pass19/19, including existing lifecycle, conversion, concurrency and
requirement-projection controls: `artifacts/t53-combined-explicit.log`.

## Assertion quality and static gap assessment

Microsoft assertion-quality and test-gap-analysis, with their .NET extension,
were applied to all three methods and shared assertions. None is assertion-free,
trivial-only or a self-referential equality check. Real storage roundtrips are
intentional transformation checks with independent EDM/value and side-effect
oracles. No sleep, polling or timeout-as-success assertion is used.

| Method | Assertion categories and distinguishing checks |
| --- | --- |
| `CorruptedRow_FaultsBeforeConsumerAndRecoversAfterRepairAsync` | Exception/type/string, negative, state and deep equality: independent native/JSON error category, zero handler/success effects, complete rows and ETags, exact repaired type, one accepted successor and one fault after stop. |
| `LegacyRow_PreservesDefaultsAndExplicitEmptyValuesAcrossUpdateAsync` | Null/type/collection/state/equality: absent constructor defaults distinguished from explicit false/zero/empty/null; exact persisted EDM representations and new reference value after update. |
| `StorageBoundary_RejectsWritesWithoutEffectsAndAcceptsExactLimitAsync` | Exception/type/string/negative/state/deep equality: zero outgoing writes on rejection, absent rejected insert, full existing and neighbor snapshots, one accepted write and actual provider readback at each limit. |

Static mutation candidates are swallowed conversion errors, inclusive maximum
size rejection and missing-property default overwrite. These are hypotheses,
not claimed survivors. The first isolated probe replaces the native conversion
throw by return: exactly3/6 corruption cases fail because public load incorrectly
succeeds;3 serialized cases pass. Manual restoration matches original SHA-256
`77623deff985072d8cc302dc9cf08aba3c2f60856d8f3e14c82aee0e49980933`.
Evidence: `artifacts/t53-corruption-mutation-build.log` and
`artifacts/t53-corruption-mutation.log` in GATE. Remaining gates are still open.

The second isolated probe changes only the string limit comparison from `>` to
`>=`: exactly2/5 boundary cases fail at the rejected valid insert (BMP and
surrogate-pair text);3 binary/date cases pass. Manual restoration matches SHA-256
`7f62e1a8d69f2f00fe5541e41708f143c50606a7498d4810befee4b88645ed7a`.
Logs: `artifacts/t53-boundary-mutation-build.log` and
`artifacts/t53-boundary-mutation.log` in GATE. Both original product hashes match
before rebuilding. Final restored controls pass19/19, zero skips, build zero
warnings/errors (`artifacts/t53-restored-build.log`, `t53-restored.log`). Final
verify-only formatting exits0 without changes (`artifacts/t53-format-final.log`).
All5,889 source/test inputs match MAIN/GATE byte-for-byte, retained in MAIN's
`artifacts/t53-main-gate-inputs.json`. No product source change remains.

The reviewer confirms the JSON-category correction and fresh entity repair do
not weaken corruption or recovery checks; no other concrete static blocker.
Both material injected faults are empirically detected, not a complete mutation
score for the package. Full33, independent aggregate audit and publication follow.
