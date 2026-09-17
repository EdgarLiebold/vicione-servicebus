# Iteration 184 — Courier result and event-state admission

## Result

This connected packet admits all seven Courier result-evaluation files and ten result-event state
files (17 source files / 1,103 current lines). Three GPT-5.6-Sol xhigh agents worked concurrently
under disjoint execution-result, compensation-result and lifecycle-message ownership. The lead
personally read all admitted source and the 1,001-line owning baseline, then owned integration,
requirements, mutation, coverage, final gates, evidence and publication.

Twenty-five requirement variants produce 32 cases. They bind execution delay validation, direct
forwarding and scheduling, ordered terminal/revision/termination publication, fault compensation,
LIFO compensation continuation, variable updates and removal, cancellation boundaries, public
message materializers, constructor completeness and detached read-only state snapshots.

Four source corrections follow directly from those tests:

1. revised and terminated lifecycle events publish the captured result duration instead of a
   later `Context.Elapsed` value;
2. compensation continuation rechecks cancellation after asynchronous endpoint resolution and
   before forwarding; and
3. lifecycle state snapshots retain the originating public parameter name while explicitly
   rejecting null activity and exception entries.

Cumulative personal source admission is now 434/4,118 current C# files (10.539%).

| Admission manifest | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 17 / 1,103 | `6cc831e3782e41f9297480271f42f517b7e6fcc5801bb269c0183e95065efd1c` | `bbf48b8fb9c66e2be0cea71f5c4e53bd6d338469da0ae91ca7ea567a336434d4` |
| Tests/support | 4 / 1,994 | `45898cdee09f66d87841e219b7ffee1f5e7b437fb11fa765c6aa7ea9768757f4` | `72b1c281ed6ba80e7b618c12e39458286435acc0369209f429f76716132ee5ab` |

Each manifest hash covers ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Each chain hash covers the preceding iteration chain followed by this manifest hash, each
with a terminal newline.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `Courier/Messages/RoutingSlipActivityCompensatedMessage.cs` | 57 | `a00754c57ce5deedd83beef19e38ac1f8b63d694a10c5e123919c4f61181978b` |
| `Courier/Messages/RoutingSlipActivityCompensationFailedMessage.cs` | 62 | `06f8917e3485705969fedb7ce7854fbdd9daa0c1165b888df84a095d0fccd7d1` |
| `Courier/Messages/RoutingSlipActivityCompletedMessage.cs` | 62 | `527dc8982507d327105a9e5436e30abe03e48873a9d1cf12faba0db0d0217a34` |
| `Courier/Messages/RoutingSlipActivityFaultedMessage.cs` | 62 | `5bf7a7019925fb2eab35045021805855fc8549d3ab41fa834ea0801d86df867a` |
| `Courier/Messages/RoutingSlipCompensationFailedMessage.cs` | 52 | `7908557e30758702ef9bd541def910035f06ec9738f41fc13c07bba0f145baf9` |
| `Courier/Messages/RoutingSlipCompletedMessage.cs` | 40 | `0d2bce45b3fc259b2dfad4878c3a76f547c64fdca1814a8a9f08040540a5dee1` |
| `Courier/Messages/RoutingSlipFaultedMessage.cs` | 64 | `7494c09c05200922c1abb163ad2b68d2ccae7c62392e07771e15e6250f1a708b` |
| `Courier/Messages/RoutingSlipMessageState.cs` | 72 | `0d33ddf824049f11520a8906c647904d8fd9a81c4a0cbc30d4380b1831fe5baf` |
| `Courier/Messages/RoutingSlipRevisedMessage.cs` | 62 | `ee5545ddf2719bc5411e97558b30a5e7d05b4443e48f809af67d558596f42c09` |
| `Courier/Messages/RoutingSlipTerminatedMessage.cs` | 60 | `67adc5eef0235a26b3cab453a0a57cebdd42170231e25b9c8455da340dc4653c` |
| `Courier/Results/BaseExecutionResult.cs` | 75 | `5de485735e99c4fdb384ebacc6020512abc7ba309eaa0cab29ee1886b2ac858b` |
| `Courier/Results/CompensatedCompensationResult.cs` | 121 | `e23fff95f9a227f34e488487cfbefb123f1edcf029d2350c886873296b4a2507` |
| `Courier/Results/CompletedExecutionResult.cs` | 120 | `1ff9428299f039273824ca286aff1906d319f0d0f7190344fc6b90368bd6de31` |
| `Courier/Results/FailedCompensationResult.cs` | 45 | `32d1830d84750c08a05774078c443c3992ea23f8f2739f0c1a71a21128fb90dd` |
| `Courier/Results/FaultedExecutionResult.cs` | 96 | `907e591c0593b05f430bed7a8377c842b31888de4886df44dea41a9c66eabb10` |
| `Courier/Results/ReviseItineraryExecutionResult.cs` | 43 | `558ab7003a18babab7f42ae49a5981296be9971d7d29d12236e027560ddbc9ac` |
| `Courier/Results/TerminateExecutionResult.cs` | 31 | `f2a92418632c810c74233dd3b083a7c921dc035cd6a826b840466ff7adad2622` |

