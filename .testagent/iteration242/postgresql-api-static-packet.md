# PostgreSQL-SQL-Transport — vorläufiges statisches A+-Paket

Status: **provisorische Lead-Zuarbeit, kein Test-/Build-/Freigabenachweis**. Stand: 19.09.2026, Repository-HEAD `b0899dafcb7006aa8c267a35aba7042e9fa86e6a`. A-0071: Kein `dotnet`-, MSBuild- oder Testprozess durch diesen Agenten; keine Produkt- oder Teständerung. Dieses Paket betrachtet ausschließlich `src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql/**` und die direkt korrespondierenden `tests/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests/**`. Weder `review/**` noch `TestResults/**` wurden enumeriert oder gelesen.

## Geltungsbasis und Methode

Workspace-`AGENTS.md` und die dort verlangten sieben Dokumente (`CLAUDE.md`, `AI_WORKING_AGREEMENT.md`, `GLOSSARY.md`, `DECISIONS.md`, `current/README.md`, `CURRENT_ORDER.yaml`, `FINDINGS.md`) sowie Repository-`AGENTS.md` wurden vollständig gelesen. Auftragsbezug: `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12`, A-0071, hashgebundener Slice Revision 0002, SHA-256 `4d3e3ebe55a2a6cfa07e647754723ecfc3436db2f91baf3b3a6d059328ea7d75`. Die spätere PO-Entscheidung vom 08.09.2026 verlangt hier die manuelle Source-/API-/Parameterprüfung. Die im Order dokumentierten 71/71- und 375/375-Stände sind historische Claims, kein hier neu erzeugter Testnachweis.

Alle **19 C#-Quelldateien** dieses Produktprojekts wurden manuell komplett gelesen; die beiden Projektdateien und die PostgreSQL-Requirement-Projektion wurden ebenfalls betrachtet. Die 32 C#-Testdateien wurden innerhalb des engen Testprojekts inventarisiert und gehasht; Requirement-Metadaten, API-Aufrufe und ausgewählte Testkörper wurden gelesen. Eine vollständige manuelle Aussage- oder Mutationsprüfung jedes Testkörpers ist **nicht** geleistet. Die beiden aktivierten statischen Testanalyse-Skills (`find-untested-sources`, `test-gap-analysis`) wurden deshalb als manuelle Heuristik angewandt: der bevorzugte Roslyn-Analyzer würde einen verbotenen .NET-Prozess erfordern, und ein root-weiter heuristischer Scanner wäre mit der Scope-Schranke unvereinbar. Alle folgenden Lücken sind statische Kandidaten, keine gemessenen Coverage-Werte.

## Schicht, Contract und Public API

Das Projekt ist die PostgreSQL-Provider-Schicht des generischen SQL-Transports: Bus-/Host-Registrierung, `Npgsql`-Verbindungsaufbau, SQL-Schema und Migration, Nachrichten-/Queue-/Topic-Routinen, Benachrichtigung, Dapper-Parameter und typisierte Sendefehler-Klassifizierung. Öffentliche API-Typen sind nur die drei Extension-Klassen unten. Die übrigen 16 Source-Dateien deklarieren interne Implementierungstypen; deren `public` Member oder `protected` Overrides sind keine von außen zugängliche Provider-API. Kein öffentlicher Provider-Member trägt ein `Async`-Suffix; die asynchronen Operationen liegen hinter internen Typen bzw. dem generischen SQL-Contract. Cancellation-Token-Parameter der internen Async-Methoden folgen grundsätzlich dem üblichen letzten Platz; die auffälligen Punkte sind unten benannt.

