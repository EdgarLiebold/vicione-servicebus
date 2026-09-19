# Event Hubs Testing — statisches Public-API-Paket (Iteration 242)

## Aussagegrenze und Quellenstand

Für **nur** `ViciOne.ServiceBus.EventHubs.Testing` ist im aktuellen Source genau ein öffentlicher Typ mit einer selbst deklarierten öffentlichen Methode und drei Parametern vorhanden. Der vorhandene reflektierte API-Text bestätigt diese 1/1/3-Oberfläche, ist aber kein frisch aus diesem Source-Hash erzeugter Laufzeitbeleg. Dieses Paket ist eine manuelle statische Lesung und ein bidirektionaler Abgleich, **kein** Build, Testlauf, Coverage- oder Freigabebericht. Repository-`HEAD`: `1519a4772b66a43fae3e0056a004e71a1229fc1b`; die drei Assembly-Source-/Projektdateien und die gebundenen Test-/Requirements-Dateien waren beim Lesen im Arbeitsbaum unverändert. `review/**` und `TestResults/**` wurden nicht gelesen.

Normative Einordnung nach vollständiger Pflichtlektüre: `CURRENT_ORDER.yaml` führt `PO-2026-09-08-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01` als aktiven, hashgebundenen Lead-Slice. `PO-2026-09-08-01` Nr. 2, 5, 7 und 8 verlangt den konkreten API-/Owner-/Testspiegelabgleich, verhaltensunterscheidende Tests je öffentlicher API und neuem Parameter sowie die bidirektionale Async-Prüfung; `PO-2026-09-04-02` Nr. 5–6 verlangt XML-Dokumentation, Schichtung, `Async`-Suffix und letzten `CancellationToken`. Dieses Teilpaket trifft nur eine statische Aussage zur genannten Assembly und ersetzt weder die Lead-eigene Gesamtlesung noch dynamische A+-Abnahme.

| Aktuelle Lesemenge | SHA-256 der aktuellen Bytes | Befund |
| --- | --- | --- |
| `src/Transports/ViciOne.ServiceBus.EventHubs.Testing/EventHubTestHarnessExtensions.cs` | `d1831ff3a1667bf06e498dd16dac969f64d63f8ffee2984400abf60f82dd5c9c` | vollständig manuell gelesen; 22 Zeilen, einziger C#-Source der Assembly |
| `src/Transports/ViciOne.ServiceBus.EventHubs.Testing/ViciOne.ServiceBus.EventHubs.Testing.csproj` | `9c5890f9872e474d61e020ed0d4b55749fa46b4b78b08147b185f11475002c43` | vollständig manuell gelesen |
| `src/Transports/ViciOne.ServiceBus.EventHubs.Testing/packages.lock.json` | `4df4175cb611798e85a31cccd6e9bbd22726095ba8734676b6e6836c933f45f9` | vollständig manuell gelesen; Abhängigkeits-/Versionsbeleg, kein API-Beleg |
| `tests/Transports/ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests/EventHubIntegration/EventHubProducerResolutionTests.cs` | `e185391a3fe4be3e78f8e17f61239d88e7a9629512acdd253db6d9b085466eaf` | vollständig manuell gelesen; 123 Zeilen |
| `tests/Transports/ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests/Requirements/EventHubLocalIntegrationRequirements.json` | `89c6c2c22235aa75a0f16268d574bae8e5885c092d8274b66afdabcae8a2143b` | vollständig manuell gelesen; 48 Einträge, davon drei zur Producer-Resolution |

Zusätzlich wurden das zugehörige Testprojekt und dessen Requirement-Projektionsprüfung gelesen: Die Test-Assembly referenziert dieses Produktprojekt direkt und bettet das Requirements-JSON als Ressource ein; die Projektionsprüfung vergleicht Metadaten mit der Ressource. Die gesamte übrige Event-Hubs-Produkt- und Testfläche wird hier nicht als manuell gelesen oder disponiert behauptet. Scanner-/`rg`-Treffer und der frühere Iteration-148-Bericht dienen nur als Wegweiser beziehungsweise Historie, niemals als Ersatz dieser Lesung.

## Öffentliche Schicht und exakte Deklarationen

Physische Schicht: separates `net10.0`-Paket `ViciOne.ServiceBus.EventHubs.Testing` unter `src/Transports/`; es referenziert die allgemeinen Harness-APIs aus `ViciOne.ServiceBus.Testing`, die Event-Hubs-Provider-APIs aus `ViciOne.ServiceBus.EventHubs` und `Microsoft.Extensions.DependencyInjection.Abstractions`. Es ist eine optionale **Testintegrations-/Convenience-Schicht**, nicht Transportkern, Provider-SPI oder Runtime-Consumer-API. Projektbeschreibung: „producer and consumer test harness APIs“; die aktuelle öffentliche Oberfläche enthält allerdings nur Producer-Auflösung. Das ist eine Beschreibungs-/Angebotsdiskrepanz, keine nachgewiesene fehlende Consumer-Anforderung.

