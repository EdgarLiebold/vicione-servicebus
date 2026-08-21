# R0-CORE-B — Befunde

Kohorte `R0-CORE-B`, Basiscommit `ae73c6da748e3bc3257dffa4971ee8680e086207`.
140 Dateien, 22 973 Zeilen, 459 gelesene Testidentitäten, 475 Ledgerzeilen.
Alle Fundstellen sind `Pfad:Zeile` im Basiscommit.

---

## 1. Qualitätsmängel für den Neubau (TLP-011)

### 1.1 Feste Wartezeit als Synchronisation

| Fundstelle | Wirkung |
|---|---|
| `Middleware/Caching/TwoIndex_Specs.cs:29,58,82,102,126,136` | Sechs `Task.Delay(100)`/`Task.Delay(300)` sind die **einzige** Synchronisation zwischen dem Füllen des Caches und `cache.Statistics.Count`. Der Sweep läuft auf dem Thread-Pool. Die Schwesterfixture `Bucket_Specs.cs` wartet an derselben Stelle auf bestätigte `ICacheValueObserver`-Ereignisse — das ist das Muster für den Neubau. |
| `Pipeline/ContentFilter_Specs.cs:22` | `await Task.Delay(50)` soll belegen, dass die DENY-Nachricht **nicht** ankommt. Eine negative Zusicherung auf 50 ms Wartezeit ist bei Last wertlos. |
| `Pipeline/Transaction_Specs.cs:114` | `Thread.Sleep(5000)` blockiert einen Pool-Thread, um ein Transaktionstimeout von 1 s zu überschreiten. Fünf Sekunden je Lauf, blockierend. |
| `ContainerTests/Common_Tests/MultiBusPublishEndpoint_Specs.cs` (beide Fälle) | `await Task.Delay(1000)` ist die gesamte „Prüfung"; es folgt keine Assertion. |
| `Middleware/OrCanceled_Specs.cs:35,44` | `Task.Delay(600)` für den Fehlzeitpunkt plus `GC.Collect(); await Task.Delay(1000); GC.WaitForPendingFinalizers(); GC.Collect();` als Barriere für den Finalizerlauf. |
| `Middleware/ConcurrencyLimit_Specs.cs:28,62,96`, `Pipeline/Concurrency_Specs.cs:30,62` | `await Task.Delay(10)` im Filterrumpf hält den Slot offen. Kein Ergebniswartepunkt, aber es macht das gemessene Maximum von der Planung abhängig — `maxCount == 32` ist auf einem sehr langsamen Rechner nicht garantiert. |
| `ContainerTests/Scenarios/AnotherMessageConsumerImpl.cs:25` | `ManualResetEvent.WaitOne(TimeSpan.FromSeconds(8))` — **blockierendes** Warten in einem Property-Getter, feste 8-Sekunden-Grenze, `TimeoutException` als einziges Signal. Der Typ wird in `Common_Consumer.cs` als Scope-Abhängigkeit registriert. |

### 1.2 Assertionsfreie Erfolgsstrecke

**76 der 459 gelesenen Fälle (16,6 %) enthalten keinen einzigen Assertionsausdruck.** Sie belegen
ihre Zusicherung nur dadurch, dass ein `await` zurückkehrt oder keine Ausnahme fliegt. Nach §9 des
Leadplans ist das **kein** Löschgrund; es ist eine Verstärkungspflicht. Verteilung nach Datei:

| Anzahl | Datei |
|---:|---|
| 8 | `ContainerTests/Handler_Specs.cs` (alle Request-Formen außer der mit `custom` benanntem Endpunkt) |
| 6 | `Configuration/SendSpecification_Specs.cs` (alle sechs Fälle) |
| 5 | `ContainerTests/TenantScope_Specs.cs` |
| 4 | `Common_Tests/Common_Consumer.cs`, `Common_Tests/Common_Courier.cs`, `Common_Tests/MultiBus_Specs.cs`, `Initializers/Class_Specs.cs` |
| 3 | `Configuration/InstanceSubscription_Specs.cs`, `Middleware/Agents/Agent_Specs.cs`, `Middleware/Caching/Bucket_Specs.cs`, `Pipeline/ConnectConsumer_Specs.cs`, `Pipeline/ConnectObserver_Specs.cs` |
| 2 | `Common_SagaStateMachine.cs`, `Common_Scope.cs`, `MultiBusPublishEndpoint_Specs.cs`, `RequestClientOutbox_Specs.cs`, `HealthCheck_Specs.cs`, `Conventional/ConventionConsumer_Specs.cs`, `Pipeline/Message_Specs.cs` |
| 1 | `AccessScope_Specs.cs`, `Common_MissingDependency.cs`, `ContainerTestHarness_Specs.cs`, `Dispatcher_Specs.cs`, `EmptyBody_Specs.cs`, `EndpointConfiguration_Specs.cs`, `ExcludeTypeFromFilter_Specs.cs`, `KillSwitch_Specs.cs`, `MediatorFilter_Specs.cs`, `Bind_Specs.cs`, `Dispatch_Specs.cs`, `ConnectHandler_Specs.cs` |

