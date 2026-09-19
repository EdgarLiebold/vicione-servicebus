# ServiceBus A+ iteration 242 — in progress

This is an engineering checkpoint, not an A+ acceptance or a completion claim.

Current first-read accounting is [the one remaining-file list](../source-read-remainder.txt). Under the PO rule, a current `src` path changed in or after commit `e01a5e5eb3411412229221bd58b170b583ce6caa`, including the working tree, counts as completely read. At source checkpoint `bac2c88f91f08fbef7cbc4ca6d391ced51c31793`, 100 of 4,207 current paths remain: 87 C# and 13 other files. This is the agreed accounting rule, not proof of the reading act or A+ acceptance. The old cumulative counters are historical only.

The separate API review still follows [API_A_PLUS_CONTROL.md](API_A_PLUS_CONTROL.md). Packet-level source admissions and dispositions are retained as historical review evidence; they do not define the current first-read balance.

## Current evidence

- The active `src` first-read remainder is exactly the 100 paths in `../source-read-remainder.txt`. The richer per-file A+ review remains incomplete and is a separate gate.
- The Core native test project passes 6,217/6,217 in Release with zero skips. Its Release build has zero warnings and errors. The 58 Core async-name violations and 11 affected requirement mappings were repaired; targeted requirement projection and the dynamic namespace-case probe each pass 1/1.
- Focused Architecture checks pass for payload admission (4/4), inbound limits (9/9), configuration message shape (1/1), native source layout (1/1), Quartz external async contract signatures (17/17), and bidirectional async names (1/1). The last scan takes about four minutes and finds no violations.
- Real RabbitMQ local integration passes 31/31, zero skips, run `vicione-d9ffa5e2ad83`. The new real mandatory-unroutable publish-boundary test passes 1/1 independently, run `vicione-29fd321af7e4`. This proves the shared provider return path, not an atomic queue-deletion race at the durable dispatcher.
- Real ActiveMQ/Artemis local integration passed 97/97, zero skips, with its controlled broker-outage gate in this iteration. The refined SQL Server scheduling/job-service tests pass 9/9, run `vicione-506f5be91fd8`; the complete refined SQL Server matrix passes 69/69, zero skips, run `vicione-07466aef91f5`.
- The later complete unfiltered Release Unit/Architecture profile passed 9,535/9,535, zero skips (log `artifacts/iteration242-unit-unfiltered-escalated.log`), after the EF-saga, AmazonSqs, and MessagePack source fixes. The final MessagePack project, rebuilt after its end-to-end test extension, passed 115/115, zero skips (`artifacts/iteration242-messagepack-full-final.log`); the new object-inline regression was separately red before the fix and green after it. EF PostgreSQL LocalIntegration passed 60/60 and AmazonSqs LocalStack LocalIntegration passed 59/59 after their fixes. The earlier broad LocalIntegration profile was red: 325/384 pass, 59 fail, zero skip; its remaining provider failures still need a fresh whole-matrix run. The pre-fix source coverage/CRAP snapshot is in `artifacts/coverage-iteration242-wholefork/coverage-analysis.md`; a fresh measurement is in progress.
- A separate read-only Red Team reviewed the Core/Architecture/Sagas diff and found no concrete regression. The RabbitMQ/SQL/EF/runner review found three test weaknesses; SQL oracle and timeout catches were strengthened, while the RabbitMQ mandatory-return path received a real shared-boundary test.

## Open gates

- The 15 EF outbox fixture failures caused by missing required payload-admission runtime were repaired without weakening production admission; the later EF-saga lifetime/model-cache fix added eight regression cases and the complete EF module passed 257/257. The AmazonSqs Unit hang was repaired at the test fixture boundary and its complete module passed 147/147. The full Unit/Architecture profile is now green after those source fixes; the remaining real-provider matrix, current coverage/CRAP, API review, and detailed A+ decisions still prevent an acceptance claim.
- RabbitMQ's passive/active queue-proof sequence is not atomic against simultaneous external queue deletion. The new return test does not close that concurrency proof.
- The full canonical per-file A+ review, bidirectional API/test mapping, product-wide coverage/CRAP disposition, mutation and assertion-quality review, final package/API journeys, current real-provider matrix, and final repeated independent review remain open. Do not mark the Goal complete from this checkpoint.
- The first-read balance is the 100-path list above. Canonical detailed A+ decisions remain incomplete. A generated packed-public-API baseline exists, but no complete member-level personally read/API-to-test manifest or defensible API percentage exists.
