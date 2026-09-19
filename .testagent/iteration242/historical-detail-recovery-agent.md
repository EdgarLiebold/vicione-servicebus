# Historical detail recovery: iteration 123 in-memory outbox packet

Read-only reconciliation on 19 September 2026 against ServiceBus `HEAD` `229b8759b8d30aba261b6a9a175d71c67e0e580c`. This is evidence recovery, not a new source review or an A+ approval.

## Result

Five current `src` files have a previously uncounted, file-specific historical complete-manual-read and bounded disposition record. The original packet attests that the Lead completely read all nine productive files and comments (`evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-123/SAGA_CORE_READING_PACKET.md:49-51`), then states five separate behavioral/documentation dispositions (`:52-68`). Its binding table records the final SHA-256 for each (`ITERATION-123/SOURCE_BINDINGS.md:10-15`). The packet and binding first entered Git at `10d1198187cf7ab1050351cfc0cac97489111b79`, a descendant of `944d0e235` (`git merge-base --is-ancestor` exited 0).

| Current path | Current SHA-256 = historical final SHA-256 | Historical file-specific disposition |
|---|---|---|
| `src/ViciOne.ServiceBus/Middleware/InMemoryOutboxFilter.cs` | `76fe17f24f9e3e88e2a315c3733c4b4dfb7b2b8a49e5a93ab4937902cc1c1e67` | Corrected the contract description for consume-pipeline/outbox types, optional scoped rebinding, deferred concurrency, discard and cleanup error ordering; it does not promise exception preservation (`SAGA_CORE_READING_PACKET.md:52-55`; `SOURCE_BINDINGS.md:12`). |
| `src/ViciOne.ServiceBus/Middleware/Outbox/InMemory/InMemoryOutboxContextFactory.cs` | `1ff39dbbc161c3463a0a7394764d57c9ecfcabc58602a4d2352ca82d0c8abd0f` | Clarified that process-local locking is outbox-aware, not durable-provider acceptance; completion failure is observed without replacing the delivery exception (`SAGA_CORE_READING_PACKET.md:56-58`; `SOURCE_BINDINGS.md:13`). |
| `src/ViciOne.ServiceBus/Middleware/InMemoryOutbox/InMemoryOutboxConsumeContext.cs` | `647fedc48fe37b1f8a8ae1825d2df970e5fafb5dbebcbb3a83c81d73f3cc243c` | Clarified deferred-delivery/scheduler order, cancellation-request versus guaranteed cancellation, and conditional warning logging (`SAGA_CORE_READING_PACKET.md:59-62`; `SOURCE_BINDINGS.md:14`). |
| `src/ViciOne.ServiceBus/Middleware/InMemoryOutbox/OutboxContext.cs` | `46f1e3d0bf20d17a6d2036319e2769524217f3b05016dd0d91fb765a668b2812` | Clarified that discard processing/cancellation requests do not guarantee every schedule was canceled (`SAGA_CORE_READING_PACKET.md:63-64`; `SOURCE_BINDINGS.md:15`). |
| `src/ViciOne.ServiceBus/InMemoryTransport/Runtime/InMemoryDelayProvider.cs` | `3a48b29ec8165d8af994138dcc9222f2fa19120278db72fa91f136a4c39b449c` | Clarified the actual per-call `DisposeAsync` completion behavior, without promising shared completion (`SAGA_CORE_READING_PACKET.md:65-68`; `SOURCE_BINDINGS.md:10`). |

I computed current SHA-256 values with `shasum -a 256` on the exact five paths; all five equal the historical **final**, not input, column. `git status --short --` for the five paths was empty. An exact-path search across iteration-242 `*source*tsv` and `historical-review-*.tsv` returned no row for any of the five, so this packet does not overlap those current detail formats. The other four files in the nine-file packet also still hash-match, but the packet describes them collectively as unchanged neighbors (`SAGA_CORE_READING_PACKET.md:69-75`); I have **not** promoted them to five more individually reasoned dispositions.

## Boundaries

The historical corrections were comment-only and the nine-file executable/signature comparison was scoped to that packet (`SAGA_CORE_READING_PACKET.md:73-75`; `SOURCE_BINDINGS.md:19-24`). The same packet leaves runtime/API candidates explicitly open (`SAGA_CORE_READING_PACKET.md:120-136`), including delay-provider disposal completion and outbox cleanup/error masking. These five records prove prior manual reading and a bounded file-specific comment/behavior disposition for unchanged present bytes. They do **not** close those candidates, present-day whole-path architecture/API/test ownership, current coverage, provider acceptance, or the product-wide A+ gate. A historical Git edit, test count or packet-level approval was not used as a substitute for file-specific evidence.

Only this report was written. Existing ledgers and historical evidence were read, not edited; no `review/**` or `TestResults/**` content was accessed. No .NET/MSBuild/test command was run.
