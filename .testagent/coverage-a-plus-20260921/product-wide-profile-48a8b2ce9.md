# T55 — consumer outbox retention and recovery

All 33 frozen profiles and four provider fixture groups pass at
`48a8b2ce903c8a3dc794de304b2317ee46ce5977`. Global A+ remains open.

## Frozen inputs and behavior

| Input | Identity |
| --- | --- |
| Implementation | `48a8b2ce903c8a3dc794de304b2317ee46ce5977` |
| Source tree | `647dad3ba1c3b20302d9b65df840e7cda251add3` |
| Test tree | `7cb68aeb97f9aee20c9b20b789df9995b13a0812` |
| Previous complete measurement | `0fa2c85bb8f130970e6de9dcb94efca97dcb05fc` (T54b) |

The [acceptance map](t55-consumer-outbox-recovery.md) records Microsoft testing
skills, the bounded Roslyn source pairing, source findings, requirement bindings,
read-only review and counterprobes. The product now fails on a persisted outbox row
without a destination before advancing delivery, and passes the linked timeout/
caller cancellation token to endpoint resolution. The test matrix exercises the
public EF consumer-outbox route with real PostgreSQL: corruption and repair,
first/middle/last send failures across delivery windows, Save/Commit failures,
cleanup failure, pending send deadline/late failure, exact neighboring records,
allowed replay and same-intent recovery. A separate strict core seam proves
missing-destination rejection and endpoint-resolution cancellation. Pending send
failure injection is a send-pipeline observer, not native provider I/O.

The unchanged-product core control fails 3/3 for the expected behaviors and
passes 4/4 after correction. The restored relational local control passes 34/34
(33 behavior cases and requirement projection). Deleting the consumer key filter
fails all three neighbor assertions. Omitting acknowledgment in the final short
batch fails two exact watermark assertions while the full-batch control passes.
Both mutations were manually restored and hash checked. Verify-only format and
restored builds pass with zero warnings/errors. The final whitespace-only edit
was rebuilt and tested by the frozen 33-profile measurement.

## Complete measurement

| Metric | T54b | T55 |
| --- | ---: | ---: |
| Passing executions | 13,182 | **13,218** |
| Profiles / product assemblies | 33 / 32 | **33 / 32** |
| Covered / valid physical lines | 85,876 / 93,753 | **85,899 / 93,754** |
| Line coverage | 91.59813552632983% | **91.62169080785887%** |
| Covered / valid conservative branches | 31,031 / 36,847 | **31,035 / 36,845** |
| Conservative branch coverage | 84.21581132792357% | **84.23123897408061%** |
| Method identities | 26,071 | **26,071** |
| CRAP > 30 | 0 | **0** |
| Line-gap identities | 4,299 | **4,292** |
| Zero / partial line coverage | 2,660 / 1,639 | **2,655 / 1,637** |
| Additional branch-only gaps | 1,527 | **1,527** |
| Union of gaps | 5,826 | **5,819** |

Every receipt binds the same source and test trees and an exact build/test/Cobertura
run. There are no failed or skipped tests. All four fixture groups exit0. The
aggregate covers 32 product assemblies; conservative branches take the maximum
covered count per physical line across profiles. CRAP uses method complexity and
unioned line coverage. The counterprobes and focused controls are separate from
this frozen aggregate.

Net coverage rises by 23 covered physical lines while the source adds one valid
line. The valid branch count falls by two because the rejected-null path changes
OutboxMessagePipe complexity. In unchanged source files, physical observations
gain 21 covered positions and lose one; the changed OutboxMessagePipe file rises
from 59/64 to 62/65 covered/valid lines. Its generated delivery method becomes
34/34 lines, 23/26 conservative branches and CRAP 26.0 (previously 31/33,
21/28, CRAP 28.17453). Seven line-gap identities close, none open; there are no
new or removed method identities. Other coverage movement is not automatically
attributed to this packet. Positions in changed source are not compared as
identical statements.

## Remaining work and limits

Independent read-only reviewer `/root/outbox_proof_redteam` verifies all33 receipts,
13,218 successful test executions,487 binary/log/XML SHA256 values,66 runner/
settings bindings, the exact commit and both source/test trees. All four fixtures
exit0 and nine named broker logs exist. Its separate reconstruction from all33
Cobertura reports agrees exactly on physical lines, conservative branches,
26,071 methods, CRAP threshold and all gap counts. No integrity discrepancy or
review blocker remains. This is internal adversarial review, not external
product-team acceptance.

Highest remaining line-gap CRAP methods are EventHubProducer.BatchSendPipe.SendAsync
(28.15958; 32/34 lines, 22/28 branches),
InMemoryReliableInboxContextFactory.SendAsync (28.01829; 68/70, 24/28), and
DefaultEndpointNameFormatter.GetTemporaryQueueName (26.676; 27/30, 22/26).
These names indicate risk selection, not license to write shallow coverage tests.
The 5,819 gap identities include compiler-generated methods and are not 5,819
distinct defects. Zero CRAP > 30 does not establish global A+. No numerical A+
line/branch cutoff has been agreed; behavioral quality and all remaining gaps
still require review. The accepted complete source reading is not reopened.
The all-repository Roslyn API/XML-comment review follows coverage/CRAP closure.

## Evidence artifact hashes

| Artifact | SHA-256 |
| --- | --- |
| t55-aggregate.json | `7ecb48870fad8db8f9cb406813b3cda9dabdf04873ccf1d4ad456c3fcbb1498b` |
| t55-all-methods.json | `f7816776b0f0c2f66809fb23b1926810d787ef013a9ea1d18e798e1e4da513a8` |
| t55-method-gaps.json | `d746ca912760557d6132d9c0d58599d11a8beb819020b8a0e05c06e72bf58d5b` |
| t55-branch-only-gaps.json | `860f7567a64a70d675ce862fe586921ea3ea9178738277e019ed42a58e6cd260` |
| t55-gap-summary.json | `66476a944ef223f57e0a66621223bfa9bd7c9d5e8d92c7a97a0d46c92cd16cd2` |
| t55-profile-progress.json | `dc965180ca5d232e58f7f59e6cc024d552ce0f1ff195ef286fb7ea71d3ce3af2` |
