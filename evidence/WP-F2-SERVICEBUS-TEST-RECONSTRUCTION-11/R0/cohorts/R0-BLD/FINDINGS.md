# R0-BLD — findings

Cohort `R0-BLD`, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`,
baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`.

Every finding names the file and line it was measured from. Nothing here was changed; this is a
read-only census. Findings marked **QUESTION** need a Lead decision before the affected work can
be planned; findings marked **DEFECT** are facts about the baseline; findings marked **RISK** are
about the transition, not about the baseline.

---

## F-01 — DEFECT — the benchmark compose file breaks the "no moving tag" invariant

`build/test-infrastructure/images.lock.json` states the invariant:
*"Single source of truth for every pinned test fixture input. … No moving tag is permitted
anywhere."*

`benchmarks/ViciOne.ServiceBus.Benchmark/docker-compose.yml` contains three contradictions of it:

1. `image: otel/opentelemetry-collector` — no tag and no digest at all, i.e. `:latest`.
2. It republishes the pinned RabbitMQ fixture on **fixed** host ports `5672:5672` and
   `15672:15672`, overriding the deliberate `127.0.0.1::5672` ephemeral-loopback design of
   `build/test-infrastructure/compose.yaml` (whose header records that a fixed port collides with
   anything already listening and that probing first would only trade the collision for a binding
   race). It also drops the `127.0.0.1` binding, so the broker is published on every host
   interface.
3. `version: '2.3'` is an obsolete Compose file-format key.

The file's own comment says it "now extends the single pinned fixture definition", which is true
of the RabbitMQ service and false of the collector. This is a benchmark-tool developer convenience,
not part of any required run, but it is inside my scope and it contradicts a stated repository
invariant.

## F-02 — DEFECT — the benchmark Dockerfile escapes the central build contract

`benchmarks/ViciOne.ServiceBus.Benchmark/Dockerfile` copies
`Directory.Build.props`, `Directory.Packages.props`, `signing.props`, `ViciOne.ServiceBus.snk`,
`src/` and the benchmark project directory. It does **not** copy:

* `Directory.Build.targets` — so none of `VOSB0001`…`VOSB0008` runs inside the image build. The
  lock-file assertions, the licence/readme assertions and the whole target-framework allowlist are
  simply absent. `docs/build.md:98–102` says explicitly: *"There is no documented way to leave the
  contract"* — this image build leaves it.
* `NuGet.config` — so the restore inside the image uses whatever sources the base image and machine
  configuration provide, instead of the single named `nuget.org` source whose entire point
  (`NuGet.config:5–8`) is that machine configuration does not participate.
* `global.json` — so the SDK is whatever `mcr.microsoft.com/dotnet/sdk:10.0` currently is, not the
  pinned `10.0.302` with `rollForward: disable`.

`RestoreLockedMode` still defaults to `true` from the copied `Directory.Build.props`, so the lock
files are honoured; the missing piece is the *assertion* that they are, plus the source and SDK
pins.

## F-03 — RISK — `Console.Out` redirection in a fixture that is not marked non-parallel

`benchmarks/ViciOne.ServiceBus.Benchmarks.Tests/RabbitMqOptionSetTests.cs:268–283` redirects the
process-global `Console.Out` to a `StringWriter` and restores it in a `finally`. The fixture is
**not** `[NonParallelizable]`, unlike `BusOutboxDatabaseSettingsTests.cs:9` and
`SqlOptionSetTests.cs:9`, which are.

Today this is safe by accident: the project has no assembly-level `[Parallelizable]` attribute
(checked: `grep -rn "assembly:" benchmarks/ViciOne.ServiceBus.Benchmarks.Tests/` returns nothing),
and NUnit's default is no parallelism. Under xUnit v3 the default is the opposite — test
collections run in parallel — so a literal port of `Capture(options.ShowOptions)` would race any
other case that writes to the console. The two identities affected are
`The_reported_options_are_the_effective_ones` (which also carries the secret-leak negative) and
`The_reported_options_say_when_no_password_was_given`.

Successor requirement: `ShowOptions` should take a `TextWriter`, or the capture must be a
collection-scoped fixture. Do not carry the process-global redirect across.

## F-04 — DEFECT (against the target contract) — three fixtures mutate process environment variables

Lead plan §8: *"Tests verändern globale Prozesskultur, Zeitzone oder Umgebungsvariablen nicht
selbst."*

Five of the 75 benchmark identities do exactly that, through two identical `WithEnvironmentVariable`
helpers:

* `BusOutboxDatabaseSettingsTests.cs:48–60` — sets and restores
  `VICIONE_BENCHMARK_TEST_SQLSERVER_CONNECTION_STRING` (3 identities).
* `SqlOptionSetTests.cs:43–55` — sets and restores
  `VICIONE_BENCHMARK_TEST_POSTGRES_ADMIN_PASSWORD` (2 identities).

Both fixtures are `[NonParallelizable]`, which is the honest mitigation under NUnit and which
disappears under xUnit's parallel default. The obligation itself is sound — "a missing
configuration is refused before connecting, with no built-in fallback" — but the mechanism must
change: `BusOutboxDatabaseSettings.ResolveConnectionString` and `SqlOptionSet.ResolveAdminPassword`
both call `Environment.GetEnvironmentVariable` directly, so an injected reader is a **product
edit** of the benchmark tool.

**QUESTION for the Lead:** the benchmark tool is not `src/**`, so §2 no. 2/no. 3 do not literally
apply, but it is still non-test code. Is a small seam (an injectable `Func<string,string>` or
`IEnvironment`) authorised here, or must the successor keep mutating the process environment inside
a serialised collection?

## F-05 — DEFECT (against the target contract) — two cases assert against a wall clock

`AnalyticsTests.cs:92–106` and `AnalyticsTests.cs:109–123`:

```
System.Threading.Thread.Sleep(10);
…
Assert.That(capture.GetMessageMetrics().Single().SendCompletionLatency,
    Is.GreaterThan(Stopwatch.Frequency / 200));
```

That is a fixed sleep plus a >5 ms wall-clock threshold. Lead plan §8 forbids both
(*"feste Sleeps, unbegrenzte Polls und Retry als Fehlerverdeckung sind verboten"*), and the model's
own `NOT_DUE_TIMING_SENSITIVE` class exists for exactly this shape.

The repository already knows better: `MessageMetricCaptureTests.cs:239–286`
(`The_direct_mode_still_completes_its_own_measurement`) records in its comment that *"The earlier
form slept and then required more than five milliseconds to have elapsed, which is a wall clock
threshold: it could fail under scheduling noise without any defect, and it proved nothing the
ordering below does not prove exactly"* — and replaced it with a `TaskCompletionSource` barrier plus
`Is.GreaterThan(0)`.

The two `AnalyticsTests` cases were not converted. Their obligation (the start is registered before
the delegate runs) is genuine and must survive; the mechanism must become the same barrier form.
Affected identities: `LatencyCapture_StartsBeforeTheSendDelegateIsInvoked`,
`RequestCapture_StartsBeforeTheRequestDelegateIsInvoked`.

## F-06 — RISK — coverage gaps inside the benchmark cohort worth filling on reconstruction

These are not deletions and not deviations from the anchor; they are places where the 75 identities
leave a stated branch of the code untested. Listed so the successor does not silently inherit them.

| Unit | Untested branch |
|---|---|
| `Analytics.Histogram` | only `segmentCount == 0` is refused; a negative count is the same branch but is not a variant |
| `Analytics.Percentile` | `ArgumentNullException` for a null source (line 38–39) is never reached |
| `BenchmarkReporting.FormatStopwatchTicks` | the negative-ticks `ArgumentOutOfRangeException` (line 120–121) and the ms and us branches are never asserted; only the ns branch is |
| `BenchmarkReporting.WriteLatencySummary` / `WriteHistogram` | no case at all: three argument guards and the whole output shape are untested |
| `BenchmarkRunOutcome.IsInformationalOnly` | case-insensitivity is implemented (`OrdinalIgnoreCase`) but never asserted (`--HELP`) |
| `BusOutboxDatabaseSettings.ResolveConnectionString` | the empty/whitespace variable-*name* guard, the `ArgumentException`→`OptionException` wrap for a syntactically invalid string, and "Server given but Initial Catalog missing" are untested |
| `SqlOptionSet.ResolveAdminPassword` | the empty/whitespace variable-name guard is untested |
| `RabbitMqOptionSet.SetPort` | the accepted extremes 1 and 65535 are never asserted as positives |
| `RabbitMqOptionSet` | `--vhost`, `--username`, `--heartbeat`, `--confirm`, `--split` and `FormatHostAddress`'s virtual-host path segment have no case |

## F-07 — QUESTION — does the HAProxy relay survive the move to Testcontainers?

`build/test-infrastructure/haproxy/haproxy.cfg` exists for one reason, recorded in its own header:
Compose publishes an ephemeral loopback port per container start, so a **restarted** broker returns
on a different port and no client could reconnect to the address it was given (measured twice:
32955→32958 and 33002→33005). The proxy is never restarted, so the three addresses handed to the
test process stay valid while the broker behind them really goes away.

