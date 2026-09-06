# Iteration 7 Validation

## Scope

Iteration 7 removes runtime capability probing from the application-facing send, publish, scheduling, and typed consume contracts. Required application behavior is now visible to the compiler. Advanced interfaces retain exact adapters only where their lower-level contracts can implement the complete application operation.

## Red/green evidence

- The initial architecture test failed on nine default application members: typed consume outgoing access, response operations, deferred response, message conversion, send options, publish options, schedule options, and scheduled-message cancellation.
- Removing the typed consume defaults produced 18 compiler errors across forwarding contexts. The shared `ConsumeContext` and `BaseConsumeContext` contracts were completed, after which all production implementations compiled without local capability probes.
- The complete unit solution exposed two intentionally minimal test endpoints that did not implement the newly required options members. Both test doubles now make their supported behavior explicit.
- The focused architecture guard passed after remediation.
- Existing direct behavior tests passed for all send, publish, schedule, consume-outgoing, and response options paths.

## Mutation evidence

Two isolated regressions were introduced and then restored:

1. Reintroducing a default `ISendEndpoint.SendAsync` implementation caused `ApplicationInterfaces_DeclareRequiredCapabilitiesWithoutRuntimeProbingDefaults` to fail and report the exact member.
2. Bypassing `SendOptionsPipe<T>` in the advanced send adapter caused `ApplicationOutgoingOptionsTests` to fail on the missing envelope header.

Both mutations were removed before final validation.

## Final validation

- Release unit/architecture solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,803 passed, 0 failed, 0 skipped across 21 assemblies.
- Release engineering-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Engineering whitespace verification: passed.
- Engineering style verification at warning severity: passed.
- Requirement manifests and Git whitespace: passed.

This iteration is internal engineering evidence and is not represented as an independent external acceptance.