| Öffentlicher Typ / Namespace | API und zu prüfende Parametersemantik | Statischer Beleg |
|---|---|---|
| `PostgreSqlBusFactoryConfiguratorExtensions`, `ViciOne.ServiceBus.Configuration` | Vier `UsingPostgreSql`-Overloads auf `IBusRegistrationConfigurator`: DI-Optionen; `string connectionString`; `NpgsqlDataSource`; `Func<IBusRegistrationContext,NpgsqlDataSource>`; jeweils optionaler `configure`-Callback. Null-/Whitespace-Guard, Callback-Ausführung, Factory-Bindung und Singleton-Klassifizierer sind unabhängige Verhaltensachsen. | `Configuration/PostgreSqlBusFactoryConfiguratorExtensions.cs:17-94` |
| `PostgreSqlHostConfigurationExtensions`, `ViciOne.ServiceBus.SqlTransport.PostgreSql` | Vier `UsePostgreSql`-Overloads auf `ISqlBusFactoryConfigurator`: `Uri hostAddress`, `string connectionString`, `NpgsqlDataSource`, `IBusRegistrationContext`, jeweils `Action<ISqlHostConfigurator>? configure`. Zu prüfen: Hostableitung, Optionenauflösung, Callback/Schema, Null-/Whitespace-/ungültige Adresse, Ownership der gelieferten DataSource. | `Configuration/PostgreSqlHostConfigurationExtensions.cs:17-80` |
| `PostgreSqlTransportConfigurationExtensions`, `ViciOne.ServiceBus.SqlTransport.PostgreSql` | Zwei `AddPostgreSqlMigrationHostedService`-Overloads auf `IServiceCollection`: `bool create=true, bool delete=false` sowie `Action<SqlTransportMigrationOptions>? configure`. Zu prüfen: Schalter-Mapping, Null-Receiver, ausgewählte Stage, Options-Startup-Validierung, Mehrfachregistrierung. | `Configuration/PostgreSqlTransportConfigurationExtensions.cs:17-71` |

Die `UsePostgreSql`- und Migrations-DI-Extensions liegen nicht im üblichen `Microsoft.Extensions.DependencyInjection`-Namespace. Das ist ein API-Discoverability-/PO-2026-09-04-02-Abgleichskandidat, **kein** bewiesener Funktionsfehler; die explizite Namespace-Entscheidung ist vom Lead gegen den freigegebenen Contract zu prüfen. Die `UsingPostgreSql`-Extensions sind bewusst im Bus-Konfigurationsnamespace.

## Source ↔ Test/Requirement: statische Zuordnung

Die folgende Tabelle ist eine **bidirektionale Arbeitszuordnung**: links die gelesenen Source-Bereiche und relevanten Tests, rechts die Gegenrichtung der in der eingebetteten Projektion deklarierten Requirement-IDs. `OBL-R0-SQL-` ist bei vierstelligen Nummern weggelassen. Die Projektion hat 62 Einträge, ist aber kein Ausführungs- oder Qualitätsnachweis.