Zwei Klassen sind zu unterscheiden:

- **Belegt, aber nicht als Assertion formuliert.** `Common_Consumer`, `Common_Courier`,
  `ConnectConsumer_Specs`, `Agent_Specs`, `TenantScope_Specs`: die Zusicherung wird durch einen
  `TaskCompletionSource` oder eine Ausnahme aus dem Consumer belegt (`TenantScope_Specs.cs` wirft
  `InvalidOperationException("The tenantId was not properly initialized prior to consumer
  resolution")`). Beim Neubau: die Bedingung aus dem Consumer in eine Assertion des Tests heben,
  mit begrenztem, diagnostischem Timeout.
- **Gar nicht belegt.** `SendSpecification_Specs` (sechs Fälle senden nur durch die Pipe),
  `Handler_Specs` (die Antwort wird angefordert und weggeworfen), `MultiBusPublishEndpoint_Specs`
  (siehe 1.1). Hier ist die benannte Zusicherung nirgends beobachtet.

### 1.3 Tautologische und selbstwidersprüchliche Zusicherungen

| Fundstelle | Befund |
|---|---|
| `ContainerTests/ExcludeFromConfigureEndpoints_Specs.cs:107` | `Should_exclude_saga_by_attribute` veröffentlicht `InitiateMessage`, prüft aber `harness.Consumed.Any<Ping>()` ist `false`. `Ping` wird in dieser Fixture **nie** veröffentlicht — die Zusicherung ist leer wahr und prüft den Ausschluss nicht. Die Schwesterfälle im selben Typ prüfen korrekt `Any<InitiateMessage>()`. |
| `ContainerTests/ExcludeFromConfigureEndpoints_Specs.cs:148` | `Should_exclude_state_machine_by_instance_attribute` — derselbe Fehler, ebenfalls `Any<Ping>()` statt `Any<InitiateMessage>()`. |
| `Configuration/SendSpecification_Specs.cs:15` | `Should_get_interfaces_in_proper_order` verspricht eine Reihenfolge, schreibt die Typnamen aber nur mit `Console.WriteLine` heraus. Keine Reihenfolgeprüfung. |
| `ContainerTests/MediatorFilter_Specs.cs:50` | `And_also_notify_consumed` verspricht zwei Tatsachen (Antwort aus dem Filter, `NotifyConsumed`). Die Antwort wird geholt und nie geprüft; `"It works from filter"` ist von `"It works"` nicht unterscheidbar, und `NotifyConsumed` wird gar nicht beobachtet. |
| `Common_Tests/Common_Discovery.cs:296` | Die negative Hälfte (`DiscoveryPongConsumer` darf **keinen** Endpunkt bekommen) steht nur als auskommentierte Assertion mit `// TODO`. Der Ausschlussfilter `filter.Exclude<DiscoveryPongConsumer>()` ist damit unbelegt. |

### 1.4 Geteilter veränderlicher statischer Zustand

