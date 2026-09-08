# Iteration 54: MessagePack serialization architecture

Date: 2026-09-08

Branch: `feature/servicebus-a-plus-api`

Baseline: `077ef83b6a7628e9108c25683822c946d218ce99`
(`servicebus-a-plus-remediation-iteration-53-2026-09-08`)

## Scope and review method

This iteration continues the complete, file-by-file source architecture review authorized by
`WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01`. The coherent source cohort is the complete
`src/ViciOne.ServiceBus.MessagePack` product project: configuration surface, serializer factory,
envelope, forwarding, body and context implementations, serialization runtime, resolver, formatter
invokers and message-data formatter. The project file, lock files, direct and transitive
dependencies, all owning tests, and the relevant repository architecture rule were reviewed with
the source.

Every one of the 14 final production C# files was read, interpreted and reviewed in context before
the iteration was closed. The review covered runtime behavior, failure semantics, public API,
nullability, mutability, concurrency, serialization security, payload admission, naming,
namespaces, type-to-file placement, folder and project placement, dependencies, comments, tests and
packaged usability. Comments were written manually after the owning code was understood. No source
comment generator or bulk comment-rewrite script was used.

The current source census contains 4,124 C# files below `src`. This document closes the MessagePack
cohort; it does not claim that the remaining source files have already completed their personal
file-by-file review.

## Findings and corrections

### Forwarding isolation and payload admission

The former forwarding serializer retained and mutated one envelope instance. A caller could obtain
a lazy first body, call the serializer again with different send metadata, and then observe the
second call's metadata when evaluating the first body. Each `GetMessageBody` call now owns a private
envelope snapshot before metadata is applied.

Payload admission also reserialized a byte array that was already the encoded overlay payload and
unconditionally marked it as native MessagePack. That added an encoding layer and changed the
payload interpretation. Existing encoded bytes are now retained as bytes and the envelope's
native-versus-projected meaning is preserved.

### Overlay semantics

The previous overlay behavior differed from the standard JSON serializer: it replaced nested
objects and arrays, matched property names case-sensitively and allowed null replacements to erase
existing values. The forwarding serializer now:

- matches properties case-insensitively;
- recursively merges nested objects;
- appends overlay array elements to existing arrays;
- retains an existing property when its replacement is null;
- adds new overlay properties; and
- creates a valid projected payload when the supplied MessagePack envelope has no message.

The overlay boundary now applies the serializer's documented payload normalization instead of
assuming that every non-null envelope value is already a byte array. Raw bytes, Base64 text and
arbitrary object values therefore enter the same merge path; malformed Base64 remains an explicit
`FormatException` rather than an accidental cast failure. The resulting projected payload is marked
non-native and remains consumable both with and without payload admission.

### Object conversion and failure boundaries

Whitespace-only string values previously entered Base64 or MessagePack decoding and failed rather
than representing an absent value. Reference- and value-type deserialization now return their
declared default for null, an equal default, or blank text while preserving direct values, scalar
type conversion, dictionary projection, Base64 input, raw bytes and arbitrary object serialization.

Malformed outer envelopes remain observable serialization failures. Once a valid envelope exists,
an unsupported or malformed requested contract remains the non-throwing `TryGetMessage` result
required by the serializer-context contract. The payload-admission exception unwrap retains the
original domain exception without unreachable code after `ExceptionDispatchInfo.Throw`.

### Public API

`UseMessagePackDeserializer(IReceiveEndpointConfigurator, bool isDefault = false)` completes the
same serializer/deserializer matrix already offered at bus scope. The overload rejects a missing
configurator, registers exactly one `MessagePackSerializerFactory`, forwards both `isDefault`
values, and does not add an outgoing serializer. Reflection tests bind the exact four public
configuration overloads and their reviewed defaults.

The assembly still exports only the intended composition surface and advanced factory:

- `MessagePackConfigurationExtensions`;
- `MessagePackSerializerFactory`.

No compatibility alias or legacy overload was added.

### Responsibility and type names

Names that described historical implementation mechanics rather than current responsibilities were
replaced:

- `InternalMessagePackResolver` became `MessagePackSerializationRuntime`, the single owner of
  hardened options and all direct MessagePack operations;
- `MessagePackMessageBodySerializer` became `MessagePackForwardingSerializer`;
- `MessagePackMessageSerializerContext` became `MessagePackSerializerContext`;
- `ConcreteFormatterAccess` and `ConcreteFormatterCache` became
  `ConcreteFormatterInvoker` and `ConcreteFormatterInvokerCache`;
- `OverrideMessage` became `Overlay`;
- `IsMessageNativeMessagePackSerialized` became `IsNativeMessagePackPayload`;
- `EnsureObjectBufferFormatIsByteArray` became `GetSerializedPayloadBytes`;
- related fields and locals now describe serialized bytes, message types and invokers directly.

The unused private `Merge` dispatcher, formatter-level compiled-count projection and cache-level
test counter were removed. Cache tests now observe the injected factory at the behavioral boundary,
proving one build per concrete type, concurrent cold-access sharing and deterministic caching of a
failed lazy build without retaining test telemetry in product state.