| Source-/Feature-Bereich → direkte Testgruppen | Requirement-Projektion → Testgruppe |
|---|---|
| Konfiguration (`PostgreSqlBusFactoryConfiguratorExtensions`, `PostgreSqlHostConfigurationExtensions`, `PostgreSqlHostConfigurator`, `PostgreSqlHostSettings`, `PostgreSqlTransportConfigurationExtensions`) → Basic, Isolation, BusOutbox, JobService, SendFailureClassifier, Provisioning; der Migration-Extension-Aufruf fehlt im gesichteten Testprojekt. | Basic: `0025,0033-0036`; Isolation `0108`; BusOutbox `0060`; JobService `0070,0072,0074,0076,0078,0080,0082`; SendFailureClassifier: `REQ-VSB-POSTGRES-SEND-FAILURE` (zwei Varianten); Provisioning `0037,0038,0130,0131` plus Routine-/Trigger-REQs. |
| Schema/Routinen (`PostgreSqlDatabaseMigrator`, `PostgreSqlIdentifier`, `PostgreSqlNotificationChannel`, `PostgreSqlStatements`) → Provisioning, ProvisioningCredential, Notification, TopologyCycle, Maintenance, ReceiveConfiguration, DeliveryState, Routing. | Provisioning `0037,0038,0130,0131`, `REQ-VSB-POSTGRES-ROUTINE-NAMES`, `REQ-VSB-POSTGRES-NOTIFICATION-TRIGGER`; ProvisioningCredential `0129`; Notification `0104`; TopologyCycle `REQ-VSB-POSTGRES-TOPIC-CYCLE`; Maintenance `0124,0125`; ReceiveConfiguration `0106,0107`; DeliveryState `0110,0114,0133,0135`; RoutingAndFailure `0055-0058`. |
| Runtime-Verbindungen (`IPostgreSqlTransportConnection`, `PostgreSqlTransportConnection`, `PostgreSqlConnectionContextFactory`, `PostgreSqlDbConnectionContext`) → Basic, Isolation, Notification, LockRenewal, Redelivery, DeliveryLimit, Unlock, Scheduling; kein gezielter `OpenAsync`-Fehler-/Cancellation-Disposal-Test erkennbar. | Basic `0025,0033-0036`; Isolation `0108`; Notification `0104`; LockRenewal `0084,0112`; BuiltInRedelivery `0059`; Redelivery `0102`; DeliveryLimit `0092,0094,0116`; Unlock `0111,0120`; Scheduling `0086,0088`. |
| Client/Parameter (`PostgreSqlClientContext`, `EnumParameter`, `JsonParameter`, `UriTypeHandler`) → Basic, PublishAndPurge, SerializationAndRequest, MessagePackRoundTrip, ScheduleCancellation, RoutingSlip. | PublishAndPurge `0066,0068`; SerializationAndRequest `0090,0100`; MessagePackRoundTrip `REQ-VSB-POSTGRESQL-MESSAGEPACK`; ScheduleCancellation `0062,0064`; RoutingSlip `0061`. |
| Sendefehler (`PostgreSqlSendFailureClassifier`) → SendFailureClassifier; Queue-/Delivery-Routinen zusätzlich → AutoDelete, ConcurrencyAndPriority. | SendFailureClassifier: zwei `REQ-VSB-POSTGRES-SEND-FAILURE`-Varianten; AutoDelete `0115`; ConcurrencyAndPriority `0096,0098,0132,0134`. RequirementCoverageProjectionTests: `REQ-VSB-SQL-POSTGRES-REQUIREMENT-PROJECTION` prüft Metadaten, nicht Transportverhalten. |

Konkreter Gegenrichtungsabgleich: `PostgreSqlSendFailureClassifierTests.cs:29-67` ruft alle vier `UsingPostgreSql`-Overloads auf, prüft dabei jedoch nur **einen** Singleton-Descriptor (`ITransportSendFailureClassifier`), nicht die vier resultierenden Host-/Factory-Pfade, Provider-Delegate-Null-Rückgabe oder `configure`-Callback-Wirkung. `PostgreSqlTestDatabase.cs:104-105` benutzt nur den `UsePostgreSql(string, configure)`-Hostpfad; `PostgreSqlBasicTransportTests.cs:24` den `NpgsqlDataSource`-Pfad. Der `Uri`- und `IBusRegistrationContext`-Hostpfad ist durch die gesichteten direkten Aufrufstellen nicht abgedeckt. `PostgreSqlProvisioningTests.cs:152-180` prüft die Stagereihenfolge am generischen `SqlTransportMigrationHostedService` mit `RecordingMigrator`, nicht die PostgreSQL-DI-Extension oder reale Startup-Validation.

## Konkrete Risiken und prüfbare Lücken

