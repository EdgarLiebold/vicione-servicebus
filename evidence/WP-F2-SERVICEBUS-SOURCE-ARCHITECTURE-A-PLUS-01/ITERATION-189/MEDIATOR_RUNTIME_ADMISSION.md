# Iteration 189 — mediator runtime admission

## Outcome

The mediator configuration, dependency-injection boundary, endpoint contexts, request handlers and
runtime lifetime are admitted. The packet adds deterministic concurrent disposal, cancellation
preflight and forwarding, explicit null-task contracts, and direct tests for every material
source-authored branch found by coverage review. Supported behavior is retained.

This admission adds 22 personally read current source files. Cumulative exact source coverage is
510/4,118 files (12.385%).

## Corrections

- `ScopedMediator` now serializes lazy client-factory materialization against disposal and makes
  disposal terminal, preventing post-disposal creation and double ownership.
- Mediator send/publish contexts reject pre-cancellation before work, forward the caller token to
  configuration pipes, and reject null tasks from configured/internal pipe layers explicitly.
- Both mediator request-handler forms distinguish a missing handler task from a null response.
- `InProcessMediator.DisposeAsync` is single-flight and preserves deterministic owner-order
  cleanup failures while retaining single failures and cancellation without aggregate wrapping.
- Tests exercise every remaining configuration delegation, runtime publish null boundary, endpoint
  collaborator boundary, fallback destination, bounded-stream growth path, enabled error-log path
  and the mediator's explicit empty route table.

## Source manifest

The manifest record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`88dafb00495346e2608ecdee938ca4df6c2092ac34619b9110f416469243da7a`. Chaining that hash from
iteration 188 yields
`e366f3194d56be5e09d876ba0ae27b334516740a52761fc2b1081f57330ae239`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Mediator/Advanced/MediatorRequestExtensions.cs` | 45 | `46ca33c23e77917bd6fd1bed932e5c56615f5fa866ccb5c37014db6ab120b259` |
| `src/ViciOne.ServiceBus.Mediator/Configuration/IMediatorConfigurator.cs` | 10 | `2400c7b55f9126c8f8a5f504906c914be37cf7ad5badbef7084b2b48223005e4` |
| `src/ViciOne.ServiceBus.Mediator/Configuration/IMediatorRegistrationConfigurator.cs` | 12 | `6f78825e1054fab6f3cd9b84169d8850549f93a2152fae7014b5117d18c541e7` |
| `src/ViciOne.ServiceBus.Mediator/Configuration/IMediatorRegistrationContext.cs` | 7 | `2f4776b77cf981b24d1254835b5a08456b290fe503daab3fab2aa8983baa510a` |
| `src/ViciOne.ServiceBus.Mediator/Configuration/MediatorConfiguration.cs` | 86 | `ac84f4ed0cbaddcb05d4b801f1427986595cb5941c65997931131a4ac6c5d3c3` |
| `src/ViciOne.ServiceBus.Mediator/Configuration/MediatorMessageLimitsConfigurationExtensions.cs` | 56 | `54eb10dccbdaa66360836071c773c5bb42fd430d5964b214a67a47e53fd3d498` |
| `src/ViciOne.ServiceBus.Mediator/Configuration/MediatorRegistrationContext.cs` | 113 | `c8c1d96acad5b43aaab04e0f7492eef5d91dff6edf73fdd75e62a00aca286e19` |
| `src/ViciOne.ServiceBus.Mediator/DependencyInjection/MediatorServiceCollectionExtensions.cs` | 61 | `c38aa3ba1e0ff0be84eee9f6b93662dd7d5c90c31ba9f92229f6abd129aa17b4` |
| `src/ViciOne.ServiceBus.Mediator/DependencyInjection/ScopedMediator.cs` | 322 | `aa2975fd5d6b8ebbaa13e951f074811e6b0ee0232187d4e381010fa789659106` |
| `src/ViciOne.ServiceBus.Mediator/GlobalUsings.cs` | 15 | `a461256ef9816a82cd5972b9c4d8055747165d184937adbea65d7ef4d66cd6a8` |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/AddressedMediatorSendEndpoint.cs` | 46 | `78a6c0e6cc7697c7a18360df44d298c383dbb224415e16234a4cdc99070e1583` |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorClientFactoryContext.cs` | 81 | `515f2d22951982993a5a5dd7293a2bb4c303337556ea4bfeb3ed4123fbcd958f` |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorMessageBodySerializer.cs` | 181 | `84c2a9986eaf79fa9878acdaa5f9c2da2bfec02e4f90d2de5abc322b8673cd54` |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorPublishSendEndpoint.cs` | 84 | `ea9872a8dbb53bb56c96893a05becb4963d9b497c7f490cae7f4e842adaf365a` |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorRequestSendEndpoint.cs` | 31 | `e05afe071a1276dfa370099fd99520efe847c71d0d259726d067c1cf3257fc2f` |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Contexts/MediatorSendEndpoint.cs` | 428 | `7e758521f96a9e4425d95cb90950674e6eacca1869a85131e95cf0b521944f5a` |
| `src/ViciOne.ServiceBus.Mediator/Mediator/IMediator.cs` | 17 | `a9509c77631d8ebc12e9682df3921f17ed520811225f0298d35c401b0de3c25c` |
| `src/ViciOne.ServiceBus.Mediator/Mediator/IRequest.cs` | 10 | `9427e24f197e954008f5d54ca94f07342e3149b44cb96f5aab0089ab5b62fa11` |
| `src/ViciOne.ServiceBus.Mediator/Mediator/IScopedMediator.cs` | 7 | `1384ffbbb124a202c4dfde42dfdd5276fa56f918f23920b8d1b35f044d49d503` |
| `src/ViciOne.ServiceBus.Mediator/Mediator/MediatorFactory.cs` | 95 | `fcad16b81658be55696cae7bd9de69a5e2cfad85bbdc5f884c2451f4eb16ab9a` |
| `src/ViciOne.ServiceBus.Mediator/Mediator/MediatorRequestHandler.cs` | 63 | `921fd4a710d3ca83f78e9987b27311caf691c641a32f9bb78ee1530bf6894c55` |
| `src/ViciOne.ServiceBus.Mediator/Mediator/Runtime/InProcessMediator.cs` | 477 | `7e60e97d61dceca2b937eca929ae9e3dbb5c6d7b577b0cd0f82ea3373f3f900d` |

## Test manifest and requirements

Test manifest SHA-256 is
`b7be26cb88e0a393cc36a504c58d49eab7450c4cf91d1e0c2f83beff3990ed07`; chained from iteration
188 it yields
`4713ae90b6e7b41071d8a395ba4f03a7e646bc4b616f8a4069570531245e1761`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Mediator/MediatorConfigurationDependencyInjectionDeepContractTests.cs` | 612 | `61af0432130f57ec1addd447da5ecdf5a5f533bf07c242cb985dee407fa628ac` |
| `tests/ViciOne.ServiceBus.Tests/Mediator/MediatorEndpointContextDeepContractTests.cs` | 1,009 | `5856a35ef539f3ce7b925dbda650e14887cde709f586ada7d55111dcedb2bf91` |
| `tests/ViciOne.ServiceBus.Tests/Mediator/MediatorRuntimeRequestDeepContractTests.cs` | 459 | `68d84843b61d852891813342330eecfdc6e9a4b199b53c67eec5d3f7b9302f28` |

