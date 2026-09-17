# Iteration 191 — saga policy and state-accessor admission

## Outcome

The existing/new saga policies, context/query adapters, state accessors and message/provider
delegate contracts are admitted. Invalid owners and collaborator outcomes now fail explicitly,
state identity is consistent across read and predicate paths, convention selection excludes
indexers, and public delegate constraints match every supported reference-message consumer.

The lead personally read all 25 current sources. Thirteen are newly unique and twelve are deliberate
current-version re-admissions from the pre-expanded inventory. Cumulative exact unique source
coverage is 546/4,118 files (13.259%). The current packet contains 1,061 physical source lines.

## Corrections

- Existing/new policies validate every required context and pipeline owner, preserve exact valid
  tasks/faults/cancellation, and convert impossible null tasks or created instances into explicit
  failures.
- Saga consume-context construction retains exact context, message, state and supported mode;
  concurrent saga-query callers receive one cached compiled delegate.
- Default state discovery accepts only one public, non-indexed `IState` property with accessors and
  produces stable diagnostics for zero or multiple candidates.
- Raw state storage uses the canonical state name for reads and predicates. Raw, string and integer
  accessors consistently validate inputs, skip unchanged writes and reject null observer tasks.
- State indexing validates owners, elements and compatible instance types, retains reserved order,
  deduplicates entries, and reports exact name/index parameters.
- Exception-produced message delegate outputs now carry the same `class` constraint as ordinary
  event messages. A source audit found 74 occurrences and 52 call sites, no concrete/value-type
  output, and class constraints at every consumer boundary.

## Source manifest

