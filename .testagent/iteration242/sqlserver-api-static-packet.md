# SQL-Server-Transport: vorläufiges statisches API-/Testpaket

**Status: PROVISORISCH, rein statisch, keine Abnahme.** Arbeitskopie auf `HEAD b0899dafcb7006aa8c267a35aba7042e9fa86e6a` / Tree `7523770c54cafd3d204ad4e603ae635742cab3b3` gelesen. Im Zieltestprojekt waren `SqlServerTransportInspection.cs`, `SqlServerJobServiceTests.cs` und `SqlServerSchedulingTests.cs` bereits fremd verändert; die unten gebundenen SHA-256 sind **Working-Copy-Hashes**, keine Behauptung eines sauberen Git-Freeze. Kein `dotnet`-, MSBuild-, Test-, Coverage-, Mutations-, Pack- oder Providerlauf durch diesen Beitrag. Kein Produkt-/Testedit, Commit oder Push. `review/**` und `TestResults/**` wurden weder betreten noch gelesen.

## Grenze und Norm

Vollständig manuell gelesen: alle 15 aktuellen C#-Produktdateien des Projekts `src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer`, die Projektdatei, alle 21 C#-Dateien und die Projektdatei/Requirementprojektion des unmittelbar zugehörigen `tests/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests`. Dies ist **keine** repositoryweite API-, Consumer-, Build- oder Testaussage. Das Providerprojekt ist ein optionaler SQL-Server-/Azure-SQL-Transport für externe ServiceBus-Verbraucher; der Suite-A05-Carriervetrag bleibt InMemory/RabbitMQ und die Suite bindet nur diese Carrier. Aus `PO-2026-09-08-01` folgen Einzellesung, API-/Schicht-/Testspiegel- und Async-Prüfung; `PO-2026-09-04-01/-02` verlangen Startvalidierung, klare fünf API-Eigentümer und für jede öffentliche API/Parametergrenze unterscheidende Tests. Keine SQL-Server-Durable-Send-Acceptance wird aus dieser lokalen Testklasse behauptet.

### Vollständige Governance-Nachlese und Re-Audit

Die erste Paketfassung entstand **vor** der vollständigen Lektüre des langen Entscheidungsregisters und Auftragsdokuments; dies wird nicht rückwirkend als anfängliches Pre-Read ausgegeben. Danach wurden alle unten stehenden Pflichtdokumente vollständig bis EOF gelesen, einschließlich `DECISIONS.md` (2182 Zeilen) und `CURRENT_ORDER.yaml` (3286 Zeilen), und **dieses** Paket erneut gegen sie geprüft. Die aktive Direktzuweisung `PO-2026-09-08-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01` bindet den gesamten Sourcebaum und dessen dynamische Abnahme an den Lead, nicht an diesen schmalen statischen Beitrag. `PO-2026-08-13-03` erhält den SQL-Server-Transport ausdrücklich als optionalen Adapter. Die ältere SQL-Transport-Closure in `CURRENT_ORDER.yaml` beschreibt 135 Pflichten und einen damaligen lokalen Providerlauf; die hier gelesenen 47 Requirements sind **nur der aktuelle zugehörige SQL-Server-Testprojekt-Ausschnitt** und weder Wiederholung noch Verifikation jener 135er-Abnahme. Reale SQL-Server-/Azure-SQL- und umfassende API-/Coverage-/Mutationsgates bleiben durch dieses Paket unberührt. `A-0071`/`nachweissteuerung.dotnet_process_owner` sowie die explizite Lead-Zuweisung verbieten diesem Beitrag jeden .NET-/MSBuild-/Testprozess. Nach dieser Nachlese ändert sich keine der sieben statischen Risikodispositionen; sie bleiben ungeprüfte Kandidaten. Eine erneute eng begrenzte Statusprüfung zeigte weiterhin nur die drei eingangs genannten fremd geänderten Testdateien; deren SHA-256 stimmen mit der unten stehenden Lesebindung überein, und die Produktdateien sind im abgefragten Projektscope unverändert.

