# Iteration 143 — initializer capability and runtime dispatcher cache ownership

## Purpose and admissions

This packet personally reads the complete eight-file `ViciOne.ServiceBus.Initializers` capability project, its direct advanced capability contracts/extensions, all three runtime endpoint dispatchers and the five owning test files. The capability project itself has no material source finding: its validation, overload forwarding, generic contract/response selection, task and cancellation identity, context-variable sharing and isolation agree with current comments and behavior evidence.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 20 / 2,285 | `1b93f065293b65bf14c5f09b9a7b5f9072c4997723e52117afbd6104650ec179` | `c927f785f4727b9d9e81d1f03a1d698c58c7dd9ac991f4d0071b37d2fba9c6d7` |
| Owning tests | 5 / 1,093 | `c663654f8ab40b097678d95d8b3070ad96f06c619f5c8d403a9ccc2e506f3f69` | `e0675caa2eff1c4e90cc9a6f59b93960845550d92e996af4ffa9d6869f8aa173` |

The source chain extends Iteration-142 source-manifest hash `1a9235e43ede49c300e7eeecd0596538c8b4554dede9fdf7e842245888e30024`; the test chain extends `38cacda58f8995d529a51a9bf71b32f0ef4eb021cf32effb3e2f982ec2e96a5d`. Manifests were frozen after productive and test edits.

## Finding and correction

`SendEndpointDispatcher`, `PublishEndpointDispatcher` and `ResponseEndpointDispatcher` each used a process-lifetime `ConcurrentDictionary<Type, Lazy<TConverter>>`. Every runtime contract type supplied by callers became a strong key, while the corresponding closed generic converter also referenced that type. A contract emitted into a collectible assembly therefore remained rooted after every caller-owned reference was gone.

All three caches now use `ConditionalWeakTable<Type, Lazy<TConverter>>`. This gives the key/value cycle ephemeron ownership: live runtime types still reuse one lazily created converter, but the cache does not independently keep a collectible key graph alive. Public signatures, null validation, assignability rejection, selected generic converter, forwarded object/context/token values and returned `Task` identity remain unchanged. `LazyThreadSafetyMode.ExecutionAndPublication` remains explicit.

## Handwritten behavior evidence

One handwritten theory produces three native cases under requirement `REQ-VSB-RUNTIME-DISPATCH / collectible-contract-caches-do-not-pin`. Each case creates a sealed runtime contract with `AssemblyBuilderAccess.RunAndCollect`, dispatches it through send, publish or response, verifies exact returned-task identity and one proxy invocation, clears the proxy recorder's closed `MethodInfo`, then requires both weak `Type` and assembly references to die after bounded forced full collections.

The unchanged-product fixture compiled with 0 warnings/errors and passed 0/3: each runtime `Type` remained alive. The corrected fixture passed 3/3. The entire dispatcher class passed 10/10, and the four directly owning capability/variable classes passed 20/20.

Three compile-valid one-at-a-time mutations replaced the relevant weak cache with its former strong-key dictionary:

| Mutated dispatcher | Focused result | Causal observation |
| --- | ---: | --- |
| Send | 0/1 | Runtime contract `Type` remained alive. |
| Publish | 0/1 | Runtime contract `Type` remained alive. |
| Response | 0/1 | Runtime contract `Type` remained alive. |

Each mutation was immediately restored. The post-mutation fixture returned to 3/3 green.

## Static pairing review

The mandatory pairing analyzer ran exactly once successfully in isolated directory `/private/tmp/vsb-iteration143-pairing.7DVTXD`. Its raw 21-source count included the copied analyzer itself; excluding that tool file gives the actual packet: 20 source files, five test files, 14 paired and six unpaired. All three corrected dispatchers pair to `EndpointDispatcherTests`.

The six heuristic misses are:

- `AdvancedMessageSchedulerExtensions.cs`
- `AdvancedPublishEndpointExtensions.cs`
- `AdvancedRequestClientExtensions.cs`
- `AdvancedSendEndpointExtensions.cs`
- `InternalInitializerEndpointExtensions.cs`
- `InVar.cs`

Static extension invocation, reflection and higher-level behavior can be owned indirectly, so these misses are navigation evidence rather than proof of missing assertions or runtime coverage. Analyzer-tool trimming warnings are not product build warnings.

## Terminal validation

| Gate | Result | Duration |
| --- | --- | ---: |
| `ViciOne.ServiceBus.slnx` strict Release build, no restore | 0 warnings / 0 errors | 5.81 s |
| `ViciOne.ServiceBus.Tests.Unit.slnx` strict Release build, no restore | 0 warnings / 0 errors | 2:12.65 |
| Engineering format verification | Exit 0, no findings | — |
| Unit format verification | Exit 0, no findings | — |
| Unfiltered native Abstractions | 752 passed; 0 failed/skipped/other | 1.429 s |
| Unfiltered native Core | 4,799 passed; 0 failed/skipped/other | 33.029 s |
| Separate Abstractions coverage | 752 passed; 0 failed/skipped/other | 1.329 s |
| Separate Core coverage | 4,799 passed; 0 failed/skipped/other | 28.494 s |