It is deliberately dumb: `retries 0`, no load balancing, no health-based failover, because *"a retry
would hide the outage this fixture exists to produce"*.

This backs `allowBrokerOutage: "activemq"` and the identities
`Recovering_from_a_broker_outage.AMQP`, `Recovering_from_a_broker_outage.OpenWire` and the whole
`Asking_the_runner_to_take_the_broker_away` fixture (14 identities in `expected/activemq.txt`).

Testcontainers can stop and start a container while keeping its mapped port, which would make the
relay unnecessary — but that is a claim to be **measured**, not assumed. If it holds, the whole
`haproxy` directory and the `activemq-proxy` service become obsolete; if it does not, the relay
must be reproduced as a second container in the ActiveMQ fixture. **This decision blocks the
ActiveMQ LocalIntegration fixture design.**

## F-08 — QUESTION — two dueness classes have no counterpart among the terminal dispositions

The reading rules name four terminal dispositions: `REPLACED_EXECUTING`,
`REMOVED_WITH_PRODUCT_CAPABILITY`, `BENCHMARK_ONLY`, `DIAGNOSTIC_ONLY`.

The model's seven dueness classes map cleanly for three of them
(`NOT_DUE_BENCHMARK` → `BENCHMARK_ONLY`, `NOT_DUE_MANUAL_OBSERVATION` → `DIAGNOSTIC_ONLY`,
`NOT_DUE_REAL_CLOUD_RESOURCE` → the `External` profile). Two have no obvious counterpart:

* `NOT_DUE_DEFECTIVE_IMPORTED_ASSURANCE` — *"Asserts an outcome its own configuration never engages.
  … none of [the other five classes] can state that the case itself is unsound, and forcing it into
  one of them would be false."* A case that is unsound is neither replaced, nor removed with a
  product capability, nor a benchmark, nor a diagnostic.
* `NOT_DUE_TIMING_SENSITIVE` — see F-05: the two `AnalyticsTests` cases are exactly this shape and
  are nevertheless required identities today.

The list is empty at this baseline (`openDefectsNotInventoried.count: 0`, every `notExecuted` array
empty except the single rabbitmq entry), so nothing is currently blocked. The question is whether
the successor needs a fifth terminal disposition or whether such a case must become a `QUESTION`
forever.

## F-09 — QUESTION — Artemis is "not a required gate" but sits inside a required category

`VERIFICATION_MODEL.json`, `images.lock.json` and `compose.yaml` all say the same thing about
Artemis: *"It is not the ViciOne fixture and is not a required gate."*

But `expected/activemq.txt` — the anchor of the **required** `activemq` category, floor 178 —
contains three identities parameterized on `("artemis")`:

* `Configuring_ActiveMQ.Should_do_a_bunch_of_requests_and_responses("artemis")`
* `Configuring_ActiveMQ.Should_do_a_bunch_of_requests_and_responses_explicit_configuration("artemis")`
* `Delayed_redelivery.Should_properly_redeliver("artemis")`

and `VERIFICATION_MODEL.json` lists `artemis` in that run's `brokers` array. So the required run
does start Artemis and does expect three of its identities to pass. The prose and the model
disagree with each other.

**Decision needed:** in the target state, is Artemis a second `LocalIntegration` fixture with its
own obligations, or do those three identities move to a non-required profile? Either answer is
fine; the current state is ambiguous and a successor gate would inherit the ambiguity.

## F-10 — DEFECT — removing the global logger touches 42 lock files, not 16

`Directory.Packages.props:63` declares `GitHubActionsTestLogger` `3.0.5` as a
`GlobalPackageReference`. All **42** tracked `packages.lock.json` files record it as a `Direct`
dependency — every shipped product package, the three `netstandard2.0` analyzer projects, the two
benchmark tools, the diagnostics tool and `tests/ViciOne.ServiceBus.TestInfrastructure` included.

Lead plan §6 no. 10 requires that the change of the central test package graph updates *all*
affected lock files "including product, benchmark and diagnostics tool projects". This finding
quantifies it: 42, and the count is measured by parsing every lock file, not estimated.

A plan that only touches the 16 projects with a direct NUnit reference will leave 26 stale lock
graphs, and the locked-mode restore in `.github/workflows/build.yml:67` will fail on them.

## F-11 — DEFECT — a packable **product** package ships NUnit as a public dependency

`src/ViciOne.ServiceBus.TestFramework/ViciOne.ServiceBus.TestFramework.csproj:15`:

