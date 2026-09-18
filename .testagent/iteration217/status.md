# Iteration 217 status

- Scope: seven state-machine composite-status accessor, event-filter and unhandled-callback
  sources, now admitted.
- Progress: 791/4,118 personally read C# sources (19.208%).
- Accessor outcome: both representations bind the exact supplied `PropertyInfo`, including
  inherited and hidden same-name members; public saga/probe boundaries fail deterministically,
  status bits round-trip exactly, and probe metadata remains ordered and representation-specific.
- Filter outcome: all public context boundaries reject null deterministically; selected filters
  reject a missing condition, preserve condition result/failure/context identity, honor compatible
  behavior-context covariance, and reject untyped, erased, inverse and unrelated contexts.
- Callback outcome: the delegate's constructor/invoke/APM surface, generic constraint, parameter
  names and nullable metadata are frozen; context, state, returned task, fault and cancellation
  identity are directly proven.
- Concurrency outcome: shared accessor instances remain independent across gated parallel saga
  instances, and selected filters preserve every concurrently supplied context without hidden
  serialization or cross-call state.
- Tests: 21/21 new focused cases; 1,198/1,198 Saga-wide; 5,979/5,979 full Core; and 249/249 EF unit.
- Requirement projections: Core, EF unit and EF local integration are each 1/1 green.
- Builds: final Core, EF unit and EF local integration Release builds are warning-clean and
  error-free.
- Formatting: scoped `dotnet format --verify-no-changes`, requirement JSON and
  `git diff --check` gates are clean.
- Mutation proof: 10/10 isolated single-cause mutants were killed and restored. They cover exact
  property-descriptor ownership, integer and struct read/write behavior, probe representation,
  selected-filter construction/condition/untyped-context policy, all-filter result/null policy,
  and callback API parameter identity. Nine were compiled and killed by focused tests; the callback
  surface mutation was rejected by the warning-as-error XML/API build gate.
- Coverage: 44/44 executable owner class lines and 2/2 owner branches. Contract-only interfaces
  and the delegate have no executable sequence points. Maximum owner-method CRAP is 2.000.
- Coverage artifact: `/private/tmp/vsb-iteration217-core-final.cobertura.xml`, SHA-256
  `5f9d44dcbf18afdb58091074f9b7d6b7c7571ce5e7bc1ee89c3d7750521eec17`.
- Three independent Sol-xhigh final counter-audits of the disjoint packets report no findings after
  remediation.

## Final source manifest

- `a6d1ed310a4ba08a69c492b055939da4a080423ab9a224c8c557d5083b19dc8a` — `ICompositeEventStatusAccessor.cs`
- `2aeb546457d8a00851fe6930eb73f27ae692829509f8365993110bf199cfaab3` — `IntCompositeEventStatusAccessor.cs`
- `b42c1f0ec76fa3b751a40a7d594f29ba47110f58c881f91832a35e5a4986fd4d` — `StructCompositeEventStatusAccessor.cs`
- `3c67e1ddeb254cf8a2016fdbd3afbb78e8a39e831420d81b1c514c18c2cc1dab` — `IStateEventFilter.cs`
- `7244a9885d1ed4879ce63bfa2d2518a2656aa05074a323697874c2b6ed9f67be` — `AllStateEventFilter.cs`
- `1a3f07047bf23aa8dfbe31f313bf40dc0c43b026dc86431d3f66de767bd1501c` — `SelectedStateEventFilter.cs`
- `89d928be29f45150c6c435bc56e744d901fcd93cd62da510e65437fd8a8037a3` — `StateMachineUnhandledEventCallback.cs`

No unresolved correctness, concurrency, exception-identity, public-surface, nullability, formatting
or material test-risk finding remains in this packet.
