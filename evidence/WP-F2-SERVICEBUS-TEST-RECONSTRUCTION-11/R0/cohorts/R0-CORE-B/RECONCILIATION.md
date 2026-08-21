# R0-CORE-B — Ankerabgleich

Kohorte `R0-CORE-B`, Team 1, `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`.
Basiscommit `ae73c6da748e3bc3257dffa4971ee8680e086207`, Baum `e5897e7632be4f491e01d51221ee59081d4d2aa0`.
Anker: `build/verification/expected/core.txt`,
SHA-256 `bc2910d3b7611aed036211fc69e67155bdbfe403ed8af5ccadfabc914ca865f5`, 1873 Identitäten
(1876 Zeilen minus drei dokumentierende Kopfzeilen). Der Hash wurde in diesem Arbeitsbaum neu gemessen
und stimmt mit Abschnitt 9 des Leadplans überein.

## 1. Lesevollständigkeit (TLP-017)

| Verzeichnis | `git ls-files` | gelesen |
|---|---:|---:|
| `tests/ViciOne.ServiceBus.Tests/ContainerTests/` | 66 | 66 |
| `tests/ViciOne.ServiceBus.Tests/Middleware/` | 40 | 40 |
| `tests/ViciOne.ServiceBus.Tests/Pipeline/` | 14 | 14 |
| `tests/ViciOne.ServiceBus.Tests/Initializers/` | 7 | 7 |
| `tests/ViciOne.ServiceBus.Tests/Configuration/` | 6 | 6 |
| `tests/ViciOne.ServiceBus.Tests/Conventional/` | 7 | 7 |
| **Summe** | **140** | **140** |

Jede Datei wurde vollständig gelesen, zeilenweise und ohne Stichprobe; die Lesung erfolgte in
zusammenhängenden Zeilenbereichen, die den gesamten Umfang jeder Datei abdecken (22 973 Zeilen,
756 773 Bytes).

`READ_MANIFEST.tsv` (`path<TAB>sha256`, `LC_ALL=C` sortiert) enthält genau diese 140 Pfade. Gegen
`evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0/BASELINE_TRACKED_FILE_MANIFEST.tsv`, auf den
Kohortenumfang gefiltert und auf die Spaltenreihenfolge des Regelwerks gedreht, ist der Vergleich
**identisch in beiden Richtungen** — kein Pfad und kein Hash weicht ab.

> Formhinweis für den Integrator: das Baselinemanifest führt `sha256<TAB>path`, die Regeldatei
> (`COHORT_READING_RULES.md` §2) verlangt `path<TAB>sha256`. Diese Kohorte folgt der Regeldatei.

## 2. Herleitung der gefilterten Ankermenge

Der Auftrag nennt die Namensraumsegmente. Ich habe die Menge nicht aus den genannten Zahlen
übernommen, sondern selbst abgeleitet und danach gegen die genannten Zahlen gehalten.

Schritt 1 — Segmentfilter auf `ViciOne.ServiceBus.Tests.<Segment>.`:

| Segment | Identitäten | vom Auftrag genannt |
|---|---:|---:|
| `ContainerTests` | 196 | 196 |
| `Middleware` | 114 | 114 |
| `Initializers` | 88 | 88 |
| `Pipeline` | 35 | 35 |
| `Configuration` | 23 | 23 |
| `Conventional` | 2 | (selbst zu bestimmen) |
| Zwischensumme | **458** | |

`Conventional` trägt genau zwei Identitäten:
`Conventional.Configuring_a_consumer_by_custom_convention.Should_find_the_message_handlers` und
`Conventional.Configuring_a_consumer_by_default_conventions.Should_find_the_message_handlers`.
Die übrigen fünf Dateien des Verzeichnisses sind Konventions- und Konnektorbausteine ohne
Testmethode.

Schritt 2 — drei Dateien meines Umfangs deklarieren nicht das Verzeichnisnamensraumsegment, sondern
den Wurzelnamensraum `ViciOne.ServiceBus.Tests`. Ihre Identitäten liegen deshalb nicht unter einem der
sechs Segmente und wären bei reiner Segmentfilterung verloren gegangen:

| Datei | Typ | Identitäten |
|---|---|---:|
| `ContainerTests/Metrics_Specs.cs` | `ViciOne.ServiceBus.Tests.ConsumeMetrics_Specs` | 5 |
| `ContainerTests/InstrumentationRegistration_Specs.cs` | `ViciOne.ServiceBus.Tests.InstrumentationRegistration_Specs` | 6 |
| `ContainerTests/KillSwitchInstrumentation_Specs.cs` | `ViciOne.ServiceBus.Tests.KillSwitchInstrumentation_Specs` | 1 |
| | | **12** |

