# Iteration 212 — saga connector composition admission

Date: 2026-09-18

Branch: `feature/servicebus-a-plus-api`

Scope: nine previously unadmitted `ViciOne.ServiceBus.Sagas` connector discovery, cache,
pipeline-composition and specification-adapter sources

## Outcome

Saga discovery now has explicit four-category precedence, one owner per message contract and a
stable read-only connector view. Exact saga-type checks happen after signature-order null
validation. Lazy cache publication uses `ExecutionAndPublication`, preserving one success identity
or one cached construction-failure identity under contention.

Multi-message connection rejects null child handles, releases every partially acquired handle and
preserves the primary failure followed by every cleanup failure. Its successful aggregate owns all
children and disposes them once in order. Correlated and query connectors preserve exact shared and
connector-specific filter order, dependency identity, topology overload selection and returned
handle ownership. Every collaborator null result and phase failure stops at its owning boundary.

Specification adapters retain their public nested type, constructor, interface, method and
nullability contracts. Required collaborators fail fast. The previously documented optional null
callbacks on `BuildMessagePipe` and `Message` remain no-ops. Observer notification is one-shot and
disconnectable; validation ordering and failure identity are stable. Repeated builds append in
order without mutating previously built pipes. Both proxy constructor paths forward apply and
validation with the exact wrapped filter, builder and context identities.

Coverage analysis exposed an unreachable general constructor catch after the connector factories'
own causal `ConfigurationException` boundary. Removing that redundant rewrap retained supported
diagnostics and eliminated the packet's only uncovered sequence points.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`e5245e1008d6f0d49ddacd10ed1709d355f2f6595d0d556c37adc29b6b435a70`. Chaining that hash from
iteration 211 yields
`ac6a70c1b987e72eca1bd90d629a7a219b61a4d32004407e14226516c8096210`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaConnector.CorrelatedSagaMessageConnector.cs` | 45 | `6c2f2d572fa5728c70bcb1998530a9c4569068e569d5aeea9553a8c151101119` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaConnector.QuerySagaMessageConnector.cs` | 39 | `630e073824fb870a5b0a8cd95759d0e0968757a1302b8ce507a1147087d7b4e0` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaConnector.SagaMessageConnector.cs` | 74 | `f181777a0bb50e54d10135e62733f9a59aa57fb10d3675ebfa7c981a4918b11a` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaConnector.SagaMessageSpecification.cs` | 138 | `d5865570611be50581171fea2e64722f513fe10711ea58896ecb66d03bcafc8e` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaConnector.SagaMessageSplitFilterSpecification.cs` | 60 | `c14c77a8aaae8542d2b7fb07475dec257c171b99cc1ce1273f2cb7620bc6ff30` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaConnector.SagaPipeSpecificationProxy.cs` | 50 | `83beeafa3168ed68233194d5f61481275f4c0133990d3ba4738a5c345e1a50c1` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaConnector.SagaSplitFilterSpecification.cs` | 59 | `52cc3769cc3ec28f91daae2731c75f540169ac14ffa8ef4758ab118ee332e8ac` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaConnector.cs` | 124 | `2e2a1e5dcc9155daba7d3d01b214f152969f530c02e5b51603a9a247302af800` |
| `src/ViciOne.ServiceBus.Sagas/Configuration/SagaConnectorCache.cs` | 33 | `48395997c309eafed72687b66f15bc97575dc519b97179f125d4a716c45b9d1f` |

The exact final source packet is 622 lines. Lead-read progress is 751 of 4,118 C# sources, or
18.237%.

## Test manifest, API mapping and requirements

The mandatory code-testing workflow, static source/test pairing, pseudo-mutation gap analysis,
assertion-quality review and independent parallel counter-audits drove the research-to-plan-to-test
sequence. Three classes contain 31 test methods, 39 executed cases and 31 unique requirement
variants. Every compile-visible or behaviorally material member is mapped to direct evidence.

