# Core Serializer Configuration and Forwarding Mutation Validation

Date: 2026-08-24

Each effective mutation changed one product behavior or one requirement-projection fact, compiled
the owning native Core test project in Release, ran only the owning tests, and was then reverted.
The final unfiltered profiles ran only after all mutations had been removed.

| Mutation | Owning verdict | Result |
|---|---|---|
| Accept only serializer media types that are not registered | `SerializationConfigurationTests` | Exit 2; all six configuration-validation facts rejected the inverted registry decision. |
| Accept only default deserializer media types that are not registered | `SerializationConfigurationTests` | Exit 2; all six configuration-validation facts rejected the independently inverted default-content decision. |
| Discard the per-message options callback result | `SystemTextJsonPerMessageConfigurationTests` | Exit 2; replacement and null-result boundaries failed while the independent mutate-and-return control stayed green. |
| Give the global callback the live shared options instance instead of a defensive copy | `CallbackReceivesADefensiveCopyOfTheSharedOptions` | Exit 2; the exact identity and unchanged-prior-instance assertions failed. |
| Ignore the replacement returned by the global callback | `CallbackReplacement_BecomesTheExactSharedOptionsInstance` | Exit 2; the exact replacement-identity assertion failed. |
| Replace the original forwarding serializer context with a fresh System.Text.Json context | `ConsumedInterfaceMessage_ForwardsItsCompleteBodyAndMetadataExactlyOnce` | Exit 2; the concrete forwarded delivery was lost and the bounded wait failed. |
| Dereference the absent optional child pipe while probing `ForwardMessagePipe<T>` | `ConsumedInterfaceMessage_ForwardsItsCompleteBodyAndMetadataExactlyOnce` | Exit 2; probing the no-child forwarding pipe threw the exact `NullReferenceException`. |
| Classify `Pipe.Empty<T>()` as non-empty | `EmptyClassification_DistinguishesMissingEmptyAndExecutablePipes` | Exit 2; the complementary empty/non-empty assertions rejected the wrong classification. |
| Remove the explicit forwarding `MessageId` projection | `ConsumedInterfaceMessage_ForwardsItsCompleteBodyAndMetadataExactlyOnce` | Exit 2; the pre-serialization projection contained no original message identifier. |
| Remove the forwarding-specific all-header projection | `ConsumedInterfaceMessage_ForwardsItsCompleteBodyAndMetadataExactlyOnce` | Exit 2; the ordinary header still arrived through consume inheritance, but the ViciOne-internal original-message header was absent from the pre-serialization projection. |
| Disable the normal-transport expiration discard | `ExpiredMessage_IsObservedAndDiscardedBeforeTransportDispatch` | Exit 2; the expired message reached the destination transport exactly once. |
| Disable the persistent-outbox expiration discard | `ExpiredMessage_IsDiscardedBeforePersistentOutboxStorage` | Exit 2; the expired message was added to durable outbox storage exactly once. |
| Disable the mediator expiration discard | `ExpiredMessage_IsDiscardedBeforeMediatorDispatch` | Exit 2; the same-type mediator handler received an unintended second delivery. |
| Treat a cleared final TTL as revival | `ClearingTheInheritedExpiration_DoesNotReviveAnExpiredMessage` | Exit 2; clearing the inherited expiration allowed one unintended destination delivery. |
| Remove the structured discard log | `ExpiredMessage_IsObservedAndDiscardedBeforeTransportDispatch` | Exit 2; no `FORWARD-EXPIRED` event with the required structured fields was observed. |
| Remove the forwarding row from `CoreRequirements.json` | `CoreRequirements_MatchCompiledRequirementMetadata` | Exit 2; the compiled fact was rejected as unprojected. |

An initial experiment observed only the final envelope and therefore could not distinguish an
ordinary non-ViciOne header inherited by `ConsumeSendPipeAdapter<T>` from the forwarding-specific
projection. It is not counted as evidence. The final test first observes the outgoing send context,
uses both ordinary and ViciOne-internal headers, and then independently observes the materialized
envelope. The preservation mutations above were rerun against that stronger boundary. The five
expiration mutations independently cover the normal transport, persistent outbox, mediator, revival
rule, and structured observability rather than treating one green end-to-end scenario as proof of
all five.

Every effective product mutant compiled before its deliberate test failure. No mutant survived,
no ineffective experiment is counted, and the final working tree contains none of the mutation
code.

Verdict: **PASS**.