| Vollständig gelesene Pflichtquelle | Zeilen | SHA-256 der Lesefassung |
|---|---:|---|
| Workspace `AGENTS.md` | 40 | `9e2f39f719f9beeaea81bedb7718ce8ae67f8299e184a40c53e124adc439b836` |
| `CLAUDE.md` | 8 | `df4372ab4a600692d339c670e1180c25081ded3c74599a10e2b24555e152fa47` |
| `AI_WORKING_AGREEMENT.md` | 555 | `dc4659df3847aec06fe912d6bfe804601c4102fd92add48449ba9a12e8ad2721` |
| `GLOSSARY.md` | 243 | `cf265a224990e371eb957eb346dc83f1a7d79fe961f11707286614165a6034fb` |
| `DECISIONS.md` | 2182 | `7c66b78b0cc37971e2c1466f3a22be08a57ec496686d5c1cd08dad7681fa3ca1` |
| `current/README.md` | 36 | `a7bd61f878b84fb6f93f48402a21becd37ed253e20bab62b7026bdd165469a39` |
| `CURRENT_ORDER.yaml` | 3286 | `49691c76d63dea1591fd5a7fa17450d1e254ec62d616ef4705aee082517bfc3d` |
| `FINDINGS.md` | 127 | `4d2b7951b0d386a60137e27565484c79e7c0acd7076037fe3e40a4304b9f8a83` |
| ServiceBus `AGENTS.md` | 18 | `e4d2c7b118642c17250d81ada50c5f59ec3e38e02856fafef14f2ee95e23ab58` |
| Aktiver ServiceBus `DEVELOPMENT_SLICE.json` | — | `5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199` |

Für den statischen Testgap-Abgleich wurden `dotnet-test:test-gap-analysis`, `dotnet-test:test-analysis-extensions` mit .NET-Erweiterung und `dotnet-test:find-untested-sources` vollständig gelesen. Letzterer fordert einen Analyzerlauf; der Roslyn-Analyzer würde einen verbotenen .NET-Prozess starten, der Polyglot-Analyzer kann am Repositoryroot `review/**`/`TestResults/**` nicht ausschließen und ein isolierter künstlicher Scanroot wäre nicht der verlangte Projektbaum. Daher wurde **kein** Find-Untested-Analyzer ausgeführt. Die folgende Source→Test-Zuordnung beruht auf manueller Dateilektüre, nicht auf dem Skill-JSON, und darf weder als statischer Analyzerbefund noch als Line-/Branch-Coverage gelten.

## Öffentliche Oberfläche und Schichtentscheidungskandidaten

Außer den folgenden drei öffentlichen statischen Klassen mit sieben Methoden besitzt das gelesene Produktprojekt **keinen öffentlich oder geschützt exportierten Typ/Member**; die vielen `public`-Member auf `internal`-Typen sind nur interne Implementierungs-/Testseams. Keine `[Obsolete]`-Fläche, kein `NotImplemented` und keine Präprozessor-/Suppressionsdirektive in den 15 C#-Dateien. Keine asynchrone exportierte Methode, also hier kein `Async`-Namenskonflikt.

| Exportierter Owner / Methode | Parameter und Contract-Kandidat | Gelesener Testnachweis / offen |
|---|---|---|
| `ViciOne.ServiceBus.Configuration.SqlServerBusFactoryConfiguratorExtensions.UsingSqlServer(IBusRegistrationConfigurator, Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>?)` | DI/Provider-Komposition; `configurator` nullgeprüft; `configure` optional; Options-Auflösung über `UseSqlServer(context)` | Klassifikatorregistrierung und MultiBus-Deskriptor in `SqlServerSendFailureClassifierTests`; `SqlServerJobServiceTests` nutzt Callback, überschreibt aber Host per Connection String. Kein positiver Busstart nur aus registrierten `SqlTransportOptions`, kein Null-/Callback-Wirkungsorakel. |
| `UsingSqlServer(IBusRegistrationConfigurator, string, Action<IBusRegistrationContext, ISqlBusFactoryConfigurator>?)` | zweiter Anwendungseinstieg; `connectionString` nicht leer; Callback optional | Nur Deskriptorregistrierung in `SqlServerSendFailureClassifierTests`; kein Start-/Route-/Options-Vorrangtest gerade dieses Overloads. |
| `ViciOne.ServiceBus.SqlTransport.SqlServer.SqlServerHostConfigurationExtensions.UseSqlServer(ISqlBusFactoryConfigurator, Uri, Action<ISqlHostConfigurator>?)` | Provider-Host-API; `hostAddress` nullgeprüft; absolute-Adresse laut XML, aber keine sichtbare `IsAbsoluteUri`-Prüfung | Kein Aufruf des Uri-Overloads im zugehörigen Testprojekt; relative/Query/Port-/Credential-Grenzen offen. |
| `UseSqlServer(ISqlBusFactoryConfigurator, IBusRegistrationContext, Action<ISqlHostConfigurator>?)` | DI-Options-Einstieg | Indirekt beim `UsingSqlServer`-Callback; kein eigener Optionswert-/Fehler-/Callback-Oracle. |
| `UseSqlServer(ISqlBusFactoryConfigurator, string, Action<ISqlHostConfigurator>?)` | direkter Provider-Host-Einstieg; String nur auf Nicht-Leere geprüft, SQL-Builder validiert später | Breite positive Nutzung über `SqlServerTestDatabase.ConfigureHost`; kein gezielter Null-/Malformed-String-/Callback-Override-/Protokollpräfixfall. |
| `SqlServerTransportConfigurationExtensions.AddSqlServerMigrationHostedService(IServiceCollection, bool create = true, bool delete = false)` | DI-/Operations-Einstieg mit zwei booleschen Schaltern; `delete` kann Datenbank bei Shutdown entfernen | Keine Verwendung im zugehörigen Testprojekt; `SqlServerTestDatabase` ruft stattdessen den internen Migrator direkt. Default, Create-/Delete-Matrix, DI-Lifetime und Startfehler unbelegt. |
| `AddSqlServerMigrationHostedService(IServiceCollection, Action<SqlTransportMigrationOptions>?)` | Options-Callback, `ValidateOnStart`, Hosted Service | Kein Aufruf im zugehörigen Testprojekt; Positivstart sowie jede Validator-Invariante ohne kausalen Test. `configure = null` bleibt erlaubt. |

