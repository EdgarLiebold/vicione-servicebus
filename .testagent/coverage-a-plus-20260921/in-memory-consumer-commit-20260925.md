# In-memory consumer commit: inbox/outbox atomicity

Test commit: `9f623dfd1`. Product source is unchanged.

The new tests use a signed, xUnit-free internal-access driver to exercise the
store's `CompleteConsumer` boundary. They prove count and byte capacity
rollback independently, rollback when a later message conflicts with an
existing intent, rollback when two new messages in one batch conflict, stale
lease fencing, idempotent duplicate intent, missing-inbox rejection, and a
successful retry using the same still-owned inbox lease. Assertions observe
both the inbox disposition and retained outbox messages, not only exceptions.
They do not claim coverage of the full `ConsumeContext.SetConsumedAsync` path.

The Core test project and bridge built with zero warnings and errors from the
existing restored SDK artifact. The final full Core test and projection-gate
run passed 6,438/6,438 with no failures or skips. Its Microsoft CodeCoverage
report is `artifacts/coverage-consumer-commit-20260925/coverage-final.cobertura.xml`,
SHA-256 `ae283c08fd6385d651d71ac7ab7f4af761c79e529d1b9de9336064319de18dab`.
The Core assembly reports 87.3588% line and 79.4340% branch coverage.
`InMemoryReliableStore<TBus>.CompleteConsumer` moved from 24/34 covered lines
in the preceding partial profile to 34/34 and 100% branches; the reported
complexity is 20 and CRAP is 20. The previous partial profile and this full
Core-only run differ in scope, so only the method-level change is compared.

Read-only adversarial review first identified two gaps: the capacity test
checked count only, and the new tests lacked requirement projection entries.
Both were corrected. The final review passed with no remaining finding; the
full Core run executed the projection gate successfully.

The global A+ goal remains open. A fresh isolated build receipt and complete
provider coverage profile still require the host's restore and Docker paths to
work; recent bounded attempts stalled. This iteration establishes a verified
Core test slice, not a global Line/Branch/CRAP result.