| Fundstelle | Befund |
|---|---|
| `ContainerTests/Scenarios/SimpleConsumer.cs:11,43` | `static readonly TaskCompletionSource<SimpleConsumer> _consumerCreated` bzw. `<SimplerConsumer>`. Diese Quelle wird von **fünf** Fixtures gelesen (`Common_Consumer`, `Common_Consumer_Service_Scope`, `Registering_a_consumer_directly_in_the_container`, `Common_Consume_Filter`, `Using_mediator_alongside_the_bus`). Nach dem ersten Lauf ist sie abgeschlossen; jede spätere Fixture erhält die **Consumerinstanz eines fremden Laufs**. Die Fälle können nicht unabhängig fehlschlagen und ihr Ergebnis hängt von der Ausführungsreihenfolge ab. Schwerster Ordnungsdefekt der Kohorte. |
| `ContainerTests/Dispatcher_Specs.cs:102`, `ContainerTests/EmptyBody_Specs.cs:100` | `static readonly TaskCompletionSource<ConsumeContext<SimpleEvent>> _source` je Datei. Beide Fälle sind zudem assertionsfrei; ein zweiter Lauf im selben Prozess wäre sofort grün. |
| `ContainerTests/ContainerTestHarness_Specs.cs:162` | `public static int Attempts` wird nie zurückgesetzt; `Assert.That(SubmitOrderConsumer.Attempts, Is.EqualTo(4))` ist nur beim ersten Lauf im Prozess korrekt. |
| `Pipeline/PartitionByKey_Specs.cs:48,106` | `static int _count` in zwei Consumern, nie zurückgesetzt; `count == Limit` hängt am Erstlauf. |
| `Conventional/ConventionConsumer_Specs.cs:23,39` | `ConsumerConvention.Register<CustomConsumerConvention>()` ist prozessweiter statischer Zustand. `TearDown` entfernt sie wieder — nur wenn `TearDown` läuft. Bei einem Abbruch bleibt die Konvention für jeden späteren Test des Prozesses aktiv. |
| `Middleware/OrCanceled_Specs.cs:20,26` | `AppDomain.CurrentDomain.UnhandledException` und `TaskScheduler.UnobservedTaskException` werden abonniert und **nie abgemeldet**. |

### 1.5 Unbeobachtete beziehungsweise blockierende Nebenläufigkeit

- `Middleware/ConcurrencyLimit_Specs.cs:37,73,105`, `Middleware/OneTime_Specs.cs:34`,
  `Pipeline/Concurrency_Specs.cs:39,71`: 500 bzw. 50 `Task.Run`, gesammelt und per `Task.WhenAll`
  abgewartet — korrekt, aber `maxCount == 32` verlangt, dass alle 32 Slots **gleichzeitig** belegt
  waren; das ist eine Planungsaussage, keine Vertragsaussage.
- `Pipeline/Transaction_Specs.cs:23,78`: `Task.Run` innerhalb `UseExecuteAsync`, damit der
  Transaktionsscope auf einem anderen Thread entsteht. Beabsichtigt und dokumentiert.
- `ContainerTests/Scenarios/AnotherMessageConsumerImpl.cs:25`: blockierendes `WaitOne` (siehe 1.1).

Kein `async void` in der gesamten Kohorte. Kein leerer `catch`-Block. Vier Dateien unterdrücken
`NUnit1032` (nicht abgewarteter `Task` als Feld): `Common_Courier.cs`,
`RequestClientOutbox_Specs.cs`, `HeaderInitializer_Specs.cs`, `ContentFilter_Specs.cs` — beim Neubau
entfällt die Unterdrückung, wenn die Aufgabe lokal statt als Feld gehalten wird.

### 1.6 Negative Zusicherung über eine Ablaufzeit

Diese Fälle **beweisen** ihre negative Aussage über einen Timeout und werden bei Last falsch grün
oder falsch rot:

- `Common_Tests/Common_Consumer.cs` (`Common_Consume_FilterScope`):
  `Assert.ThrowsAsync<TimeoutException>(async () => await context.ConsumeContextEasyB.Task.OrTimeout(s: 2))`
- `Common_Tests/Common_Mediator.cs` (`Common_Mediator_FilterScope`): dasselbe Muster mit `OrTimeout(100)`
- `Common_Tests/Common_Consumer.cs` (`Common_Consumer_ConfigureEndpoint`): `TestInactivityTimeout = 2 s`
  als Beleg dafür, dass **kein** Fault veröffentlicht wird
- `ContainerTests/ReceiveEndpointDependency_Specs.cs`: 2-Sekunden-Token für
  `Consumed.Any<DependentMessage>() is False`

Das saubere Gegenmuster steht in derselben Kohorte: `Middleware/RateLimit_Specs.cs` und
`Pipeline/Concurrency_Specs.cs` belegen „wird zurückgehalten" über `pending.IsCompleted is False`
plus eine eigene Cancellation, die beweist, dass genau der Limiter der Wartegrund war. Das ist die
Vorlage für den Neubau.

---

## 2. Verpflichtungen, deren Zusicherung schwächer ist als ihr Name (Verstärkungspflichten)