Schichtkandidat: Die beiden `UsingSqlServer`-Einstiege liegen in `.Configuration`, die drei Host-Overloads im Provider-Namespace; die beiden `IServiceCollection`-Erweiterungen ebenfalls im Provider-Namespace statt in `Microsoft.Extensions.DependencyInjection`, obwohl `PO-2026-09-04-02` diesen Standard für DI-Erweiterungen nennt. Das ist **eine API-Discoverability-/Consumer-Compile-Prüffrage**, keine eigenmächtige Umbenennungs- oder Move-Entscheidung. Projektname, `PackageId` und `RootNamespace` sind identisch; `Configuration`, `Database`, `Runtime` bilden kohärente technische Unterbereiche. `ApiSurfaceGlobalUsings.cs` importiert viele fremde Schichten global; ob sie nötig sind, braucht einen separaten Compile-/Diff-Beweis. Alle aktuellen XML-Kommentare sind codebezogen; der `CreateInfrastructureAsync`-Kommentar nennt auch „functions“, während die sichtbare Statementliste Prozeduren/Views/Tabellen/Sequenzen enthält — redaktioneller Kandidat.

## Dateivolle Produktdisposition, nur statische Empfehlung

| Datei (relativ zum Projekt) | Gelesene Rolle / vorläufige Disposition |
|---|---|
| `ApiSurfaceGlobalUsings.cs` | interne Namensauflösung; behalten bis compile-gestützte Minimalisierung. |
| `Configuration/SqlServerBusFactoryConfiguratorExtensions.cs` | öffentlicher DI-Bus-Einstieg und idempotenter Failure-Classifier; API-Parametertests ergänzen. |
| `Configuration/SqlServerHostConfigurationExtensions.cs` | drei öffentliche Host-Einstiege; Uri/DI/String-Verhalten getrennt beweisen. |
| `Configuration/SqlServerHostConfigurator.cs` | interner Adapter auf gemeinsame Hostkonfiguration; Behalten. |
| `Configuration/SqlServerHostSettings.cs` | SQL-DataSource-/Credential-Projektion; Präfix-/Rekonfigurationsgrenzen prüfen. |
| `Configuration/SqlServerTransportConfigurationExtensions.cs` | öffentlicher Hosted-Migration-Einstieg; Start-/Options-/Delete-Nachweis fehlt. |
| `Database/SqlServerDatabaseMigrator.cs` | einziger DDL-/Prozedur-/View-Owner; behalten, aber Expiry/Autodelete/Index- und Fehlerszenarien gezielt prüfen. |
| `Database/SqlServerIdentifier.cs` | DDL-Identifier-Whitelist; behalten; Grenz-/Fehlertests fehlen. |
| `Runtime/ISqlServerTransportConnection.cs` | interne SQL-Connection-Grenze; Behalten. |
| `Runtime/SqlServerClientContext.cs` | Dapper-Aufrufe, Send/Receive/Lock/Headers; Behalten, Sekundenkonversion und Fehlerpfade prüfen. |
| `Runtime/SqlServerConnectionContext.cs` | Connection-/Transaction-/Retry-/Maintenance-Owner; Behalten, Startfehler/Shutdown prüfen. |
| `Runtime/SqlServerConnectionContextFactory.cs` | interne Supervisor-Fabrik; Behalten. |
| `Runtime/SqlServerSendFailureClassifier.cs` | interne numerische Sendfehlertaxonomie; getestete positive/permanente/verschachtelte Pfade. |
| `Runtime/SqlServerTransportConnection.cs` | Connection-Builder, Admin-/Runtime-Credentials; Behalten, Konfigurationsmatrix prüfen. |
| `Runtime/UriTypeHandler.cs` | Dapper-URI-Konvertierung; Behalten, direkte Null-/Typ-/Malformed-Tests fehlen. |