```
<PackageReference Include="NUnit" />
```

with **no** `PrivateAssets`. The project inherits `IsPackable=True` from
`src/Directory.Build.props:23`, and lines 9–10 make the intent explicit:
`<PackageTags>ViciOne.ServiceBus;NUnit</PackageTags>` and
`<Description>ViciOne.ServiceBus Test Framework (NUnit); …</Description>`.

So the shipped package `ViciOne.ServiceBus.TestFramework` declares `NUnit` `4.6.1` as a public
NuGet dependency of the product. `NUnit.Analyzers` on line 16–19 *does* carry `PrivateAssets=all`,
which shows the distinction was understood and not applied to `NUnit`.

This is the concrete reason the project cannot be modernised in place: Lead plan §6 no. 1 excludes
every test helper from product, package, lock, publish and non-executable tool graphs regardless of
reference kind. It is also the reason the model classes this capability `COMPILE_AND_PACK_PROOF`
with no run at all: it is packed, and what is packed carries NUnit.

Twelve of the sixteen executable test projects `ProjectReference` it, so its removal is a
sixteen-project change, not a one-project change.

## F-12 — DEFECT — the test-side MSBuild layer does not yet have the shape the plan requires

`tests/Directory.Build.props` is 10 lines:

```
1  <Project>
2
3    <Import Project="..\Directory.Build.props"/>
4
5    <PropertyGroup>
6      <IsTestProject>true</IsTestProject>
7      <IsPackable>false</IsPackable>
8    </PropertyGroup>
9
10 </Project>
```

Against Lead plan §6 no. 6 that is three gaps:

1. The parent is imported by a **hard-coded relative path** (`..\Directory.Build.props`), not by
   `$([MSBuild]::GetPathOfFileAbove(...))`. It happens to resolve correctly today because
   `tests/` sits directly under the root, and it would silently break for a nested
   `tests/Foo/Directory.Build.props`. It is at least correct in the one property that matters: the
   import carries **no `Exists` guard**, so a missing parent fails evaluation.
2. There is **no `tests/Directory.Build.targets`** at all. The late half of the contract therefore
   comes from the root file alone. That is not currently fail-open — MSBuild walks up and finds
   `Directory.Build.targets` at the root — but the plan's two import-removal mutants need two files
   to remove an import from.
3. There is **no `tests/testconfig.json`** and **no `tests/testsettings.json`**. `TZ=UTC` is set
   nowhere; the only globalization setting in the repository is
   `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: false` on the two SQL jobs in
   `.github/workflows/build.yml:205` and `:225`.

Also: `benchmarks/ViciOne.ServiceBus.Benchmarks.Tests` sits **outside** `tests/`, so it never sees
`tests/Directory.Build.props` and sets `IsPackable`/`IsTestProject` itself
(`ViciOne.ServiceBus.Benchmarks.Tests.csproj:5–6`). In the target tree it stays under
`benchmarks/`, so it will still need its own settings or a `benchmarks/Directory.Build.props`.

## F-13 — RISK — the benchmark test project drags 74 transitive packages for 75 hermetic cases

`benchmarks/ViciOne.ServiceBus.Benchmarks.Tests/packages.lock.json`: 5 `Direct` packages,
**11 `Project` references**, **74 `Transitive` packages**, because it references both benchmark tool
projects, which in turn reference the ActiveMQ, Amazon SQS, Azure Service Bus, RabbitMQ, EF Core and
PostgreSQL SQL transports.

None of the 75 identities touches a broker, a database or `BenchmarkDotNet` at run time — they
exercise `Analytics`, `BenchmarkReporting`, `BenchmarkRunOutcome`, the two `MessageMetricCapture`
types, `SendMetricReporter`, `BusOutboxDatabaseSettings`, `RabbitMqOptionSet` and `SqlOptionSet`.

The consequence for the reconstruction is a real one: a Testcontainers/xUnit architecture rule of the
form "a `UnitArchitecture` project must not reference a transport package" would fail this project
on its `ProjectReference` graph, although every case is hermetic. The rule must be written against
what the project *starts*, not against what it can link.

## F-14 — DEFECT (minor) — the two expected-file headers disagree about their own generator

Ten of the eleven anchor files carry a three-line header naming
`tools/ci/verify.py --record-expected`. `expected/diagnostics.txt` carries a **four**-line header
naming `tools/ci/record_expected.py` and adding *"tools/ci/verify.py reads this file and does not
write it."*

