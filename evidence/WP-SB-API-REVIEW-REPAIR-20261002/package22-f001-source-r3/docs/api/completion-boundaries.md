# Endpoint completion boundaries

`SendAsync`, `PublishAsync`, response forwarding, initializer overloads and scheduling wrappers complete at the boundary chosen by the active endpoint or scheduler. Awaiting their task observes that operation's failure or cancellation. Successful completion alone does not prove that a consumer received or processed the message, or that a broker durably stored it.

| Active configuration | Successful operation completion | Later boundary |
| --- | --- | --- |
| Direct transport endpoint | The selected transport operation completed. Provider-specific acknowledgement and durability depend on that transport's configuration. | Consumer receipt and processing require their own application acknowledgement. |
| `AddBufferedBus` | Capacity was reserved and the operation was queued in memory. The outgoing callback and transport send can still be pending. | Explicitly await `IBufferedBus.FlushAsync`. Disposing the DI scope does not flush the buffer. Flushing still does not await consumer processing. |
| `UseVolatileOutbox` before release | The outgoing operation was captured in the consume scope. | Successful consumption releases pending operations; a consume failure discards them. After release, new operations use the actual outgoing task. Awaiting a send inside the consumer does not await its later outbox release. |
| Transactional EF bus outbox | Serialization and admission completed and outbox entries were added to the active `DbContext` change tracker. | Await `IEntityFrameworkTransactionalOutbox.CommitAsync` to save that context. A caller-owned outer transaction must then be committed by its owner. Dispatch and consumer processing remain separate. |
| Scheduling operation | The configured scheduler produced a scheduling result. A command endpoint can itself buffer or capture work in an outbox. | The selected provider determines scheduling acceptance, dispatch and cancellation support. A schedule handle does not guarantee future delivery. |

For a transactional EF outbox, saving the `DbContext` inside a caller-owned transaction does not commit that transaction; its owner can still roll it back. Disposing an uncommitted outbox session discards its owned pending entries and reports the missing commit. Use the session's explicit abort operation when discarding is intended. Outbox persistence, later dispatch and final consumer processing are different events.

Buffer and outbox ownership also affects cancellation. A buffered flush uses its own dispatch token. A volatile outbox retains the captured send token for the later invocation; cancellation after capture can affect that later send even though the capture task already completed. Policies can discard expired messages successfully. A successful task is therefore insufficient as an application delivery receipt.

Request send operations return the send result or initialized request. Waiting for a response is a separate request-client operation. Responding, forwarding, publishing a routing-slip completion event and calling an advanced initializer preserve the selected endpoint's completion boundary.

Use explicit flush or commit where applicable, and a correlated application acknowledgement when the caller needs evidence of processing. Confirm any broker durability requirement against the selected provider's acknowledgement settings. Do not wait inside a consumer for the volatile outbox release that depends on that same consumer finishing.
