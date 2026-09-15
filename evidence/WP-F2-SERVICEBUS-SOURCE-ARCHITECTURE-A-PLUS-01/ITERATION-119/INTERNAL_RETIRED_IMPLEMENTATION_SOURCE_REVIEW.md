# Iteration 119 — internal implementation visibility counterreview

Status: bounded read-only internal Lead advice, not external Red Team acceptance
or whole-product feature-losslessness certification.

Reviewer: separate internal `gpt-5.6-sol` high-effort advisor,
`/root/iteration119_retired_api_source_review`. The main Lead authorizes a concrete
independent read-only structural/source subtask while personally reading the
DynamicFilter/SagaConnector/StateMachineInterfaceType families. The advisor fully
reads its mandatory normative/navigation inputs and exact 1,468-line slice before
actions; truncated normative chunks are recovered. It edits nothing, executes no
.NET/build/test/consumer, installs nothing and delegates no further task.

## Bound input and comparison

Product input: secured `e282a224f1f5f8a6ced6b7162e27bb16997575b7`.
Old baseline SHA256:
`59ea05a49d8d99e64715ac60b79bc68f9b657948f0742fd9c3d3e972babd054b`.
Actual fresh output SHA256:
`96436e3b855dee3c678da4430a01c0947b14126227faff5a37be040455790676`.
Exact slice SHA256:
`5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199`.
All three entry/exit bindings and ten normative/navigation input hashes are
reported unchanged; the main separately checks the retained source checksums.

Comparison groups arrays of **all** type blocks by assembly/represented name.
No duplicate old block is overwritten. Old: 3,287 blocks / 3,262 keys. Fresh:
3,230 blocks / 3,230 unique keys. The six old colliding keys contain 7, 8, 6, 2,
6 and 2 blocks respectively, with 25 extra old blocks in total.

| Corrected owner family | Old / fresh complete family blocks |
|---|---:|
| PipeConfigurator | 8 / 8 |
| SagaConnector prefix | 10 / 10 |
| StateMachineInterfaceType | 6 / 6 |
| ViciOneServiceBusStateMachine | 14 / 14 |
| DynamicFilter | 5 / 5 |

The 35 newly nested-qualified identities are structurally retained, not the
57 actual visibility retirements. Equal block counts alone are not full member,
generic contract, source behavior or extension equivalence proof.

## Complete advisor reading and checksum qualification

The advisor fully reads 50 retired-implementation source files, 3,531 lines, and
19 retained entrypoint/interface files, 1,381 lines. No selected source read is
truncated. Every implementation remains in its tracked source and is internal.
The advisor's individual retired-source entry/exit snapshots are unchanged.
Its first retained-source snapshot is **after** complete reading, not before;
the subsequent checkpoint is unchanged. That weaker entry observation is not
represented as a pre-read hash freeze.

Canonical manifests are UTF-8/ASCII rows `SHA256  tracked-relative-path\n`, sorted
by path, with a final LF:

- 50 retired-source rows:
  `ead33639c524cde200b7f22fc55de9634686971748a90d81cc6f66977638d592`.
- 19 retained-source rows:
  `2d1ceb97f3c7333f4500ebd800f6b126c6ed42763bba3ab6063f2cf13110a4d5`.

The advisor's follow-up transcription incorrectly places ConsumeRetryContext
before ITechnicalFailureClassifier in the 19-file list. The main detects this:
its presented-order digest is `619810a2285792cff5e3a998bc362b0c0bec66b5d7c099f1f954fae1d945db52`,
not the canonical digest. Sorting the exact same paths matches the stated
`2d1ceb97...` digest; individual file hashes match. This is a corrected evidence
ordering issue, not concealed as exact-byte transcription or a source change.
The 50-file sorted digest also independently matches the stated digest.
Complete current path/checksum scope is retained in the owned raw manifest logs
and [the main checkpoint hash manifest](NESTED_API_SOURCE_RECONCILIATION_HASHES.md).

## Retained capabilities and entrypoints

| Capability | Completely read retained public entrypoint/contracts |
|---|---|
| Message retry composition | `RetryConfigurationExtensions.UseMessageRetry` and `MessageRetryConfigurationExtensions`, including typed/untyped message, consumer, handler, batch, activity and bus-shutdown cancellation configuration |
| Arbitrary context pipe retry | `RetryConfigurationExtensions.UseRetry<T>` |
| Immediate/interval/incremental/exponential policies | Configuration extensions and `Advanced.Retry`, returning public `IRetryPolicy` |
| Exception selection | `Advanced.Retry.All`, `Selected`, `Except`, `Filter`; `IExceptionFilter`, `ExceptionSpecification` |
| Built-in and custom rescue projection | `RescueConfigurationExtensions`, including public custom projection/branch methods |
| Retry observation | `IRetryObserver`, `IRetryObserverConnector`, `IRetryConfigurator` |
| Custom policy/context implementation | `IRetryPolicy`, `RetryPolicyContext<T>`, `RetryContext<T>`, `ConsumeRetryContext` |
| Technical failure classification | `TechnicalRetryPolicy.DefaultFailureClassifier`, public `ITechnicalFailureClassifier`, classifier override injection |
| Delayed redelivery | `DelayedRedeliveryExtensions`, technical-policy configuration extensions |