### Resolver and formatter behavior

The hardened MessagePack options use untrusted-data security and one reviewed resolver composition.
Owned ServiceBus contracts resolve through the exact declared formatter mappings; unrelated
concrete and generic contracts defer to the composite fallback. Interface serialization uses the
runtime concrete formatter when one exists, falls back to the declared implementation for an
interface/null value, shares a weak-keyed lazily compiled invoker per concrete type and rejects a
runtime implementation that violates an explicit concrete mapping.

All non-generic mapped contracts, the open `MessageData<>` mapping, the absent open-generic mapping,
fallback behavior, null handling, invalid runtime implementation, concurrent formatter reuse,
weak-key collection and MessageData value shapes have direct tests.

## Type, file, namespace, folder and project placement

Every top-level production type has one matching file. Former multi-purpose formatter and cache
definitions were separated into `ConcreteFormatterInvoker.cs` and
`ConcreteFormatterInvokerCache.cs`. Test contracts formerly collected as 18 public top-level types
in `MessagePackTestContracts.cs` now each have a matching file; interfaces use the standard `I`
prefix. `RoundTripResult<T>` was likewise separated from the round-trip helper. Constructor-bound
fixtures use primary constructors without losing the no-default-constructor/private-setter contract.

The six repository source-navigation architecture tests passed after the final split and rename.

No physical project move is justified. Direct `src/ViciOne.ServiceBus.*` directories represent
first-class or cross-cutting product capabilities. `Persistence`, `Scheduling` and `Transports`
group multiple provider families. MessagePack is a cross-cutting wire-format capability rather than
a transport provider, so `src/ViciOne.ServiceBus.MessagePack` is the coherent first-level home.
There are no loose C# files in the `src` root. This evidence-based classification remains subject to
the responsibility and dependency review of every remaining project; directory symmetry alone is
not an architectural reason to move a project.

## Dependencies

The project directly depends on MessagePack 3.1.8 and FastExpressionCompiler 5.4.1. The direct
dependency check found both current, the NuGet audit reported no vulnerable package, and lock files
remain in locked mode. An older transitive `Microsoft.NET.StringTools` dependency is owned by the
upstream dependency graph; adding an unneeded direct reference would obscure ownership and was not
done. The product assembly has no optional Courier or JobService dependency.

## Comments and repository hygiene

All comments in the reviewed production project were checked against the final implementation and
corrected manually where needed. They describe present responsibilities, invariants, wire meaning,
arguments and behavior; they do not describe the migration path or how the code was produced.

The reviewed product and test cohort contains no preprocessor or warning-suppression directive. A
broad static scan found no dummy, placeholder, stub, TODO, FIXME, hack or
`NotImplementedException` marker in the cohort. These scans support but do not replace the personal
source review. They do not claim completion for source projects outside this iteration.

## Requirement and test coverage

The source-owned requirement projection was extended for every new test before final validation.
The focused suite covers:

- exact public overloads, defaults, null ownership and both endpoint `isDefault` values;
- factory identity, independent content-type descriptors and concurrent first access;
- MessagePack envelope metadata, clone isolation, native/projected payload identity and body access;
- standalone reference/value conversion from every supported input form and malformed input;
- every owned resolver mapping, concrete and generic fallback, hardened security and concurrency;
- interface nulls, explicit implementation guards, runtime implementations and cache lifetime;
- inline, external, empty and null MessageData shapes;
- isolated forwarding bodies, recursive overlay semantics, missing payload, byte/Base64/object
  normalization, malformed Base64 and payload admission;
- durable send retention, interface dispatch, delayed redelivery, mixed serializers and expiration;
- requirement metadata projection and product-boundary source scanning.

Focused final result: 99 passed, 0 failed, 0 skipped.

Package-focused coverage over the complete `ViciOne.ServiceBus.MessagePack` production assembly:

- line coverage: 99.8%;
- branch coverage: 92.2%;
- methods analyzed: 92;
- methods below 80% line or 70% branch coverage: 0;
- CRAP score above 30: 0;
- highest CRAP score: 18.00 for `MergeObject`, complexity 18 and 100% line coverage.

Raw coverage data and its working report remain untracked below `TestResults`; generated coverage
output is not part of the product commit.

## Test effectiveness

Fourteen deliberate mutations were applied one at a time. Each mutation was built, killed by the
expected focused test and restored before the next mutation:

