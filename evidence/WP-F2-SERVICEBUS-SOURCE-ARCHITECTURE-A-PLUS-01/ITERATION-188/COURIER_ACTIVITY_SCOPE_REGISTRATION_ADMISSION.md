# Iteration 188 — Courier activity-scope and registration admission

## Result

This connected packet admits the 17-file Courier dependency-injection activity-scope lifecycle,
provider, registration and scoped-factory surface (1,100 final lines). Three GPT-5.6-Sol xhigh
agents worked concurrently under disjoint provider/API, context/lifetime and registration/factory
ownership. The lead personally read every admitted source file and integrated requirements,
mutation, coverage, final gates, evidence and publication.

Twenty-four unique requirement variants produce 26 cases. They bind exact generic/API shape,
created versus borrowed ownership, ambient-context restoration, concurrent asynchronous disposal,
dual-failure ordering, activity resolution, exact cancellation tokens, definition caching,
configuration transitions, endpoint identity, scoped-factory probing and pipeline/release behavior.

Two causally demonstrated correction groups affect three product files:

1. `ActivityScopeLifetime` now gives every overlapping disposer the same incomplete cleanup task,
   completes all waiters with the same ordered cleanup outcome, and remains idempotent afterward.
2. Both scoped activity factories preserve operation and asynchronous release failures together in
   operation order instead of allowing `await using` cleanup to replace the pipeline failure.

All 17 files were already present in the unique personal-read inventory from Iterations 178 and
179. This packet rereads and rehashes their current bytes but does not count them twice. Cumulative
personal source admission therefore remains 488/4,118 current C# files (11.850%).

| Admission manifest | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 17 / 1,100 | `51d3072d4f8a559c4f688d888059428de5e9e7da3aafb2e1a34b7d2ceffa6e41` | `df2435e14ae0ca1f86e19bdcd32bc4d82dcad5105dc1bdea607d1a73c32245f7` |
| Tests | 3 / 1,799 | `9c3ee72a027edab00a7f2ba14644950ccf4d44c0665913b9d55e1bf47366bce2` | `7eeea9928ddd73382f5a875499036671a405c8465dc8f848c9d215ae876d41f7` |

