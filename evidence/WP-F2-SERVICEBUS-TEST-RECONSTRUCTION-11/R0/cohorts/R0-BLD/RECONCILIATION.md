# R0-BLD — build, package and profile inventory

Cohort `R0-BLD`, Team 1, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`.
Baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`, tree `e5897e7632be4f491e01d51221ee59081d4d2aa0`.

Every number below names the file it was read from. Nothing was built, restored or executed;
this is a read-only census. `git status --porcelain` for the cohort scope is empty at the time
of writing, and all 164 rows of `READ_MANIFEST.tsv` match
`evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0/BASELINE_TRACKED_FILE_MANIFEST.tsv`
byte-for-byte (0 missing, 0 hash mismatches; the baseline file stores `sha256<TAB>path`, this
manifest stores `path<TAB>sha256` as the reading rules require).

## 0. Scope and completeness

| Scope element | Tracked files | Read |
|---|---|---|
| `benchmarks/**` | 92 | 92 |
| `build/test-infrastructure/**` | 9 | 9 |
| `build/verification/**` | 12 | 12 |
| root build files (`Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `NuGet.config`, `.editorconfig`, `.gitignore`, `global.json`) | 7 | 7 |
| `tests/Directory.Build.props` | 1 | 1 |
| `ViciOne.ServiceBus.slnx`, `ViciOne.ServiceBus.Engineering.slnx` | 2 | 2 |
| `.github/workflows/build.yml` | 1 | 1 |
| `docs/build.md` | 1 | 1 |
| tracked `packages.lock.json` outside `benchmarks/` | 39 | 39 |
| **total** | **164** | **164** |

Absent from the tree at this commit, and therefore reported as absent rather than unread:
`tests/Directory.Build.targets`, `tests/testconfig.json`, `tests/testsettings.json`.
`git ls-files tests | awk -F/ 'NF==2'` returns `tests/Directory.Build.props` alone.

All eleven `build/verification/expected/*.txt` anchor files were read in full, not sampled,
including `core.txt` (1876 lines / 1873 identities).

Files read **outside** the assigned scope, because the task required a verdict about them, and
therefore not in `READ_MANIFEST.tsv`: `src/Directory.Build.props`, `signing.props`,
`src/ViciOne.ServiceBus.Analyzers/ViciOne.ServiceBus.Analyzers.csproj`,
`src/ViciOne.ServiceBus.Analyzers.CodeFixes/ViciOne.ServiceBus.Analyzers.CodeFixes.csproj`,
`src/ViciOne.ServiceBus.Analyzers.Package/ViciOne.ServiceBus.Analyzers.Package.csproj`,
and the sixteen `*.Tests.csproj` / `ViciOne.ServiceBus.TestInfrastructure.csproj` /
`ViciOne.ServiceBus.Diagnostics.csproj` files under `tests/**` and `tools/**` (property and
reference lines only, for the profile map in section 7).

---

## 1. Current package graph

### 1.1 Reference kinds, distinguished exactly

`Directory.Packages.props` sets `ManagePackageVersionsCentrally=true` and contains **two**
`ItemGroup`s:

* lines 5–60: **54 `PackageVersion`** entries. A `PackageVersion` declares a version and
  reaches no project by itself.
* lines 61–64: **one `GlobalPackageReference`** — `GitHubActionsTestLogger` `3.0.5`
  (line 63). A `GlobalPackageReference` is an implicit `PackageReference` in *every* project of
  the repository.

`PackageReference` occurs only in the individual `.csproj` files and never carries a `Version`
attribute anywhere in the repository (checked across all csproj in scope and in `tests/**`).

### 1.2 The test-related packages that exist centrally today

| Package | Version | Declared as | Declared where | Direct consumers | Transitive only |
|---|---|---|---|---|---|
| `NUnit` | 4.6.1 | `PackageVersion` | `Directory.Packages.props:45` | 16 projects (see 1.3) | — |
| `NUnit3TestAdapter` | 6.1.0 | `PackageVersion` | `Directory.Packages.props:50` | 15 projects | — |
| `NUnit.Analyzers` | 4.11.2 (`PrivateAssets=all`) | `PackageVersion` | `Directory.Packages.props:46–49` | 16 projects | — |
| `Microsoft.NET.Test.Sdk` | 18.5.1 | `PackageVersion` | `Directory.Packages.props:42` | 15 projects | — |
| `GitHubActionsTestLogger` | 3.0.5 | **`GlobalPackageReference`** | `Directory.Packages.props:63` | **all 42 projects with a lock file** | — |
| `Microsoft.Testing.Platform` | 2.0.2 | not declared | — | none | 15 test projects |
| `Microsoft.Testing.Platform.MSBuild` | 2.0.2 | not declared | — | none | 15 test projects |
| `Microsoft.Testing.Extensions.VSTestBridge` | 2.0.2 | not declared | — | none | 15 test projects |
| `Microsoft.Testing.Extensions.Telemetry` | 2.0.2 | not declared | — | none | 15 test projects |
| `Microsoft.Testing.Extensions.TrxReport.Abstractions` | 2.0.2 | not declared | — | none | 15 test projects |
| `Microsoft.TestPlatform.TestHost` / `.ObjectModel` / `.AdapterUtilities`, `Microsoft.CodeCoverage` | 18.5.1 / 18.0.1 | not declared | — | none | 15 test projects |
| `BenchmarkDotNet` | 0.15.8 | `PackageVersion` | `Directory.Packages.props:17` | `benchmarks/ViciOne.ServiceBus.BenchmarkConsole` | `benchmarks/ViciOne.ServiceBus.Benchmarks.Tests` (via ProjectReference) |

**Not present anywhere at this commit** — measured, not assumed, by parsing all 42 tracked
`packages.lock.json` files and grepping `Directory.Packages.props`:

* no `xunit*` package of any kind (no `xunit.v3.mtp-v2`, no `xunit.v3.core`, no
  `xunit.v3.extensibility.core`);
* no `Testcontainers*` package;
* no `TngTech.ArchUnitNET` package;
* no second VSTest logger besides `GitHubActionsTestLogger`.

`Microsoft.Testing.Platform` 2.0.2 is present in every test project's lock graph **transitively**,
pulled in by `Microsoft.NET.Test.Sdk` 18.5.1 through `Microsoft.Testing.Extensions.VSTestBridge`.
That is the VSTest-bridge shape, not the MTP-v2 shape the Lead plan requires.

### 1.3 Direct consumers, per project

Sixteen projects reference `NUnit` directly:

* fifteen with the full VSTest quartet `Microsoft.NET.Test.Sdk` + `NUnit` + `NUnit.Analyzers` +
  `NUnit3TestAdapter`:
  `tests/ViciOne.ServiceBus.Tests`, `tests/ViciOne.ServiceBus.Abstractions.Tests`,
  `tests/ViciOne.ServiceBus.Analyzers.Tests`, `tests/ViciOne.ServiceBus.SignalR.Tests`,
  `tests/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests`,
  `tests/Tools/ViciOne.ServiceBus.Diagnostics.Tests`,
  `tests/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests`,
  `tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests`,
  `tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests`,
  `tests/Transports/ViciOne.ServiceBus.AmazonSqsTransport.Tests`,
  `tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests`,
  `tests/Transports/ViciOne.ServiceBus.EventHubIntegration.Tests`,
  `tests/Persistence/ViciOne.ServiceBus.Azure.Table.Tests`,
  `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests`,
  `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests`,
  and **`benchmarks/ViciOne.ServiceBus.Benchmarks.Tests`**;
* one **product** project with `NUnit` + `NUnit.Analyzers` but no adapter and no test SDK:
  `src/ViciOne.ServiceBus.TestFramework` (`packages.lock.json`, both `Direct`).

`tests/ViciOne.ServiceBus.TestInfrastructure` carries no NUnit package at all — its only
test-related lock entry is the global logger. It is `IsTestProject=false`, `IsPackable=false`.

### 1.4 The `GlobalPackageReference` reach

All **42** tracked `packages.lock.json` files record
`GitHubActionsTestLogger` as `Direct`, `[3.0.5, )`, resolved `3.0.5` — including the three
`netstandard2.0` analyzer projects, the fifteen shipped product packages, the two benchmark tool
projects and the diagnostics tool. That is what Lead plan §6 no. 10 means when it says the change
of the central test package graph must update **all** affected lock files "including product,
benchmark and diagnostics tool projects": removing the `GlobalPackageReference` changes 42 lock
files, not 16.

Lock file schema `version: 2` throughout. Target frameworks: `net10.0` for 39 of them,
`.NETStandard,Version=v2.0` for the three analyzer projects.

### 1.5 `benchmarks/ViciOne.ServiceBus.Benchmarks.Tests` lock graph

5 `Direct` packages (the four test packages plus the global logger), 11 `Project` references
(both benchmark tools plus nine transitively reached product projects), 74 `Transitive` packages.
It therefore drags `AWSSDK.Core`, `Azure.Core`, `Apache.NMS`, `Microsoft.EntityFrameworkCore`,
`Npgsql`-side types, `RabbitMQ.Client` and `BenchmarkDotNet.Annotations` into a project whose 75
cases touch none of them at runtime.

---

## 2. `LangVersion` — verdict

**The Lead's closed allowlist is confirmed exactly, with no contradiction.**

Repository-wide scan (`git grep -n -i LangVersion`) over the whole tracked tree yields exactly
**two** live project-level pins, both `14.0`:

| Path | Line | Value | `TargetFramework` |
|---|---|---|---|
| `src/ViciOne.ServiceBus.Analyzers/ViciOne.ServiceBus.Analyzers.csproj` | 12 | `<LangVersion>14.0</LangVersion>` | `netstandard2.0` (line 5) |
| `src/ViciOne.ServiceBus.Analyzers.CodeFixes/ViciOne.ServiceBus.Analyzers.CodeFixes.csproj` | 11 | `<LangVersion>14.0</LangVersion>` | `netstandard2.0` (line 5) |

Both carry the compiler reason in a comment beside the pin: *"That framework gets neither a
language version nor a warning level from the SDK, so both are pinned here to the level the rest
of the repository builds at."* `docs/build.md:66–70` states the same rule in prose and adds that
the third `netstandard2.0` project sets neither, because it compiles no source.

Central files, read in full, contain **no** `LangVersion` element at all:

* `Directory.Build.props` (46 lines) — `NoWarn`, lock-file properties, artifacts output, DebugType. No `LangVersion`.
* `Directory.Build.targets` (89 lines) — four assertion targets and the framework allowlist. No `LangVersion`.
* `Directory.Packages.props`, `NuGet.config`, `global.json`, `signing.props` — no `LangVersion`.
* `src/Directory.Build.props` (36 lines) — product metadata, package metadata, readme item. No `LangVersion`.
* `tests/Directory.Build.props` (10 lines) — import plus `IsTestProject`/`IsPackable`. No `LangVersion`.

There is no second central file: `git ls-files` finds exactly six `.props`/`.targets` files in the
whole repository (`Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`,
`signing.props`, `src/Directory.Build.props`, `tests/Directory.Build.props`).

`src/ViciOne.ServiceBus.Analyzers.Package/ViciOne.ServiceBus.Analyzers.Package.csproj:9–11` names
`LangVersion` only inside a comment explaining why it is absent — a text search that does not
parse XML would report a third pin here. Any successor gate must read the evaluated property or
the XML, not the file text.

The remaining `LangVersion` hits in the tree are all under `evidence/**` (historical records of
earlier states, including a `Directory.Build.props` that once pinned `12`) and under `tools/ci/**`
(`tools/ci/policies/msbuild.py:166` treats `langversion` and `warninglevel` as "meaningless"
properties; `tools/ci/tests/test_policy_validator.py:790` uses `<LangVersion>14.0</LangVersion>`
as fixture text). Neither is a live pin.

**Verdict: confirmed.** No central pin exists. Exactly the two `netstandard2.0` Roslyn analyzer
projects carry a project-level pin, both `14.0`, both with the compiler reason recorded, matching
the closed allowlist of Lead plan §2 no. 8. Removing either would be a product edit forbidden by
§2 no. 2 and no. 3.

The framework exception itself is granted **by path** in `Directory.Build.targets:58–87`, not by a
self-set property: `VOSB0005` refuses `netstandard2.0` from a fourth project, `VOSB0007` refuses
`IsRoslynComponent` from anything but those two, `VOSB0008` refuses the retired self-marker.

---

## 3. `.editorconfig`

The file is 29 lines. The NUnit-specific exception is line 18:

```
16  [*.cs]
17  end_of_line = lf
18  dotnet_diagnostic.NUnit1032.severity = none
```

Quoted with its line number, byte-exact:
`.editorconfig:18` → `dotnet_diagnostic.NUnit1032.severity = none`

**Every other test-framework-specific rule in that file: none.** The complete list of settings is
`root`, `trim_trailing_whitespace`, `insert_final_newline`, `indent_style`, `indent_size`,
`end_of_line`. There is exactly one `dotnet_diagnostic.*` entry in the whole file, and it is line
18. There is no `xunit*`, no `NUnit` rule other than NUnit1032, no `MSTEST*`, no
`dotnet_analyzer_diagnostic.category-*` and no `severity` line anywhere else. Removing line 18
per Lead plan §6 no. 10 therefore removes the whole test-framework surface of `.editorconfig`,
and leaves the `[*.cs]` section with `end_of_line = lf` alone.

NUnit1032 is "an IDisposable field should be disposed" — it would fire across the whole NUnit
suite. Nothing else in the repository references it (`git grep NUnit1032` finds only this line
plus evidence records).

`.gitignore` carries three test-platform traces that belong to the same removal: line 31
`/interim/` (category runner TRX and counter records), line 33 `.pytest_cache/` and line 63
`*.trx` with line 64 `evidence/**/*.log`. Line 49 whitelists
`src/ViciOne.ServiceBus.TestFramework/ViciOne.ServiceBus.TestFramework.log4net.xml`, which becomes
dead the moment TestFramework is removed.

---

## 4. `build/verification/VERIFICATION_MODEL.json` — the full model

Schema `version 1`, kind `SERVICEBUS_VERIFICATION_MODEL`, work package
`WP-F2-SERVICEBUS-A-PLUS-RECOVERY-03`. It explicitly replaces two files that no longer exist at
this commit: `build/test-infrastructure/capability-matrix.json` and
`build/test-infrastructure/not-executed-inventory.json`.

### 4.1 Selections (13)

| Selection | Members |
|---|---|
| `all` | `local`, `fixture` — defined here so a narrower selection can never satisfy an `all` receipt |
| `local` | `analyzer`, `core`, `abstractions`, `signalr`, `quartz`, `benchmarks`, `diagnostics` |
| `fixture` | `rabbitmq`, `activemq`, `sql-transport`, `entity-framework-core` |
| `engineering` | `benchmarks`, `diagnostics` |
| `analyzer` | `analyzer` |
| `core` | `core`, `abstractions` |
| `signalr`, `quartz`, `benchmarks`, `diagnostics`, `rabbitmq`, `activemq`, `sql-transport` | each its own single category |
| `entity-framework` | `entity-framework-core` (selection name and category name differ) |

The union of `local` + `fixture` is exactly the eleven categories that have an anchor file. There
is no twelfth category anywhere in the model.

### 4.2 Jobs (9) → selections

`analyzer→analyzer`, `core-unit→core`, `signalr→signalr`, `quartz→quartz`,
`rabbitmq→rabbitmq`, `activemq→activemq`, `sql-transport→sql-transport`,
`entity-framework→entity-framework`, `benchmarks→engineering`.

`.github/workflows/build.yml` has exactly these nine `python3 tools/ci/verify.py --selection X`
jobs plus `policy`, `build` and `pack`; `pack` `needs:` all eleven of them.

### 4.3 Verification classes (6) and dueness classes (7)

Classes: `LOCAL_REQUIRED_RUN`, `PINNED_FIXTURE_REQUIRED_RUN`, `REAL_EPHEMERAL_CLOUD`,
`COMPILE_AND_PACK_PROOF`, `DEVELOPER_TOOL_COMPILE_PROOF`, `SUPPORT_LIBRARY`.

Dueness: `NOT_DUE_EXTERNAL_INFRASTRUCTURE`, `NOT_DUE_BENCHMARK`, `NOT_DUE_TIMING_SENSITIVE`,
`NOT_DUE_MANUAL_OBSERVATION`, `NOT_DUE_ENVIRONMENT_DEPENDENT`,
`NOT_DUE_DEFECTIVE_IMPORTED_ASSURANCE`, `NOT_DUE_REAL_CLOUD_RESOURCE`. Only the last may be used
by a pinned-fixture category, and `NOT_DUE_EXTERNAL_INFRASTRUCTURE` is explicitly forbidden there
because nine cases once hid behind it while the broker they needed was the one the required
profile starts.

### 4.4 Capabilities (19) → projects

| Capability | Class | src projects | test projects | tool/support |
|---|---|---|---|---|
| `analyzers` | LOCAL_REQUIRED_RUN | Analyzers, Analyzers.CodeFixes, Analyzers.Package | `tests/ViciOne.ServiceBus.Analyzers.Tests` | — |
| `benchmarks` | DEVELOPER_TOOL_COMPILE_PROOF | — | `benchmarks/ViciOne.ServiceBus.Benchmarks.Tests` | Benchmark, BenchmarkConsole |
| `core` | LOCAL_REQUIRED_RUN | ViciOne.ServiceBus, ViciOne.ServiceBus.Abstractions | `tests/ViciOne.ServiceBus.Abstractions.Tests`, `tests/ViciOne.ServiceBus.Tests` | — |
| `diagnostics` | DEVELOPER_TOOL_COMPILE_PROOF | — | `tests/Tools/ViciOne.ServiceBus.Diagnostics.Tests` | `tools/diagnostics/ViciOne.ServiceBus.Diagnostics` |
| `message-data-amazon-s3` | REAL_EPHEMERAL_CLOUD | Persistence/AmazonS3 | — (none) | — |
| `message-data-azure-storage` | REAL_EPHEMERAL_CLOUD | Persistence/Azure.Storage | — (none) | — |
| `persistence-azure-table` | REAL_EPHEMERAL_CLOUD | Persistence/Azure.Table | Azure.Table.Tests | — |
| `persistence-dynamodb` | REAL_EPHEMERAL_CLOUD | Persistence/DynamoDbIntegration | DynamoDbIntegration.Tests | — |
| `persistence-entity-framework-core` | PINNED_FIXTURE_REQUIRED_RUN | Persistence/EntityFrameworkCoreIntegration | EntityFrameworkCoreIntegration.Tests | — |
| `rider-azure-event-hubs` | REAL_EPHEMERAL_CLOUD | Transports/EventHubIntegration | EventHubIntegration.Tests | — |
| `scheduling-quartz` | LOCAL_REQUIRED_RUN | Scheduling/QuartzIntegration | QuartzIntegration.Tests | — |
| `serialization-messagepack` | LOCAL_REQUIRED_RUN | ViciOne.ServiceBus.MessagePack | — (`verifiedThroughCapability: core`) | — |
| `signalr` | LOCAL_REQUIRED_RUN | ViciOne.ServiceBus.SignalR | SignalR.Tests | — |
| `state-machine-visualizer` | LOCAL_REQUIRED_RUN | ViciOne.ServiceBus.StateMachineVisualizer | — (`verifiedThroughCapability: core`) | — |
| `test-framework` | COMPILE_AND_PACK_PROOF | ViciOne.ServiceBus.TestFramework | — | — |
| `test-infrastructure` | SUPPORT_LIBRARY | — | — | support: `tests/ViciOne.ServiceBus.TestInfrastructure` |
| `transport-activemq` | PINNED_FIXTURE_REQUIRED_RUN | Transports/ActiveMqTransport | ActiveMqTransport.Tests | — |
| `transport-amazon-sqs` | REAL_EPHEMERAL_CLOUD | Transports/AmazonSqsTransport | AmazonSqsTransport.Tests | — |
| `transport-azure-service-bus` | REAL_EPHEMERAL_CLOUD | Transports/Azure.ServiceBus.Core | Azure.ServiceBus.Core.Tests | — |
| `transport-rabbitmq` | PINNED_FIXTURE_REQUIRED_RUN | Transports/RabbitMqTransport | RabbitMqTransport.Tests | — |
| `transport-sql` | PINNED_FIXTURE_REQUIRED_RUN | Transports/SqlTransport.PostgreSql, SqlTransport.SqlServer | SqlTransport.Tests | — |

### 4.5 The eleven runs, and their mapping to the eleven anchors

| Category | Job | Project | Floor | Anchor file | Identities in anchor | budget s | brokers | outage | vhost refusal | explicit |
|---|---|---|---|---|---|---|---|---|---|---|
| `abstractions` | core-unit | `tests/ViciOne.ServiceBus.Abstractions.Tests` | 74 | `expected/abstractions.txt` | **74** | 600 | — | — | — | 0 |
| `activemq` | activemq | `tests/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests` | 178 | `expected/activemq.txt` | **178** | 2400 | activemq, artemis | `activemq` | — | 0 |
| `analyzer` | analyzer | `tests/ViciOne.ServiceBus.Analyzers.Tests` | 115 | `expected/analyzer.txt` | **115** | 900 | — | — | — | 0 |
| `benchmarks` | benchmarks | `benchmarks/ViciOne.ServiceBus.Benchmarks.Tests` | 75 | `expected/benchmarks.txt` | **75** | 900 | — | — | — | 0 |
| `core` | core-unit | `tests/ViciOne.ServiceBus.Tests` | 1873 | `expected/core.txt` | **1873** | 1200 | — | — | — | 0 |
| `diagnostics` | benchmarks | `tests/Tools/ViciOne.ServiceBus.Diagnostics.Tests` | 44 | `expected/diagnostics.txt` | **44** | 300 | — | — | — | 0 |
| `entity-framework-core` | entity-framework | `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests` | 160 | `expected/entity-framework-core.txt` | **160** | 2400 | mssql, postgres | — | — | 0 |
| `quartz` | quartz | `tests/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests` | 89 | `expected/quartz.txt` | **89** | 900 | — | — | — | 0 |
| `rabbitmq` | rabbitmq | `tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests` | 370 | `expected/rabbitmq.txt` | **370** | 2400 | rabbitmq | — | `test-exclusive-*` | 1 |
| `signalr` | signalr | `tests/ViciOne.ServiceBus.SignalR.Tests` | 26 | `expected/signalr.txt` | **26** | 900 | — | — | — | 0 |
| `sql-transport` | sql-transport | `tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests` | 110 | `expected/sql-transport.txt` | **110** | 2400 | mssql, postgres | — | — | 0 |
| **sum** | | | **3114** | | **3114** | | | | | **1** |

Measured, not assumed: each anchor file's identity count is `grep -vc '^#'` over the file
(`abstractions.txt` carries a 3-line comment header, `diagnostics.txt` a 4-line one, the rest
3 lines). Every floor equals its anchor size exactly — the floor is never below the recorded set
at this baseline.

The single `notExecuted` entry in the whole model:
`ViciOne.ServiceBus.RabbitMqTransport.Tests.AmazonMqTests.Connecting_to_RabbitMQ_via_Amazon.Should_connect`,
mechanism `EXPLICIT`, dueness `NOT_DUE_REAL_CLOUD_RESOURCE`. It is therefore **not** in
`expected/rabbitmq.txt`: 370 expected + 1 explicit = 371 discovered.

`openDefectsNotInventoried`: `cases: []`, `count: 0`, with the one-way rule that a case leaves
the list by being fixed and executed, never by being reclassified.

### 4.6 `testAnchors` — the five fixtures that carry a capability without a test project

| Capability | Category | Anchor fixture | Present in `expected/core.txt`? |
|---|---|---|---|
| `serialization-messagepack` | core | `ViciOne.ServiceBus.Tests.Serialization.Hardening_the_message_pack_reader` | yes (3 identities) |
| `serialization-messagepack` | core | `ViciOne.ServiceBus.Tests.Serialization.Owning_the_message_pack_option_set` | yes (2 identities) |
| `state-machine-visualizer` | core | `...SagaStateMachineTests.Automatonymous.When_visualizing_a_state_machine` | yes (3 identities) |
| `state-machine-visualizer` | core | `...SagaStateMachineTests.Automatonymous.When_visualizing_a_state_machine_again` | yes (1 identity) |
| `state-machine-visualizer` | core | `...SagaStateMachineTests.Dynamic_Modify.When_visualizing_a_state_machine` | yes (3 identities) |

All five verified present at this commit. Each of them is an obligation the successor must be able
to name, because in the target tree `serialization-messagepack` and `state-machine-visualizer` get
test projects of their own (`tests2/ViciOne.ServiceBus.MessagePack.Tests`,
`tests2/ViciOne.ServiceBus.StateMachineVisualizer.Tests`, Lead plan §4) and the anchor construct
disappears with the model.

### 4.7 Every promise, and where its successor is proposed

The 25 assurance obligations `OBL-R0-BLD-0080` … `OBL-R0-BLD-0104` in `LEDGER_DRAFT.jsonl` carry
one row per promise, each with a proposed native owner. Summary:

| Promise | Proposed successor |
|---|---|
| Total, disjoint project→capability ownership | xUnit architecture test over the evaluated MSBuild graph + new owner map (Lead §12.2 no. 3, no. 14) |
| Six verification classes | profile membership + terminal disposition per capability; `REAL_EPHEMERAL_CLOUD` → `External` |
| Selections, and `all` defined centrally | the five solution files; the profile solution is bound into the frozen evidence |
| job ↔ category bijection, checked both ways | CI-shape architecture test reading `build.yml` and the five solutions (Lead §12.2 no. 2) |
| run names its project | profile solution membership + MTP run record |
| eleven `minimumExecutedCases` floors (sum 3114) | Lead §12.2 no. 8, native discovery/execution minimums per frozen project and profile |
| floor asymmetry (raise normal, lower a decision) | Lead-hash-bound ledger + freeze record |
| eleven expected identity sets (3114 identities) | in-process xUnit completeness sentinel (§12.1) + pairwise-disjoint frozen projections (§12.2 no. 17) |
| generated-never-hand-edited, `null` blocks a pass | projections produced from a real MTP run, hash- and commit-bound |
| named `notExecuted` with mechanism/dueness/reason | §12.2 no. 6 forbids skips; the one entry becomes an `External` obligation or a terminal disposition |
| `explicitAttributeCount` per run | zero-tolerance architecture test instead of a counter |
| seven dueness classes and their restriction | terminal dispositions of §9 + the `External` profile (mapping gap flagged, see FINDINGS F-08) |
| empty open-defect list with a one-way exit | the ledger is the only place an obligation may be dispositioned, and only terminally |
| eleven `budgetSeconds` values | native MTP timeout options via the central `testconfig.json` (§8) |
| per-category broker sets + named-missing-contract failure | per-project Testcontainers fixtures (§6 no. 4); `RunnerContract_Specs` in the SQL transport project must survive |
| `allowBrokerOutage: activemq` | an explicit outage capability on the ActiveMQ fixture; the HAProxy relay property must survive |
| `oneRefusalPerVhost: test-exclusive-*` | assertion inside the RabbitMQ fixture instead of a runner parameter |
| `verifiedThroughCapability` + `testAnchors` | §12.2 no. 14; the five anchor fixtures map to concrete ledger obligations |
| "one active truth", replaced two hand-kept lists | ledger + frozen projections stay the only inventory |
| receipt binding and closed-shape validation | §12.2 no. 9 and no. 17 |
| "a blanket `dotnet test` is not a statement about the product" | the five-solution / three-profile split (§8) |
| run owns everything it writes; bare path refused | per-run Testcontainers ownership + MTP result paths (**TLP-005**) |
| per-run broker credentials, never stored or logged | Testcontainers fixture generating run-scoped credentials in process |
| every fixture input pinned by digest, no moving tag | the same digests handed to Testcontainers image parameters, read from `images.lock.json` |
| ephemeral loopback ports + six readiness definitions | Testcontainers wait strategies; each readiness definition is a separate promise |

---

## 5. `build/test-infrastructure/` — what survives, what becomes obsolete

Nine tracked files.

### 5.1 `compose.yaml` (145 lines) — six services

| Service | Image source | Published ports | Readiness |
|---|---|---|---|
| `rabbitmq` | build `./rabbitmq` | `127.0.0.1::5672`, `127.0.0.1::15672` | `rabbitmq-diagnostics check_running` **and** `check_port_connectivity` — the management API must answer too, because the setup creates the `test` vhost over HTTP |
| `artemis` | `quay.io/artemiscloud/activemq-artemis-broker@sha256:f22d0bf1…` | `127.0.0.1::61616`, `127.0.0.1::8161` | `artemis check node --url tcp://$HOSTNAME:61616` — dials the container's own name, because the acceptor does not bind loopback |
| `mssql` | `mcr.microsoft.com/mssql/server:2025-CU8-ubuntu-24.04@sha256:4bab24f3…`, `platform: linux/amd64` | `127.0.0.1::1433` | `sqlcmd -Q 'SELECT 1'` — a started process is not a usable server |
| `postgres` | `postgres:16@sha256:95206741…`, data directory in `tmpfs` | `127.0.0.1::5432` | `pg_isready` |
| `activemq-proxy` | `haproxy:3.4.3-alpine@sha256:fb87fc81…` | `127.0.0.1::61616`, `::5672`, `::8161` | `nc -z 127.0.0.1 61616` — deliberately only "is listening", because whether anything is behind it is what the outage changes |
| `activemq` | build `./activemq` | `127.0.0.1::61616`, `::5672`, `::8161` | Jolokia read returning `"status":200` |

Every published port is `127.0.0.1::<container port>`: Docker assigns a free loopback port per run
and publishes it atomically; the runner asks Docker afterwards which ports were bound. Every
credential variable uses the `${VAR:?message}` form, so an unset variable fails the run instead of
falling back to a well known account.

### 5.2 `images.lock.json` — pinned images and digests

| Key | Repository | Tag | Digest |
|---|---|---|---|
| `activemq` | `apache/activemq-classic` | `6.2.0` | `sha256:992bc29a8459f6772ff14d168a8b011c9cde7a769ec01d72788c9cc4e9229564` |
| `activemq-proxy` | `haproxy` | `3.4.3-alpine` | `sha256:fb87fc81943143b9acaea7442973e6ba654035fff76ffe7af6829dd1bcb0f7a5` (manifest list, amd64+arm64) |
| `artemis` | `quay.io/artemiscloud/activemq-artemis-broker` | *(empty)* | `sha256:f22d0bf16d7dbb7b928a048f27e4e4f537ac9af00a5fd31cf4b73a889cc0bd2a` |
| `mssql` | `mcr.microsoft.com/mssql/server` | `2025-CU8-ubuntu-24.04` | `sha256:4bab24f36c1ecd48e85f7d37df26e6bf301641d84c3fe652f9a0dcc947d512e1` (`platform: linux/amd64`) |
| `postgres` | `postgres` | `16` | `sha256:95206741a5b214807675e14165369d05b93a9cf692223b616d07cca227e74b0b` |
| `rabbitmq` | `rabbitmq` | `4.2-management` | `sha256:a2751b3b5eed89e47ebbd5e776de0d6d85d924002773933ab881da49c073b85d` |

Plugin: `rabbitmq_delayed_message_exchange` `4.2.0`,
`sha256:f168b2c09810cde3726961d31f38e3408e6a7fbff3929908d6f962061d8e70a1`, 42795 bytes, downloaded
from the GitHub release. The rationale records that 15 call sites use `UseDelayedMessageScheduler`
and 6 declare `x-delayed` exchange arguments, so the tests cannot run without it.

`publication.registryPush: false` — this slice publishes no image; both fixtures are built from the
pinned bases during the run, identically for local Compose and CI. The stated invariant is
**"No moving tag is permitted anywhere."**

### 5.3 `new-run-credentials.sh` (77 lines)

Generates five run-scoped accounts (`RMQ`, `AMQ`, `ARTEMIS`, `PG`, `MSSQL`), each with a 32-hex-char
secret from `hexdump -n 16 /dev/urandom`. The comment records why `hexdump` and not `tr | head`:
a `tr | head` pipeline raises SIGPIPE and aborts under `set -o pipefail`. Account names are fixed
(`vicione_ci`, and `sa` for SQL Server) because ActiveMQ authorises the web console by role and the
role binding lives in a config file; only the secret is regenerated. Under GitHub Actions every
secret is `::add-mask::`-ed **before** being appended to `GITHUB_ENV`. Nothing is echoed; the only
output line is `"Run-scoped broker credentials generated (values are not printed)."`.

Sourcing is detected per shell (`BASH_SOURCE` vs `ZSH_EVAL_CONTEXT`); a direct execution outside CI
refuses with an explanation.

### 5.4 `activemq/`, `rabbitmq/`, `haproxy/`

* `activemq/Dockerfile` — `FROM apache/activemq-classic:6.2.0@sha256:992bc29a…`, copies
  `activemq.xml` and `groups.properties`. The `groups.properties` delta exists because the official
  entrypoint writes `ACTIVEMQ_WEB_USER` into `users.properties` but leaves `groups.properties` at
  the stock `admins=admin`, so the run account would authenticate and then be refused with HTTP 403.
* `activemq/activemq.xml` — derived from the unmodified `conf/activemq.xml` of the pinned image,
  with **one** delta: `schedulerSupport="true"` on the `<broker>` element, because `DelayRetry_Specs`
  needs scheduled delivery. Five transport connectors (openwire 61616, amqp 5672, stomp 61613,
  mqtt 1883, ws 61614).
* `activemq/groups.properties` — one line: `admins=vicione_ci`.
* `rabbitmq/Dockerfile` — `FROM rabbitmq:4.2-management@sha256:a2751b3b…`; `ADD --chmod=644
  --checksum=sha256:f168b2c0…` for the delayed-message plugin; copies `enabled_plugins`.
* `rabbitmq/enabled_plugins` — `[rabbitmq_management,rabbitmq_delayed_message_exchange].`
* `haproxy/haproxy.cfg` — TCP relay in front of ActiveMQ for openwire/amqp/jolokia, with
  `retries 0` and no health-based failover, because a retry would hide the outage the fixture
  exists to produce. Resolves through Docker's embedded DNS (`127.0.0.11:53`) on every connection,
  so a restarted container with a new internal address is reached again without restarting the
  proxy. The comment records the measured evidence: the broker came back on 32955→32958 and
  33002→33005 in two runs.

### 5.5 Disposition: survives as a Testcontainers fixture vs. becomes obsolete

| Element | Disposition |
|---|---|
| Pinned image references and digests (`images.lock.json`) | **survives** — the same repository/tag/digest values feed Testcontainers image parameters. A fixture must read this file rather than repeat a literal. |
| Plugin pin by SHA-256 and size | **survives** — the RabbitMQ image must still be built (or a pre-built image pinned) with the delayed-message plugin. |
| `activemq.xml` `schedulerSupport="true"` delta | **survives** — a config delta a Testcontainers fixture must still apply. |
| `groups.properties` role binding | **survives** — same reason: the Jolokia call is a 403 without it. |
| `enabled_plugins` | **survives**. |
| Readiness definitions (six of them, per service) | **survives** as Testcontainers wait strategies. A generic port-open wait is weaker than five of the six. |
| Ephemeral `127.0.0.1::port` publishing | **obsolete as Compose syntax, survives as behaviour** — Testcontainers does exactly this by default and reports the mapped port. |
| `new-run-credentials.sh` | **obsolete as a script, survives as behaviour** — the per-run secret generation moves into the fixture. The `::add-mask::` half has no successor because nothing reaches a job environment any more. |
| `compose.yaml` as an orchestration file | **obsolete** once every stateful service is a per-project Testcontainers fixture. |
| `activemq-proxy` / `haproxy.cfg` | **open** — Testcontainers can restart a container while keeping the mapped port, which is exactly what the proxy exists to work around. Whether the relay is still needed is a measurement, not an assumption. Flagged in FINDINGS F-07. |
| `artemis` service | **open** — the model calls Artemis "not the ViciOne fixture and not a required gate", yet three `activemq` anchor identities carry `("artemis")`. Flagged in FINDINGS F-09. |

### 5.6 TLP-005 — shared mutable fixture ownership

The promise exists today and is stated in `docs/build.md` ("Running the tests", last two
paragraphs): one identity per run gives it its own Compose project and a run root under
`artifacts/run-output/<identity>/` carrying a token file; a run root is minted by the process that
owns it or handed down with the token that proves it, and **a bare path in the environment is
refused**. A fixture that could not be cleaned before starting is a startup failure, and a control
thread still alive after its bounded join stops the fixture from being removed underneath it.

At the Compose layer the ownership is a per-run project name plus ephemeral ports; nothing in
`compose.yaml` or `new-run-credentials.sh` is shared between two concurrent runs except the Docker
daemon itself. `postgres` puts its data directory in `tmpfs` precisely so no stale volume can
survive with an earlier run's credentials.

**Risk carried forward:** the moment brokers become per-project Testcontainers fixtures, two test
projects in the same profile solution can start against the same daemon at the same time. The
run-root token discipline has no automatic successor. It is recorded as `OBL-R0-BLD-0101`.

### 5.7 TLP-008 — recursive deletion of a caller-supplied path

**Checked, and no defect found in this cohort's scope.** A scan for `rm -r`, `rm -rf`, `rmtree`,
`shutil.rmtree`, `Directory.Delete`, `unlink` and `find … -delete` over the whole of `build/**`
returns nothing. `new-run-credentials.sh` contains no `rm` at all: it writes nothing to disk, and
its only file interaction is appending to `$GITHUB_ENV` under GitHub Actions. `compose.yaml` has no
delete; the two Dockerfiles have no delete; `haproxy.cfg`, `activemq.xml`, `groups.properties` and
`enabled_plugins` are static configuration.

The one recursive delete anywhere in my scope is `.github/workflows/build.yml:275`
`rm -rf artifacts/packages` inside the `pack` job. It is a **fixed, repository-relative literal**,
not a caller-supplied path, and its purpose is recorded in the surrounding comment: the output
directory is emptied first so that "a package is there" means this run produced it. It is not a
TLP-008 defect, but it is the pattern a successor must not generalise into a parameter.

`docs/build.md` describes deletion behaviour that lives in `tools/ci/**` (run roots, fixture
removal) — outside this cohort's scope and belonging to the tooling cohort.

---

## 6. `benchmarks/ViciOne.ServiceBus.Benchmarks.Tests` — obligation rows

Six source files, seven fixture classes, **63 obligation rows covering all 75 bound anchor
identities** (`OBL-R0-BLD-0001` … `OBL-R0-BLD-0063`). A parameterized method is one obligation row
carrying its resolved variant list, per reading rule §3; the row-to-identity expansion is the table
below and it closes in both directions.

| Fixture | File | Rows | Anchor identities |
|---|---|---|---|
| `AnalyticsTests` | `AnalyticsTests.cs` | 13 | 14 |
| `BenchmarkRunOutcomeTests` | `BenchmarkRunOutcomeTests.cs` | 6 | 10 |
| `BusOutboxDatabaseSettingsTests` | `BusOutboxDatabaseSettingsTests.cs` | 3 | 3 |
| `MessageMetricCaptureTests` | `MessageMetricCaptureTests.cs` | 17 | 17 |
| `RabbitMqOptionSetTests` | `RabbitMqOptionSetTests.cs` | 18 | 25 |
| `SendMetricReporterTests` | `MessageMetricCaptureTests.cs` (second class in the same file) | 4 | 4 |
| `SqlOptionSetTests` | `SqlOptionSetTests.cs` | 2 | 2 |
| **total** | | **63** | **75** |

`SendMetricReporterTests` lives inside `MessageMetricCaptureTests.cs`; a census that mapped
fixtures to files by name would have lost four identities.

### 6.1 `BENCHMARK_ONLY` versus xUnit obligation — the line drawn

The distinction the task asks for is drawn on **what the code asserts**, not on where it lives:

* **`PROPOSED_BENCHMARK_ONLY` (16 rows, `OBL-R0-BLD-0064` … `OBL-R0-BLD-0079`)** — the actual
  measurement classes. Thirteen `[Benchmark]`-carrying classes in
  `benchmarks/ViciOne.ServiceBus.BenchmarkConsole` (`ChannelBenchmark`,
  `ConcurrentChannelBenchmark`, `CronExpressionBenchmark`, `DeserializationBenchmark`,
  `InMemoryRequestResponseBenchmark`, `JsonSerializationBenchmark`, `MediatorBatchBenchmark`,
  `MediatorBenchmark`, `NewIdConversionBenchmarks`, `NewIdGenerationBenchmarks`, `SendBenchmark`,
  `SerializationBenchmark`, `SupervisorBenchmark`) and three transport-measurement drivers in
  `benchmarks/ViciOne.ServiceBus.Benchmark` (`MessageLatencyBenchmark`,
  `RequestResponseBenchmark`, `BusOutboxBenchmark`). None of them asserts anything; a throughput or
  latency figure carries no pass or fail statement. They are started by a human, never by a
  required run.
* **`PROPOSED_REPLACED_EXECUTING` (63 rows)** — the 75 identities of
  `ViciOne.ServiceBus.Benchmarks.Tests`. Every one of them is a normal, hermetic behavioural test of
  benchmark *code*: percentile/histogram arithmetic, adaptive time formatting, the console exit-code
  contract, the two metric captures' send/complete ordering and idempotence, the send observer's
  task-return contract, and the two option sets' parsing, TLS and secret-reporting behaviour. Not
  one of them takes a measurement or asserts a duration budget, and not one of them contacts a
  broker or a database. Profile: `UnitArchitecture`.

Two of the 75 are the exception that has to be named rather than glossed over:
`AnalyticsTests.LatencyCapture_StartsBeforeTheSendDelegateIsInvoked` and
`AnalyticsTests.RequestCapture_StartsBeforeTheRequestDelegateIsInvoked` assert
`Is.GreaterThan(Stopwatch.Frequency / 200)` after a `Thread.Sleep(10)`. They are still correctness
obligations (they state an *ordering*), but their current form is a wall-clock threshold. Recorded
in the rows and in FINDINGS F-05.

### 6.2 Identity ↔ obligation, both directions

| # | anchor identity (build/verification/expected/benchmarks.txt) | obligationId |
|---|---|---|
| 1 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.FormatStopwatchTicks_UsesAnAdaptiveUnitWithoutRoundingToZero` | OBL-R0-BLD-0010 |
| 2 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.Histogram_IncludesMaximumValueAndEverySample` | OBL-R0-BLD-0006 |
| 3 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.Histogram_WithExtremeValues_DoesNotOverflow` | OBL-R0-BLD-0008 |
| 4 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.Histogram_WithIdenticalSamples_ProducesOneCompleteBucket` | OBL-R0-BLD-0007 |
| 5 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.Histogram_WithNonPositiveSegmentCount_IsRejected` | OBL-R0-BLD-0009 |
| 6 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.LatencyCapture_StartsBeforeTheSendDelegateIsInvoked` | OBL-R0-BLD-0011 |
| 7 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.Median_UsesTheSameQuantileDefinition` | OBL-R0-BLD-0002 |
| 8 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.MetricCompletionTasks_RunContinuationsAsynchronously` | OBL-R0-BLD-0013 |
| 9 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.Percentile_OutsideClosedRange_IsRejected(-0.01d)` | OBL-R0-BLD-0005 |
| 10 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.Percentile_OutsideClosedRange_IsRejected(100.01d)` | OBL-R0-BLD-0005 |
| 11 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.Percentile_UsesLinearInterpolationForSmallEvenSample` | OBL-R0-BLD-0001 |
| 12 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.Percentile_WithEmptySample_ReturnsNull` | OBL-R0-BLD-0004 |
| 13 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.Percentile_WithOneSample_ReturnsThatSample` | OBL-R0-BLD-0003 |
| 14 | `ViciOne.ServiceBus.Benchmarks.Tests.AnalyticsTests.RequestCapture_StartsBeforeTheRequestDelegateIsInvoked` | OBL-R0-BLD-0012 |
| 15 | `ViciOne.ServiceBus.Benchmarks.Tests.BenchmarkRunOutcomeTests.ExecutableRun_WithAnyFailedReport_Fails` | OBL-R0-BLD-0015 |
| 16 | `ViciOne.ServiceBus.Benchmarks.Tests.BenchmarkRunOutcomeTests.ExecutableRun_WithCriticalValidationError_Fails` | OBL-R0-BLD-0016 |
| 17 | `ViciOne.ServiceBus.Benchmarks.Tests.BenchmarkRunOutcomeTests.ExecutableRun_WithNoExecutedReports_Fails(0,0,0)` | OBL-R0-BLD-0014 |
| 18 | `ViciOne.ServiceBus.Benchmarks.Tests.BenchmarkRunOutcomeTests.ExecutableRun_WithNoExecutedReports_Fails(1,0,0)` | OBL-R0-BLD-0014 |
| 19 | `ViciOne.ServiceBus.Benchmarks.Tests.BenchmarkRunOutcomeTests.ExecutableRun_WithOnlySuccessfulReports_Succeeds` | OBL-R0-BLD-0017 |
| 20 | `ViciOne.ServiceBus.Benchmarks.Tests.BenchmarkRunOutcomeTests.Filter_IsAnExecutableCommand` | OBL-R0-BLD-0019 |
| 21 | `ViciOne.ServiceBus.Benchmarks.Tests.BenchmarkRunOutcomeTests.InformationalCommand_WithNoExecutedReports_Succeeds("--help")` | OBL-R0-BLD-0018 |
| 22 | `ViciOne.ServiceBus.Benchmarks.Tests.BenchmarkRunOutcomeTests.InformationalCommand_WithNoExecutedReports_Succeeds("--list")` | OBL-R0-BLD-0018 |
| 23 | `ViciOne.ServiceBus.Benchmarks.Tests.BenchmarkRunOutcomeTests.InformationalCommand_WithNoExecutedReports_Succeeds("-?")` | OBL-R0-BLD-0018 |
| 24 | `ViciOne.ServiceBus.Benchmarks.Tests.BenchmarkRunOutcomeTests.InformationalCommand_WithNoExecutedReports_Succeeds("-h")` | OBL-R0-BLD-0018 |
| 25 | `ViciOne.ServiceBus.Benchmarks.Tests.BusOutboxDatabaseSettingsTests.ResolveConnectionString_WhenConfigurationIsMissing_FailsBeforeConnecting` | OBL-R0-BLD-0020 |
| 26 | `ViciOne.ServiceBus.Benchmarks.Tests.BusOutboxDatabaseSettingsTests.ResolveConnectionString_WhenLocalDbIsConfigured_RejectsWindowsOnlyEndpoint` | OBL-R0-BLD-0021 |
| 27 | `ViciOne.ServiceBus.Benchmarks.Tests.BusOutboxDatabaseSettingsTests.ResolveConnectionString_WhenServerAndDatabaseAreExplicit_ReturnsValidatedConfiguration` | OBL-R0-BLD-0022 |
| 28 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.A_completion_reported_after_the_delegate_returns_is_counted_once` | OBL-R0-BLD-0026 |
| 29 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.A_completion_reported_before_the_delegate_returns_is_counted_once` | OBL-R0-BLD-0025 |
| 30 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.A_failed_send_is_not_counted_as_sent` | OBL-R0-BLD-0035 |
| 31 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.A_failed_send_leaves_no_pending_measurement` | OBL-R0-BLD-0034 |
| 32 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.A_failure_before_any_completion_is_terminal` | OBL-R0-BLD-0027 |
| 33 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.A_message_id_can_be_used_again_after_a_failed_send` | OBL-R0-BLD-0028 |
| 34 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.A_repeated_completion_counts_once` | OBL-R0-BLD-0031 |
| 35 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.A_repeated_completion_does_not_add_a_sample` | OBL-R0-BLD-0033 |
| 36 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.A_repeated_completion_keeps_the_first_timestamp` | OBL-R0-BLD-0032 |
| 37 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.A_send_that_reports_completion_and_then_fails_is_not_counted` | OBL-R0-BLD-0024 |
| 38 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.An_incomplete_send_contributes_no_sample` | OBL-R0-BLD-0038 |
| 39 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.An_unregistered_completion_does_not_advance_the_send_count` | OBL-R0-BLD-0030 |
| 40 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.An_unregistered_completion_is_refused_rather_than_invented` | OBL-R0-BLD-0029 |
| 41 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.Every_sent_message_is_counted_exactly_once` | OBL-R0-BLD-0039 |
| 42 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.Registering_the_same_message_twice_is_refused` | OBL-R0-BLD-0036 |
| 43 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.The_direct_mode_still_completes_its_own_measurement` | OBL-R0-BLD-0037 |
| 44 | `ViciOne.ServiceBus.Benchmarks.Tests.MessageMetricCaptureTests.The_start_is_registered_before_the_send_delegate_runs` | OBL-R0-BLD-0023 |
| 45 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.A_bracketed_ipv6_host_reaches_the_address_with_its_port` | OBL-R0-BLD-0055 |
| 46 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.A_given_port_survives_a_later_ssl_switch` | OBL-R0-BLD-0047 |
| 47 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.A_given_port_survives_an_earlier_ssl_switch` | OBL-R0-BLD-0046 |
| 48 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.A_host_without_a_port_is_accepted_in_every_form("127.0.0.1")` | OBL-R0-BLD-0044 |
| 49 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.A_host_without_a_port_is_accepted_in_every_form("[::1]")` | OBL-R0-BLD-0044 |
| 50 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.A_host_without_a_port_is_accepted_in_every_form("rabbit.example.invalid")` | OBL-R0-BLD-0044 |
| 51 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.A_non_default_port_reaches_the_address` | OBL-R0-BLD-0042 |
| 52 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.A_port_inside_the_host_is_refused_while_it_is_parsed` | OBL-R0-BLD-0043 |
| 53 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.A_port_outside_the_valid_range_is_refused("-1")` | OBL-R0-BLD-0045 |
| 54 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.A_port_outside_the_valid_range_is_refused("0")` | OBL-R0-BLD-0045 |
| 55 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.A_port_outside_the_valid_range_is_refused("65536")` | OBL-R0-BLD-0045 |
| 56 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.An_explicit_tls_server_name_wins_over_the_host` | OBL-R0-BLD-0050 |
| 57 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.Defaults_DoNotWeakenTlsValidation` | OBL-R0-BLD-0040 |
| 58 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.Every_batch_option_order_ends_in_one_effective_configuration("--batch-limit=7","--batch-timeout=23","--batch=true")` | OBL-R0-BLD-0053 |
| 59 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.Every_batch_option_order_ends_in_one_effective_configuration("--batch-limit=7","--batch=true","--batch-timeout=23")` | OBL-R0-BLD-0053 |
| 60 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.Every_batch_option_order_ends_in_one_effective_configuration("--batch=true","--batch-limit=7","--batch-timeout=23")` | OBL-R0-BLD-0053 |
| 61 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.SslOption_ControlsTlsAndDefaultPort(False,5672)` | OBL-R0-BLD-0041 |
| 62 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.SslOption_ControlsTlsAndDefaultPort(True,5671)` | OBL-R0-BLD-0041 |
| 63 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.Ssl_still_moves_the_default_port_when_none_was_given` | OBL-R0-BLD-0048 |
| 64 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.Switching_batching_off_reaches_the_effective_settings` | OBL-R0-BLD-0054 |
| 65 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.The_parsed_batch_limit_reaches_the_effective_settings` | OBL-R0-BLD-0051 |
| 66 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.The_parsed_batch_timeout_reaches_the_effective_settings` | OBL-R0-BLD-0052 |
| 67 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.The_reported_options_are_the_effective_ones` | OBL-R0-BLD-0056 |
| 68 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.The_reported_options_say_when_no_password_was_given` | OBL-R0-BLD-0057 |
| 69 | `ViciOne.ServiceBus.Benchmarks.Tests.RabbitMqOptionSetTests.The_tls_server_name_follows_the_host_that_was_given` | OBL-R0-BLD-0049 |
| 70 | `ViciOne.ServiceBus.Benchmarks.Tests.SendMetricReporterTests.A_faulted_capture_task_is_not_swallowed` | OBL-R0-BLD-0060 |
| 71 | `ViciOne.ServiceBus.Benchmarks.Tests.SendMetricReporterTests.A_registered_message_is_completed_through_the_reporter` | OBL-R0-BLD-0061 |
| 72 | `ViciOne.ServiceBus.Benchmarks.Tests.SendMetricReporterTests.A_send_without_a_message_id_is_refused` | OBL-R0-BLD-0058 |
| 73 | `ViciOne.ServiceBus.Benchmarks.Tests.SendMetricReporterTests.An_unregistered_completion_surfaces_through_the_reporter` | OBL-R0-BLD-0059 |
| 74 | `ViciOne.ServiceBus.Benchmarks.Tests.SqlOptionSetTests.ResolveAdminPassword_WhenConfigurationIsMissing_FailsBeforeConnecting` | OBL-R0-BLD-0062 |
| 75 | `ViciOne.ServiceBus.Benchmarks.Tests.SqlOptionSetTests.ResolveAdminPassword_WhenConfigured_ReturnsEnvironmentValue` | OBL-R0-BLD-0063 |

Anchor identities: 75. Ledger obligation rows covering them: 63.
Anchor identities without a ledger row: none
Ledger identities without an anchor: none

---

## 7. The two solutions today, and the five-solution / three-profile target

### 7.1 `ViciOne.ServiceBus.slnx` — 31 projects

Folder `/Analyzers/` (4): `src/ViciOne.ServiceBus.Analyzers`,
`src/ViciOne.ServiceBus.Analyzers.CodeFixes`, `src/ViciOne.ServiceBus.Analyzers.Package`,
`tests/ViciOne.ServiceBus.Analyzers.Tests`.

Folder `/Interoperability/` (1): `src/ViciOne.ServiceBus.MessagePack`.

Folder `/Persistence/` (8): `src/Persistence/ViciOne.ServiceBus.AmazonS3`,
`…Azure.Storage`, `…Azure.Table`, `…DynamoDbIntegration`, `…EntityFrameworkCoreIntegration`,
`tests/Persistence/ViciOne.ServiceBus.Azure.Table.Tests`,
`tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests`,
`tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests`.

Folder `/Scheduling/` (2): `src/Scheduling/ViciOne.ServiceBus.QuartzIntegration`,
`tests/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests`.

Folder `/SignalR/` (2): `src/ViciOne.ServiceBus.SignalR`, `tests/ViciOne.ServiceBus.SignalR.Tests`.

Folder `/Transports/` (11): `src/Transports/ViciOne.ServiceBus.ActiveMqTransport`,
`…AmazonSqsTransport`, `…Azure.ServiceBus.Core`, `…RabbitMqTransport`,
`…SqlTransport.PostgreSql`, `…SqlTransport.SqlServer`,
`tests/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests`, `…AmazonSqsTransport.Tests`,
`…Azure.ServiceBus.Core.Tests`, `…RabbitMqTransport.Tests`, `…SqlTransport.Tests`.

Folder `/Transports/Riders/` (2): `src/Transports/ViciOne.ServiceBus.EventHubIntegration`,
`tests/Transports/ViciOne.ServiceBus.EventHubIntegration.Tests`.

Root (7): `src/ViciOne.ServiceBus.Abstractions`, `src/ViciOne.ServiceBus.StateMachineVisualizer`,
`src/ViciOne.ServiceBus.TestFramework`, `src/ViciOne.ServiceBus`,
`tests/ViciOne.ServiceBus.Abstractions.Tests`, `tests/ViciOne.ServiceBus.TestInfrastructure`,
`tests/ViciOne.ServiceBus.Tests`.

Solution folder `/.solution/` also carries six non-project files: `.github/workflows/build.yml`,
`Directory.Build.props`, `Directory.Packages.props`, `NuGet.README.md`, `README.md`,
`signing.props`. `Directory.Build.targets` is **not** among them.

Not in this solution: the three `benchmarks/**` projects, `tools/diagnostics/…Diagnostics`,
`tests/Tools/ViciOne.ServiceBus.Diagnostics.Tests`.

### 7.2 `ViciOne.ServiceBus.Engineering.slnx` — 14 projects

Folder `/benchmarks/` (3): `benchmarks/ViciOne.ServiceBus.Benchmark`,
`benchmarks/ViciOne.ServiceBus.BenchmarkConsole`,
`benchmarks/ViciOne.ServiceBus.Benchmarks.Tests`.

Folder `/diagnostics/` (2): `tools/diagnostics/ViciOne.ServiceBus.Diagnostics`,
`tests/Tools/ViciOne.ServiceBus.Diagnostics.Tests`.

Folder `/dependencies/` (9): `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration`,
`src/Transports/ViciOne.ServiceBus.ActiveMqTransport`, `…AmazonSqsTransport`,
`…Azure.ServiceBus.Core`, `…RabbitMqTransport`, `…SqlTransport.PostgreSql`,
`src/ViciOne.ServiceBus.Abstractions`, `src/ViciOne.ServiceBus.MessagePack`,
`src/ViciOne.ServiceBus`.

`src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer` is deliberately absent here — the
benchmark only uses the PostgreSQL SQL transport.

Union of both solutions = 31 + 14 − 9 shared = **36 distinct projects**; `git ls-files '*.csproj'`
finds 36. Every tracked project is in at least one of the two solutions.

### 7.3 Proposed project → profile assignment

Rules applied: every **executable** test project (`IsTestProject=true`, has a test SDK / runner)
gets exactly one profile (Lead §12.2 no. 1). Product, tool and support projects get no profile;
they get a solution membership. `REAL_EPHEMERAL_CLOUD` → `External`;
`PINNED_FIXTURE_REQUIRED_RUN` → `LocalIntegration`; everything that needs no external service →
`UnitArchitecture`.

**Executable test projects that exist today (16):**

| # | Project | Category today | Class | **Profile** | Solutions in target |
|---|---|---|---|---|---|
| 1 | `tests/ViciOne.ServiceBus.Tests` | core | LOCAL_REQUIRED_RUN | `UnitArchitecture` | product + Unit |
| 2 | `tests/ViciOne.ServiceBus.Abstractions.Tests` | abstractions | LOCAL_REQUIRED_RUN | `UnitArchitecture` | product + Unit |
| 3 | `tests/ViciOne.ServiceBus.Analyzers.Tests` | analyzer | LOCAL_REQUIRED_RUN | `UnitArchitecture` | product + Unit |
| 4 | `tests/ViciOne.ServiceBus.SignalR.Tests` | signalr | LOCAL_REQUIRED_RUN | `UnitArchitecture` | product + Unit |
| 5 | `tests/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests` | quartz | LOCAL_REQUIRED_RUN | `UnitArchitecture` | product + Unit |
| 6 | `tests/Tools/ViciOne.ServiceBus.Diagnostics.Tests` | diagnostics | DEVELOPER_TOOL_COMPILE_PROOF | `UnitArchitecture` | product + Engineering (see Q-2) |
| 7 | `benchmarks/ViciOne.ServiceBus.Benchmarks.Tests` | benchmarks | DEVELOPER_TOOL_COMPILE_PROOF | `UnitArchitecture` | product + Engineering (see Q-2) |
| 8 | `tests/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests` | activemq | PINNED_FIXTURE | `LocalIntegration` | product + LocalIntegration |
| 9 | `tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests` | rabbitmq | PINNED_FIXTURE | `LocalIntegration` | product + LocalIntegration |
| 10 | `tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests` | sql-transport | PINNED_FIXTURE | `LocalIntegration` | product + LocalIntegration |
| 11 | `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests` | entity-framework-core | PINNED_FIXTURE | `LocalIntegration` | product + LocalIntegration |
| 12 | `tests/Transports/ViciOne.ServiceBus.AmazonSqsTransport.Tests` | — (no run) | REAL_EPHEMERAL_CLOUD | `External` | product + External |
| 13 | `tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests` | — (no run) | REAL_EPHEMERAL_CLOUD | `External` | product + External |
| 14 | `tests/Transports/ViciOne.ServiceBus.EventHubIntegration.Tests` | — (no run) | REAL_EPHEMERAL_CLOUD | `External` | product + External |
| 15 | `tests/Persistence/ViciOne.ServiceBus.Azure.Table.Tests` | — (no run) | REAL_EPHEMERAL_CLOUD | `External` | product + External |
| 16 | `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests` | — (no run) | REAL_EPHEMERAL_CLOUD | `External` | product + External |

**New test projects the target tree adds (Lead §4):**

| Project | **Profile** | Solutions | Note |
|---|---|---|---|
| `tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests` | `UnitArchitecture` | product + Unit | new; carries the architecture and graph rules of §12.2 |
| `tests2/ViciOne.ServiceBus.MessagePack.Tests` | `UnitArchitecture` | product + Unit | new; today the capability is `verifiedThroughCapability: core` with two anchor fixtures |
| `tests2/ViciOne.ServiceBus.StateMachineVisualizer.Tests` | `UnitArchitecture` | product + Unit | new; today three anchor fixtures inside core |

**Non-executable projects — no profile, solution membership only:**

| Project | `IsTestProject` | Disposition |
|---|---|---|
| `tests/ViciOne.ServiceBus.TestInfrastructure` | `false` (set in its own csproj, overriding `tests/Directory.Build.props`) | succeeded by `tests2/Testing/ViciOne.ServiceBus.Testing` (framework-neutral, no xUnit package) |
| `tests2/Testing/ViciOne.ServiceBus.Testing` | `false` | new; product/pack/publish graphs must not reach it (§6 no. 7) |
| `tests2/Testing/ViciOne.ServiceBus.Testing.Xunit` | `false` | new; the only project that may reference `xunit.v3.extensibility.core` directly |
| `src/ViciOne.ServiceBus.TestFramework` | product, packable | removed after terminal disposition (§2 no. 3); its `NUnit`+`NUnit.Analyzers` direct references are the reason it cannot survive |
| `benchmarks/ViciOne.ServiceBus.Benchmark` | `False` | Engineering solution only |
| `benchmarks/ViciOne.ServiceBus.BenchmarkConsole` | `False` | Engineering solution only |
| `tools/diagnostics/ViciOne.ServiceBus.Diagnostics` | not set (default false) | Engineering solution only |
| 20 `src/**` product projects | — | product solution, plus the profile solutions that reference them |

**Resulting profile counts:** `UnitArchitecture` 10 executable test projects (7 existing + 3 new),
`LocalIntegration` 4, `External` 5. Every executable test project appears in exactly one profile
row. Total 19 executable test projects in the target state versus 16 today.

### 7.4 The five solutions

| Solution | Contains |
|---|---|
| `ViciOne.ServiceBus.slnx` | product + the **complete** built test graph: all 20 `src` projects (minus TestFramework once removed), all 19 executable test projects, both `tests2/Testing/**` projects |
| `ViciOne.ServiceBus.Tests.Unit.slnx` | the 10 `UnitArchitecture` executables + `Testing` + `Testing.Xunit` + the `src` projects they reference |
| `ViciOne.ServiceBus.Tests.LocalIntegration.slnx` | the 4 `LocalIntegration` executables + `Testing` + `Testing.Xunit` + the transport/persistence `src` projects |
| `ViciOne.ServiceBus.Tests.External.slnx` | the 5 `External` executables + `Testing` + `Testing.Xunit` + the cloud adapter `src` projects |
| `ViciOne.ServiceBus.Engineering.slnx` | the 3 `benchmarks/**` projects + the diagnostics tool + `Diagnostics.Tests` + the `src` projects they reference |

`docs/build.md:17–31` ("Two solutions") and its two-row table are stale the moment the split lands
and must be rewritten together with the change.

---

## 8. Deviations found, reported and not adopted

Per reading rule §6, differences are reported, never adopted as the new truth.

| # | Deviation | Where |
|---|---|---|
| D-1 | `docs/build.md` says "Two solutions"; the target is five | `docs/build.md:17` |
| D-2 | `ViciOne.ServiceBus.slnx` `/.solution/` folder lists `Directory.Build.props` but not `Directory.Build.targets`, although the targets file carries all eight VOSB gates | `ViciOne.ServiceBus.slnx:2–9` |
| D-3 | The `entity-framework` selection name and the `entity-framework-core` category name differ; the anchor file is named after the category | `VERIFICATION_MODEL.json` `selections` vs `capabilities` |
| D-4 | `benchmarks` and `diagnostics` are members of both `local` (hence `all`) and `engineering`; the `benchmarks` **job** runs the `engineering` selection, so the job name and the category name are not the same thing | `VERIFICATION_MODEL.json` `jobs`, `selections` |
| D-5 | `expected/diagnostics.txt` has a **four**-line comment header and names `tools/ci/record_expected.py` as its writer; the other ten have three lines and name `tools/ci/verify.py --record-expected`. Both statements cannot be the single generator. | `expected/diagnostics.txt:1–4` vs the other ten |
| D-6 | No anchor file exists for the two `REAL_EPHEMERAL_CLOUD` capabilities that *do* have test projects, so those five test projects are compiled but never discovered — consistent with the model, but it means 5 of 16 executable test projects have **no** identity census at this baseline | `VERIFICATION_MODEL.json`, `runs: []` |

None of these was adopted; each is reported as found.