## Bidirektionaler Test-/Requirement-Spiegel

Die eingebettete Projektion enthält **47** `requirementId`+`variantKey`→Testmethoden-Zeilen; im gelesenem C#-Projekt stehen **47** `[RequirementCoverage]`-Methoden plus genau ein ungebundenes Meta-Oracle `RequirementCoverageProjectionTests.SqlServerRequirements_ProjectOntoExactlyOneExecutingTest`. Die Projektion selbst wird dort gegen die Assembly geprüft; dies ist nur statisch festgestellt, nicht ausgeführt. Folgende Liste ist zugleich die Rückrichtung Testklasse→gebundene Anforderungen (Suffixe `OBL-R0-SQL-` abgekürzt; `REQ-…` ausgeschrieben):

| Testklasse | Requirement → konkrete Methode(n) |
|---|---|
| `SqlServerProvisioningTests` | `0040`→`ExplicitInstanceAndPort_AreProjectedIntoConnectionAndBusAddressesAsync`; `0041`→`InstanceWithoutPort_IsProjectedOnlyAsTheBusQueryOptionAsync`; `0045`→`Provisioning_CreatesEveryRequiredTableAndIndexAsync`; `0046`→`Disposal_DropsTheRunScopedDatabaseEvenWithAnAttachedSessionAsync`; `0047`→`ReceiveEndpointWithoutTopology_CreatesItsThreeQueueRowsAsync`; `0127`→`RunScopedHost_IsPreservedInTheProviderDataSourceAsync`; `REQ-VSB-SQLSERVER-ROUTINE-NAMES`→`Provisioning_ExposesOnlyTheUnversionedProviderProcedureSetAsync`. |
| `SqlServerProvisioningCredentialTests` | `0129`→`ProvisionedAccountAcceptsQuotedPasswordWithoutPublishingItInStatementTextAsync`. |
| `SqlServerConfigurationAndRetryTests` | `0109`→`TransientErrorNumberSetIsClosed` (Theory); `0105`→`ProviderPollingDelayIgnoresQueueIdAndCompletesAtTheConfiguredBoundaryOrCancellationAsync`; `0113`→`SubSecondLockDurationIsRejectedBeforeTheEndpointCanStartAsync`. |
| `SqlServerSendFailureClassifierTests` | `REQ-VSB-SQLSERVER-SEND-FAILURE/numeric-transient-and-permanent-taxonomy`→`NumericTaxonomy_ClassifiesEveryOwnedErrorNumberWithoutUsingMessageText`; `REQ-VSB-SQLSERVER-SEND-FAILURE/registration-is-singleton-across-overloads`→`BusRegistration_AddsExactlyOneClassifierAcrossConfigurationOverloads`. |
| `SqlServerDeliveryLimitTests` | `0093`→`ConfiguredMaxDeliveryCount_IsPersistedInsteadOfTheDatabaseDefaultAsync`; `0095`→`ExhaustedDelivery_IsExcludedFromFetchAndMovedByDeadLetterMaintenanceAsync`; `0130`→`FetchProcedures_ProjectTransportTimestampsAsUtcDateTimeOffsetsAsync`; `REQ-VSB-SQLSERVER-QUEUE-REDECLARATION`→`QueueRedeclaration_WithoutALimitPreservesTheConfiguredDeliveryLimitAsync`. |
| `SqlServerMaintenanceAndTopologyTests` | `0117`→`MaintenanceExecutesOrphanCleanupForEmptyAndPopulatedBatchesAsync` (Theory); `0118`→`DeadLetterPassReportsFewerThanAndExactlyTheRequestedBatchAsync` (Theory); `0119`→`DeadLetterMetricIsAbsentForNoOpAndExactForMovedRowsAsync` (Theory); `0121`→`ClientFacingStoredProcedureReportsItsExactOutcomeAsync` (Theory); `0122`→`ConcurrentQueueDeclarationsCreateOneRowForEachQueueTypeAsync`; `0123`→`DeletingMiddleTopicRemovesIncomingAndOutgoingSubscriptionsAsync`; `REQ-VSB-SQLSERVER-TOPIC-GRAPH`→`CyclicTopicGraph_UsesUniqueTopicsAndCreatesOneDeliveryPerQueueAsync`; `REQ-VSB-SQLSERVER-TOPOLOGY-CONCURRENCY`→`ConcurrentTopicAndSubscriptionDeclarations_ReturnOneIdentityPerTopologyElementAsync`. |
| `SqlServerPublishAndPurgeTests` | `0067`→`UnsubscribedPublish_LeavesNeitherMessageNorDeliveryWhileSubscribedControlArrivesAsync`; `0069`→`PurgeOnStartup_RemovesExistingDeliveryAndThenConsumesOnlyTheNewMessageAsync`; `REQ-VSB-SQLSERVER-PURGE-ISOLATION`→`PurgeQueue_RemovesOnlyPrimaryDeliveriesAndPreservesErrorAndDeadLetterRowsAsync`. |
| `SqlServerScheduleCancellationTests` | `0063`→`ConsumeContextCancellation_DeletesThePersistedFutureDeliveryAsync`; `0065`→`CallerCancellation_DeletesThePersistedFutureDeliveryAsync`; `REQ-VSB-SQLSERVER-SCHEDULE-CANCELLATION`→`Cancellation_PreservesEveryPublishedDeliveryWhenAnyDeliveryHasStartedAsync`. |
| `SqlServerSchedulingTests` | `0087`→`DelayedSend_PersistsFutureEnqueueTimeAndDeliversExactlyOnceAsync`; `0089`→`DelayedPublish_PersistsFutureEnqueueTimeAndDeliversExactlyOnceAsync`. |
| `SqlServerJobServiceTests` | `0071`→`CancelJob_CancelsTheRunningConsumerAndPublishesTheReasonAsync`; `0073`→`CancelJob_UpdatesStartedStatusToCanceledWithReasonAsync`; `0075`→`RetryJob_AfterCancellationUsesANewAttemptAndCompletesAsync`; `0077`→`CancelJob_WhileWaitingPublishesTheWaitAndCanceledTransitionsAsync`; `0079`→`SubmitJob_CompletesTheAcceptedLifecycleAsync`; `0081`→`PublishJob_GeneratesOneNonEmptyIdentityAcrossTheLifecycleAsync`; `0083`→`GetJobState_ForUnknownIdentityReturnsNotFoundAsync`. |
| `SqlServerConcurrencyTests` | `0097`→`PartitionedReceive_ThirtyMessagesPreserveOrderWithinBothKeysAtConcurrencyTenAsync`; `0099`→`ParallelPublish_OneThousandMessagesFromTenPublishersArriveExactlyOnceAsync`. |
| `SqlServerRedeliveryTests` | `0103`→`RedeliveryHeader_IsConsumedInternallyAndDoesNotLeakToPublishedMessageAsync`. |
| `SqlServerLockRenewalTests` | `0085`→`SlowConsumer_RenewsItsProviderLockThreeTimesAndCompletesExactlyOnceAsync`. |
| `SqlServerSerializationAndRequestTests` | `0091`→`JsonExtensionData_RoundTripsStringAndNumberForElementAndObjectDictionariesAsync`; `0101`→`RequestClient_FiveSequentialRequestsReturnTheirExactResponsesAsync`. |
| `SqlServerMessagePackRoundTripTests` | `REQ-VSB-SQLSERVER-MESSAGEPACK`→`SendAndPublish_RoundTripTypedMessagesThroughBinaryStorageAsync`. |