Builds disabled build servers and shared compilation, used one MSBuild node and no node reuse. The initial format and coverage attempts were rejected as environmental evidence after sandbox-denied local named-pipe creation; the identical gates were rerun successfully outside that IPC restriction.

The Abstractions parent inventory contained 749 tests. Exact CTRF name reconciliation removed none and added exactly these three forms:

- `RuntimeDispatcherCaches_DoNotRetainCollectibleContractTypes(dispatcher: 0)`
- `RuntimeDispatcherCaches_DoNotRetainCollectibleContractTypes(dispatcher: 1)`
- `RuntimeDispatcherCaches_DoNotRetainCollectibleContractTypes(dispatcher: 2)`

Its parent/final sorted-name SHA-256 values are `02540c6715bf20a0dde3c4c4890bcd2c428ce0d3e0a6725f5d84b635247acbc6` and `0b70ce37e9d971e85168c4b152029d052f125838e7fdb199a9c297b7ebac1921`; parent/final CTRF hashes are `76f9894c0bb834d05ecd1aa1687b9be7d8698a358032485ec369f8d4b441d659` and `6cbedfe2fc60c8f8da1606d5a0efa26a6f614490a61f4e6ad345264eae5f2d3d`.

Core retained all 4,799 Iteration-142 names with no addition or removal. Its sorted-name SHA-256 remains `6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`; the current final CTRF SHA-256 is `2af43ec29a2e8563c47fdd0be5ef2ff4661568e4308fe460ec6559e96274aeb5`.

## Coverage and CRAP risk

Coverage uses repository configuration SHA-256 `3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`. Overlapping graphs are reported separately and never summed.

| Graph | Lines | Branches | Methods | CRAP > 30 | Below 80% / zero |
| --- | ---: | ---: | ---: | ---: | ---: |
| Selected Core | 49,679/61,228 (81.1377%) | 17,161/23,340 (73.5261%) | 18,155 | 121 | 4,480 / 3,690 |
| Targeted Abstractions | 5,332/8,310 (64.1637%) | 1,865/2,996 (62.2497%) | 2,762 | 51 | 1,170 / 1,011 |

The documented unique-line CRAP formula was cross-checked by reproducing Iteration 142's exact totals and canonical below-80%-set hash `7055d81d0514f003483f6af57803c5e2a1bcac3e806bb2d539079832128085d0`. Current canonical set hashes are `175955020d1b4ece85dc99376330ad1c6177b0d3d0def84b66260eddbe7514ab` for Core and `c49a3fd20cba95e68f7e446edf2a3090066021627835794c3c2041c0d754dbac` for targeted Abstractions.

Core Cobertura/CTRF hashes are `ee80466400c598ec3d50ea204a8fb16b849162535d5918e0db755c8dcd1a6b0c` and `6092fe0eaf4a744bb3f2ce65f80c73c31c1228b2a7548fead9b9a3a57f70bcc4`. Targeted Abstractions Cobertura/CTRF hashes are `6af371b51586458938e84b2af9d4c5a3b9eafaea261eb910714961dc6aa12a37` and `0cd979cc1da12c36775ac78d8d45c3d14e8986eef5df369322935d282f777fde`.

| Rank | Selected Core method | Complexity | Line coverage | CRAP |
| ---: | --- | ---: | ---: | ---: |
| 1 | `RequestRateAlgorithm..ctor` | 34 | 0% | 1,190 |
| 2 | `DashedHexFormatter.Format` | 20 | 0% | 420 |
| 3 | `NewIdGenerator.NextGuid` | 18 | 0% | 342 |
| 4 | `NewId.CompareTo` | 16 | 0% | 272 |
| 5 | `AssemblyFinder.FindAssemblies.MoveNext` | 16 | 0% | 272 |
| 6 | `StateMachineSagaMessageFilter.SendAsync.MoveNext` | 38 | 48.8% | 231.39 |

These selected-graph risks are not attributed to this correction. The two additional Core-covered lines, three additional covered branches and one fewer below-80% method may include execution-order variation and are not claimed as causal coverage gain. The bundled PowerShell analyzer was not run because `pwsh` is absent; the same documented method identity, unique-line coverage and CRAP formula were evaluated directly. Optional ReportGenerator/HTML output was not requested or generated.

## Checkpoint and continuing goal

Raw artifacts remain under `/private/tmp/vsb-iteration143-*`, including baseline, candidate, mutation, post-mutation, final, coverage and pairing outputs. Protected review, TestResults and legacy trees were neither read nor modified.

The intended annotated tag is `servicebus-a-plus-iteration-143-initializer-capability-and-runtime-dispatcher-cache-ownership-2026-09-16`. Commit, atomic private push and independent branch/tag-object/peeled-tag verification occur after final non-protected diff review; this report does not pre-claim them.

Whole-fork personal source/comment completion, global API/new-parameter behavior coverage, global bidirectional naming/type/file/directory consistency, legacy/dummy/directive absence, whole-fork coverage/CRAP and real durable-provider/external acceptance remain open. This subsystem packet proves the initializer capability read and the three runtime dispatcher cache-ownership corrections only.
