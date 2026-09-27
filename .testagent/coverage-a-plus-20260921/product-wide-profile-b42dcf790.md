# T51 — receive ownership and provider settlement

Status: all33 profiles and all four fixture groups pass. Independent integrity
and numerical audits pass without discrepancies. Global A+
acceptance remains open.

## Exact measured inputs

| Input | Identity |
| --- | --- |
| Implementation commit | `b42dcf790cfff3b56903b9fd7f2ed28caa06453e` |
| Product source tree | `c5d128da84f6094a2217fe7f775fcb692c6ae361` |
| Test tree | `006d80f4b3aaa29eff3902418709de26e81d86cb` |
| Previous complete measurement | `a7462781823373ff9a9cdbaa9a9e9dfcd7390442` (T50b) |

The [acceptance map and evidence](t51-receive-settlement-journeys.md) bind ten
test methods,39 cases and ten requirement variants across Core, Amazon SQS,
SQL transport and Azure Service Bus. Microsoft testing skills were applied as
listed there. The SQL test project adds the centrally pinned fake-time package.
Product source is byte-identical to T50b; no product defect fix is claimed.

The tests distinguish FIFO exhaustion, exact final exception identity, canceled
waiters, active delivery ownership, pending receive completion, renewal failure,
renewal drain, rejected SQL settlement, exact headers and awaited abandonment.
Three isolated counterprobes are detected at their intended assertions: premature
dispatcher completion2/5, missing SQS drain2/7, ignored SQL move refusal1/11.
All deliberate changes were manually restored and SHA-checked. Final combined
controls pass72/72; all four builds pass. Verify-only formatting exits0 without
changes. Read-only test review reports no concrete blocker.

Initial authoring failures remain documented: AggregateException semantics,
four missing analyzer-required test tokens and a locked SQL test dependency.
They were corrected before the frozen measurement; no failed full33 attempt was
discarded or overwritten. This packet has one complete full33 run.

## Complete measurement

| Metric | T50b | T51 |
| --- | ---: | ---: |
| Successful executions | 13,076 | **13,115** |
| Profiles / product assemblies | 33 / 32 | **33 / 32** |
| Physical covered / valid lines | 85,794 / 93,753 | **85,813 / 93,753** |
| Line coverage | 91.51067165850693% | **91.53093767666102%** |
| Conservative covered / valid branches | 30,978 / 36,847 | **30,983 / 36,847** |
| Conservative branch coverage | 84.07197329497652% | **84.08554292072624%** |
| Method identities | 26,071 | **26,071** |
| Methods with CRAP >30 | 0 | **0** |
| Line-gap identities | 4,326 | **4,323** |
| Zero / partial line coverage | 2,676 / 1,650 | **2,672 / 1,651** |
| Additional branch-only identities | 1,525 | **1,527** |
| Union of remaining gap candidates | 5,851 | **5,850** |

No failures or skips; all four fixture groups exit0. Test-count increases are
Core11, SQL11, SQS7 and Azure10, exactly39. Profile selection, CPU portability
settings and provider fixtures are retained. All denominators and method
identities remain unchanged.

CRAP uses per-method complexity and unioned line coverage. Conservative branch
counts take the maximum observed covered count per physical line; they are not
a union of stable branch identities. The complete method worklists retain
compiler-generated identities and all remaining gaps.

## Reconciled observations

Physical observations gain28 covered lines and lose9, net+19. The five selected
receive/settlement source files gain24 with no losses. Four other gains are
SqlTransportMessage.Headers(1), FutureExtensions(2), BatchConsumer(1).
Nine losses occur in TaskExtensions(2), JobService(1), SagaInstance(3) and
ReliableMessaging delivery/telemetry(3). No unrelated difference is attributed
causally to the new tests or declared a product defect.

Six method identities leave the line-gap list and three enter it. Two leave the
branch-only list and four enter it. Azure ServiceBusReceiveLockContext.FaultedAsync
moves from a line gap to a branch-only gap; it is not called fully covered.
No method identity is added or removed. The union shrinks by one, not by the
number of line-gap departures.

Selected targets gain nine conservative branch observations: SQS3, Pending3,
SQL2 and Azure1. Other files net minus four, yielding the overall plus five.
The two branch-only departures become fully observed: PublishEndpoint's callback
has4/4 branches and MessageReceiver.HandleAsync at line59 has2/2. The four
branch-only entries comprise the Azure migration and three new observed gaps.

## Highest CRAP values with remaining line gaps

| Method | Complexity | Covered lines | Conservative branches | CRAP |
| --- | ---: | ---: | ---: | ---: |
| OutboxMessagePipe.DeliverOutboxMessagesAsync (generated MoveNext) | 28 | 31/33 | 21/28 | 28.17453 |
| EventHubProducer.BatchSendPipe.SendAsync (generated MoveNext) | 28 | 32/34 | 22/28 | 28.15958 |
| InMemoryReliableInboxContextFactory.SendAsync (generated MoveNext) | 28 | 68/70 | 24/28 | 28.01829 |

Their uncovered source lines are respectively109/110,251/300 and49/127. These
are candidates for contract-based investigation, not proof of missing behavior
tests. The complete gap lists remain authoritative rather than this short table.
Further packets must examine delivery failure/recovery contracts, producer batch
failure boundaries and inbox ownership against existing tests before adding cases.
Zero CRAP>30 does not establish global A+ or settle branch coverage gaps.

## Independent audit and retained limits

Read-only reviewer `/root/outbox_proof_redteam` confirms487 report/log/binary
hashes,66 runner/settings bindings, nine broker-log hashes,33 receipts,32 expected
product assemblies and four fixture groups without findings. Commit and both
tree hashes match. Independent reconstruction from all66 T50b/T51 XML reports
agrees with all26,071 method rows in both measurements, including complexity,
CRAP, branch counts and gap membership. All physical deltas and list transitions
agree. No discrepancy or blocking finding remains.

The new local provider-boundary fixtures are not themselves broker integration
tests; full33 separately includes the relevant provider integrations. Dispatcher
tests do not independently gate delayed fault-observer/settlement phases; Azure
tests do not assert warning-log contents. Those claims are not made.

The5,850 remaining identities include compiler-generated methods and are not
5,850 independent product defects. First full source reading remains complete.
The all-repository Roslyn API/comment audit follows coverage/CRAP completion.
No global A+ clearance or external product-team acceptance is claimed.

## Artifacts

Evidence remains under ignored `artifacts/`. Protected untracked `TestResults/`
and `review/` remain untouched. No optional HTML report or additional measurement
was generated for analysis.

| Artifact | SHA-256 |
| --- | --- |
| t51-aggregate.json | `68e3bb9f387284ecb8e58895437d7381ffa78c35a019864d727bd4cc87ae4ee7` |
| t51-all-methods.json | `4f832886be9c8c590c11efe14c5291d19609563db764e769fc2a4bf727b0b8c8` |
| t51-method-gaps.json | `9d79c012bb7edd53d118f25a0815ca0d606d52a9ba0aca922f2fb1394978917c` |
| t51-branch-only-gaps.json | `9f481fe448af9dec1d8fbcb15c3a39d204e1a9b01882e5b79c6e72e8ec9837f9` |
| t51-profile-progress.json | `e34f87a1b5abff105d01e19e57d7c69541d003d43cc10574d30c29eb561c5902` |
| t51-gap-delta.json | `759b0e35ab4be47920f0df311b2df257706ca51673b1e9823e285a99b9488d75` |
| t51-gap-summary.json | `563dfa172af263967e37d3643cb8c58e4dc2468a0b19464f185da5b7390b91ba` |