Source→Test: `HostSettings`/`TransportConnection`→Provisioning; `DatabaseMigrator`→Provisioning, DeliveryLimit, MaintenanceAndTopology, PublishAndPurge, ScheduleCancellation; `ClientContext`→DeliveryLimit, Scheduling, Redelivery, LockRenewal, Concurrency, SerializationAndRequest, MessagePack; `ConnectionContext`→ConfigurationAndRetry, MaintenanceAndTopology, LockRenewal; `SendFailureClassifier`/BusFactory extension→SendFailureClassifier; SQL transport composition→JobService. `Identifier`, `UriTypeHandler`, `ConnectionContextFactory` and beide Migration-Registrierungen besitzen im **zugehörigen** Projekt kein isolierendes Verhaltensorakel. Tests, die auf interne Typen zugreifen, tun dies über `InternalsVisibleTo`; sie sind kein Public-NuGet-Consumer-Compile-Beweis.

## Konkrete Korrektheitsrisiken und gezielte statische Gegenmutanten

Alle Urteile hier sind **unverifiziert (static reasoning)**, keine behaupteten Surviver, kein Mutationsscore. Bestehende End-to-End-Tests könnten einen Teil indirekt abdecken; ein gezieltes unterscheidendes Oracle ist in den gelesenen Dateien nicht erkennbar.