Über die assertionsfreien Fälle aus 1.2 hinaus:

| Fundstelle | Name verspricht | Zusicherung leistet |
|---|---|---|
| `Middleware/Caching/Bucket_Specs.cs` (`Should_fill_them_even_fuller`, `..._remove_old_entries`, `..._with_smart_values_...`) | Kapazitätsgrenze | Die drei Fälle delegieren an `AssertShrunkToCapacity`; inhaltlich korrekt, formal ohne eigene Aussage. |
| `Middleware/Internals/TypeProperty_Specs.cs` (7 Fälle) | „enthält die Eigenschaften der Basisklasse / der Schnittstelle" | Es wird ausschließlich `properties.Count()` geprüft (2/3/2/4/4/1/1). **Welche** Eigenschaften zurückkommen, prüft niemand; eine Implementierung, die die falschen in richtiger Anzahl liefert, bleibt grün. |
| `Middleware/DynamicRouter_Specs.cs` | „routet an die passende Pipe" | Der Fall, dass **keine** Pipe passt, wird nicht geprüft. |
| `Middleware/Dispatch_Specs.cs` (`Dispatching_a_pipe_by_type`) | Typbasiertes Dispatching | Der dispatchte Rumpf schreibt nur auf die Konsole; ob er lief, prüft niemand. |
| `Middleware/PartitionByKey_Specs.cs`, `Pipeline/PartitionByKey_Specs.cs` | „für Konsistenz" | Geprüft wird nur, dass alle 100 Nachrichten ankamen. Die eigentliche Zusicherung — gleicher Schlüssel, gleiche Partition, keine Nebenläufigkeit innerhalb einer Partition — wird nirgends beobachtet. |
| `Middleware/Bind_Specs.cs` | „ContextPipe wird ausgeführt" | Die `ContextPipe` schreibt nur auf die Konsole; belegt ist nur, dass der Filter den gebundenen Kontext sah. |
| `Configuration/InstanceSubscription_Specs.cs` (3 Fälle) | „hat die Nachricht empfangen" | Belegt über `await _consumer.Task` ohne Assertion; die Feldzuweisung `_message = new MessageA()` wird nie gelesen. |
| `Common_Tests/Common_Consumers_Endpoint` | Endpunktzusammenlegung und Definitionsvorrang | Es werden zwei Antworten abgewartet; dass beide Consumer auf `queue:shared` liegen und `ConsumerBDefinition` mit `"broken"` überschrieben wurde, prüft niemand. |
| `Common_Tests/Common_Consumer_ServiceEndpoint.Should_just_startup` | Startfähigkeit | Leerer Methodenrumpf. Nach §9 kein Löschgrund — die Verpflichtung „die Fixture baut und startet" bleibt und ist beim Neubau als benannte Startzusicherung zu formulieren. |
| `ContainerTests/HealthCheck_Specs` (`Should_be_degraded_...`, `Should_be_healthy_after_restarting`) | Zustandsfolge | Es wird ausschließlich `WaitForHealthStatus(...)` abgewartet, ohne Assertion. Bei den anderen beiden Fällen derselben Datei wird der Anfangszustand geprüft — das Muster ist vorhanden, nur nicht durchgehalten. |
| `ContainerTests/EndpointConfiguration_Specs` (`Should_include_concurrency_filter_if_*`) | „enthält den Concurrency-Filter" | Geprüft werden `prefetchCount` und `concurrentMessageLimit` aus dem Probe-Ergebnis. Ob der Filter in der Pipe steht, prüft niemand; die dafür vorhandene Hilfsmethode `GetConcurrencyLimit` (Zeile 313) ist **unbenutzt**. |
| `ContainerTests/ExcludeTypeFromFilter_Specs` | „der Filter wird nur einmal aufgerufen" | Der Test enthält keine Assertion; der Beleg liegt in zwei `InvalidOperationException` im Consumer. |

---

## 3. Semantik, die ich als besonders risikoreich einstufe

Die Zusicherungen, deren Bruch beim Neubau am leichtesten unbemerkt bleibt.

