# Work package C verified counterexamples

This package retained behavioral counterexample evidence for the highest-risk correction discovered
while hardening the renamed API. It does not claim a repository-wide mutation score or independent
certification.

## Durable-send completion boundary

The original failure test signaled as soon as the consumer observer reported a fault, before the
receive pipeline had reached terminal completion. Replacing that timing-sensitive observation with a
causal `IReceiveObserver.PostReceiveAsync` barrier made an existing product defect deterministic: the
in-memory durable-send completion filter retired an intent after consumer failure.

Before the product correction, the hardened class failed in six of six executions: the store contained
zero awaiting intents where one was required. The completion filter now retires an intent only when
the receive context is delivered and not faulted. The same terminal barrier also protects the related
unconsumed/dead-letter case.

After the correction:

- `InMemoryDurableSendIntegrationTests` passed 3/3 in each of six repeated focused executions;
- the full UnitArchitecture profile passed 3,502/3,502 in all three accepted runs;
- no delay, polling interval, or increased timeout was used to obtain the result.

The counterexample proves that removing the `IsDelivered`/`IsFaulted` guard, or signaling before
terminal receive completion, is detected by the focused regression.

## Transport-selection namespace boundary

The first packaged-Journey build after moving provider namespaces failed in Journeys 06, 07, and 09
because four transport selection calls could no longer resolve without the Configuration namespace.
That consumer-compile failure exposed the missing discoverability import and was corrected before the
accepted run. A later breadth scan found the legacy Azure `CreateUsingServiceBus` entry point; the
architecture gate now discovers all public `Using…` and `CreateUsing…` transport selectors and rejects
any file outside `ViciOne.ServiceBus.Configuration`.

The accepted migration dry run reports zero pending rewrites and zero pending renames, and the final
Developer Journey gate compiles all 14 scenarios from fresh packages.