| Ebene | Deklaration / Semantik | Nachweis |
| --- | --- | --- |
| Namespace | `ViciOne.ServiceBus.EventHubs.Testing` | Source Zeile 5 |
| Typ | `public static class EventHubTestHarnessExtensions`; kein selbst deklarierter öffentlicher Konstruktor, Feld, Property, Event, geschachtelter Typ oder weiterer Member | Source Zeilen 7–22; reflektierter Text Zeilen 7876–7878 |
| Methode | `public static Task<IEventHubProducer> GetProducerAsync(this ITestHarness harness, string eventHubName, CancellationToken cancellationToken = default)`; Extension auf `ITestHarness`, kein Overload; Rückgabe ist ein `Task<IEventHubProducer>` | Source Zeile 15; reflektierter Text Zeile 7878 |
| `harness` | erster, erforderlicher, nicht-nullbarer Extension-Receiver; `null` ergibt vor Scopezugriff `ArgumentNullException` mit `ParamName=harness` | Source Zeilen 15, 17; Test Zeilen 20–26 |
| `eventHubName` | zweiter, erforderlicher, nicht-nullbarer `string`; `null`, leer und nur Whitespace werden vor Scopezugriff durch `ArgumentException.ThrowIfNullOrWhiteSpace` abgewiesen, `ParamName=eventHubName` | Source Zeilen 15, 18; Test Zeilen 28–37 |
| `cancellationToken` | dritter, optionaler Werttyp-Parameter mit `default(CancellationToken)`; der empfangene Wert wird namentlich an `IEventHubProducerProvider.GetProducerAsync` weitergegeben; keine eigene Abbruchprüfung oder Lifetime-Garantie | Source Zeilen 15, 20; Test Zeilen 65–72 |
| Delegation | `harness.Scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>()`, danach Producer-Auflösung mit Entityname und Token; gibt den Provider-Task unmittelbar zurück, ohne eigene `async`-Hülle | Source Zeile 20; Test Zeilen 59–73 |

XML-Dokumentation ist am Typ, an der Methode, allen drei Parametern und der Rückgabe vorhanden (Source Zeilen 7, 10–14). Ob eine fehlende DI-Registrierung, ein ungültiger Scope oder ein Providerfehler ausgelöst wird, ergibt sich aus den aufgerufenen fremden Grenzen; diese Assembly definiert dafür keinen eigenen Fehlervertrag. Insbesondere beweist der vorhandene Test nur die **Weitergabe** eines bereits abgebrochenen Tokens, nicht erfolgreiche Cancellation des Providers.

## Reflexions- und Hashabgleich

`.testagent/iteration242/current-api-reflected.txt`, SHA-256 `2536d15de991345234fdf3f9618bdfa27f6b4a34c38f8c80d391237ffbe8a0a2`, weist in Zeilen 7876–7878 für die Assembly-Version `1.0.0.0` genau den obigen Typ und `GetProducerAsync(ITestHarness, String, CancellationToken = default(CancellationToken))` aus. `current-api-denominator.tsv` Zeile 1400 meldet passend ein öffentliches Klassenobjekt, eine Methode und drei Parameterslots. Dieser frühere Reflexionstext enthält keine Bindung auf den hier genannten **aktuellen Source-SHA**; die Übereinstimmung ist ein statischer Kreuzcheck, keine heute frisch reflektierte Assembly.

`docs/api/packed-public-api.txt`, SHA-256 `59ea05a49d8d99e64715ac60b79bc68f9b657948f0742fd9c3d3e972babd054b`, Zeilen 7553–7555 nennt zwar denselben Typ und die drei Parametertypen, gibt aber den optionalen `CancellationToken` als `= null` wieder. Das widerspricht der aktuellen Source-Deklaration und dem detaillierteren reflektierten Text; diese gepackte Darstellung ist für den Optionalwert **stale/ungenau** und wird nicht als aktuelle Signaturwahrheit verwendet. Frühere Iteration-148-Manifeste oder ihre historischen Prüfergebnisse sind ebenfalls keine neue Hash-/Laufzeitbestätigung dieses Pakets.

## Bidirektionale Requirement-/Test-/API-Zuordnung