Test manifest SHA-256 is
`fbd0292b84bcdb7f5dd9409abc8a0c9d099af43cfec533ca5f65f650ec9c3a9f`; chained from iteration
211 it yields
`f6fc8eba57e4b2df6b8d0c8f3752d60837768a6b0f25cabe87dae978c01a8ca3`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaConnectorDiscoveryCacheDeepContractTests.cs` | 451 | `b2fd8bf20a16fb8485acbfcf58a6f306b92fb1d97fc519af4cecd8cbdcd7fa30` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaConnectorPipelineDeepContractTests.cs` | 690 | `c395308d438234cb0eafc82827f4ca98063962220a1822aa0b1b79a9d314a7b2` |
| `tests/ViciOne.ServiceBus.Tests/Configuration/SagaConnectorSpecificationAdapterDeepContractTests.cs` | 684 | `05913610ee2f7c7c8c7452799e9a0cac1e51f1cd33ee393b046d2653900c75f2` |

The exact test packet is 1,825 lines. `CoreRequirements.json` SHA-256 is
`1428ff30d255bdca4a9fd78799c49a679c2cd5b2a4aa3e5dbbd8aea265f51c8f`.

## Independent final re-audit

The initial disjoint counter-audits found one compatibility regression, a mutable collection escape,
missing successful aggregate ownership and combined-invalid-input evidence, probabilistic cache
contention, incomplete category implementation checks, and gaps around proxy forwarding, observer
disconnect and exact reflected signatures. All findings were corrected in their owning packets.

Three ultimate read-only Sol-xhigh audits then independently reconciled 13 discovery/cache tests,
eight connector-pipeline tests and ten specification-adapter tests with their 31 requirement entries.
All report no finding. They confirm optional-null callback compatibility, deterministic cache and
ownership behavior, exact filter/topology composition, both adapter paths, observer lifecycle,
assertion quality, comments, naming and asynchronous suffix correctness.

## Mutation proof

Eight compiled, isolated, material single-cause mutants were killed and restored:

1. remove correlated identifier-filter registration;
2. remove query-filter registration;
3. change default topology configuration from enabled to disabled;
4. accept a null consume-pipe connection handle;
5. notify observers on every validation instead of once;
6. omit the terminal saga consume filter from the built consumer pipe;
7. suppress saga split-specification application; and
8. suppress proxy forwarding to the selected split adapter.

Every mutant built with zero warnings and errors and failed its intended focused invariant. The
eighth mutant had been a predicted survivor before the independent audit added its direct proxy
forwarding test. Every source was restored before the final strict build and instrumented run.

## Coverage and CRAP

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-212-admission.A5XXfU/final/iteration212-final.cobertura.xml`,
SHA-256 `40e2768b08fe9c69281d790f5c019f8dc89b2bde1781774e99252dd89aff4322`.
Exact source-basename selection includes compiler-generated classes belonging to the same source;
unique lines and branches use maximum coverage across duplicate entries.

| Owner source | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| Correlated connector | 7/7 | 4/4 | 4.0 |
| Query connector | 6/6 | 4/4 | 4.0 |
| Message connector base | 23/23 | 12/12 | 10.0 |
| Message specification | 39/39 | 8/8 | 2.0 |
| Message split specification | 15/15 | 6/6 | 2.0 |
| Pipe specification proxy | 12/12 | 0/0 | 1.0 |
| Saga split specification | 13/13 | 4/4 | 2.0 |
| Connector discovery/ownership | 55/55 | 28/28 | 12.0 |
| Connector cache | 10/10 | 2/2 | 2.0 |
| **Total executable** | **180/180** | **68/68** | **12.0** |

There is no owner coverage residual. Sorted display-name SHA-256 is
`f2b6fd19d9ff4da130b7a2b518cf61b0f53fc40f976e882756207df7f62487ba` across 5,846 unique
displays. The final Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-212-admission.A5XXfU/final/iteration212-final.ctrf.json`,
SHA-256 `f67ad1d0a8d94b8e0e039ec673e605797c6bf91148b405b1b3ed4ff4910b9f55`.

## Gates

| Gate | Result |
| --- | --- |
| Three final owned classes | 39/39 passed |
| Saga-wide regression (`*Saga*`) | 1,083/1,083 passed |
| Full Core Release | 5,846/5,846 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core / EF-unit / EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 8/8 compiled isolated material mutants killed |

All genuine asynchronous APIs in the packet retain the `Async` suffix; synchronous construction,
properties, validation, pipeline build and connection APIs do not. No unresolved connector
discovery, cache concurrency, type-safety, pipeline ordering, topology, ownership, observer,
adapter, public-contract, formatting, coverage or material test-risk finding remains in this
admitted packet.
