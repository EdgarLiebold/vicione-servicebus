# T61 — ActiveMQ producer ownership and native delivery

T59 remains the last complete product-wide coverage/CRAP baseline. T61 groups
the ActiveMQ session executor, destination-keyed producer cache, native send
settings and broker delivery as one behavior packet. The source/test pairing
from T58 is a static search aid; product source has not changed since that
pairing before this packet. Wrapper-only uncovered lines are not acceptance
targets by themselves.

| Contract | Test and direct oracle |
| --- | --- |
| One canceled cache waiter cannot cancel another caller's shared factory | `MessageProducerCacheTests.CanceledWaiter_DoesNotCancelAnotherSendersProducerCreationAsync`: factory waits on the cache-owned token, second caller receives the committed producer, one factory and one final disposal |
| Different destinations have independent creation and lifetime | `MessageProducerCacheTests.DifferentDestinations_CreateConcurrentlyAndReleaseTheirOwnProducersAsync`: both factories start before either is released; distinct cached producers and one disposal each |
| Canceling the first sender while its producer creation waits in the session executor cannot abort the second sender | `ActiveMqSessionProducerCancellationTests.CanceledFirstSender_DoesNotAbortAnotherSendersQueuedProducerCreationAsync`: one executor worker is held by an unrelated producer creation; two sends share the pending destination; only the first is canceled; the survivor alone sends, with exactly one creation and disposal |
| Native routing and delivery settings remain per-message across two destinations | `ActiveMqProducerIsolationTests.SequentialSends_PreservePriorityDurabilityAndDestinationAsync`: three messages across two queues and OpenWire/classic AMQP/Artemis AMQP retain exact payload, service-bus ID, native priority and delivery mode, without duplicates |

The read-only adversarial review found a high-severity gap in the first cache
test: its original factory ignored the canceled sender token, whereas the real
`ActiveMqSessionContext` captured that token for shared producer creation. A
new session-level regression was run before the fix and failed exactly on the
surviving sender with `TaskCanceledException` in `TaskExecutor.ExecuteAsync`
(`/private/tmp/vsb-t61-cancellation-red.log`). The fix passes the cache-owned
creation token through an internal method into the executor. The original
public `MessageProducerCache.GetMessageProducerAsync` signature remains intact,
avoiding a new public overload and its null-call ambiguity. The cache test now
uses the actual creation token. Final Red Team re-review found no concrete
blocker; it was read-only and did not run tests.

The first focused cache class passed 5/5. The three broker variants passed 3/3
in a fresh Classic/Artemis fixture with empty findings. After the product fix,
the complete ActiveMQ Unit project passed 229/229, zero skipped, with a clean
build. A first whole LocalIntegration attempt passed 104/106; its only two
failures were existing outage-control cases because the runner lacked their
required `--allow-broker-outage activemq` fixture option. A fresh full run with
that option and final product bytes passed 106/106, zero skipped, with a clean
build and empty fixture findings (`vicione-e3c049530c96`). Both broker logs
match their fixture hashes. The failed setup attempt remains in its log; it
was not counted as a clean pass. No T61 global line, branch or CRAP figure is
claimed. The next exact 33-profile measurement is batched with further
connected packets under the agreed cadence.