1. **Expiry/Retention – priorisiert.** `SqlServerDatabaseMigrator.cs:809,970` selektiert Fetches nach Queue, Fälligkeit und Delivery-Limit, nicht `ExpirationTime`; `:1162` schließt abgelaufene erschöpfte Zeilen vom Dead-Letter-Pass aus. Mutation/Probe: eine abgelaufene noch nicht erschöpfte Delivery direkt per beiden Fetch-Prozeduren anbieten und eine abgelaufene erschöpfte Zeile über Maintenance verfolgen; prüfen, dass kein Handler nach Ablauf erreicht wird und jede Zeile einen bounded terminalen Weg besitzt. Aus SQL allein ist nicht bewiesen, ob der höhere Receive-Pfad vor dem Handler verwirft.
2. **Auto-Delete/Orphans – priorisiert.** `SqlServerDatabaseMigrator.cs:291-299` definiert `MessageDelivery.QueueId` ohne FK auf Queue; `:1675-1678` löscht Auto-Delete-Queuezeilen, ohne sichtbare Delivery-Löschung. Ein gelöschter Queue-ID kann Delivery/Message unerreichbar zurücklassen, während `RemoveOrphanedMessages` nur Messages *ohne* Delivery entfernt. Probe: Auto-Delete bei pending, locked und delayed Delivery; danach Queue-Neudeklaration und DB-Zeilen/Erreichbarkeit/Retention prüfen. Der vorhandene Topology-Test löscht nur Topic, nicht Queue.
3. **Sekundenkonversion/Overflow – priorisiert.** `SqlServerClientContext.cs:187,204,357,381,453` castet `TimeSpan.TotalSeconds` ungeprüft nach `int` (auch Nullable) für Lock, Renewal und Delay. Ein Wert über `Int32.MaxValue` Sekunden kann wraparound/SQL-Fehler bzw. falsche Fälligkeit bewirken; Fractionalwerte werden abgeschnitten, außer der Subsekunden-Unlock-Pfad klemmt auf 1. Probe: exakt 1 s, knapp darunter, knapp über Integergrenze, fractional und negative Delay; vor DB-Nebenwirkung fail-closed oder explizite definierte Rundung beweisen. Bestehende Tests decken nur `LockDuration=999 ms` am Endpoint und kurze positive Delays.
4. **Connection-Open-Failure/Shutdown – mittel.** `SqlServerConnectionContext.cs:131-138` erzeugt Wrapper und öffnet, entsorgt ihn bei `OpenAsync`-Fehler aber nicht sichtbar. Maintenance benutzt nach Stop-Cancellation in `:220-225,239` `CancellationToken.None` für DB-Operationen; bei nicht erreichbarer DB ist ein endliches Stop-Budget nicht aus diesem Code ersichtlich. Fault-/Cancellation-/Ressourcenprobe benötigt.
5. **DataSource-Projektion – mittel.** `SqlServerHostSettings.cs:125-135` nimmt bei nicht-IP-Host mit `:` das Segment hinter dem ersten Doppelpunkt; bei SQL-Client-Präfix `tcp:host` wird `tcp:` bei `GetConnectionString()` (`:87-99`) nicht restauriert. Ein explizit gewähltes Netzwerkprotokoll könnte still geändert werden. Probe für `tcp:`, benannte Instanz, Port, IPv6 und Callback-Wechsel der `ConnectionString`-Property. Bisher geprüft sind nur Instanz-/Portbeispiele ohne Protokollpräfix.
6. **Public-API-/Startupmatrix – mittel.** Die zwei Migration-Registrierungs-Overloads haben keinen entsprechenden Testaufruf; die Uri- und Options-Host-Einstiege keinen separaten vollständigen Startbeweis. Konkrete Tests: `IServiceCollection`-Consumer-Compile ohne Provider-Namespaceimport, positive/negative `ValidateOnStart`-Matrix (Adresse, Port, Limit, Operationsflags), `create=false/delete=true` und expliziter Shutdown-Löschpfad mit run-scoped DB. `delete` nie an fremder/shared DB testen.
7. **Identifier und Fehlerpräzision – niedriger.** `SqlServerIdentifier` validiert Datenbank, Schema/Rolle und Principal unterschiedlich; es gibt keine direkten Grenztests für Länge 128/129, führende Ziffer, Unicode, Quotes und ungültige Principalzeichen. `SqlServerDatabaseMigrator.cs:515-518` meldet bei fehlendem *SourceTopic* den Text „Destination topic name was null or empty“; falsche Diagnose ist im Fehlerpfad zu prüfen. Für `UriTypeHandler` fehlen direkte Null-/Nonstring-/Malformed-Tests.