Alle drei Typnamen wurden gegen `tests/` und `src/` geprüft und sind eindeutig; es gibt keine
gleichnamige Deklaration außerhalb meines Umfangs, die diese zwölf Identitäten beanspruchen könnte.

**Selbst abgeleitete gefilterte Ankersumme: 458 + 12 = 470 Identitäten.**

Gegenprobe je Identität: für alle 470 liegt der Typteil (alles vor dem letzten Punkt) in einer der
140 Dateien dieser Kohorte, einschließlich der zwei verschachtelten Namensräume
`ContainerTests.MediatorFilter` (`MediatorFilter_Specs.cs`) und `Middleware.Rescues`
(`RescueContext_Specs.cs`). Es ist keine der 470 Identitäten einer fremden Kohorte zuzuordnen.

## 3. Abgleich Anker ↔ Ledger

- Aus dem Quelltext gelesene, im Umfang deklarierte Testidentitäten: **459**
- Gefilterte Ankeridentitäten: **470**
- Ledgerzeilen: **475** (459 gelesene + 16 geerbte)

### 3.1 Anker ohne gelesene Deklaration — 16 Identitäten, alle erklärt

`tests/ViciOne.ServiceBus.Tests/ContainerTests/Future_Specs.cs` deklariert zehn `[TestFixture]`, die
jeweils nur einen Konstruktor besitzen und ihre Testmethoden von Basisfixtures erben. Die
Basisfixtures liegen unter `tests/ViciOne.ServiceBus.Tests/TestFramework/` (Namensräume
`TestFramework.Futures.Tests` und `TestFramework.ForkJoint.Tests`) und gehören **nicht** zu dieser
Kohorte.

| Abgeleitete Fixture | Basisfixture | geerbte Identitäten |
|---|---|---:|
| `InMemoryFryFutureSpecs` | `FryFuture_Specs` | 1 |
| `InMemoryShakeFutureSpecs` | `ShakeFuture_Specs` | 2 |
| `InMemoryFryShakeFutureSpecs` | `FryShakeFuture_Specs` | 2 |
| `InMemoryBurgerFutureSpecs` | `BurgerFuture_Specs` | 2 |
| `InMemoryCalculateFutureSpecs` | `CalculateFuture_Specs` | 1 |
| `InMemoryOrderFutureSpecs` | `OrderFuture_Specs` | 4 |
| `InMemoryComboFutureSpecs` | `ComboFuture_Specs` | 1 |
| `InMemoryPriceCalculationFuture_Specs` | `PriceCalculationFuture_Specs` | 1 |
| `InMemoryPriceCalculationFuture_RegistrationSpecs` | `PriceCalculationFuture_RegistrationSpecs` | 1 |
| `InMemoryPriceCalculationFuture_Faulted` | `PriceCalculationFuture_Faulted` | 1 |
| | | **16** |

Diese 16 Identitäten sind im Ledger als eigene Zeilen geführt, Disposition `QUESTION`. Der
lesbare Anteil (In-Memory-Transport, `AddSagaRepository<FutureState>().InMemoryRepository()`, keine
zusätzlichen Dienste, leeres Setup/Teardown) ist im `behaviorContract` festgehalten; der
Assertionsinhalt ist aus dieser Kohorte heraus **nicht** lesbar und deshalb ausdrücklich als solcher
markiert statt geraten. Der Lead muss festlegen, welche Kohorte die Basisfixtures liest.

`ContainerTests/Scenarios/WhenAllCompletedOrFaulted.cs` erbt ebenfalls (von `BatchFuture_Specs`),
deklariert aber drei eigene Testmethoden; diese drei sind gelesen und normal disponiert.

### 3.2 Gelesene Deklaration ohne Ankeridentität — 5 Identitäten, alle erklärt

