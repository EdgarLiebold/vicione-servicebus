# T52 — scheduling admission and replacement recovery

Status: all33 profiles and all four fixture groups pass. Independent integrity
and numerical audits pass without discrepancies. Global A+ remains open.

## Exact measured inputs

| Input | Identity |
| --- | --- |
| Implementation commit | `ad84a5ac6b49f9b4e7d2a241965e5488fb2b7379` |
| Product source tree | `c5d128da84f6094a2217fe7f775fcb692c6ae361` |
| Test tree | `4b281e5db4b9779914d7f019cef49a8c581823be` |
| Previous complete measurement | `b42dcf790cfff3b56903b9fd7f2ed28caa06453e` (T51) |

The [acceptance map](t52-scheduling-admission-journeys.md) binds four test methods,
32 cases and four requirement variants across Core scheduling and Quartz. Microsoft
test skills were applied as recorded there. Product source is unchanged from T51.
This packet adds verification; it does not claim a newly fixed product defect.

The tests distinguish rejected publish admission before initializer/pipe/endpoint
effects, control resolution and delivery ownership, invalid replacement of existing
Quartz triggers and provider failures before successful recovery. Exact payload,
contract, token, control time, target identity and replacement headers are checked.
Quartz retains a same-name neighbor in a different schedule group across both
rejection and recovery. Its provider seam delegates successful operations to the
real scheduler through a real bus consumer.

Read-only review corrected the asynchronous provider-failure oracle and added exact
replacement-header checks. Three isolated counterprobes are detected: missing Quartz
await1/4, premature Cancel completion2/12, invalid publish fallback2/16. Each is
manually restored with original source hashes verified. Final restored controls pass
Core160/160 and Quartz19/19, no skips; both builds have zero warnings/errors.
Verify-only formatting exits0 without edits. All5,887 source/test paths match MAIN
and the isolated gate checkout.

Initial Quartz test-authoring errors (typed misfire enum, collection-size assertion
and nullable retry policy) were corrected before measurement and remain documented.
There is exactly one complete T52 full33 run; no failed full-product run was hidden
or overwritten.

## Complete measurement

| Metric | T51 | T52 |
| --- | ---: | ---: |
| Successful executions | 13,115 | **13,147** |
| Profiles / product assemblies | 33 / 32 | **33 / 32** |
| Physical covered / valid lines | 85,813 / 93,753 | **85,823 / 93,753** |
| Line coverage | 91.53093767666102% | **91.54160400200527%** |
| Conservative covered / valid branches | 30,983 / 36,847 | **30,992 / 36,847** |
| Conservative branch coverage | 84.08554292072624% | **84.10996824707575%** |
| Method identities | 26,071 | **26,071** |
| Methods with CRAP >30 | 0 | **0** |
| Line-gap identities | 4,323 | **4,317** |
| Zero / partial line coverage | 2,672 / 1,651 | **2,671 / 1,646** |
| Additional branch-only identities | 1,527 | **1,526** |
| Union of remaining gap candidates | 5,850 | **5,843** |

No failures or skips; all four fixture groups exit0. Core adds28 cases and Quartz4,
exactly32. Provider selection and CPU portability profiles are retained. Source
denominators and method counts are unchanged. CRAP uses per-method complexity and
unioned line coverage. Conservative branch observations use the maximum covered
count per physical line; they are not a union of stable branch identities.

## Reconciled observations

Physical observations gain12 covered lines and lose2, net+10. The two recurring
schedulers gain three lines each with no losses. Other gains are four lines in
ConsumeContextEventHubProducerProvider and one each in ServiceBusInstrumentation
and ReliableMessagingDeliveryService. Losses are BatchConsumer line91 and
ResourceCache.Creation line193. None of these unrelated differences is causally
attributed to the new tests or declared a product defect.