Positive statische Teststärken: run-scoped DB-Namen; reale SQL-Prozedurorakel für eindeutige Topic-/Queue-Identität, Cycle-Schutz, Purge-Isolation und Schedule-Cancel mit mehreren Deliveries; numerische statt Message-Text-Fehlerklassifikation; explizite Lock-ID-Negativprobe; binäre MessagePack- und JSON-Roundtrips; zwei getrennte Transport-Fetch-Timestamp-Prozeduren. Diese Beobachtung ist **kein grüner Lauf**.

## SHA-256-Lesebindung

Alle Pfade in den nächsten zwei Tabellen sind relativ zum jeweils genannten Projektverzeichnis; Hashes binden die gelesene Working Copy. `packages.lock.json` wurde nur gehasht, nicht inhaltlich geprüft, und gehört nicht zur folgenden Lesebindung.

| Produktdatei | SHA-256 |
|---|---|
| `ApiSurfaceGlobalUsings.cs` | `c657ca08588c18e09c330473f8e268bf614adf889962bfe698974f581e233c53` |
| `Configuration/SqlServerBusFactoryConfiguratorExtensions.cs` | `8d7cbc3cf58fe5aa5cb56691c923b3a59d4fd3d16acccd4d3e26902651f85270` |
| `Configuration/SqlServerHostConfigurationExtensions.cs` | `8f25de5c338d0f8dddc5a2081db4d109512738f93d6070d836f0204c942b15e9` |
| `Configuration/SqlServerHostConfigurator.cs` | `1eba4e42e7e59b48116e37db98abdc3210efc357c8101adf4ce999b582256688` |
| `Configuration/SqlServerHostSettings.cs` | `3646c1e252cadcecee1cbb285c68419d9dedd248a6c5dced7b316b6f975f72e3` |
| `Configuration/SqlServerTransportConfigurationExtensions.cs` | `4839c3f9dba4f0034633416074efa37f76bfafb1cdc007b9c3d97f5a9f89426b` |
| `Database/SqlServerDatabaseMigrator.cs` | `bac9c8780365043fa15ae262545c4c6f7879a26e3858be845b186e4fc36fd24a` |
| `Database/SqlServerIdentifier.cs` | `1617e94e3d7ae5a915653cfc80174870e7fb6d83b6f3ca252dce1e684f7e9e9c` |
| `Runtime/ISqlServerTransportConnection.cs` | `5519bd654a747c6934c85258a384cb9c74c94058aa5efdea9124e5a0ab32877e` |
| `Runtime/SqlServerClientContext.cs` | `72807cd60fe54d434736aa203033b97150260997cdd9d87c41e3a73c23b615a0` |
| `Runtime/SqlServerConnectionContext.cs` | `11b51fb785253f6a6d96768f2a27fc3b0a23018f2ef0e7e66287ef9751560a34` |
| `Runtime/SqlServerConnectionContextFactory.cs` | `5af64bb594653d7f9b065872f0563406f792e8eac06082ad8624d953ed69aefb` |
| `Runtime/SqlServerSendFailureClassifier.cs` | `87bdc6ed8a24fa5a42f13fbef72fe32807518bd716c2215d06da8d4db9789e7b` |
| `Runtime/SqlServerTransportConnection.cs` | `f6d2e0c83320d924692f0b49d102212690e7fa708fb3dc9003056e01e601ee7a` |
| `Runtime/UriTypeHandler.cs` | `1a08a084b9b012db500a307d54ee099ddeaf071c0430d5c71624e84a6f1308ae` |
| `ViciOne.ServiceBus.SqlTransport.SqlServer.csproj` | `217813c484e929936c92f5986eb7f523e13dd4ee0f3f1c9e7affded7cb145106` |

