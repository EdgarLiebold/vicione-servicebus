# Iteration 204 — saga factory, query selector and metadata admission

## Outcome

Saga creation, query-property selection and metadata discovery now own their public boundaries,
publish accurate nullable contracts and reject malformed types before delegate compilation or lazy
connector activation. Metadata role discovery is deterministic and excludes observation contracts
owned by another saga. Connector descriptors bind one exact saga/message pair and isolate all four
lazy role factories.

The lead personally read all eleven current sources before delegating three mutually exclusive
instance-factory, query-selector and metadata/specification source/test pairs to Sol 5.6 xhigh
agents. They are newly unique, moving cumulative exact unique source coverage to 674/4,118 files
(16.367%). The packet contained 416 physical source lines before the change and 490 after admission.

## Corrections and direct contracts

- Constructor- and property-based compiled factories reject interface and abstract saga types
  before constructor/property discovery. Exact public constructor shapes, diagnostics, Guid
  assignment, freshness and unwrapped user exceptions are covered.
- `DefaultSagaFactory` owns `context` and `next` in causal order, preserves correlation failures,
  created saga/source context identity and exact downstream task/exception identity, and rejects a
  null downstream task with a stable diagnostic.
- All three query-selector constructors reject a null callback; all three operations reject a null
  context before callback invocation. Nullable, default and present output matrices preserve exact
  callback/context/output identity.
- The selector contract and concrete reference selector expose the legitimate nullable false-path
  output with `NotNullWhen(true)`. Value implementations mirror the conditional contract without a
  binary signature change.
- Metadata discovery filters invalid messages, orders type identities ordinally and requires the
  second `IObserves<TMessage, TSaga>` argument to identify the current saga.
- Saga factory selection retains Guid-constructor precedence and only accepts a public writable
  `CorrelationId` property for fallback construction.
- Connector descriptors validate required message/saga types before creating lazy state, require
  the exact descriptor saga type before lazy access, isolate four role factories and wrap invalid
  role activation in a stable causal `ConfigurationException`.
- The public message/specification interfaces are frozen through exact inheritance, constraints,
  member and nullability reflection tests.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`2535b0c4f201bffd90fb01219fffa9763e771e36e2e9d0b3935472e442e71fca`. Chaining that hash from
iteration 203 yields
`c09b904a7d155630e6bddd86a49254d4f4565469368d02560d60d944fc204fee`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Configuration/ConstructorSagaInstanceFactory.cs` | 33 | `dbb5b5767f5506e77f8e1c7d22585fee7f2c8cae9bb224e56b33285ed95fb140` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/DefaultSagaFactory.cs` | 49 | `26fec0e1987a890dfaf05dcf370f107f8e0a2d2fd2a67ebcf6782cdcf7cc13b8` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/PropertySagaInstanceFactory.cs` | 36 | `dce6920a952aa45192067b69c883f6099cd8834afd1c079a18b1177131769faf` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/HasValueTypeSagaQueryPropertySelector.cs` | 37 | `91d8fffa4cf2232280e2bf5a84621b19c01333a41367e2ea17e3539520988bfc` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/NotDefaultValueTypeSagaQueryPropertySelector.cs` | 38 | `9efb69973cdeacb21753025110981d524388c976417045b01ef63948dfb46d48` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaQueryPropertySelector.cs` | 37 | `6f1e12ad8116934d03a01ad44d1987b26103276618f2b476ea614c423e162197` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/ISagaQueryPropertySelector.cs` | 16 | `248de7335909c99c071820f386d19223e46d0d95d359e2d7a820867d33da38cb` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaMetadataCache.cs` | 70 | `3f06ec4ed692a0afb98e08c040eb667d51b49e56c6856500047ed5eb9e7ba6da` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaMessageConnectorDescriptor.cs` | 107 | `6df619de7186ff927f996a2dd58892913be13aedb5716907a89656f8c06f2d86` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/ISagaMessageSpecification.cs` | 46 | `2be39677e956b94c12651981770aa67bc638d7b4de20a13f4fe147bdf5373457` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/ISagaSpecification.cs` | 21 | `c8bec5be858fb0292a0c7c5f241edba8ddf64cf854622720053a6c77522fc9b5` |

## Test manifest, assertions and requirements

The three classes contain 30 test methods and 30 unique requirement variants. They assert exact
surface and nullability, type/constructor/property admissibility, delegate caching and identity,
causal guards, correlation and pipeline behavior, selector output matrices, deterministic role
discovery, factory precedence, descriptor preflight, exact pairing, lazy identity and role-failure
isolation. The mandatory code-testing workflow, static source/test pairing, pseudo-mutation gap
analysis and assertion-quality audit drove the research-to-plan-to-test sequence.

Test manifest SHA-256 is
`5492a25e059f7608c81c81118ae527684710cde0ad1cd1f0506f8b9f3c42c312`; chained from iteration
203 it yields
`f4dcca10fef0492b901a83aa50c6159dee2c1ac7354feb6d55c4d81d90765ed9`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaInstanceFactoryDeepContractTests.cs` | 694 | `f4a0201dcf42e191bc62d2a74a6847951584e37fa586ecc0228dde0ceee650b8` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaQueryPropertySelectorDeepContractTests.cs` | 412 | `71f1ba57178cb03832fb3e2863cd016d6c46137fd3be3afc5145b139043600a8` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaMetadataAndSpecificationDeepContractTests.cs` | 495 | `90497967912ef61a4df817c3bef3bb067d9780a6e7cce0290de213b551af94f2` |

