# T54 — transport ownership and message isolation

All33 profiles and four fixture groups pass. Independent integrity and numerical
audits find no discrepancy. Global A+ remains open.

## Frozen inputs and behavioral evidence

| Input | Identity |
| --- | --- |
| Corrected implementation | `0fa2c85bb8f130970e6de9dcb94efca97dcb05fc` |
| Source tree | `c5d128da84f6094a2217fe7f775fcb692c6ae361` |
| Test tree | `322689b5134bba8cfac4a4bcaa516dcdc010d1ab` |
| Previous complete measurement | `f41b145f6bd709c2c9d2b04bb5c782ad64bfd53f` (T53) |

The [acceptance map](t54-transport-ownership-and-isolation.md) records the mandatory
Microsoft testing skills, bounded Roslyn pairing, six requirement bindings and22
new cases. Product source is unchanged. The connected families prove:

- ActiveMQ native group metadata remains isolated across grouped, ungrouped and
  regrouped messages on one resolved endpoint, over OpenWire, AMQP and Artemis.
  Exact public and native metadata, payload identities and post-stop cardinality
  are checked. Consumer affinity, ordering and native producer identity are not
  claimed.
- Event Hubs rejected replacements preserve effective clients and actual delivery;
  incomplete configuration can be repaired before first build. Duplicate endpoint
  identities fail before callbacks; distinct groups build separate endpoints.
  The distinct-group case is build-only, not an emulator delivery claim for cg2.
- Deferred producer resolution preserves envelopes and overload routing and awaits
  actual downstream completion, including late failure and cancellation. The
  provider seam proves wrapper behavior, not broker I/O or initializer semantics.

Read-only review strengthened cleanup and overload discrimination. Missing-await
and missing-native-group-sequence counterprobes fail their behavioral assertions;
both product sources were manually restored and their original hashes verified.
Restored combined controls pass27/27 without skips. Builds have zero warnings or
errors and verify-only format checks exit0. All5,891 source/test paths agree between
MAIN and GATE; the corrected SQS file has its own matching hash in the manifest.

## Failed attempt and correction

The initial full measurement at `f97172b65` failed after26 verified profiles: the
existing SQS Quartz test observed delivery before Quartz removed its trigger.
Its59/60 result and partial artifacts remain preserved under `artifacts/t54-*`;
they are not used in the complete aggregate.

The corrected test observes matching finalization and bounded actual store removal,
retains the absence assertion and verifies delivery count after bus stop. A gate
that prevents finalization from returning causes the removal check to fail; the
gate was manually removed and the corrected hash restored. Focused controls pass
2/2. This is a test synchronization repair, not a claimed product delivery defect.
Only the fresh `artifacts/t54b-*` run at the corrected commit is accepted below.

## Complete measurement

| Metric | T53 | T54b |
| --- | ---: | ---: |
| Passing executions | 13,160 | **13,182** |
| Profiles / product assemblies | 33 / 32 | **33 / 32** |
| Covered / valid physical lines | 85,832 / 93,753 | **85,876 / 93,753** |
| Line coverage | 91.5512036948151% | **91.59813552632983%** |
| Covered / valid conservative branches | 30,995 / 36,847 | **31,031 / 36,847** |
| Conservative branch coverage | 84.11811002252558% | **84.21581132792357%** |
| Method identities | 26,071 | **26,071** |
| CRAP > 30 | 0 | **0** |
| Line-gap identities | 4,314 | **4,299** |
| Zero / partial line coverage | 2,672 / 1,642 | **2,660 / 1,639** |
| Additional branch-only gaps | 1,523 | **1,527** |
| Union of gaps | 5,837 | **5,826** |

No failures or skips. Event Hubs local integration passes89 cases, ActiveMQ103.
All four fixture groups exit0. Conservative branches take the maximum covered
count per physical line, not a union of stable branch identities. CRAP uses method
complexity and unioned line coverage. Net changes are+44 physical lines and+36
conservative branches; the independent detailed delta audit agrees. Physical
observations gain52 lines and lose8, with49 gains and no losses in the target
files. Outside those files, net−5 is not causally attributed to this packet.
Seventeen line-gap identities reach full line coverage:15 close completely and
two migrate to branch-only. ActiveMqSendTransportContext.SendAsync now has26/26
lines and13/16 branches; EventHubProducerSpecification.Validate has4/4 lines and
3/6 branches. Two unrelated identities enter the line-gap list. Four branch-only
identities close completely; eight enter, comprising those two migrations and
six newly observed gaps. No method or physical line identity is added or removed.
Target branches gain34; other observations net+2 without causal attribution.

## Independent audit and remaining work

Read-only reviewer `/root/outbox_proof_redteam` confirms all33 fresh profiles,
13,182 tests,487 binary/log/report hashes,66 runner/settings bindings, nine broker
log hashes, commit/trees and four clean fixture groups. Independent reconstruction
from66 T53/T54b XML reports agrees on all52,142 method rows, complexity, CRAP,
uncovered lines, physical observations and gap transitions. No discrepancy
remains. This is a separate agent review, not external model-independent
product-team acceptance.

Highest CRAP identities with remaining line gaps are unchanged:

| Method (generated MoveNext) | Complexity | Lines | Branches | CRAP |
| --- | ---: | ---: | ---: | ---: |
| OutboxMessagePipe.DeliverOutboxMessagesAsync | 28 | 31/33 | 21/28 | 28.17453 |
| EventHubProducer.BatchSendPipe.SendAsync | 28 | 32/34 | 22/28 | 28.15958 |
| InMemoryReliableInboxContextFactory.SendAsync | 28 | 68/70 | 24/28 | 28.01829 |

The5,826 gap identities include compiler-generated methods and do not represent
5,826 independent defects. Zero CRAP>30 does not establish global A+. No mapping
exemption is established by a generated method name. The accepted complete source
reading is not reopened. The all-repository Roslyn API/comment review follows
coverage/CRAP completion.

Next implementation must combine more connected behavior families before a single
complete measurement. Existing scheduling success/async matrices must not be
duplicated. Consumer-outbox corrupt-row retention needs a real persistence
counterexample and contract review before correction. Numerical gaps alone are
not sufficient justification for tests. Protected `TestResults/` and `review/`
remain untouched; analysis artifacts stay under ignored `artifacts/`.

## Evidence artifact hashes

| Artifact | SHA-256 |
| --- | --- |
| t54b-aggregate.json | `03a4073683c84d72a08b80e6c001a4fd0222b24ac64459c5adc97e87244fcf40` |
| t54b-all-methods.json | `2264570ffc2b449333c5b1f1543f6bd352733a0a40e0d6c6abd24c6efd316092` |
| t54b-method-gaps.json | `3afd9e8aa057d365f997b7bb00f7c99fe423e06a6cdea197630d5295b4e7322e` |
| t54b-branch-only-gaps.json | `cc67ea125f5f81e7cb9885f7d27db7bceb182ac3470f89c1742f1912ec4fcd91` |
| t54b-profile-progress.json | `20017a7846c24a1ce7d3f07dfb8f844ac0ed97506c80ea631c7cc19d7f24d261` |
| t54b-gap-summary.json | `e58a7ec9bd00107a58aaf82451acb0794ed7e86564bd952147e7abe96a35af5a` |
| t54b-gap-delta.json | `3a167e032ebdb1c72be2adb1ce9a8b4b4a1ab49033adfc9c1cfa8cb2bd2e5e0e` |