The manifest record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`c7c9af58325225a03963e65e87c7a452b8a00904a1501bbc86ff9a42199fb123`. Chaining that hash from
iteration 190 yields
`2e5c35502ee948b1f7c128f1a409a419d3011066fbd302c86f36b743d866901c`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Saga/AnyExistingSagaPolicy.cs` | 58 | `2cb0880b2f29b3c449e8c9226549ecb06cc451f895e6074d77b26aa3a9c703c6` |
| `src/ViciOne.ServiceBus.Sagas/Saga/ISagaConsumeContextFactory.cs` | 38 | `9c011b68b2f29cccf7fbd6a18ac8219c4a350cc7ba045607046ddf531c2938dd` |
| `src/ViciOne.ServiceBus.Sagas/Saga/NewOrExistingSagaPolicy.cs` | 68 | `c63ca35ce82bd8f21c90c97ff6a22fd4ed10981336146c30d256a43d771ea1ea` |
| `src/ViciOne.ServiceBus.Sagas/Saga/SagaConsumeContextFactory.cs` | 34 | `d25c48f7feb4fec798b54c5ae0468ffec21937d57981c6b951ab43492e601dcf` |
| `src/ViciOne.ServiceBus.Sagas/Saga/SagaQuery.cs` | 31 | `1f06f4338dfcea9c72763594031f4f57d63a14569e006455214c216719e1284b` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Accessors/ViciOneServiceBusStateMachine.DefaultInstanceStateAccessor.cs` | 98 | `fcf30ec988b553f2c5695de5b5fab7acc05c3035ae230236fdfe1d70571d3c56` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Accessors/ViciOneServiceBusStateMachine.InitialIfNullStateAccessor.cs` | 62 | `286bdc1922d766f9109642bb3d982cd501dee6f6cbc8d7595f0a31c8aff2d28b` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Accessors/ViciOneServiceBusStateMachine.IntStateAccessor.cs` | 94 | `07169e1279f6ae7cf3503edeae996d5476676dd3786b2228ffc9c038f04bfa40` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Accessors/ViciOneServiceBusStateMachine.RawStateAccessor.cs` | 100 | `5d30234065b49495d508a2e5750ef2b3bbc62702a03cdf229eb7b751da202d2c` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Accessors/ViciOneServiceBusStateMachine.StateAccessorIndex.cs` | 65 | `8be5685622f370bf0018ebadb100dfce5d7519483ba48ec1435eb7c93f54a902` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/Accessors/ViciOneServiceBusStateMachine.StringStateAccessor.cs` | 96 | `f8be2119a7ea29d3d623854e66b212ea3ead8654a87cd0f835f1f1896185d485` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/AsyncEventExceptionMessageFactory.cs` | 30 | `78abeaba39d8332687cb19a2581264cec4bd1a2b21c92225f5048c20136f0a06` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/AsyncEventMessageFactory.cs` | 24 | `fc871fe12ed447074bfbf7bae8742cff1abcbf8a47886ea90b5f38263884ac89` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/DestinationAddressProvider.cs` | 20 | `0aedfc62654e4dd39a33aa58b1df63ace86abbd7c0ecc7be6a00b54db6302ee0` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/EventExceptionMessageFactory.cs` | 28 | `bc498cf83973ad5f088111bb42ed72b32e523cc3c67a9f01c318e93e10f4e8f3` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/EventMessageFactory.cs` | 22 | `6a32b3757afc0843ee60518b9a8aa21c7d8703be43fc929d5a9d87923ed15a13` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ISagaFactory.cs` | 22 | `3126225d7c6e457502f37ea4f50ad710fcb93daeef5e7298371aba734a1ad017` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ISagaPolicy.cs` | 33 | `477d438fb8fc5ca9ad43e66a05e6eff3e37ea451efb36a352aa384100e016c7a` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ISagaQuery.cs` | 17 | `e335397a6ad4b0ca0ea9024bb7c81095e1af95572c0f08566baff440c276e5e6` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/SagaFilterFactory.cs` | 13 | `3af02f33034653a8fef45c43d3d9bd6761b8b6b5b8a2e4be4de25d27c5ceed04` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ScheduleDelayExceptionProvider.cs` | 24 | `d66e08f9ed75c17b8b706544af0b28ed5bb024d6484ccccdf00840b82a86c766` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ScheduleDelayProvider.cs` | 20 | `ba7e04c258fa24f12546a061fcce631c0b1ed2f10a173a5c2ba852b885915004` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ScheduleTimeExceptionProvider.cs` | 24 | `d093b70e32819f323627899257fd1b3771096b72507c8e742697c931d8da672f` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ScheduleTimeProvider.cs` | 20 | `0724722d76b3c19ee93fb549c24521170dafe05bacddec838b9d098d6dc11311` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ServiceAddressProvider.cs` | 20 | `a385a981423d40c6bd49c6d5b853ef03671d72fc59163a2f7ad6a3638a391eb7` |

## Test manifest and requirements

Test manifest SHA-256 is
`f52097a4592e7b1ce5f9997503e28a86765d406ecb4c69bfeecc27fed51e4a92`; chained from iteration
190 it yields
`735d9620b213de38d7cc8a7186be439a83a7c5db216249a1b951ac65083bdf55`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Saga/SagaPolicyContextQueryDeepContractTests.cs` | 402 | `862a9f1f6507cfbe6a337fb9df1050453d09b2ba439e37d3375e334427bbdbd2` |
| `tests/ViciOne.ServiceBus.Tests/SagaStateMachine/StateMachineStateAccessorDeepContractTests.cs` | 596 | `e31ef07c60bd0d2aada823a7a073a3957a74c203983c4656e19ba6ab3a0c5600` |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaFactoryProviderDelegateContractTests.cs` | 339 | `731eac9d4aaaf5eec6b807fe1c56ffb6717851d514d13144d00eb2de3af74e92` |

The three classes contain 29 test methods, 47 expanded cases and 29 unique requirement variants.
`CoreRequirements.json` SHA-256 is
`8eb11edcc9e9f9daa0618e402cae4198880d56caec3c69052a92bfd69c953e52`.

## Proof

Eight compiled, isolated single-cause mutants were killed and restored:

1. the configured any-existing missing pipe was ignored;
2. a null factory-created saga instance was accepted;
3. a null new-or-existing factory task escaped the policy boundary;
4. consume-context construction discarded the exact saga instance;
5. saga predicates were recompiled instead of cached concurrently;
6. raw-state predicates compared reference rather than canonical name;
7. incompatible state-index entries leaked an incidental cast exception; and
8. a raw-state observer null task escaped its explicit failure boundary.

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-191-final3.cobertura.xml`, SHA-256
`f526e473f3fe99ccf085386a205c8a3be1348c076b8403bdeecbde4bb2786096`.
Admitted coverage is 253/253 executable lines (100.000%) and 100/104 branches (96.154%). The four
residual branches are invariant arms: valid property expressions cannot lack getters in the raw,
string or integer accessors, and the state-index scan starts after its sole reserved null slot.
Maximum admitted method CRAP is 12, equal to the fully covered convention-discovery method's
structural complexity rather than a coverage penalty.

Sorted display-name SHA-256 is
`a54e0a2ab814be80e8f19074cdd4328074d43c78a38b36e0361a4ea8d8fa7407` across 5,221 displays.

| Gate | Result |
| --- | --- |
| Three final owned classes | 27/27, 15/15 and 5/5 passed |
| Saga / Sagas / SagaStateMachine namespace regressions | 40/40, 88/88 and 155/155 passed |
| Full Core Release | 5,221/5,221 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format and diff checks | Exit 0; no whitespace errors |
| Mutation probes | 8/8 compiled isolated mutants killed |

No unresolved correctness, lifecycle, cancellation, concurrency, compatibility or architecture
finding remains in this admitted packet.
