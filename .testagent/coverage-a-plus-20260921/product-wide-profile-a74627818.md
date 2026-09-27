# T50 — JSON conversion, admission, forwarding and scheduling

Status: all 33 profiles and four fixture groups pass at the corrected commit.
Independent numerical/integrity audit passes without discrepancies. Global A+
acceptance remains open.

## Exact measured inputs

| Input | Identity |
| --- | --- |
| Implementation commit | `a7462781823373ff9a9cdbaa9a9e9dfcd7390442` |
| Product source tree | `c5d128da84f6094a2217fe7f775fcb692c6ae361` |
| Test tree | `7b41a49370c313d85562075f98f3324fc6ebf315` |
| Previous complete measurement | `ad3ddbd4050f04d8096c1054d9b509cd015ab883` (T49) |
| Failed first implementation | `352ee8a7da6c09d24bac1b36b084a9839af10024` |

The [acceptance map and evidence](t50-json-boundary-journeys.md) bind seven test
methods, 44 cases and seven requirement variants across decimal/dictionary wire
values, malformed payload isolation, configured copy-envelope admission and
independent forwarding targets. Microsoft testing skills were applied as listed
there. No tests were added solely to increase a metric. Local overlay tests
prove serializer behavior; they are not described as transport integration tests.

## Defect, regression and corrective evidence

The original raw forwarding path lost concrete contract URNs when consuming an
interface. An exact regression fails against the original implementation with
three companion controls passing. The first correction preserved original URNs
unconditionally. Its full33 attempt verified twelve profiles but exposed three
raw scheduling failures in Quartz (281/284 passing).

Quartz stores contract URNs separately from application headers. During replay,
an empty deserialized raw contract list overwrote the restored send contracts.
The corrected parameterless serializer factory preserves nonempty original URNs
and otherwise retains the send-context contracts. Explicit replacement-contract
overloads are unchanged. The earlier review missed this path; subsequent
read-only review confirmed the cause and the bounded correction.

The three existing scheduling regressions and their envelope controls pass 6/6;
combined JSON and existing forwarding controls pass 56/56. Both builds have zero
warnings/errors. Verify-only formatting changes 0/5,730 files. The final full
Quartz profile passes 284/284. Earlier failed artifacts remain under `t50-*`;
the corrected measurement uses new `t50b-*` paths without overwriting receipts.

Three isolated counterprobes detect incorrect resolver priority (3 failures),
acceptance of invalid decimals (5 failures) and omitted forwarding customization
(2 failures). All deliberate changes are manually restored and SHA-checked;
the packet evidence records passing companion controls and restored 56/56.

## Complete measurement

| Metric | T49 | Corrected T50 |
| --- | ---: | ---: |
| Successful executions | 13,032 | **13,076** |
| Profiles / product assemblies | 33 / 32 | **33 / 32** |
| Physical covered / valid lines | 85,771 / 93,753 | **85,794 / 93,753** |
| Line coverage | 91.48613911021514% | **91.51067165850693%** |
| Conservative covered / valid branches | 30,948 / 36,845 | **30,978 / 36,847** |
| Conservative branch coverage | 83.99511466956168% | **84.07197329497652%** |
| Method identities | 26,071 | **26,071** |
| Methods with CRAP > 30 | 0 | **0** |
| Line-gap identities | 4,333 | **4,326** |
| Zero / partial line coverage | 2,678 / 1,655 | **2,676 / 1,650** |
| Additional branch-only identities | 1,522 | **1,525** |
| Union of remaining gap candidates | 5,855 | **5,851** |

All profiles pass without failures/skips and all four fixture groups exit 0.
Core increases from 6,773 to 6,817 executions; profile selection and CPU/provider
settings are retained. This is a corrected measurement after a real regression,
not a claim that the first full33 passed. The new conditional adds two valid
branches. Physical line count and method identity count are unchanged.

