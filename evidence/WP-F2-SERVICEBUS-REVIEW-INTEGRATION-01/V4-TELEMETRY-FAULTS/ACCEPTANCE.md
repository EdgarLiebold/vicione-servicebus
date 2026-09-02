# V4 telemetry and fault-envelope acceptance

## Bound inputs

- product baseline commit: `894985b3c8b6db3bfe06e04926becee11c7774ba`;
- product baseline tree: `6f0aa701c05e833c30ee747cc77ecc6c3aee0f75`;
- aggregate read-only review SHA-256: `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`;
- V4 bundle SHA-256: `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`;
- V4 donor commit: `f8050928d1065145bf76fa76488644ca5c834c86`;
- V5 bundle SHA-256: `af76f8f4266efc7aa6d2bb34d29b30c0b73bb04dee7cb8237e4ab42717e1fdf5`;
- V5 cumulative patch SHA-256: `88f2c61a3b2fc470f525ce68a3972463e731bea418cfa9de0d58f4e745a1a344`;
- V5.1 delta SHA-256: `fac6328e141fd71daf01db0efe9987a79a5d561e8929ab209c4e70b9dbe7562c`.

The reviewer material was inspected from a read-only extraction and used only as semantic evidence.
Nothing below `review/` was edited, deleted, staged, or generated.

## Accepted behavior

- Activity creation, start, tag, baggage, event, status and stop are isolated from hostile listener
  and secondary-logger callbacks.
- When no Service Bus child activity is created or started, the captured ambient activity ID, trace
  state and non-reserved, non-blank baggage still cross the message boundary.
- Normal send/receive/process traces carry trace state and baggage together with the existing parent
  relationships and messaging tags.
- `FaultEvent<T>` and `ReceiveFaultEvent` retain at most the first 16 aggregate exceptions; the public
  explicit exception collection rejects null and retains at most its first 16 values.
- A projected exception retains at most 16 inner nodes and 32 distinct case-insensitive data keys;
  keys are bounded to 256 characters and diagnostic text to 2,048 characters.
- Diagnostic projection preserves primary-wrapper data precedence and remote exception identity.
  Hostile diagnostic getters and arbitrary application `ToString()` implementations cannot replace
  the original messaging failure.

## Verification

- locked restore of `ViciOne.ServiceBus.Tests.Unit.slnx`: exit 0;
- locked restores of the general, Azure Service Bus, RabbitMQ and SQL Server LocalIntegration
  solutions: exit 0; these generated props are required because the Architecture project evaluates
  every native test project;
- non-incremental Release build of `ViciOne.ServiceBus.Tests.Unit.slnx` with analyzers enabled:
  exit 0, zero warnings, zero errors;
- focused fault owner after final refinements: 14 passed, zero failed, zero skipped;
- focused activity owner: five materialized cases passed, zero failed, zero skipped;
- focused evaluated-build-graph control after all profile restores: 17 passed, zero failed,
  zero skipped;
- complete native Unit/Architecture solution: 2,996 passed, zero failed, zero skipped;
- scoped whitespace verification of all nine changed C# paths: exit 0;
- `git diff --check`: exit 0;
- 15 buildable one-cause mutations were each killed by their intended owner and then reverted;
  details are in `MUTATION_VALIDATION.md`.

The first complete run exposed one precondition failure rather than a product failure: three isolated
LocalIntegration solutions had not yet been restored, so their generated MTP props were absent and
`IsTestingPlatformApplication` evaluated empty. Restoring every evaluated profile and rerunning the
unchanged architecture test produced 17/17, followed by the single green 2,996/2,996 acceptance run.
This extends the prior documented restore-order diagnosis from two graphs to all five evaluated
solution graphs.