Die einzig zugeordnete Requirement-ID ist `REQ-VSB-EVENTHUB-PRODUCER-RESOLUTION` in der lokalen Event-Hubs-Requirement-Projektion. Die drei Varianten zeigen auf genau drei Methoden in `EventHubProducerResolutionTests`; umgekehrt hat jede dieser Methoden genau die genannte `RequirementCoverage`-Annotation. Für **diese** Assembly berühren zwei Varianten unmittelbar die Harness-Extension; die dritte gehört zur Provider-Extension der anderen Assembly und darf nicht als eigenständiger Test dieser API verbucht werden.

| Requirement-Variante → Testmethode | Bezug zur hier inventarisierten API | Statisches Orakel |
| --- | --- | --- |
| `public-boundaries-reject-missing-required-input` → `ProducerResolution_RejectsMissingRequiredInputAsync` | direkt für `harness` und `eventHubName`; derselbe Test prüft daneben den Provider-Eingang einer anderen Assembly | Null-Receiver und drei ungültige Namen werfen mit exaktem Parameternamen; Proxy verweigert unerwarteten Zugriff |
| `harness-scope-resolves-provider-and-forwards-input` → `HarnessExtension_ResolvesProviderFromScopeAndForwardsInputAsync` | direkt; deckt Auflösung aus `harness.Scope`, Entityname, Token und Rückgabeidentität | registrierter Recording-Provider liefert dasselbe Producer-Objekt; Adresse `topic:orders` und Tokenidentität werden geprüft; die Adressprojektion erfolgt nach Delegation außerhalb dieser Assembly |
| `entity-name-and-cancellation-forwarded-to-provider` → `ProviderExtension_ForwardsEntityAddressAndCancellationAsync` | **nicht direkt**; prüft `EventHubProducerExtensions` in `ViciOne.ServiceBus.EventHubs` | Provider-Extension erhält Name/Token und erzeugt `topic:orders`; keine Harness-Scope-Ausführung |

Rückrichtung vom einzigen Public Member: `EventHubTestHarnessExtensions.GetProducerAsync` → `REQ-VSB-EVENTHUB-PRODUCER-RESOLUTION` → zwei unmittelbar relevante Testmethoden oben. Rückrichtung von den drei Requirement-Varianten: zwei auf die Harness-Extension, eine ausschließlich auf die benachbarte Provider-Extension. Es gibt keine ungebundene **deklarierte** öffentliche Methode dieser Assembly und keine vierte Producer-Resolution-Variante in der gelesenen JSON-Projektion. Die Projektionsprüfung (`RequirementCoverageProjectionTests.EventHubLocalRequirements_MatchCompiledRequirementMetadata`) kontrolliert die Zuordnung als Metadaten, nicht das Produktverhalten; sie wurde hier nicht ausgeführt.

Parameterbezogene A+-Grenze: Der direkte Negativtest unterscheidet einen fehlenden `harness` und drei ungültige `eventHubName`-Werte; der direkte Delegationstest bindet die Provider-Auflösung an den Harness-Scope und unterscheidet `eventHubName` und die Identität eines explizit übergebenen `cancellationToken`. Damit ist konkretes Verhalten aller drei Parameter statisch testgebunden, nicht aber die Optionalform des Tokens oder eine tatsächliche Provider-Cancellation. `GetProducerAsync` erfüllt die Namensregel bei awaitbarer `Task`-Rückgabe auch ohne eigenes `async`/`await`; XML-Parameternamen und letzter Tokenparameter stimmen mit der Deklaration überein.

## Offene Grenzen, keine stillen PASS-Behauptungen

1. Kein aktueller Build, keine frische Reflection, kein Testlauf und keine Mutations-/Coverage-Messung durch diesen Agenten (`.NET`-Ausführung lag ausschließlich beim Lead). Die frühere Iteration-148-Aussage „3/3 ausführbare Zeilen“ wird nicht als neuer Messwert fortgeschrieben.
2. Der optionale Aufruf ohne `cancellationToken` wird durch die zwei direkten Tests nicht explizit kompiliert/ausgeführt; die Defaults sind statisch und im detaillierten Reflexionstext sichtbar. Bei einer API-Vollständigkeitsabnahme wäre ein direkter Package-Consumer-Compilefall sinnvoll.
3. Fehlende `IEventHubProducerProvider`-Registrierung, fehlerhafte/disponierte Harness-Scope-Grenze und Provider-Fault/Cancellation besitzen keinen eigenständigen direkten Negativtest dieser Extension. Daraus folgt **kein** nachgewiesener Produktfehler oder zusätzlicher versprochener Fehlervertrag; für ein vollständiges Nutzer-API-Orakel sind diese Grenzen offen zu entscheiden.
4. Die Projektbeschreibung nennt Consumer-Test-Harness-APIs, während das Paket aktuell nur Producer-Auflösung exportiert. Beschreibung an tatsächliches Angebot binden oder eine separat begründete Consumer-Anforderung nachweisen; aus dem Text allein wird keine neue API erfunden.