| Mutation | Detecting behavior | Result |
| --- | --- | --- |
| Reuse the shared forwarding envelope | Lazy-body metadata isolation | KILLED |
| Force a forwarded admitted payload to native | Wire-envelope semantic flag assertion | KILLED |
| Serialize every non-null object as an empty body | Standalone object body contract | KILLED |
| Replace inline MessageData bytes with empty bytes | Inline byte value and address round trip | KILLED |
| Remove blank-text handling | Reference and value default semantics | KILLED |
| Change the endpoint deserializer default to true | Exact public overload/default matrix | KILLED |
| Remove the `Fault` formatter mapping | Complete owned mapping inventory | KILLED |
| Remove null-safe interface runtime-type handling | Interface null round trip | KILLED |
| Let a null overlay erase an existing property | Overlay null-preservation contract | KILLED |
| Replace rather than recursively merge an object | Nested overlay contract | KILLED |
| Match overlay property names case-sensitively | Case-insensitive overlay contract | KILLED |
| Replace rather than append an array | Array overlay contract | KILLED |
| Reserialize existing admitted payload bytes | Payload-admission byte identity | KILLED |
| Restore the unchecked `byte[]` overlay cast | Object-payload normalization | KILLED |

The admitted-payload flag mutation initially survived a weaker high-level assertion. The test was
strengthened to inspect the actual wire envelope, after which the same mutation was killed. This
failure-and-correction is retained as evidence that mutation testing evaluated the oracle rather
than merely confirming test execution. The final cast mutation failed exactly through
`Overlay_NormalizesAnObjectPayloadBeforeMergingIt` with the historical `InvalidCastException`, then
the normalized implementation was restored and the complete focused and Unit suites were rerun.

`git diff --check` passed after every mutation had been restored.

## Validation results

### Build

Strict Release builds with warnings treated as errors passed after the final source and test state:

- focused MessagePack project graph: 0 warnings, 0 errors;
- Unit solution: 0 warnings, 0 errors, 1 minute 29 seconds;
- Engineering solution: 0 warnings, 0 errors, 2 minutes 7 seconds.

.NET and Microsoft Testing Platform commands were executed outside the filesystem sandbox because
MSBuild and the test host require local IPC endpoints. The repository uses the .NET 10 MTP command
surface: `dotnet test --project ... --no-progress`, not a VSTest-style argument tail. Two attempted
VSTest-shaped calls correctly returned exit code 5 with zero tests; they were not accepted as
validation. The final corrected command included `--minimum-expected-tests 99` and passed.

### Tests and architecture

`ViciOne.ServiceBus.Tests.Unit.slnx`, Release, Microsoft Testing Platform v2, one test module at a
time and a hard minimum of 4,597 expected tests:

- 4,597 passed;
- 0 failed;
- 0 skipped;
- 4 minutes 1 second.

The six focused `SourceFileNamingArchitectureTests` passed in 45 seconds after the final production
and test renames.

### Formatting

`dotnet format --verify-no-changes --no-restore --severity warn` passed for the MessagePack product
and test projects and for both the Unit and Engineering solution graphs. The known workspace-load
warning does not represent a formatting or analyzer difference; both solution commands exited 0.
At info level, CA1859 deliberately remains where a body test accepts the public `MessageBody`
abstraction in order to test that contract rather than its concrete implementation.

### Packaged developer journeys and public API

The packed API contract was updated once for the intentional endpoint overload, then the package
gate was rerun without update mode and passed unchanged:

- 18 executable developer-journey scenarios;
- exactly 30 freshly packed ViciOne packages;
- 3 isolated provider-testing package consumers built and executed;
- 29 runtime package APIs matched the committed baseline;
- public API contract: 21,604 lines;
- generated contract SHA-256:
  `85aa7649fdde022895adaa2d501a69d099784985b91f8f96f3cad65b3b93f5c8`.

The packaged inventory contains the new endpoint deserializer overload with `isDefault = false`.
The API was therefore verified through the shipped NuGet surface, not only through project
references.

## Internal Red Team

A separate internal read-only Red Team reviewed the complete final source cohort and tests without
editing files or running the author's validation. This is an internal adversarial review, not an
independent external acceptance. It confirmed one medium finding: the forwarding overlay violated
the serializer's own object-payload normalization contract through an unchecked `byte[]` cast. The
finding was corrected, covered for object, Base64 and malformed-Base64 forms, mutation-killed and
included in every final validation above.

The Red Team also identified and the implementation removed a low-severity test-only compiled-count
field from the production cache. It confirmed no further freeze blocker. In particular,
`headers` and `destinationAddress` belong to the transport-neutral `IMessageDeserializer` contract;
for envelope serializers the encoded envelope remains the authoritative metadata source, matching
the System.Text.Json implementation. The final static recheck found no dummy markers, preprocessor
directives, compatibility shims, stale construction-history comments, file/type mismatch or
unintended public API in the reviewed cohort.

## Iteration conclusion

The MessagePack cohort now has one hardened serialization runtime, isolated forwarding ownership,
correct payload-admission semantics, normalized parity-preserving overlays, explicit conversion and
failure boundaries, a symmetric minimal public composition API, responsibility-based internal
names, coherent files and folders, manually reviewed comments and direct behavioral and mutation
evidence.
No feature was removed; unreachable code, unused implementation members and test-only product state
were removed without changing the supported contract.

This is a secured iteration boundary, not completion of the A+ source architecture goal. The next
iteration continues the same personal file-by-file review with the next coherent source cohort.