Both cannot be true of one generator. It changes no identity — the count is 44 either way — but any
successor that parses these headers, or any statement that "one generator wrote all eleven", is
wrong for one of the two.

## F-15 — RISK — five executable test projects have no identity census at this baseline

`tests/Transports/ViciOne.ServiceBus.AmazonSqsTransport.Tests`,
`tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests`,
`tests/Transports/ViciOne.ServiceBus.EventHubIntegration.Tests`,
`tests/Persistence/ViciOne.ServiceBus.Azure.Table.Tests` and
`tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests` are `REAL_EPHEMERAL_CLOUD`
capabilities with `runs: []`. They compile and they carry the full NUnit quartet, but no anchor
file exists for any of them.

That is consistent with the model and it is honestly stated there. It means, however, that the
3114 identities are **not** the whole of `tests/**`: they are the whole of what is *executed*.
The cohorts that own those five projects must produce their obligations from the source, because
there is no recorded set to reconcile against — and §12.2 no. 17's "union of all projections equals
the frozen total" must not be read as "3114" until those five are accounted for.

Related: `message-data-amazon-s3` and `message-data-azure-storage` have **no test project at all**,
and the model records why the earlier link to the transport capability of the same provider proved
nothing: *"that capability has no run either, and sharing a test project is not sharing a proof."*

## F-16 — QUESTION — where do the two engineering test projects live in the five-solution split?

`benchmarks/ViciOne.ServiceBus.Benchmarks.Tests` (75 identities) and
`tests/Tools/ViciOne.ServiceBus.Diagnostics.Tests` (44 identities) are hermetic and therefore belong
to the `UnitArchitecture` profile by §8. But §4 puts them in `ViciOne.ServiceBus.Engineering.slnx`,
and §8 says profile membership arises *"primär durch getrennte Projekte und die drei
Solutiondateien"* — of which Engineering is not one.

Three readings are possible and they are not equivalent:

1. Both projects are in `Tests.Unit.slnx` **and** in `Engineering.slnx`; the Unit solution is the
   profile, Engineering is a build graph. (§12.2 no. 1 stays satisfied: one profile each.)
2. Both are only in `Engineering.slnx`, and the Engineering solution is a fourth de-facto profile.
   Then §12.2 no. 8's "per profile" minimums need a fourth entry.
3. They move into `Tests.Unit.slnx` and Engineering carries only the non-test tool projects.

**This blocks the solution layout**, so it needs an answer before Wave 1 lays down the five `.slnx`
files. My proposal in `RECONCILIATION.md` §7.3 assumes reading 1.

## F-17 — DEFECT (minor) — a required job is named after one of the two categories it runs

`VERIFICATION_MODEL.json` `jobs` maps `benchmarks → engineering`, and the `engineering` selection
contains both `benchmarks` and `diagnostics`. `.github/workflows/build.yml:146–163` names that job
`benchmarks` with display name `"Required: Engineering"`, and the uploaded artifact is called
`required-benchmarks`.

So the `diagnostics` category — 44 identities, its own budget of 300 s — runs inside a job whose id
and artifact name say `benchmarks`. The model's `capabilities[diagnostics].runs[0].job` is
`"benchmarks"`, which is internally consistent but reads as a mistake. Anyone looking for where the
diagnostics tests ran will look at the wrong artifact.

---

## Cohort closure statement

Files in scope: 164. Files read: 164. Files unread: 0.
All 164 hashes match `BASELINE_TRACKED_FILE_MANIFEST.tsv`; `git status --porcelain` over the scope
is empty.

Anchor reconciliation for this cohort's anchor, `build/verification/expected/benchmarks.txt`:
75 identities, 75 mapped, 0 anchor identities without a ledger row, 0 ledger identities without an
anchor. The mapping table is in `RECONCILIATION.md` §6.2.

`LEDGER_DRAFT.jsonl`: 104 rows — 63 obligations covering the 75 benchmark identities,
16 `PROPOSED_BENCHMARK_ONLY` measurement rows, 25 assurance obligations for the promises of
`build/verification/VERIFICATION_MODEL.json`, `docs/build.md` and `build/test-infrastructure/**`.
`targetTests`, `evidenceRun` and `reviewer` are empty in every row, as R0 requires; every
`disposition` is `PROPOSED_*`.

Open QUESTIONs for the Lead: **F-04** (environment seam in the benchmark tool),
**F-07** (HAProxy relay under Testcontainers), **F-08** (missing terminal disposition for an
unsound case), **F-09** (Artemis inside a required category), **F-16** (solution membership of the
two engineering test projects). F-16 and F-07 block downstream work; the other three do not.