| Test/support file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/Testing/ViciOne.ServiceBus.Tests.InternalAccess/InMemoryOutbox/InMemoryOutboxTestContextFactory.cs` | 487 | `5e27a8fcc93a351f97bb9f453b0e1b2f072176576d53fa43de93d4139ed6c633` |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierCompensationResultEvaluationContractTests.cs` | 418 | `51ea12c4c5e2124ab393005415778b7a042d6019073ce00226f057aad603e6c6` |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierExecutionResultEvaluationContractTests.cs` | 718 | `4c296f3cfb66cf5935f7b6ee268fc8f180358db2c028ed7d6de5ba3830746c3e` |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierResultMessageStateContractTests.cs` | 371 | `82b3699d4ef49b12b41b01dc55b63568e6952b42056fb7c8683d1360120fa735` |

## Proof

Six successfully compiled single-cause mutants were killed and restored:

1. replace revised-event captured duration with the later context elapsed value;
2. replace terminated-event captured duration with the later context elapsed value;
3. omit the post-resolution compensation cancellation checkpoint;
4. lose the public dictionary parameter name at the snapshot helper boundary;
5. delegate null activity entries to an unrelated nested-constructor exception; and
6. reject the valid zero-delay scheduling boundary.

Every mutant failed its exact owning test. The restored `BaseExecutionResult.cs` has no diff, and
only the four intended product files retain source changes. The final assertion audit found no
assertion-free, trivial-only or self-referential additions. New tests combine equality, identity,
type, exception, negative, collection, state-transition, ordering and side-effect assertions.

Final Courier Cobertura is
`/private/tmp/vicione-servicebus-iteration-184-final.cobertura.xml`, SHA-256
`465eb63d2f8299870d06cb13fbd701203bca9a65d9a4fc172e7ffc2119bb8da9`.
Fifteen admitted top-level classes report 100% line coverage. `CompletedExecutionResult` reports
96.67% and `FaultedExecutionResult` 94.74%; both generated `EvaluateAsync` state machines report
100%. Maximum admitted-target method complexity and CRAP are both 8, below the threshold of 30.

Twenty-five variants are embedded in `CoreRequirements.json`, final SHA-256
`7a5443a749fef9149fb24b3dc80a52a020da160dfdd7810ca625e4ca9796adcd`. Unit sorted-display-name
SHA-256 is `0dcf24404a008cef8e09c516ff0140ad209834c34bebcdbf736f3509e698ceda`
across 4,933 displays.

| Gate | Result |
| --- | --- |
| Three final owned classes | 32/32 passed |
| Complete Courier namespace with fresh coverage | 253/253 passed |
| Full Core Release | 4,933/4,933 passed with suite parallelism disabled |
| Full EF unit Release | 249/249 passed |
| Release Core-test / EF-unit / EF-local builds | 0 warnings, 0 errors on final serial runs |
| Core/EF/local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format and diff checks | Exit 0; no whitespace errors |
| Mutation probes | 6/6 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
The EF builds share dependency output paths and were therefore finalized serially. The
database-dependent local matrix remains externally configured; its requirement projection and
Release build are green, and no credential was inferred or written.

Residual direct-test gaps are delayed fault-compensation scheduling and malformed next-address
state. Once forwarding begins, the underlying Courier API owns its consume-context token rather
than the evaluation token. Arbitrary dictionary values, `HostInfo` and diagnostic payloads retain
reference identity; this is an intentional shallow-boundary contract rather than recursive graph
cloning.

The mandatory static source-pairing scanner was not executed because its fixed recursive walk has
no exclusion control for protected trees. Exact symbol pairing inside the Courier test directory
was used instead; this is a static reference map, not coverage evidence.

Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-184-courier-result-event-state-admission-2026-09-17`.

Whole-fork personal reading, global API/naming/nullability/coverage and configured external-provider
acceptance remain open. Remote publication is an independent delivery step and cannot pause or
deactivate the active goal.