1. **Filterreihenfolge und Container-Scope-Grenze.**
   `Common_Consumer_FilterOrder` und `Common_StateMachine_FilterOrder` legen fest, dass der Filter
   auf Consumer- bzw. Saga-Ebene und der auf Consumer-Message- bzw. Saga-Message-Ebene **innerhalb**
   des Containerscopes läuft (`IServiceProvider` im Payload vorhanden), der Filter auf reiner
   Message-Ebene dagegen **außerhalb** (Payload abwesend). `Common_Consumer_ScopedFilterOrder`
   verschärft das: alle drei müssen denselben `IServiceScope` sehen. Im Produkt ist das das
   Zusammenspiel von `ScopeConsumeFilter`, `ScopedConsumeFilter<T,TFilter>` und `FilterScopeProvider`
   (`src/ViciOne.ServiceBus/DependencyInjection/DependencyInjection/FilterScopeProvider.cs`: der Scope
   wird **nicht** neu erzeugt, wenn der Kontext bereits einen `IServiceProvider` oder eine
   `ConsumeContext` mit einem solchen trägt). Eine Verschiebung um eine Position ändert das
   Testergebnis nicht sichtbar, weil die drei `TaskCompletionSource` unabhängig abgewartet werden —
   die Reihenfolge selbst wird nirgends geprüft, nur die Scope-Zugehörigkeit.
   **Beim Neubau muss die Reihenfolge selbst beobachtbar werden.**

2. **Scoped-Filter läuft vor der Consumerauflösung.**
   `TenantScope_Specs` ist die einzige Stelle, die festhält, dass ein über
   `AddConfigureEndpointsCallback` registrierter `UseConsumeFilter` konstruiert und ausgeführt wird,
   bevor der Consumer und dessen Scope-Abhängigkeiten gebaut werden — und dass das auch dann gilt,
   wenn ein `UseMessageRetry` davor sitzt. Der Beleg liegt vollständig in zwei
   `InvalidOperationException` im Consumer; fünf der sechs Fälle haben keine Assertion. Eine
   Regression erschiene als Consumerfehler, nicht als klare Aussage.

3. **Identität von ConsumeContext, IPublishEndpoint und ISendEndpointProvider im Scope.**
   Die neun `Common_ConsumeContext*`-Fixtures prüfen `ReferenceEquals` zwischen dem aufgelösten
   `IPublishEndpoint`, dem `ISendEndpointProvider` und der `ConsumeContext` — in acht Varianten
   (mit/ohne Outbox, mit/ohne Registrierungskontext, einzeln/Batch, mit/ohne Zwischendienst). Das ist
   die Kernzusicherung von `ServiceCollectionBusConfigurator.AddViciOneServiceBusComponents`
   (`TryAddScoped(provider => provider.GetRequiredService<IScopedConsumeContextProvider>().GetContext() ?? MissingConsumeContext.Instance)`).
   Bricht sie, veröffentlicht ein Consumer außerhalb seiner eigenen Transaktion — und die Outbox
   greift nicht mehr.

4. **Outbox-Interception.** Dieselben Fixtures prüfen zusätzlich
   `InMemoryTestHarness.Published.Select<ServiceDidIt>().Any() is False` nach einem Fault. Das ist
   die einzige Stelle der Kohorte, die belegt, dass die Outbox bei einem Fehler wirklich verwirft.
   Sie hängt an einem `TestTimeout = 3 s`.

5. **Multi-Bus-Scope-Isolation.** `MultiBusScopeIsolation_Specs` ist die einzige Fixture, die belegt,
   dass zwei Busse je einen eigenen `IScopedConsumeContextProvider` und einen eigenen gebundenen
   `ISetScopedConsumeContext` bekommen und dass die Nachricht des einen nie im Provider des anderen
   auftaucht. Das Produkt löst das über `TypedScopedConsumeContextProvider`, der den globalen und den
   typisierten Provider gleichzeitig schiebt. Ein Rückfall auf den untypisierten Provider wäre eine
   stille Vermischung zwischen Bussen. Die Fixture ist sauber gebaut (unterschiedliche Marker je Bus,
   Richtung wird benannt) und sollte als Vorlage dienen.

6. **Retry-Verschachtelung.** `Middleware/Retry_Specs`, `Middleware/RetryDifferent_Specs`,
   `Pipeline/Retry_Specs` und `Pipeline/MessageRetryPipe_Specs` legen zusammen fest: zwei
   Retry-Filter multiplizieren sich **nicht**, in beiden Reihenfolgen, auch über eine
   `UseDispatch`-Grenze; und `UseMessageRetry` unterscheidet sich von einer einfachen
   `RetryPipeSpecification` genau dadurch, dass es einen `ConsumeRetryContext` auf den Kontext legt,
   den `GetRetryAttempt()` liest. `MessageRetryPipe_Specs` sagt das im Kommentar ausdrücklich: nur
   der letzte der drei Fälle unterscheidet die beiden Spezifikationstypen. **Diese drei Zeilen sind
   die einzige Mutationsschranke gegen ein Vertauschen der Spezifikationstypen.**

