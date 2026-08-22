# C7 implemented-message topology

## Subject

- technical commit: `f3719bdecad4e4769450dd42b6b3a100a4efc9e9`
- technical tree: `bcd0605ce48c97ea3c35a6747037e23c847228a6`
- parent: `cf0e21bb01898d77a78d113b6ce09f19d28a11ac`
- product source changed: `src/ViciOne.ServiceBus.Abstractions/Metadata/ImplementedMessageTypeCache.cs`
- inherited obligation closed: `OBL-R0-CORE-D-0174`

## Product correction

The inherited cache recursively mutated one shared set while consuming the unspecified ordering of
`Type.GetInterfaces()`. The only inherited test asserted a count of one; it did not identify the type
or verify the `direct` value. The behavior could therefore vary with reflection order while the test
still failed to describe the required topology.

The replacement computes the graph explicitly:

- one immediate valid base-class edge;
- every most-specific valid interface edge, with transitive interface ancestors removed;
- interfaces inherited through a base class remain direct edges, so an excluded base topology cannot
  hide an independently valid message contract;
- `Fault<T>` projects the same edges to `Fault<P>` and retains the non-generic `Fault` contract;
- duplicate results are removed and interface ordering is stable.

The base-inherited-interface rule preserves the behavior fixed by the upstream MassTransit
[issue 5676](https://github.com/MassTransit/MassTransit/issues/5676) and its
[fix commit](https://github.com/MassTransit/MassTransit/commit/52eeba992a25d7c7322be705cec9e328efc9d9fa).
The ViciOne implementation is independently simplified and covered by stronger source-owner tests.

## Positive acceptance

| Check | Result |
|---|---|
| Abstractions focused Release build | exit 0; 0 warnings; 0 errors |
| Abstractions focused xUnit 4 / MTP 2 run | exit 0; 121 total; 121 passed; 0 failed; 0 skipped |
| UnitArchitecture Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered xUnit 4 / MTP 2 UnitArchitecture run | exit 0; 676 total; 676 passed; 0 failed; 0 skipped |
| Engineering Release build | exit 0; 0 warnings; 0 errors |
| Product and test whitespace verification | exit 0 |
| Requirement projection and disposition JSON | valid |
| Git whitespace and final worktree | clean |

The unfiltered native command was:

```bash
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx \
  -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/unit \
  --minimum-expected-tests 676
```

## False-green rejection

Each product mutation was applied independently, built, executed through the complete 121-case
Abstractions project, and restored before the next mutation.

| Mutation | Focused native result |
|---|---|
| Stop removing transitive interface ancestors | only the chain and diamond contracts failed; 2 failed; exit 2 |
| Remove interfaces inherited through the base class | only the class and polymorphic-fault contracts failed; 2 failed; exit 2 |
| Remove `Fault<T>` topology projection | only the polymorphic-fault contract failed; 1 failed; exit 2 |
| Admit invalid `System.*` interfaces | only the invalid-interface contract failed; 1 failed; exit 2 |
| Mark every emitted edge as non-direct | every non-empty topology contract failed; 5 failed; exit 2 |

The final rebuild and unfiltered run occurred only after all mutations were restored. No mutation,
generated output, inherited NUnit fixture, or alternative expected result remains in the technical
tree.

## Inherited-tree boundary

The historical `ViciOne.ServiceBus.slnx` is not an acceptance gate during atomic test reconstruction.
Its product projects compile, but three inherited test projects correctly fail the xUnit 4
executable-project rule because they have not yet been replaced by native MTP projects. They were not
retrofitted or weakened. Their replacement and the final root-solution composition are explicitly
tracked in `TODO.md`.
