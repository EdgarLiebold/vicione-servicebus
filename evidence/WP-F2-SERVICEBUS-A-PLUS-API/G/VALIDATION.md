# Work package G validation

## Reproducible environment

Commands use .NET SDK 10.0.400 and runtime 10.0.11 with a task-scoped CLI home,
`DOTNET_MULTILEVEL_LOOKUP=0`, the existing NuGet cache, disabled MSBuild node reuse, disabled build
servers, and serial MSBuild. NuGet, MTP, format, and container commands run outside the filesystem
sandbox where local IPC, the Docker socket, or advisory endpoints are required.

## Requirement-to-evidence map

| Requirement | Evidence |
|---|---|
| 18 compile-tested journeys | `DeveloperJourneyArchitectureTests`; `verify_developer_journeys.sh`; fresh package output |
| Journal, limits, inbox, and Suite examples | `Journey15MessageJournal.cs` through `Journey18SuiteComposition.cs` |
| Current product guidance | README, contribution, build, API, observability, reliability, and database documents plus vocabulary architecture test |
| Reliability diagram and complete states | `docs/reliability.md`; `ProductDocumentation_CoversReliabilityStatesAndPublicApiChanges` |
| Complete API-change guide | `CHANGELOG.md`; call-form, namespace, provider-name, and package tables; architecture assertions |
| Apache-2.0 change notices | A–G paragraphs in `MODIFICATIONS.md`; generated `CHANGELIST.md`; identity and change-list gates |
| Test effectiveness | `TEST_RESEARCH.md`; `MUTATION_EVIDENCE.md`; 3/3 injected mutations killed |
| Controlled public API delta | application API unchanged; one Advanced provider SPI method added for source notification waits |
| No dependency advisory | `vulnerability-inventory.json`: 0 findings and 0 unresolved dependency paths |
| Complete repository acceptance | strict builds, formats, three full Unit runs, and available provider-backed profiles recorded below |

## Results

| Gate | Result |
|---|---|
| Product Release build with warnings as errors | PASS; 0 warnings, 0 errors |
| Engineering Release build with warnings as errors | PASS; 0 warnings, 0 errors |
| Unit Release build with warnings as errors | PASS; 0 warnings, 0 errors |
| Engineering format verification | PASS; 0 changed files |
| Unit format verification | PASS; 0 changed files |
| Unit profile, final run 1 | PASS; 3709/3709, 0 skipped |
| Unit profile, final run 2 | PASS; 3709/3709, 0 skipped |
| Unit profile, final run 3 | PASS; 3709/3709, 0 skipped |
| Package-only developer journeys | PASS; 18 scenarios, 15 freshly packed packages, 0 warnings |
| Packed public API | PASS; 13 assemblies, 20,889 lines, SHA-256 `c52a900e1972bf17605f575964353e8461c570cb601358c759fa94fc98141786` |
| General provider matrix | PASS; 344/344 against PostgreSQL, Azurite, LocalStack, ActiveMQ, Artemis, and Event Hubs |
| SQL provider profile | PASS; 63/63 against isolated SQL Server and PostgreSQL, run `vicione-f0cb73261ea8` |
| Azure Service Bus emulator profile | PASS; 24/24, run `vicione-be44e54195ca` |
| RabbitMQ profile | PASS; 27/27 against the real broker fixture |
| Dependency advisory scan | PASS; 0 direct or transitive vulnerable packages |
| Process vocabulary in source comments | PASS; 0 findings |
| Nullable-disable directives | PASS; 0 findings |
| Direct process-clock access | PASS; 0 findings |
| CS1591 suppressions | PASS; 0 findings |
| Compatibility reliability entries | PASS; 0 findings |
| Forbidden legacy cryptography calls | PASS; 0 findings |
| Source identity | PASS; 5,654 baseline paths, 62,239 public declaration records, 0 findings, license byte-identical |

The final Reflection inventory loaded all 31 assemblies. It reports 4,153 public types, 1,347
interfaces, 1,889 extension methods, and 2,902 asynchronous methods. All async suffix, cancellation
token, token-order, token-name, public `DateTime`, obsolete, `EditorBrowsable(Never)`, public Internals,
and root-extension violation counts are zero. The preferred application surface remains three send
shapes, two publish shapes, 24 `ConsumeContext` members, 24 completion suggestions, and 11 builder
members. Compared with the package-G starting inventory, only `AsyncMethods` changed, from 2,901 to
2,902, because `IReliableDeliverySource<TBus>.WaitForWorkAsync` adds the notification wait required by
the common delivery loop. No application API signature changed.