No built-in retry/rescue/observation/classification/redelivery deletion is found
in this static source scope. Real package journeys from the previous checkpoint
are separate execution evidence; the advisor itself runs none.

## Genuine API break and remaining extension question

Direct construction, concrete-type inspection and subclassing of retired classes
are genuinely removed public operations. The user authorizes Greenfield
modernization without compatibility-only aliases, but retaining capability is
still a requirement. Public IRetryPolicy/RetryPolicyContext/RetryContext support
custom policies, so exposing concrete timing-policy implementation classes is
not automatically required.

The narrower custom consume-aware extension remains explicitly open: consumers
formerly could derive RetryConsumeContext/RetryConsumeContext<T> and directly
compose specialized wrappers/specifications. Those implementations are internal.
Public interfaces preserve customization contracts, but this bounded static
read does not independently demonstrate an equivalent custom projection or
deferred-fault package-only journey. Main finding NST-10 requires explicit
feature-preserving Greenfield disposition and executable evidence. No overall
losslessness or full A+ API acceptance is claimed.

## Exact 57 retired represented identities

Assembly ownership precedes each represented type; generic arity is preserved.
These are the actual visibility changes, not flattened old nested aliases.

```text
ViciOne.ServiceBus.Abstractions|ViciOne.ServiceBus.Observables.RetryFaultObserverCache
ViciOne.ServiceBus.Abstractions|ViciOne.ServiceBus.Observables.RetryObservable
ViciOne.ServiceBus.Sagas|ViciOne.ServiceBus.RetryPolicies.RetrySagaConsumeContext<TSaga>
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.CompensateContextRetryPipeSpecification<TLog>
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.ConsumeContextRescuePipeSpecification
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.ConsumeContextRescuePipeSpecification<T>
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.ConsumeContextRetryPipeSpecification
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.ConsumeContextRetryPipeSpecification<TFilter,TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.ConsumerConsumeContextRescuePipeSpecification<T>
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.ExecuteContextRetryPipeSpecification<TArguments>
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.MessageRetryConfigurationObserver
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.MessageRetryConsumerConfigurationObserver<TConsumer>
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.MessageRetryHandlerConfigurationObserver
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.ReceiveContextRescuePipeSpecification
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.RescuePipeSpecification<TContext,TRescue>
ViciOne.ServiceBus|ViciOne.ServiceBus.Configuration.RetryPipeSpecification<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.Middleware.ActivityRedeliveryRetryFilter<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.Middleware.RedeliveryRetryFilter<TContext,TMessage>
ViciOne.ServiceBus|ViciOne.ServiceBus.Middleware.Rescue.RescueExceptionConsumeContext
ViciOne.ServiceBus|ViciOne.ServiceBus.Middleware.Rescue.RescueExceptionConsumeContext<TMessage>
ViciOne.ServiceBus|ViciOne.ServiceBus.Middleware.Rescue.RescueExceptionConsumerConsumeContext<TConsumer>
ViciOne.ServiceBus|ViciOne.ServiceBus.Middleware.Rescue.RescueExceptionReceiveContext
ViciOne.ServiceBus|ViciOne.ServiceBus.Middleware.RescueFilter<TContext,TRescueContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.Middleware.RetryBusObserver
ViciOne.ServiceBus|ViciOne.ServiceBus.Middleware.RetryFilter<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.BaseRetryContext<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.BaseRetryPolicyContext<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ConsumeContextRetryContext
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ConsumeContextRetryContext<TFilter,TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ConsumeContextRetryPolicy
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ConsumeContextRetryPolicy<TFilter,TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ConsumeContextRetryPolicyContext
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ConsumeContextRetryPolicyContext<TFilter,TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.DefaultTechnicalFailureClassifier
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ExceptionFilters.AllExceptionFilter
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ExceptionFilters.FilterExceptionFilter<T>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ExceptionFilters.HandleExceptionFilter
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ExceptionFilters.IgnoreExceptionFilter
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ExponentialRetryContext<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ExponentialRetryPolicy
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ExponentialRetryPolicyContext<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ImmediateRetryContext<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ImmediateRetryPolicy
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.ImmediateRetryPolicyContext<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.IncrementalRetryContext<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.IncrementalRetryPolicy
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.IncrementalRetryPolicyContext<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.IntervalRetryContext<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.IntervalRetryPolicy
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.IntervalRetryPolicyContext<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.NoRetryContext<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.NoRetryPolicy
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.NoRetryPolicyContext<TContext>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.RedeliveryRetryConsumeContext<T>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.RetryConsumeContext
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.RetryConsumeContext<TMessage>
ViciOne.ServiceBus|ViciOne.ServiceBus.RetryPolicies.RetryConsumerConsumeContext<TConsumer>
```

The advisor's message omits three generic consume-context variants and abbreviates
two generic identities. The main's initial manual report draft also contains eight
incorrect identity spellings/variants. A read-only comparison retaining every old
block independently selects the 57 absent keys whose actual basenames match the
50 fully read implementation files; comparison finds eight draft-only and eight
actual-only entries. All eight are manually corrected from actual inventory bytes
before this report is accepted. The final exact 57-entry set is verified against
that direct comparison. These documentation mistakes do not change source, but
neither abbreviated advisory names nor an unchecked manual transcription are
accepted as complete API identity evidence.