Each manifest hash covers ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Each chain hash covers the Iteration 187 chain followed by this manifest hash, each with a
terminal newline.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/ActivityScopeLifetime.cs` | 104 | `7ccaa7b1a9d28a5152c706db5e22457415db12552fbb087bbf4dd8c2de226221` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/CompensateActivityScopeProvider.cs` | 108 | `b8866cef36ce1220d8a061a0360283a2b729d0ca6d05558fb5424106cbcf9e54` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/CreatedCompensateActivityScopeContext.cs` | 44 | `8ba78f28c0961839256724b771b7f420c07d7b00fcbf1f809c979b9320c57382` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/CreatedExecuteActivityScopeContext.cs` | 44 | `352cad9adb37ffe5cc6fb8f8f38642616317140bdf3c85b5b107de8f8d21930f` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/ExecuteActivityScopeProvider.cs` | 109 | `c68615f902b87dda8773730a0cac1b0e2332aca5f6b23b0f26fefcd8059cdfd2` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/ExistingCompensateActivityScopeContext.cs` | 44 | `c184bda4111f29c187ef0f1f96a289b3b29bf1de2943b8680ee18e1542164e7b` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/ExistingExecuteActivityScopeContext.cs` | 44 | `3c4e5d38229b5d6d916ead0dcce0380683b895d0fa4a08495b95b8452a0b3ae8` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/ICompensateActivityScopeContext.cs` | 21 | `34109beae3e880e121d0953be1168451780e87700939d5a2c0b7170a2ba5ee25` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/ICompensateActivityScopeProvider.cs` | 24 | `ad70a0d21ec8d779c2c9a667bb353c17bfab3de91173198adbe4af8d7ccccdaf` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/IExecuteActivityScopeContext.cs` | 21 | `20eb17d885f458c7f4df13f6072f4118bcfeaea783cbbbfe8740f6a95084f1b5` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/IExecuteActivityScopeProvider.cs` | 24 | `db1fc04f9ec5c085a846c31453cbda0a2c841d7b3ff1533657a63bc25c87d641` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/Registration/ActivityRegistration.cs` | 176 | `c1ced58aa978c363c4b6e41120785e4a6dc3e2046084dbcd049ce2bd3392ea78` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/Registration/ActivityRegistrationConfigurator.cs` | 73 | `765efa266ee7d15ac288a86fd444b4cdca22b4718c98418d93e2072bc1661f81` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/Registration/ExecuteActivityRegistration.cs` | 100 | `fc1f71aad0ff0d12503aa01e7522afa34652efa6477b160ace523d039ebd7026` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/Registration/ExecuteActivityRegistrationConfigurator.cs` | 48 | `ad5cb69f62c7a532d234829e6a1653897a836df02e95508a05c15faddbbe89ab` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/ScopeCompensateActivityFactory.cs` | 58 | `5699f80aaacda0de93019e61e1b14e017459d83b589dcb6f827c36605fa13b04` |
| `src/ViciOne.ServiceBus.Courier/DependencyInjection/ScopeExecuteActivityFactory.cs` | 58 | `22b7bad9e4f64ba3bbbf0402bc032a4f5e77ee1da5e9ea39b9aeb18ce78f2332` |

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierActivityRegistrationAndScopeFactoryDeepContractTests.cs` | 767 | `59325259245034468046783f8d6c9573bd9e2ec722dbf9001082ad25f61e98b4` |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierActivityScopeContextDeepContractTests.cs` | 275 | `0a1982c186d7747715b2b4fe0cd45d2bd788da48cd1c2e1383b6240f2b8b4931` |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierActivityScopeProviderDeepContractTests.cs` | 757 | `0b86a55ebcc60254ee4a90cfe1de71a43c204c967cf483efdf843f18f6257c61` |

## Proof

Six successfully compiled, isolated single-cause mutants were killed and restored:

1. overlapping lifetime disposal returned a completed task instead of the active cleanup task;
2. dual lifetime cleanup failures were reversed;
3. execute-scope release discarded the pipeline failure;
4. compensate-scope release discarded the pipeline failure;
5. the execute factory skipped its downstream pipeline; and
6. the compensate factory skipped its downstream pipeline.

Every counted mutant failed its owning behavioral contract and all temporary source edits were
restored. The assertion audit found no assertion-free, self-referential or trivial-only additions.

Final Courier Cobertura is
`/private/tmp/vicione-servicebus-iteration-188-final.cobertura.xml`, SHA-256
`710df5d4f4e63646b767fba21abe594ed067163bddc57c5114ccd34742b4b0cb`.
All executable admitted files report 100% line coverage. The two provider classes report 95%
branch coverage because the compiler-generated method-group cache sequence point on `GetScopeAsync`
contains one non-executable synthetic branch; all source-authored branches execute. Every other
executable admitted class reports 100% branch coverage. Maximum admitted-target method CRAP is 8.

The 24 variants are embedded in `CoreRequirements.json`, final SHA-256
`ce1566df27e27eef6cfe0c8c170f50866fb497cd3f31bce010c48a220f879677`.
Unit sorted-display-name SHA-256 is
`1f43d33c5f42c45da0f3f369ddc9788f75f1ac757c7d2bdd369523efb4a690db`
across 5,062 displays.

| Gate | Result |
| --- | --- |
| Three final owned classes | 26/26 passed |
| Complete Courier namespace with fresh coverage | 382/382 passed |
| Full Core Release | 5,062/5,062 passed with suite parallelism disabled |
| Full EF unit Release | 249/249 passed on the isolated rerun |
| Release Core-test / EF-unit / EF-local builds | 0 warnings, 0 errors |
| Core/EF/local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format and diff checks | Exit 0; no whitespace errors |
| Mutation probes | 6/6 compiled isolated mutants killed |

The first parallel Core run collided with simultaneous coverage instrumentation and exited 139;
the isolated full rerun passed 5,062/5,062. The first EF run shared resources with the local build
and timed out one pre-existing eventually assertion; the isolated full rerun passed 249/249. Only
the isolated final runs are admission gates.

Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-188-courier-scope-registration-admission-2026-09-17`.

Whole-fork personal reading, the global completion matrix and configured external-provider
acceptance remain open. Remote publication is an independent delivery step and cannot pause or
deactivate the active goal.