The three classes contain 38 test methods, 56 expanded cases and 38 unique requirement variants.
`CoreRequirements.json` SHA-256 is
`b1bec6e600d20c8f77aedf589370527ce07b0eecfbbb145a8ca87c5e147b4901`.

## Proof

Six compiled, isolated single-cause mutants were killed and restored:

1. scoped-mediator terminal disposed state removed;
2. in-process mediator single-flight disposal removed;
3. one-way handler null-task guard removed;
4. response handler null-task guard removed;
5. mediator additional-send-pipe null-task guard removed; and
6. mediator additional-publish-pipe null-task guard removed.

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-189-final4.cobertura.xml`, SHA-256
`abeab924a5c22f08d4a36abda44522d7717f43c762b37d4bccbb9255352fd62a`.
All 767 executable admitted lines are covered. Branch coverage is 275/278 (98.921%). The three
untaken branches are not product behavior: two are compiler-generated async rethrow edges at
`MediatorMessageBodySerializer.cs:46` and `MediatorSendEndpoint.cs:351`; the third is the null arm
of a cast whose locally constructed concrete host configuration implements the required interface.
Maximum admitted method CRAP is 28 for the fully covered central send state machine, below the
material-hotspot threshold of 30.

Sorted display-name SHA-256 is
`e7d93539481974a3e507db4e10c179e1102df9c4254aaa5a17cef5df8649ce63` across 5,118 displays.

| Gate | Result |
| --- | --- |
| Three final owned classes | 11/11, 34/34 and 11/11 passed |
| Mediator direct namespace / Contexts regression | 121/121 and 18/18 passed before final additive gap closure |
| Full Core Release | 5,118/5,118 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format and diff checks | Exit 0; no whitespace errors |
| Mutation probes | 6/6 compiled isolated mutants killed |

No unresolved correctness, lifecycle, cancellation, concurrency or architecture finding remains in
this admitted packet.
