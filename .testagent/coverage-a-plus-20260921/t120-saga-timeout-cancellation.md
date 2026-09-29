# T120 — Saga timeout cancellation ownership

## Product defect and correction

Saga request timeout scheduling did not explicitly set the scheduling token.
Cancellation used the request ID. A previously registered selector could mask
this mismatch for some providers, but Azure Service Bus assigns a broker token
after send and transport delay cannot recall an accepted message. The shared
request activity now sets `ScheduledMessageId` to the request ID and admits a
positive timeout only when the resolved scheduler declares that it accepts and
cancels a caller-specified token. This preflight runs before request-ID
generation, endpoint resolution or dispatch. A timeout of zero remains valid.

The additive `IScheduleCancellationCapability` reports `Unknown`,
`CallerSpecifiedToken`, `ProviderAssignedToken` or `Unsupported`. Concrete
Endpoint, Publish and SQL providers report caller-specified; Azure Service Bus
reports provider-assigned; Delayed reports unsupported. The public base class
defaults to unknown so third-party subclasses cannot be silently admitted.
`MessageScheduler`, `ConsumeMessageSchedulerContext` and
`InMemoryOutboxMessageSchedulerContext` forward the mode.

## Behavioral evidence

| Requirement | Test oracle |
| --- | --- |
| Request/cancel identity | The real request and cancel activities execute the outgoing schedule pipe, then require the scheduled token, cancellation token and stored request ID to match; one schedule and cancel, no leftover ID, exact continuation order. The test failed red-first before the pipe correction. |
| Early admission | Unsupported, provider-assigned, unknown and undeclared schedulers must raise `ConfigurationException` with no ID generation, endpoint resolution, send, schedule, Saga mutation or continuation. |
| Provider classifications | Direct Core, SQL and Azure tests check the concrete providers against their actual token ownership; the public Base subclass defaults to unknown. |
| Scope propagation | Four-mode Theory requires the same mode through both consume and outbox adapters, each resolved once; it kills a constant caller-token implementation. |

## Verification and limits

- Focused Saga request class: 32/32; focused SQL provider: 4/4; focused Azure
  provider: 1/1; strengthened consume/outbox wrapper class: 8/8.
- Full Core after the product correction: 7,140/7,140, zero failures and skips.
  The final wrapper Theory and requirement projections were added afterward;
  all changed focused tests and Core, SQL and Azure requirement projections
  passed on those final test bytes. No product code changed after the full
  Core run.
- `git diff --check` passed. Read-only adversarial Red Team review returned
  PASS with no remaining concrete P0/P1/P2. It caught a public-base
  classification bug and a four-mode test gap; both were corrected.
- Microsoft `code-testing-agent` Research → Plan → Implement was applied
  with `test-gap-analysis`, `assertion-quality` and `run-tests` guidance.
- There is no single live-provider end-to-end Saga test. The combination of
  real request/cancel activities, executed token pipe, concrete provider
  classification and both scope adapters checks the causal contract. External
  scheduler acceptance after request dispatch remains a distributed-operation
  boundary. No new global coverage/CRAP profile was run for this packet.
