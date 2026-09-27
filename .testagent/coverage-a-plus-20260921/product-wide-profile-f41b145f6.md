# T53 — Azure Table saga integrity and recovery

Status: all 33 profiles and four fixture groups pass; independent read-only
integrity and numerical audit finds no discrepancy. Global A+ remains open.

## Frozen inputs

| Input | Identity |
| --- | --- |
| Implementation | `f41b145f6bd709c2c9d2b04bb5c782ad64bfd53f` |
| Source tree | `c5d128da84f6094a2217fe7f775fcb692c6ae361` |
| Test tree | `9a20d328c8464b513fad513d1142c555b13911ac` |
| Previous measurement | `ad84a5ac6b49f9b4e7d2a241965e5488fb2b7379` (T52) |

The [acceptance map](t53-saga-persistence-integrity.md) records the Microsoft
testing skills, one bounded Roslyn pairing, three requirement-bound methods and
13 cases. Product source is unchanged; no newly repaired product defect is claimed.

## Product evidence

| Test method | Verified behavior |
| --- | --- |
| `CorruptedRow_FaultsBeforeConsumerAndRecoversAfterRepairAsync` | Six native/serialized corruptions fail public load and real consumption before handler effects; exact row, ETag and neighbor survive, repaired saga succeeds once. |
| `LegacyRow_PreservesDefaultsAndExplicitEmptyValuesAcrossUpdateAsync` | Missing fields retain defaults while explicit zero/false/empty/null values load distinctly; real update and readback verify storage representations. |
| `StorageBoundary_RejectsWritesWithoutEffectsAndAcceptsExactLimitAsync` | Five text/binary/UTC boundaries reject invalid insert and update before HTTP writes; same repository context accepts the exact valid boundary without changing its neighbor. |

The schema test assigns a nonnull Detail before save; it does not promise a null
roundtrip because null is omitted on write. UTC instant, not local calendar year,
determines the DateTimeOffset boundary. Real Azurite storage and SDK requests are
observed; this is not a production Azure service availability claim.

Initial test-fixture repair reused an entity whose readback remained Int64 instead
of Int32. A fresh entity fixed this, with independent stored-type/value and public
load assertions. No unproven provider defect is inferred. Read-only review also
required the independent JsonException category. A wildcard filter selected zero
tests (exit 5); only the subsequent explicit filters count as successful controls.

Two isolated faults were detected: swallowed native conversion errors fail 3/6
corruption cases; inclusive string maximum rejection fails 2/5 boundary cases.
Both were manually restored and original hashes verified. These are material
counterprobes, not a complete mutation score. Restored controls pass 19/19 with
zero failures/skips and zero build warnings/errors. Verify-only formatting exits
0 without changes; its empty log alone does not prove exit status, so the separate
format receipt records the observed author tool result. All 5,889 source/test
paths match MAIN and GATE.

## Complete measurement

Exactly one complete T53 full33 run was performed.

| Metric | T52 | T53 |
| --- | ---: | ---: |
| Passing executions | 13,147 | **13,160** |
| Profiles / product assemblies | 33 / 32 | **33 / 32** |
| Covered / valid physical lines | 85,823 / 93,753 | **85,832 / 93,753** |
| Line coverage | 91.54160400200527% | **91.5512036948151%** |
| Covered / valid conservative branches | 30,992 / 36,847 | **30,995 / 36,847** |
| Conservative branch coverage | 84.10996824707575% | **84.11811002252558%** |
| Method identities | 26,071 | **26,071** |
| CRAP > 30 | 0 | **0** |
| Line-gap identities | 4,317 | **4,314** |
| Zero / partial line coverage | 2,671 / 1,646 | **2,672 / 1,642** |
| Additional branch-only gaps | 1,526 | **1,523** |
| Union of gaps | 5,843 | **5,837** |

No failures or skips. Azure Table local integration passes 40/40, exactly 13 more
than T52. All four fixture groups exit 0. Conservative branches use the maximum
covered count per physical line, not a union of stable branch identities. CRAP
uses method complexity and unioned line coverage.

Physical observations gain 15 lines and lose six, net +9. The Azure Table target
gains seven lines without losses and four branches; unrelated files net +2 lines
and −1 branch, without causal attribution to the new tests. Six line-gap identities
become fully observed and three enter the list. Five branch-only identities become
fully observed and two enter; no category migration. No method or physical line
identity is added or removed. Three native converter overloads now have 5/5 lines
and 2/2 branches each; the validator has 18/18 lines and 14/14 branches.

## Independent audit and remaining work

Read-only reviewer `/root/outbox_proof_redteam` independently verified 487
report/log/binary hashes, 66 runner/settings bindings, nine broker-log hashes,
all fixture results and the commit/trees. All 11,778 MAIN/GATE input hash checks
agree. Reconstruction from the 66 T52/T53 XML reports matches all 52,142 method
rows, including complexity, CRAP, uncovered lines, gap classifications and deltas.
No discrepancy remains. This is a separate agent review, not an external
model-independent product-team acceptance.

The highest CRAP values with remaining line gaps are unchanged:

| Method (generated MoveNext) | Complexity | Lines | Branches | CRAP |
| --- | ---: | ---: | ---: | ---: |
| OutboxMessagePipe.DeliverOutboxMessagesAsync | 28 | 31/33 | 21/28 | 28.17453 |
| EventHubProducer.BatchSendPipe.SendAsync | 28 | 32/34 | 22/28 | 28.15958 |
| InMemoryReliableInboxContextFactory.SendAsync | 28 | 68/70 | 24/28 | 28.01829 |

The 5,837 remaining identities include compiler-generated methods and are not
5,837 independent product defects. Zero CRAP > 30 does not establish global A+.
The accepted complete source reading is not reopened. The all-repository Roslyn
API/comment review follows coverage/CRAP completion.

Next packets must combine several connected implementation families before one
full-product measurement. Use the existing worklist for selection, targeted tests
during implementation and one combined adversarial review; repeat broader gates
only for a concrete remaining risk. EventHubs and ActiveMQ preparation is read-only
and does not yet constitute accepted implementation scope.

## Evidence artifacts

Artifacts remain in ignored `artifacts/`; protected untracked `TestResults/` and
`review/` were untouched. No optional HTML or extra coverage run was needed.

| Artifact | SHA-256 |
| --- | --- |
| t53-aggregate.json | `6c82293baf64e9d517521856da434c080512c147d600a88e159341fc3895b16c` |
| t53-all-methods.json | `4021b4d6f52900ca66a563847cde8f9c653acfb8193a0f7acb592ceebf89ef26` |
| t53-method-gaps.json | `49d87693abdba7efd0d506a2e11fccba435afd815b195b851ed768de962ec474` |
| t53-branch-only-gaps.json | `c004f60d17fe429b1a9d48606028ebb2342d0bb092efe77bbcf8bdb8e121c29e` |
| t53-profile-progress.json | `06e10580a2c138f79b09b5bb80264b48fb3bca791a5061fe33c01fed8872cf05` |
| t53-gap-summary.json | `21cd80440c70b10d3e0be3b205e7043b0a1710e46e693401bb7c5cda9c7d59f8` |
| t53-gap-delta.json | `dd1c4f87cc6743d704959eddd907ae09c277f163fdef96f2eafdc90369c3cad7` |