| Identität | Erklärung |
|---|---|
| `ContainerTests.Scenarios.When_registering_a_consumer.Should_receive_using_the_first_consumer` | `public abstract class`; keine abgeleitete Fixture im Repository |
| `ContainerTests.Scenarios.When_registering_a_consumer_by_interface.Should_receive_using_the_first_consumer` | `public abstract class`; keine abgeleitete Fixture |
| `ContainerTests.Scenarios.When_registering_a_saga.Should_have_a_subscription_for_the_first_saga_message` | `public abstract class` mit abstraktem `GetSagaRepository<T>()`; keine abgeleitete Fixture |
| `ContainerTests.Scenarios.When_registering_a_saga.Should_have_a_subscription_for_the_second_saga_message` | dito |
| `ContainerTests.Scenarios.When_registering_a_saga.Should_have_a_subscription_for_the_third_saga_message` | dito |

NUnit erzeugt für abstrakte Fixtures ohne konkrete Ableitung keine ausführbaren Fälle; deshalb fehlen
sie folgerichtig im Anker. Die Verpflichtung selbst ist damit nicht erledigt: die beschriebene
Zusicherung (Consumerauflösung aus dem Container, Nutzung der Scope-Abhängigkeit vor der Freigabe,
Freigabe danach) wird von `Common_Consumer` inhaltlich abgedeckt, die Sagavariante von `Common_Saga`.
Die fünf Zeilen tragen deshalb `QUESTION`, damit der Lead entscheidet: konkrete In-Memory-Fixture
nachziehen oder die abstrakte Fixture als überholt einstufen. **Keine Abweichung wurde als neue
Wahrheit übernommen.**

### 3.3 Ergebnis

470 Ankeridentitäten, 470 erklärt: 454 direkt im Quelltext gelesen und disponiert, 16 als geerbt
markiert. 5 gelesene Identitäten ohne Ankerentsprechung, alle erklärt. Keine unerklärte Abweichung in
einer der beiden Richtungen.

## 4. Gelesener Produktcode

Die Verhaltenszusicherungen im Ledger sind gegen den Produktcode gehalten, nicht aus den Testnamen
abgeleitet. Gelesen wurden:

**`src/ViciOne.ServiceBus` — Container/Dependency Injection**
`DependencyInjection/Configuration/ServiceCollectionBusConfigurator.cs`,
`DependencyInjection/Configuration/DependencyInjectionContainerRegistrar.cs` (Lifetimes),
`DependencyInjection/Configuration/ServiceCollectionMediatorConfigurator.cs`,
`DependencyInjection/Configuration/ServiceCollectionRiderConfigurator.cs`,
`DependencyInjection/Configuration/RegistrationServiceCollectionExtensions.cs`,
`DependencyInjection/Configuration/DependencyInjection{Consumer,Saga,SagaStateMachine,Activity,ExecuteActivity,Future}RegistrationExtensions.cs`
(nur die Registrierungs-/Lifetimezeilen),
`DependencyInjection/DependencyInjection/ScopedConsumeContextProvider.cs`,
`DependencyInjection/DependencyInjection/TypedScopedConsumeContextProvider.cs`,
`DependencyInjection/DependencyInjection/FilterScopeProvider.cs`,
`DependencyInjection/DependencyInjection/Bind.cs`.
Vollständige Lifetime-Erhebung über `grep` auf `Add*/TryAdd*` im gesamten Verzeichnis
`DependencyInjection/`.

**`src/ViciOne.ServiceBus` — Middleware / Pipes und Filter**
`Middleware/RetryFilter.cs`, `Middleware/RedeliveryRetryFilter.cs`, `Middleware/RateLimitFilter.cs`,
`Middleware/ConcurrencyLimitFilter.cs`, `Middleware/CircuitBreakerFilter.cs`,
`Middleware/CircuitBreaker/{CircuitBreakerSettings,ICircuitBreaker,ClosedBehavior,HalfOpenBehavior,CircuitBreakerEventExtensions}.cs`,
`Middleware/RescueFilter.cs`, `Middleware/LatestFilter.cs`, `Middleware/ContextFilter.cs`,
`Middleware/ForkFilter.cs`, `Middleware/ScopedFilter.cs`, `Middleware/ScopeConsumeFilter.cs`,
`Middleware/ScopedConsumeFilter.cs`.

**`src/ViciOne.ServiceBus` — Konfiguration / Builder**
`Configuration/DefaultEndpointNameFormatter.cs` (vollständig),
`Configuration/{KebabCase,SnakeCase}EndpointNameFormatter.cs` (Umfang),
`Configuration/Configuration/ConsumePipeSpecification.cs`,
`Configuration/Configuration/Partition/PartitionMessageSpecification.cs`,
`Configuration/DependencyInjection/DependencyInjectionFilterExtensions.cs` (Fehlermeldungen),
`Configuration/DependencyInjection/DependencyInjectionRegistrationExtensions.cs` (Fehlermeldungen),
sowie eine vollständige Erhebung aller `throw new ConfigurationException`-Stellen unter
`Configuration/`.