7. **Circuit-Breaker-Schwellwerte.** `Middleware/CircuitBreaker_Specs` und
   `Pipeline/CircuitBreaker_Specs` kodieren die Schwellwerte in einer nackten Zahl:
   `Assert.That(count, Is.EqualTo(6))` bei 100 fehlschlagenden Sendungen. Die 6 ist
   `ActiveThreshold + 1` mit dem Vorgabewert 5
   (`ClosedBehavior.IsActive => _attemptCount > ActiveThreshold`). Ändert sich der Vorgabewert,
   meldet der Test „6 erwartet, 11 erhalten" und benennt die Ursache nicht.
   `Middleware/LayeredRetry_Specs` macht es richtig: dort steht `(ActiveThreshold + 1) * 2` mit
   benanntem Grund.

8. **Endpunktnamensvorrang.** `Common_Registration` prüft sechs Kombinationen aus
   `ConsumerDefinition.EndpointName`, `ConsumerDefinition`-Konstruktor `Endpoint(...)` und inline
   `.Endpoint(x => x.Name)` — jeweils über die `SourceAddress` der vom Consumer veröffentlichten
   Nachricht. Das ist die vollständigste Beschreibung der Vorrangregel im Repository und der einzige
   Beleg dafür, dass inline die Definition schlägt.

9. **`DefaultEndpointNameFormatter` bei generischen Consumern.** `When_consumers_are_generic_classes`
   ist der einzige Beleg dafür, dass der Name aus dem **letzten** generischen Argument gebildet wird
   (`GetConsumerName`: `type.GetGenericArguments().Last()`) und dass drei generische Consumer damit
   unterscheidbar bleiben.

10. **Rate-Limit-Semantik.** `Middleware/RateLimit_Specs` und `Pipeline/Concurrency_Specs` sind die
    besten Fixtures der Kohorte: sie belegen „zurückgehalten" über `IsCompleted is False` plus eine
    eigene Cancellation, die beweist, dass genau der Limiter der Wartegrund war, und setzen das
    Intervall bewusst auf eine Stunde, damit der Timer während des Falls nicht feuern kann. Der
    zugehörige Produktvertrag steht in `RateLimitFilter.Send(CommandContext<SetRateLimit>)`: beim
    Senken werden Permits **einzeln zurückgenommen**, beim Anheben in einem Schritt freigegeben.

---

## 4. Aus dem Produktcode neu erkannte Lücken (keine alte Testmethode vorhanden)

Nach §9 des Leadplans befreit eine fehlende alte Testmethode kein beobachtbares Produktverhalten von
einer Disposition. Diese Zusicherungen habe ich im Produktcode gelesen und in dieser Kohorte
**nicht** belegt gefunden:

1. `DefaultEndpointNameFormatter` wirft `ConfigurationException` mit den Texten
   `A consumer may not be named "Consumer". Add a meaningful prefix when using ConfigureEndpoints.`,
   `A saga may not be named "Saga". ...` und `An activity may not be named "Activity". ...`
   (`src/ViciOne.ServiceBus/Configuration/DefaultEndpointNameFormatter.cs:210,246,273`). Kein Test.
2. `DefaultEndpointNameFormatter.GetTemporaryQueueName` kürzt Maschinen-, Prozess- und Tagnamen auf
   ein Drittel des Budgets, wenn der Gesamtname 72 Zeichen überschreitet (`:139-181`). Kein Test in
   dieser Kohorte.
3. `DependencyInjectionFilterExtensions` wirft an fünf Stellen
   `ConfigurationException("The scoped filter must be a generic type definition")` (`:36,99,158,217,276`)
   — der Negativpfad jeder `Use*Filter(Type, context)`-Überladung. Kein Test.
4. `DependencyInjectionRegistrationExtensions:59` wirft
   `AddMediator() was already called and may only be called once per container.` Kein Test.