CRAP uses per-method complexity and unioned line coverage. Conservative branches
use the maximum observed covered count per source line, not a union of stable
branch identities. The one changed source statement must be treated separately
from byte-identical lines when comparing physical observations.

## Reconciled observations

Byte-identical physical lines gain 25 observations and lose two, net +23.
Seventeen gains belong to the selected serialization targets, with no target
loss. The other eight gains are in JobService (1), SagaInstance (3),
TypeConverterCache (1) and ReliableMessagingDeliveryService including telemetry
(3). The two losses are InMemoryDelayProvider line 225 and
ServiceBusInstrumentation line 356. These unrelated differences are retained
without attributing them causally to the new tests or calling them product bugs.

The changed raw serializer statement is removed/added separately; both versions
are covered. Eight identities leave the line-gap list and one enters it; one
leaves the branch-only list and four enter it. The combined union shrinks by
four. No method identity is added or removed. These list transitions do not by
themselves mean that every departing identity is fully covered.

The new raw-context condition appears as 1/2 in conservative branch accounting.
Raw forwarding and Quartz replay exercise opposite contract-presence cases in
different profiles. This aggregate does not merge stable branch identities and
therefore cannot establish a 2/2 branch union for that statement. The conservative
gap is retained rather than replaced with an inferred coverage number.

## Independent final audit

Read-only reviewer `/root/outbox_proof_redteam` verified 487 binary/log/report
hashes, 66 committed runner/settings bindings, nine broker-log hashes, all 33
receipts, 32 expected assemblies and four clean fixture groups with empty
findings. Commit/tree identities and CPU/provider selections match.

Independent reconstruction from all 66 T49/T50b XML reports agrees with every
one of the 26,071 method rows in both inventories, including complexity, CRAP,
branches and gap membership. Physical observations and all transitions agree.
The eight line-gap departures and one branch-only departure become fully covered;
there is no line-gap/branch-only migration. One line-gap and four branch-only
identities enter the union, explaining its net reduction by four.

Target files gain 28 conservative branch observations; other files net +2.
The reviewer confirms the raw-factory aggregation limit: both behavioral
directions are tested, while each separate profile records 1/2. No stable
branch-ID union or actual missing behavior test is inferred from that count.
No discrepancy or blocking finding remains. This is an internal independent
read-only review, not external product-team acceptance or global A+ clearance.

## Remaining scope and artifacts

The 5,851 remaining identities include compiler-generated methods and are not
5,851 proven independent product defects. Zero CRAP > 30 is a diagnostic result,
not an agreed substitute for global A+ acceptance. Remaining behavior gaps still
require contract-based work in larger connected packages.

The accepted first full source reading remains complete. The all-repository
Roslyn API/comment audit is deferred until coverage/CRAP completion.

Evidence remains under ignored `artifacts/`: `t50b-profile-progress.json`,
`t50b-aggregate.json`, `t50b-all-methods.json`, `t50b-method-gaps.json`,
`t50b-branch-only-gaps.json`, `t50b-gap-summary.json` and the comparison output.
Protected untracked `TestResults/` and `review/` remain untouched. No optional
HTML report or additional test measurement was generated for this analysis.

| Artifact | SHA-256 |
| --- | --- |
| t50b-aggregate.json | `8029e9e3e83f0143ae027d17d456c6da42bdd52463a96ebe04bd8d195818a2c6` |
| t50b-all-methods.json | `548abc6acca97764c0dec54291a9231fe04d6b366d3638a09024f0932448cc3f` |
| t50b-method-gaps.json | `2e2f73d73fce675045d20d5720342abba38c88ac59ff872538149b369978cd4c` |
| t50b-branch-only-gaps.json | `8033c459ec00472343801502389d79366785f0fe24f04ea8607084f21bceed88` |
| t50b-profile-progress.json | `3280b2fc6a6167d1262be0b7d92bd7e1037417280ab7f72ece30df000319633f` |
| t50b-gap-delta.json | `82d8c48dbb4df511af28303b603e1ef57b166a66fbb53a2df476034a0cf4be45` |