1. **Hohe statische Sicherheit: Connection wird bei fehlgeschlagenem Open nicht disposed.** `Runtime/PostgreSqlDbConnectionContext.cs:154-160` erzeugt ein `PostgreSqlTransportConnection` um `_dataSource.CreateConnection()`, ruft `await connection.OpenAsync(cancellationToken)` auf und gibt es erst danach zurück. Wirft oder storniert `OpenAsync`, ist kein `finally`/`catch` zur Disposal vorhanden. Die `await using`-Caller `:101` und `:246` übernehmen das Objekt erst nach erfolgreichem Return. `Runtime/PostgreSqlTransportConnection.cs:21-23` besitzt die notwendige `DisposeAsync`-Implementierung. Damit kann ein fehlgeschlagener Connect Ressourcen länger als beabsichtigt halten. Ein gezielter Test mit kontrolliertem Open-Fehler/Cancellation ist im korrespondierenden Testprojekt nicht erkennbar; dortige `OpenAsync`-Aufrufe (`Infrastructure/PostgreSqlTransportInspection.cs:125`, `PostgreSqlProvisioningTests.cs:58`) betreffen direkte Test-/Fixture-Verbindungen, nicht diesen Fehlerpfad. Disposition: Lead sollte Ownership in `CreateConnectionAsync` absichern und einen deterministischen Negativtest auf Disposal ergänzen; weder Fix noch Test sind hier ausgeführt.
2. **Hohe statische Plausibilität: Startup-Validation akzeptiert unvollständige Migrationsoptionen.** `Configuration/PostgreSqlTransportConfigurationExtensions.cs:43-54` akzeptiert eine nichtleere `ConnectionString` als hinreichende Adressdeklaration und validiert Port/ConnectionLimit beim Start. Bei reinem ConnectionString und leeren `Database`/`Schema`/`Role` ruft der Migrator jedoch zunächst `PostgreSqlIdentifier.Validate(options.Database/Schema/Role)` auf, je nach Stage in `Database/PostgreSqlDatabaseMigrator.cs:1399,1422-1423,1471,1496,1511-1513`. Erst `Runtime/PostgreSqlTransportConnection.cs:104-140` schreibt Datenbank und Defaults aus dem Builder zurück, und dieser Builder folgt in diesen Pfaden teils **nach** der Identifier-Validierung. Ein `CreateInfrastructure`-only- oder Default-Migrationsstart kann daher trotz `ValidateOnStart()` später scheitern. Die Fixture belegt das nicht, da `Infrastructure/PostgreSqlTestDatabase.cs:56-67` Database/Schema/Role explizit füllt. Disposition: Lead soll die stage-abhängige Options-Normalisierung/Validation prüfen und dafür negative sowie positive Startup-Fälle setzen; ohne Lauf keine Aussage zu allen möglichen Options-Defaults.
3. **API-/Parameter-Testlücken, keine neuen Fehlerbehauptungen:** Null-Receiver/Null-Argumente und ungültige `Uri` für die zehn Extension-Overloads, `configure`-Callback-Weitergabe/Reihenfolge, DataSource-Provider gibt `null` zurück, gelieferte DataSource wird nicht vom Transport disposed, Migration-`create`/`delete`-Mapping und Options-Validierung über die **öffentliche** Registration. Der Classifier-Registrationstest und die funktionalen Bus-Tests belegen Teilpfade, nicht diese vollständige Matrix. Priorisierung: zuerst (1) und (2), dann behavior-distinguishing API-Parameterpaare statt nur positiver Happy Paths.

Keine dynamische Ausführung, kein neuer Coverage-/CRAP-/Mutation-Score, kein A+-Urteil für das gesamte ServiceBus-Repository. Auch die Testqualität außerhalb der genannten Testkörper ist in diesem Paket offen.

## Hashbindung der Scope-Dateien

SHA-256, Basis für die Source-Liste: `src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql/`.