5. `ConcurrencyLimitFilter.Send(CommandContext<SetConcurrencyLimit>)` und
   `RateLimitFilter.Send(CommandContext<SetRateLimit>)` werfen
   `ArgumentOutOfRangeException("The concurrency limit must be >= 1")` bzw. `"The rate limit must be >= 1"`.
   Kein Test.
6. `ConcurrencyLimitFilter.StopAgent` nimmt beim Stoppen alle Slots und gibt sie im `finally` wieder
   frei — die Drain-Semantik beim Herunterfahren ist unbelegt.
7. `RedeliveryRetryFilter` verpackt einen Fehler beim Neuzustellen in
   `TransportException(inputAddress, "The message delivery could not be rescheduled", AggregateException(...))`.
   Kein Test dieser Kohorte erreicht diesen Pfad.
8. `CircuitBreakerFilter` veröffentlicht `CircuitBreakerClosed` beim Schließen; nur `Opened` wird
   irgendwo beobachtet. `HalfOpenBehavior` (Wiederöffnen nach einem Fehler im halboffenen Zustand)
   ist vollständig unbelegt.
9. `MessageInitializer.AddConvention<T>()` — die Erweiterbarkeit der Initializerkonventionen ist
   unbelegt (die `Conventional`-Dateien prüfen `ConsumerConvention`, eine andere Erweiterung).
10. `PropertyProviderFactory.TryGetPropertyProvider` gibt `false` zurück, wenn kein Provider passt;
    `PropertyProvider_Specs` erreicht diesen Zweig nie.
11. `ScopedConsumeContextProvider.PushContext(null)` wirft `ArgumentNullException`; ungeprüft.
12. `SerializationConfiguration` wirft an sechs Stellen `"The serializer collection was already created."`
    sowie `"No serializer content type specified and more than one serializer was configured"` — in
    dieser Kohorte unbelegt. Möglicherweise Umfang der Serialization-Kohorte; als Hinweis gemeldet,
    nicht beansprucht.

---

## 5. Punkte, die eine Leadentscheidung brauchen (`QUESTION`, 30 Zeilen)

| Gruppe | Zeilen | Grund |
|---|---:|---|
| Geerbte Future-Identitäten | 16 | Basisfixture außerhalb der Kohorte, Assertionsinhalt nicht lesbar (`RECONCILIATION.md` §3.1) |
| Abstrakte Szenariofixtures | 5 | Keine abgeleitete Fixture, nicht im Anker (`RECONCILIATION.md` §3.2) |
| `MultiBusPublishEndpoint_Specs` | 2 | Keine Assertion, `Task.Delay(1000)` als einziger Inhalt |
| `ExcludeSagaFromConfigureEndpoints_Specs` | 2 | Leer wahre Zusicherung auf `Any<Ping>()` |
| `Dispatcher_Specs`, `EmptyBody_Specs` | 2 | Statisch geteilte `TaskCompletionSource` plus Assertionsfreiheit |
| `Common_Discovery.Should_have_properly_configured_every_endpoint` | 1 | Auskommentierte Negativzusicherung mit `TODO` |
| `MediatorFilter.And_also_notify_consumed` | 1 | Beide benannten Tatsachen ungeprüft |
| `SendSpecification_Specs.Should_get_interfaces_in_proper_order` | 1 | Versprochene Reihenfolge nirgends geprüft |

Keine dieser Zeilen ist terminal disponiert. Keine wurde gelöscht, keine Abweichung als neue Wahrheit
übernommen.

---

## 6. Was ich nicht geprüft habe

- Ich habe keinen Test ausgeführt und keine Mutationsprobe gefahren; die Kohorte ist ausdrücklich
  read-only. Alle Aussagen über Verhalten sind aus Test- und Produktquelltext gelesen, nicht gemessen.
- Die Assertionsauszüge im Feld `assertionIntent` des Ledgers sind Zeilen aus dem Testrumpf (maximal
  14 je Fall). Bei sehr langen Fällen ist der Auszug gekürzt; das Feld `notes` nennt dann den
  Zeilenbereich, unter dem der vollständige Rumpf steht.
- Die Zuordnung der 16 geerbten Future-Identitäten zu ihren Basisfixtures ist aus den
  `: Basisklasse`-Deklarationen in `Future_Specs.cs` abgeleitet; die Basisfixtures selbst habe ich
  nicht gelesen, weil sie außerhalb meines Umfangs liegen. Diese Zuordnung ist damit belegt, ihr
  Inhalt nicht.
