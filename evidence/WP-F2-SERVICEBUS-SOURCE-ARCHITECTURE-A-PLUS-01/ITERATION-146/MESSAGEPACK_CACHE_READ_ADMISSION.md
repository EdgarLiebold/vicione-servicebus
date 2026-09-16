# Iteration 146 — MessagePack cache lifetime remediation and read admission

## Scope and result

This bounded packet personally rereads every current C# file and comment in
`ViciOne.ServiceBus.MessagePack`, its project metadata, all directly owning C# tests/support, the
test project and the full requirement projection. The owner has 14 C# files / 1,344 lines; the
owning test packet has 38 C# files / 3,983 lines.

The review found one correctness/lifetime defect. The singleton
`ServiceBusMessagePackFormatterResolver` retained arbitrary runtime contract `Type` objects and
their formatter graphs forever through a `ConcurrentDictionary`. This pins contracts from
collectible plugin/dynamic assemblies. The resolver now stores the existing
`Lazy<IMessagePackFormatter>` values in a `ConditionalWeakTable`, so a live contract still receives
one execution-and-publication formatter while dead keys and their value graphs can be reclaimed.

No other correctness, architecture, naming, namespace, placement, dependency, comment or public
API finding was identified. The public assembly remains restricted to the configuration extension
and advanced serializer factory; wire-format ownership, hardened runtime options, forwarding,
metadata, body/envelope ownership and payload-admission boundaries remain coherent.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 14 / 1,344 | `fe066f340f512f714051013a37188b3ec29748cd8e06ad7b7991e688e6da1b54` | `aebc26e7a6438835af4ec5a8d1c4e282911525be42e89c4aea1951adc3539e73` |
| Owning tests/support | 38 / 3,983 | `98ff7e8e4fe24033cbd543cf31481540cb069027efa1a9ff5594addb09bcb237` | `8a7fdef6790051a5ec4a289a8baad4ab416b71358ff303cac9dd89776e261de4` |

The chains extend Iteration-145 manifest hashes
`7bc317225a0312dc8e4bd2ccda879aa92ded8e8e9f109b80bf5e67781b117a65` and
`9e61b42138411f019457243658d28c99da5435e445fbfc66f4037d54c283f746`.
Both manifests validate against the final tree with zero mismatches. Cumulative current personal
source admission is 151 of 4,116 current C# files.

## Red, green and counterchange proof

The focused test
`InterfaceMessagePackFormatterTests.Resolver_DoesNotKeepCollectibleClosedGenericContractAlive`
creates an `AssemblyBuilderAccess.RunAndCollect` payload type, closes `MessageData<T>` over that
type, resolves its formatter through the generic resolver API and releases all local strong
references. It asserts independently that both the closed contract and defining assembly become
unreachable after bounded full GC cycles. The generic mapping route isolates the resolver from the
separate generated-interface-implementation cache.

| Product state | Focused result | Observation |
| --- | ---: | --- |
| Unchanged strong dictionary | 0/1 | The closed contract remained alive. |
| Weak-key remediation | 1/1 | Contract and defining assembly were reclaimed. |
| Restored strong-dictionary counterchange | 0/1 | The same contract-liveness assertion failed. |
| Restored final weak-key source | 1/1 | Regression remained green after hash-checked restoration. |

The counterchange compiled successfully before the focused failure. All three touched files were
hashed before mutation, restored manually and verified byte-for-byte; the final source manifest
then validated every MessagePack product file. Existing
`Resolver_ConcurrentCallsReturnOneSharedFormatterAsync` continues to prove one shared formatter for
a live interface key. The requirement projection adds the exact `resolver-weak-key` binding and
its compiled-metadata parity test passes in the 114-case suite.

## Assertion and gap review

The new test fails when the resolver body is emptied (`Assert.NotNull(formatter)`) and when the
strong-cache defect is restored (contract/assembly liveness assertions). Its dynamic type name is
unique, it uses no external resource, timing sleep, skip, retry masking or random behavioral
expectation, and the no-inline helper prevents the caller frame from extending local lifetimes.