| Testdatei | SHA-256 |
|---|---|
| `ApiSurfaceGlobalUsings.cs` | `2a0f7a910dbb524ffedf10048d617bf0dbecdb848fb6ce47b65573e3b0550499` |
| `GlobalUsings.cs` | `62cc542c5b7347739f5a337f7c0cbe07b41b84ba3ed83ff55835f116d058a480` |
| `Infrastructure/SqlServerTestDatabase.cs` | `700c307af3d6cbb9c4e73ad42cc9905a8e9a7332ffc193a4a5e9d60ef858a2cd` |
| `Infrastructure/SqlServerTransportInspection.cs` | `a8ac076468aed1e017c46bf4bc21f310d7ce0f63e432ae750ddc37efd3533089` |
| `Requirements/RequirementCoverageProjectionTests.cs` | `ae2508f42bb01dc07996c7f9bb55f4e87ee8cff4cdffeb5a46d6c8f80d026376` |
| `Requirements/SqlServerTransportRequirements.json` | `634dd59aa8e178ce9718a29a2aef1ee4b746d90f597436887a5c638ab6876ec1` |
| `SqlServer/SqlServerConcurrencyTests.cs` | `d53285a59623e7bedfa48d05a189b5fb9c37d1137cf981e4e81d419e893512a3` |
| `SqlServer/SqlServerConfigurationAndRetryTests.cs` | `663235021c57a9cf3b696d5d00b418b888c34bd64f6b0fbe9dec1f9b4ec7e42c` |
| `SqlServer/SqlServerDeliveryLimitTests.cs` | `a82983e887f6e931cfd60d6e6ab6311191c9b142733756cd36c56dc4e0cb2c6c` |
| `SqlServer/SqlServerJobServiceTests.cs` | `1a51c4ae38bdcf5dbec0f2094b653fc6b0f5766241c41e7f0e79f6f50171c6bf` |
| `SqlServer/SqlServerLockRenewalTests.cs` | `23a99f50ec8b6d93fb049a7c5ac79497b28bcb2a140c98ab63811f7b961736eb` |
| `SqlServer/SqlServerMaintenanceAndTopologyTests.cs` | `435134f3c44bdf3fe2e9d797a23b8ab1a68d6c04cff2905cce5c51fbe96c85d2` |
| `SqlServer/SqlServerMessagePackRoundTripTests.cs` | `d12dfce9160ba47bc4eec0d5e1e59bdee5a6973a0448c286c105f9b3b6988587` |
| `SqlServer/SqlServerProvisioningCredentialTests.cs` | `964e4016125c3b03cfb419804d9bbbb2caf43fbb843383bf34d32ceb99ecbbc5` |
| `SqlServer/SqlServerProvisioningTests.cs` | `2d5dc00545597313987f7e0fc006da7574be2ca09d6970eafc0b7ac2d36f3ec9` |
| `SqlServer/SqlServerPublishAndPurgeTests.cs` | `be621685712a19aa33d3379a2199306604196e05961bc6528543d4b78e483d84` |
| `SqlServer/SqlServerRedeliveryTests.cs` | `72e176b456041203e1b01a4971999151623984b3ec69dc3e8904fc3ee91073dd` |
| `SqlServer/SqlServerScheduleCancellationTests.cs` | `f87f8c4c6f1cd42debc288ee7ed20397d655b29c65f7b27ffcee59c36ad9f896` |
| `SqlServer/SqlServerSchedulingTests.cs` | `1062d9eae2b3d1f3c038b04d3a6b1b29234fa0ea64534718f80bc3462c0ad549` |
| `SqlServer/SqlServerSendFailureClassifierTests.cs` | `1a5e0b1f212ca9e35679cae67fce130c0d29dd36c815ee35b2b50a723ba6d704` |
| `SqlServer/SqlServerSerializationAndRequestTests.cs` | `612ff509972a433e5525ca5743a9cbd69cb98f5b6effdf3aea00e865f167e35a` |
| `TestAssembly.cs` | `b35fd53f304dc6542d3eb92aa73767801ba385fa495f9d3de0cfd5dc26f2b4ee` |
| `ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.csproj` | `33ed9cdb338dc2447249d21abb3eb9ac373a06c335a7806fca3b0a4e89e50a1a` |

**Nicht geleistet:** Roslyn-/Public-API-Parser, Consumer-Compile, Testlauf, SQL-Server-/Azure-SQL-Realbeweis, Coverage/CRAP und empirische Mutationen. Darum weder `PASS` noch `Survived`/`Killed` behauptet. Der Lead muss Befunde an der jeweils vollständigen Produkt- und Callergrenze kausal disponieren und nur im autorisierten Slice umsetzen.