**`src/ViciOne.ServiceBus` — Initializer**
`Initializers/MessageInitializerCache.cs`, `Initializers/MessageInitializer.cs`,
`Initializers/Conventions/DefaultInitializerConvention.cs` und
`Initializers/Conventions/DictionaryInitializerConvention.cs` (Headerpräfixe),
`Initializers/Factories/MessageInitializerFactory.cs` (Headerinspektoren),
Verzeichnisinventar `Initializers/{PropertyProviders,PropertyInitializers,HeaderInitializers,TypeConverters,PropertyConverters}`.

**`src/ViciOne.ServiceBus.Abstractions` — Verträge**
`Middleware/IFilter.cs`, `Middleware/IPipe.cs`, `Middleware/IRetryPolicy.cs`,
`IEndpointNameFormatter.cs`, `Configuration/ValidationResult.cs`, `Configuration/ISpecification.cs`;
Verzeichnisinventar der Ordner `Middleware`, `Configuration`, `Initializers`.

## 5. Umgebungseinstufung

Alle 475 Zeilen tragen `profile = UnitArchitecture`. Die Kohorte ist durchgehend hermetisch: sie
benutzt ausschließlich den In-Memory-Transport (`loopback://`), In-Memory-Sagarepositorien,
`Microsoft.Extensions.DependencyInjection` und `Microsoft.Extensions.Diagnostics.Metrics.Testing`.
Kein Broker, keine Datenbank, kein Cloud-Dienst, kein Dateisystem, kein Netzwerk.

**Ausnahmen, die ich melde, ohne das Profil zu ändern** — sie sind keine Infrastrukturabhängigkeit,
aber sie machen die Hermetik zeitabhängig oder prozessweit statt fallweise:

1. `ContainerTests/KillSwitch_Specs.cs` trägt `[Category("Flaky")]` und `TestTimeout = 1 Minute`. Der
   Kommentar im Quelltext nennt eine gemessene Randlage: drei Neustartzyklen zu je einer Sekunde
   gegen ein 15-Sekunden-Gesundheitsfenster. Hermetisch ja, aber zeitlich knapp.
2. `Middleware/Caching/Bucket_Specs.cs` arbeitet mit `[CancelAfter(30000)]` und Beobachterereignissen
   statt Wartezeiten; das ist korrekt, aber die Fixture verlässt sich auf Thread-Pool-Planung.
3. `ContainerTests/{Metrics_Specs,InstrumentationRegistration_Specs,KillSwitchInstrumentation_Specs}.cs`
   greifen auf `LogContext.Current` (ein `AsyncLocal`) und auf Meter ohne Scope zu. Sie sind
   ausdrücklich darauf ausgelegt, prozessweite Zustände nicht zu verletzen, aber sie **lesen** einen
   prozessweiten Zustand und dürfen deshalb nicht parallel zu anderen Fixtures laufen, die
   `LogContext.ConfigureCurrentLogContext` aufrufen.
4. `Middleware/OrCanceled_Specs.cs` registriert Handler auf `AppDomain.CurrentDomain.UnhandledException`
   und `TaskScheduler.UnobservedTaskException` und **entfernt sie nicht wieder**; danach erzwingt es
   zwei `GC.Collect()` plus `Task.Delay(1000)`. Prozessweiter Nebeneffekt.

Keine dieser vier Stellen begründet ein `*.IntegrationTests`- oder `*.ExternalTests`-Profil. Sie
gehören in der Zielstruktur in eine nicht parallele Collection beziehungsweise brauchen ein
Aufräumen des prozessweiten Zustands.

## 6. Offene Fragen an den Lead

1. **Basisfixtures der Futures** (§3.1): welche Kohorte liest `tests/ViciOne.ServiceBus.Tests/TestFramework/`?
   Bis dahin bleiben 16 Ankeridentitäten inhaltlich unbelegt.
2. **Abstrakte Szenariofixtures** (§3.2): konkrete Fixture nachziehen oder als überholt einstufen?
3. **Spaltenreihenfolge des Manifests** (§1): Regeldatei und Baselinemanifest widersprechen sich.
4. Die 30 `QUESTION`-Zeilen des Ledgers, im Einzelnen begründet in `FINDINGS.md`.