Across the owner suite, positive and negative serialization, exact exception ownership, type and
byte identity, metadata parity, security, fallback behavior, cache concurrency/failure/weak-key
semantics, public API, dependency isolation, forwarding, payload limits and real in-memory pipeline
flows all have concrete assertions. Personal review found no assertion-free, tautological,
unawaited, skipped or swallowed-exception owner test.

The mandatory Roslyn pairing analyzer ran once against the isolated directory
`/private/tmp/vsb-iteration146-pairing.8W9sIN`; protected or unrelated trees were not part of its
input. It classified exactly 14 source and 38 test files and paired all 14 product files.

## Terminal validation

| Gate | Result | Duration |
| --- | --- | ---: |
| Unchanged owner baseline | 113 passed; 0 failed/skipped/other | 1.446 s |
| Strict MessagePack product Release build | 0 warnings / 0 errors | 20.48 s |
| Strict MessagePack test-owner Release build | 0 warnings / 0 errors | 48.51 s |
| Product format verification (`warn`) | Exit 0, no differences | — |
| Test-owner format verification (`warn`) | Exit 0, no differences | — |
| Focused final regression | 1 passed; 0 failed/skipped/other | 0.442 s |
| Native MessagePack final | 114 passed; 0 failed/skipped/other | 1.611 s |
| Native MessagePack final coverage | 114 passed; 0 failed/skipped/other | 2.430 s |
| Native Core final | 4,799 passed; 0 failed/skipped/other | 30.592 s |

The initial sandboxed coverage and both format attempts were rejected as environmental evidence:
Microsoft Testing Platform and Roslyn build hosts could not bind their local named pipes. The
identical coverage and format commands passed outside that IPC restriction. One overly strict
informational format scan identified the pre-existing deliberate primary-constructor suggestion
and a collection-initializer suggestion on the new line; the latter was adopted as `=[]`. The
terminal format contract is the repository's warning-level no-change gate and passed for both
projects.

Final MessagePack sorted-name SHA-256 is
`25baefefeb46de060e6180b90099ff30bed5ab1b1697bddcb03ffe74a16bc19f`; final coverage CTRF and
Cobertura SHA-256 values are
`394b90332cd341ed0b449fe9746fd3698e83bf08fe92044504a2cde487dad6d6` and
`226799e939d1b555dd42987aadde00ffdc461ebbc91babebd87e9053e52ce54e`.
Core retains the exact Iteration-145 4,799-name multiset at
`6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`; final Core CTRF SHA-256 is
`a54bd61acfd61ed98f90edbc49539162ecfd8248a7b30a2d03bb3ba090e77067`.

## Coverage and CRAP

Coverage uses repository configuration SHA-256
`3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`.

| Assembly | Lines | Branches | Complexity | Methods | CRAP > 30 | Below 80% / zero |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| MessagePack | 476/478 (99.5816%) | 173/176 (98.2955%) | 183 | 132 | 0 | 0 / 0 |

The two instrumented zero-hit lines are compiler-required fallbacks directly after
`ExceptionDispatchInfo.Throw()` in `MessagePackMessageBody.SerializeBounded` and
`MessagePackSerializerContext.TryGetMessage`; they are not reachable behavioral branches. The
highest method score is 22 at 100% line coverage for
`MessagePackMessageSerializer.InternalDeserializeObject<T>`; the next is 18 at 100% for recursive
forwarding merge. No CRAP remediation is warranted.

## Checkpoint and continuing goal

Raw artifacts remain under `/private/tmp/vsb-iteration146-*`. Protected review, TestResults and
legacy trees were not modified or used as evidence.

The intended annotated tag is
`servicebus-a-plus-iteration-146-messagepack-cache-read-admission-2026-09-16`. Local commit and tag
occur after exact-path verification. External publication is not pre-claimed and remains subject
to destination- and payload-specific authorization.

Whole-fork personal source/comment completion, global API/new-parameter behavior coverage, global
bidirectional naming/type/file/directory consistency, legacy/dummy/directive absence, whole-fork
coverage/CRAP and real durable-provider/external acceptance remain open. This packet proves only
the current MessagePack owner admission and its fresh evidence.