Seven identities leave the line-gap list and one enters it, leaving4,317.
Five leave the branch-only list and four enter it, leaving1,526; all five
departures become fully observed, with no migration from a line gap. The union
shrinks by seven to5,843. No method or physical line identity is added or removed.
The newly observed line gap is BatchConsumer.TimeLimitExpired.

Target branch observations gain seven: EndpointRecurringMessageScheduler four,
PublishRecurringMessageScheduler three; the remaining files net plus two. All
four generic/runtime GetPublishAddress variants now have5/5 lines and4/4 branches.
The unchanged Quartz coverage count does not reduce the value of the new existing-
trigger integrity and asynchronous provider-failure assertions.

## Highest CRAP values with remaining line gaps

| Method | Complexity | Covered lines | Conservative branches | CRAP |
| --- | ---: | ---: | ---: | ---: |
| OutboxMessagePipe.DeliverOutboxMessagesAsync (generated MoveNext) | 28 | 31/33 | 21/28 | 28.17453 |
| EventHubProducer.BatchSendPipe.SendAsync (generated MoveNext) | 28 | 32/34 | 22/28 | 28.15958 |
| InMemoryReliableInboxContextFactory.SendAsync (generated MoveNext) | 28 | 68/70 | 24/28 | 28.01829 |

Their uncovered lines remain109/110,251/300 and49/127 respectively. The complete
gap lists include every remaining identity, including generated methods. These
are investigation candidates, not a count of independent product defects. Further
connected packets must compare existing tests with delivery failure/recovery,
producer batching and inbox ownership contracts before adding tests. Zero CRAP>30
does not establish A+ or settle remaining line and branch gaps.

## Independent audit and retained limits

Read-only reviewer `/root/outbox_proof_redteam` checked the first20 completed
profiles while provider profiles were still running, then the remaining13 without
repeating the first hash audit. All487 report/log/binary hashes,66 runner/settings
bindings and nine broker-log hashes agree. Commit, both trees, profile selection,
four clean fixture groups and13,147 passing tests agree. Independent reconstruction
from all66 T51/T52 XML reports matches every one of the26,071 method rows in both
measurements, including complexity, CRAP, conservative branches and gap membership.
All physical deltas and gap-list transitions agree. No discrepancy or blocking
finding remains.

The provider failure tests inject a rejected factory or store operation; they do
not claim full broker-outage coverage. Full33 separately covers relevant provider
integrations. Quartz receive tokens are compared to factory/scheduler tokens,
not to the unrelated sender token. These replacement journeys start with a valid
shared durable job and do not promise zero scheduler effects for first-job creation.

The accepted first complete source reading remains complete. The all-repository
Roslyn API/comment audit follows coverage/CRAP completion. No global A+ clearance
or external product-team acceptance is claimed.

## Artifacts

Evidence remains under ignored `artifacts/`. Protected untracked `TestResults/`
and `review/` remain untouched. No optional HTML report or extra measurement was
generated for analysis.

| Artifact | SHA-256 |
| --- | --- |
| t52-aggregate.json | `7ee4070789cc5e3b75e5780f50dbee4411bd2d4b61da422bf254de72d108ee7a` |
| t52-all-methods.json | `35bbcbd51fc3ca8eb8c5b9b13df6f86a3a260d349ad3b31bb229c503781f7c0d` |
| t52-method-gaps.json | `9760398f0629b167b47c677687e0af79f6f7029a8ca542485200fe225b8c7df1` |
| t52-branch-only-gaps.json | `964857cf050617127f4f7e925dff968b8283c350310861994331f2cfaaf7b149` |
| t52-profile-progress.json | `08d7ff4b32c0cb21d3d10392c8fd3fb90491743240427240b0153432c7f01924` |
| t52-gap-summary.json | `7c08e01c9681202e9baa909b78b8bcba958558906b7e7a9df319f9975271ba11` |
| t52-gap-delta.json | `7617b596092dd5a5994dea15f2f18856a130fce4f2981e20357fc82a8e4e5b4b` |