```text
a513c7460d2b33ce8ce795e6b5d573d8424b024efa4aeb3aa58336dad642e4a1  ApiSurfaceGlobalUsings.cs
4b9ad3164d620c85684b5323297f49249e1f6b72cc48c20debb43aaee92cc2cc  Configuration/PostgreSqlBusFactoryConfiguratorExtensions.cs
d3405ffd28493453bbc2866df420ff22b96c727a1f5f49717cf37b76cdc3276b  Configuration/PostgreSqlHostConfigurationExtensions.cs
9ab791f178d5847a7183a1906950c1995391fd96f2de2bb107b495d44ef9cf88  Configuration/PostgreSqlHostConfigurator.cs
e47847fb957033c4d57d930581c78c0057926a79defb3bd6aee0001bf7625fa7  Configuration/PostgreSqlHostSettings.cs
13117fa7ad64665bba03e1cf17af73e3c76010a7e01547363038a50ba732f2aa  Configuration/PostgreSqlTransportConfigurationExtensions.cs
3e8fd068740b40205eb58ac6d04105bb89d048d578b2889d6c0a8c9c778cd7aa  Database/Parameters/EnumParameter.cs
cfea57ddcc300e21f17dcaa4fa6871a5991a509790af4971555e150b9c43c7cc  Database/Parameters/JsonParameter.cs
2e55ee4d7c9812ed6fc202b637b35ccc734cd665f8b577ade114234af3a36449  Database/Parameters/UriTypeHandler.cs
048bc2bd1090bec0670bfa639f599254f6cb085882ab68814ea7e40744de3d8f  Database/PostgreSqlDatabaseMigrator.cs
1f6a413dff413d0e4003f59848888abd2d2f9eee31a9fe1f7a90d4856a076361  Database/PostgreSqlIdentifier.cs
d68329541e07b700695cc9808980c0c042e4bc423861e05bde31bdba1c1e0c72  Database/PostgreSqlNotificationChannel.cs
aaa6d6f1a2435aff89cee5b51f1c33c2ebda925a284190496154369fca7c742a  Database/PostgreSqlStatements.cs
3e67247f4b8ec1be6a99617fe44b17bc260fd33b373991919f7715c03525c7f6  Runtime/IPostgreSqlTransportConnection.cs
9ac40ea8bcd65f1a95457761c10770377d4608d4a0f7c97c3252148aa0d238d4  Runtime/PostgreSqlClientContext.cs
9608814fb91b81f133e2c9db67bd09c8ec9fe6b623170f4683a42fac32662a23  Runtime/PostgreSqlConnectionContextFactory.cs
a3e5451869b32a7592c74e5e4e45cafacf4be7a9c8f7d205d136d98c89d960ae  Runtime/PostgreSqlDbConnectionContext.cs
72dc21ebf47e4206d2134f812801d35b1f95a6d51701b26c5937d930750832c8  Runtime/PostgreSqlSendFailureClassifier.cs
5eff9410ed4993b44364f8439b41b3628ba42a3782a0f02391367ecb45c3a392  Runtime/PostgreSqlTransportConnection.cs
9328f23151e379be746bd9a1b00c48fa6afd59b0a4c9f297797da67688774163  ViciOne.ServiceBus.SqlTransport.PostgreSql.csproj
```

SHA-256, Basis für die Test-Liste: `tests/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests/`. Hash vorhanden bedeutet **nicht**, dass jeder Testkörper vollständig geprüft wurde.