`CoreRequirements.json` SHA-256 is
`13ded3c6e4799832615fd88202028fa4cc2a2bb482c9106493b944dc8fdbf10d`.

## Mutation proof

Six compiled, isolated, material single-cause mutants were killed and restored:

1. constructor-factory concrete-class admission was removed;
2. the default factory's create-context guard was removed;
3. the reference selector accepted a null callback;
4. the reference selector forwarded a null context;
5. observation metadata admitted a role owned by another saga; and
6. an initiated connector bypassed exact saga-pair validation before lazy activation.

Each mutant caused its owning deep-contract method to fail at the intended invariant. The final
no-incremental build restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-204-results/final/iteration204-final.cobertura.xml`,
SHA-256 `1e84575994b808b3fbf52fd90452fae2ffc41359c90f9ec0a953c1da8fb2f066`.
Exact source-basename selection includes compiler-generated classes belonging to the same source.
Unique executable lines and branch conditions use maximum coverage across duplicate entries.

| Owner source | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| `ConstructorSagaInstanceFactory.cs` | 11/11 | 6/6 | 6 |
| `DefaultSagaFactory.cs` | 13/13 | 6/6 | 4 |
| `PropertySagaInstanceFactory.cs` | 14/14 | 8/8 | 8 |
| Three query-selector implementations | 21/21 | 0/0 | 1 |
| `SagaMetadataCache.cs` | 31/31 | 23/26 | 10 |
| `SagaMessageConnectorDescriptor.cs` | 50/50 | 11/12 | 10 |
| Three declarative interfaces | 0/0 | 0/0 | 0 |
| **Total executable** | **140/140** | **54/58** | **10** |

All executable owner lines are covered. The four residual branches are defensive fallbacks in the
stable type-name chain (`AssemblyQualifiedName`, `FullName`, `Name`) for already validated closed
message contracts and the invalid-message diagnostic fallback after `IsValidMessageType` returned
false. Every representable admitted path is directly executed. No method exceeds the CRAP threshold
of 30.

Sorted display-name SHA-256 is
`c7cf06ec38740f13858a0c933d025d2e883229cf68e6091b0bf285401bb7fff1` across 5,525 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-204-results/final/iteration204-final.ctrf.json`,
SHA-256 `7dfd130eb3e243940665ccdaffd0c1bb3b9330bb9255c4e0674cc0e5312aa7a4`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 30/30 passed |
| Configuration/Sagas namespace regression | 753/753 passed |
| Full Core Release | 5,525/5,525 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated material mutants killed |

No unresolved product correctness, factory, selector, metadata, connector, nullable-contract,
compatibility, coverage-risk or architecture finding remains in this admitted packet.
