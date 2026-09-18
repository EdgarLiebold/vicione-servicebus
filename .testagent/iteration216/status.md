# Iteration 216 status

- Scope: twelve state-machine behavior construction, terminal, adapter and fault-dispatch sources,
  now admitted.
- Progress: 784/4,118 personally read C# sources (19.039%).
- Product outcome: behavior builders reject null activities atomically, publish one ordered frozen
  snapshot under concurrent materialization, and share the public catch terminal implementation.
- Boundary outcome: empty, faulted, data, last, last-catch and execute-on-fault behaviors now own
  every required constructor, visitor, probe, execution and fault-context null boundary with exact
  parameter precedence.
- Fault outcome: ordinary failures retain causal identity even when cancellation changes later;
  matching cancellation bypasses fault handling, foreign cancellation is fault-dispatched, pending
  activity/fault tasks are awaited, and direct fault forwarding preserves context, continuation and
  task identity.
- Lifetime outcome: runtime exception dispatch now uses an ephemeron cache, retaining stable
  same-type concurrent dispatch while allowing collectible exception types and assemblies to unload.
- Tests: 47/47 new focused cases; 1,177/1,177 Saga-wide; 5,958/5,958 full Core; and 249/249 EF unit.
- Requirement projections: Core, EF unit and EF local integration are each 1/1 green.
- Builds: final Core, EF unit and EF local Release builds are warning-clean and error-free.
- Formatting: scoped product and test `dotnet format --verify-no-changes` gates are clean; JSON and
  `git diff --check` are clean.
- Mutation proof: 15/15 compiled isolated material mutants were killed and restored. They covered
  builder null/freeze/terminal policy, terminal continuation selection, null boundaries, exception
  identity, adapter forwarding, execute-on-fault construction/task identity, visitor chaining,
  runtime fault dispatch, cache lifetime/cancellation and cancellation classification.
- Coverage: 243/245 owner class-line entries and 39/42 owner branches. Every reachable owner line
  and branch is covered. The two residual lines and two branches are the typed/untyped
  `CachedConfigurator<TException>` mismatch throws, structurally unreachable because cache
  selection closes the configurator over `exception.GetType()`. The third residual branch is the
  `Activator.CreateInstance` null fallback, unreachable for the validated closed exception type and
  parameterless cached configurator.
- Maximum owner-method CRAP: 4.000; no owner method approaches the risk threshold.
- Coverage artifact: `/private/tmp/vsb-iteration216-core-final2.cobertura.xml`, SHA-256
  `a8142816ae60bcde1bc22ccf5b91eea69ea402acaf754bcb0f2fa622ce38360e`.
- Three independent Sol-xhigh final cross-audits of the disjoint packets report no findings after
  remediation.

## Final source manifest

- `47428ae820e8a197189911bc04a523c10c2af4fa9248ab8a56216a6feda9ac22` — `ActivityBehaviorBuilder.cs`
- `423a288616e4e098a6aa0a0f82d4262324312cd70036a7242a0b6a232f5eb9ca` — `IBehaviorBuilder.cs`
- `d3eee441a45566de4eb98e43de3eb4673c9c66abf77d0aa8ff9eec59e910e2f1` — `Behavior.cs`
- `8eb82c46569c2099bdc5479392a4ff8a375bf02a43831a88ab5f0ae96bf401ee` — `LastBehavior.cs`
- `eeeb8eec3ec2b38c4bb2f5af93fd3ec0618d5db34ea08bffe34ce73c5a6ad081` — `ActivityBehavior.cs`
- `188f6869fb967be9c21bc55eea4981c7909d81dd045d7a7cbcfaa5f861b6af1f` — `FaultedBehavior.cs`
- `09df9281dc8e191fc5ed49dbcd8716a35e3948376b0ee5bf8ac56a8593a0a4e0` — `EmptyBehavior.cs`
- `afa7b58af7a000ed74a7e3af8504f59ab8c8ad5f880775bac504410c90776859` — `DataBehavior.cs`
- `24b7c58b6e47c983da657b2eb521a6bc4802d8bf2a39b5148c07194192686159` — `CatchBehaviorBuilder.cs`
- `68e110e2dd025e6fd787c188fa3ec14149d7ce65e0896a023609cf448d1cdd3d` — `ExceptionTypeCache.cs`
- `a0dfc5fa46bcc8b0abcc66c162e556aa3f9636cf064b375a2b768c04b3c80853` — `LastCatchBehavior.cs`
- `a9cfbd7ca0b1f6b545a349cb5a972cdb9fda479d21654ebdb82d7d67e2104865` — `ExecuteOnFaultedBehavior.cs`

No unresolved correctness, concurrency, cancellation, exception-identity, lifetime, public-surface,
nullability, formatting or material test-risk finding remains in this packet.