```text
7dd3214b08a38b51051d454f9fcf68f7de6d58f0dd0806c52493a22e17d39b4a  ApiSurfaceGlobalUsings.cs
62cc542c5b7347739f5a337f7c0cbe07b41b84ba3ed83ff55835f116d058a480  GlobalUsings.cs
cb2a3855c32c741756f2b609abf3c25e8849b636135a8639b8d344cdf1d4fd40  Infrastructure/PostgreSqlTestDatabase.cs
c558a0dad6e5f25ef0cc235d3c1bb6a7169c0e8ae0a4b03192ab37f5b573f776  Infrastructure/PostgreSqlTransportInspection.cs
a85ac46749bf008082e2202689cdfe83169b9f6756c0a3425ce8d43d4b97ca38  PostgreSql/PostgreSqlAutoDeleteTests.cs
1e5ae06bc7c11613c16a85b87d142cd39dc039802eb2e393d0b5eca3b85408a2  PostgreSql/PostgreSqlBasicTransportTests.cs
430d1a451340cd016c6e2bf1c282fe24578074341d3618e9bc6e8a28cf767a4c  PostgreSql/PostgreSqlBuiltInRedeliveryTests.cs
bb2fc2379d69b9a9118a82dc60bb9e6f8ad2ea376c630db93101e443c0cf5b2f  PostgreSql/PostgreSqlBusOutboxTests.cs
4612af95e82176ad9c47e646902a219840dd06b15ecad642955fdc93dff68517  PostgreSql/PostgreSqlConcurrencyAndPriorityTests.cs
6f3132cbe223d72fbd4af7d55bfb09723c606eee7e05cc491d01cbfb4c99d6cd  PostgreSql/PostgreSqlDeliveryLimitTests.cs
6dffee8444cdab7330d27acce8ccc2f412c7b3a9dc92061ca5ead8dfdf004a72  PostgreSql/PostgreSqlDeliveryStateTests.cs
982470294139a3ee56bb8c5bde0301ccfb63356473ecf87a4671519fd78d7621  PostgreSql/PostgreSqlIsolationTests.cs
999368833796c51505513a1d7c7effb1853ff237092c6e8f11adfae302e7b5c2  PostgreSql/PostgreSqlJobServiceTests.cs
b5b252205ee53c477d2d43bc5020484733611b5ae23fb50df5665b8b37d11752  PostgreSql/PostgreSqlLockRenewalTests.cs
94b19457721defd3dec73bbec47c06da6e92b5520a6b71320168faff55055434  PostgreSql/PostgreSqlMaintenanceTests.cs
964a207e4c2c1e9e4a7932f88ffd53c9434297230fa5b9f1c372c4c2b2d94e7a  PostgreSql/PostgreSqlMessagePackRoundTripTests.cs
2ca5342159f9005f42f9282532ebd3a1243e3c815efdba0ea2b4d8054e70baae  PostgreSql/PostgreSqlNotificationTests.cs
7af3d8729329ab8ffc1a55c4bd0b8c94bf3d220048721ead5da289cad79aae58  PostgreSql/PostgreSqlProvisioningCredentialTests.cs
408d3c535ac6e7e87ea71a4298846e0b9265a49daf432cc3de34e1d495e7a0ef  PostgreSql/PostgreSqlProvisioningTests.cs
ed1d2c3c63eef2a141cc2680d5bc46b813d7d4b6dfc1bd82ad710a8308dabd61  PostgreSql/PostgreSqlPublishAndPurgeTests.cs
18d935417fe1b01c3672ed43c53cd5c6c18d610e4051b6596dfe57bce6624f35  PostgreSql/PostgreSqlReceiveConfigurationTests.cs
209d0f1944ae6eb05193d20f203ed49513e839ce2b8b2590476506834b3bf0bf  PostgreSql/PostgreSqlRedeliveryTests.cs
0be73ac11e1fee16732b84b17a1ce84272bd065479f8a6d8ab8535a2516e6d6d  PostgreSql/PostgreSqlRoutingAndFailureTests.cs
e51a4d41f9da1e8eba84d97daee9b4eef4ee7590f06329748f3c89e8cfaf7ef9  PostgreSql/PostgreSqlRoutingSlipTests.cs
53466d1abe898872d237b59bd1f63854670261586180fc3e9ec707a9b5254a0d  PostgreSql/PostgreSqlScheduleCancellationTests.cs
1e7173696d0e589edbf3220646dfef076810ff3d913e730d80406b178b8f14f8  PostgreSql/PostgreSqlSchedulingTests.cs
ce8512d378300a76998f82684317091e4c77edb7fdf4e63a07e0f7b8e2770a71  PostgreSql/PostgreSqlSendFailureClassifierTests.cs
a8b738263176c1037f9e4389f36788d94da9c20333fa3401b3c8bdfb63e09208  PostgreSql/PostgreSqlSerializationAndRequestTests.cs
5e29eac9844e7b0f9d368b1f6fd67e5626b9a7e38a34809faaa74c2396aeaab5  PostgreSql/PostgreSqlTopologyCycleTests.cs
cfb6ea87b195a898a4302c704bd1fa6e9bb2736d2fae887c5e68034465b9c95d  PostgreSql/PostgreSqlUnlockTests.cs
922d5f652ec5a1c4d60c7fbd8ea3499c7cacb421bd1218907747d4778b57b428  Requirements/RequirementCoverageProjectionTests.cs
59179e1fa8ccc0c22dbec95b8778ff39f5085a89e78c1fc903cec839ff696daf  TestAssembly.cs
6df8f3a83409df9fdbda50bf3fa91cc6401dc6d7154cece2e2639f9e49984179  Requirements/PostgreSqlTransportRequirements.json
a5a88b932eb41ada1fcc450cacf8287d4ee417a1df49f69e053c5cf31838ec43  ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.csproj
```
