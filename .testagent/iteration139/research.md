# Iteration 139 — scalar property-provider ownership and runtime-type research

## Bound scope and acceptance

The whole-fork A+ source/API/architecture objective remains active. This connected packet covers PropertyConverterPropertyProvider, VariablePropertyProvider, ObjectPropertyProvider, FromNullablePropertyProvider and ToNullablePropertyProvider, together with the exact factory and convention paths that select them. Every listed source file, comment, interface, direct consumer and owning test was read manually before productive correction. No source, comment or semantic disposition is generated.

Accepted provider, converter and initializer-variable operations are owned once started. Caller cancellation is forwarded to each collaborator but must not detach the caller from an already accepted task. This packet contains eight such waits: two in PropertyConverterPropertyProvider, two in VariablePropertyProvider, two in ObjectPropertyProvider and one in each nullable adapter. In contrast, caller-owned task-valued inputs in AsyncPropertyProvider remain locally cancellable and are not changed.

Existing null/default rules remain: no input returns default in the four providers that inspect HasInput; ObjectPropertyProvider deliberately does not inspect HasInput; a null source object returns null; a null variable returns default; a nullable source without a value returns the value type default; and null provider/converter/variable tasks retain their exact diagnostics. A caller-canceled token may reach a downstream stage after an accepted upstream task succeeds; that downstream task is then also owned to its original terminal outcome.

## Correctness and resource findings

DictionaryInitializerConvention's object fallback activates ObjectPropertyProvider for any requested property type even though that provider has a reference-type constraint. An unsupported custom struct target therefore throws during MakeGenericType instead of returning a clean unsupported result. The fallback must be restricted to reference-type targets without changing supported scalar converters.

ObjectPropertyProvider always asks the factory for a runtime conversion. When the runtime value already is the requested reference type, PropertyProviderFactory.Matching supplies a direct property provider but intentionally has no converter. Runtime arrays, custom collection classes and ordinary reference objects sourced through IDictionary<string, object> can consequently resolve to null instead of preserving the exact instance. All Matching consumers are local to PropertyProviderFactory; changing its global converter contract is unnecessary. ObjectPropertyProvider can apply the narrower identity rule before conversion lookup.

The current per-provider ConcurrentDictionary strongly retains every runtime Type and converter result for the provider lifetime, including unsupported runtime types. Runtime source types are open-ended, so the cache must not impose provider-lifetime retention. A ConditionalWeakTable keyed by runtime Type preserves sequential reuse while allowing collectible types and their converter instances to become unreachable together. Converter construction may still be duplicated under contention; that is an allocation observation, not a functional defect or a fabricated exactly-once guarantee.

The nested ObjectPropertyProvider interface name Converter does not express its role and will become IObjectConverter. Extra blank lines around its nested declarations are removed. Comments will distinguish cooperative token forwarding from accepted-task ownership and contain no work-process narrative.

## Existing tests and static pairing

The parent is commit 5c521abbfb72709dbc3709d8d8bde738691207e4. Its unfiltered Core multiset contains 4,645 passed cases and must be retained exactly, with only additive Iteration-139 cases. PropertyProviderContractTests currently expects PropertyConverterPropertyProvider and VariablePropertyProvider to abandon pending accepted tasks on caller cancellation; that expectation is the reproduced defect and must be corrected without deleting the existing test method or requirement binding. PropertyProviderStateMatrixTests already protects default/null/null-task, exact token, sequential converter reuse and ObjectPropertyProvider's lack of a HasInput gate.

The required find-untested-sources Roslyn engine ran once against an isolated copy containing only the Initializers source/test roots and their projects, so review, TestResults and legacy trees were neither enumerated nor read. It classified 88 source files and 48 tests: 76 paired and 12 statically unpaired. All five target providers, PropertyProviderFactory and both convention files are paired to the existing owning fixtures. Static pairing cannot establish line/branch coverage or assertion strength; its value here is the deterministic source-to-test map.

The detected stack remains .NET SDK 10 with the global Microsoft Testing Platform runner and xUnit v3. Focused execution therefore uses SDK-10 named `--project` syntax and xUnit's MTP `--filter-class` argument, with no `--` separator.
