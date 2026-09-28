# T58 — Registrierung, Scope und Fehlerpfad als ein Paket

## Research

Basis ist der veröffentlichte T57-Stand `25ef0c773` mit 13.250 grünen Tests,
91,6249 % Zeilen, 84,2665 % konservativen Branches und CRAP>30 = 0.
Die T57-Cobertura-Berichte werden für die Auswahl wiederverwendet; es gibt
keine neue Vollmessung während der Recherche. Acht zusammenhängende Core-Dateien
in Consumer-Registrierung, Bus-Konfiguration, DI-Filtern, Scope-Observer und
Retry/Rescue enthalten zusammen ungefähr 220 verschiedene ungedeckte physische
Zeilen. Methodenbasierte Zeilenlisten können dieselbe physische Zeile mehrfach
aufführen und sind deshalb keine zusätzliche Coverage-Zahl.

Ein Microsoft-Roslyn-Pairing über den aktuellen Repositorybaum wurde einmal
ausgeführt (`/private/tmp/vsb-t58-pairing.json`): 4.289 Source-Dateien,
1.553 Testdateien, 2.549 statisch gepaart und 1.740 ungepaart.
Die erste Kandidatenliste enthielt `AssemblyScanner` und die allgemeine
`RegistrationBusFactory`. Das Red-Team-Auswahlreview hat beide gestrichen:
Der Scanner wird von der Produktregistrierung nicht aufgerufen, und die
allgemeine Fabrik ist kein regulärer Transportpfad. Der tatsächliche
Consumer-Discovery-Pfad liegt in `RegistrationExtensions`, der Standard-
Receive-Rescue-Pfad in `ReceivePipeConfiguration`. Für diese beiden Ersatzdateien
liegt noch keine gesonderte statische Paarung vor. In der Erstliste waren
RetryConfigurationExtensions, TransportRegistrationBusFactory und
MessageScopeConfigurationObserver gepaart; BusFactoryConfigurator,
DependencyInjectionFilterExtensions und RescueConfigurationExtensions statisch
ungepaart. Das Pairing erkennt
Extension-Aufrufe und DI-/Reflection-Pfade oft nicht; es beweist keine
fehlende Laufzeitabdeckung. Testorte folgen deshalb den vorhandenen Core-
Integrationstests, nicht den unpassenden Architecture-Vorschlägen des Pairings.

Vorhandene starke Kontrollen verhindern neue Überladungs- und Nullparameter-
Matrizen: `TenantScopeIntegrationTests` prüft sieben echte Tenant-Scope-Wege;
`DependencyInjectionConfigurationContractTests` prüft ungültige Filtertypen,
Bus-Spezifikationen und Observer-Fehler; `RescueFilterTests` prüft exakte
Ausnahmeidentität und Rescue-Projektion; `RetryBusObserverTests` prüft den
Lebenszyklus des Stopp-Tokens. `ContainerNamespaceDiscoveryTests` belegt die
Namespace-Discovery bis zu Consumer, Saga und Activity. T48 belegt normalen
Zweibusbetrieb und Stop/Weiterlieferung; T57 belegt den Consumer-Retry/Circuit-
Breaker-Pfad. Ein T58-Test muss einen zusätzlichen zusammenhängenden Vertrag
belegen.

## Acceptance map

| Produktvertrag | Geplante konkrete Evidenz |
| --- | --- |
| Consumer-Discovery aus expliziten Assemblies oder Typen respektiert den Auswahlfilter und verbindet die passende Definition nur mit ihrem Consumer | Ein ergänzender Registrierungs- und Zustellfall für mehrere Consumer-Typen und Definitionen, exakte registrierte Typen, ausgewählten Endpunkt und Nichtzustellung an einen ausgeschlossenen Nachbarn. Der bestehende Namespace-End-to-End-Fall bleibt Kontrolle. Ein bloßer Scanner-Test ist kein Beleg für Registrierung. |
| Ein offener/geschlossener DI-Filter wirkt nur für den gewählten Vertrag und Bus; jedes betroffene Message-/Activity-Leben erhält einen eigenen Scope, auch bei Fehlern | Reale InMemory-Bus- und Courier-Journeys mit Ziel- und Nachbarkontrakt, zwei Nachrichten/Bus-Identitäten, exakten Filter-/Consumer-/Compensation-Spuren und beobachteter Scope-Freigabe. Vorhandene Tenant-Erfolgspfade bleiben Kontrolle. |
| Retry und Rescue besitzen unterschiedliche Fehlergrenzen | Registrierter Consumer mit kontrolliertem Fehler; explizit geprüfte Retry-/Rescue-Reihenfolge, Rescue nach erschöpftem Budget und ausgeschlossene Exception. Exakte Versuchs-, Rescue-, Fault- und Nachbarzustände sowie ursprüngliche Exception-Identität. Vorhandene Bus-Stopp-/Retry- und Rescue-Folgefehler-Proben bleiben Kontrolle und werden nicht verdoppelt. |
| Journal-Observer halten die Eigentümergrenze zweier aktiver Busse | Jeweils einer von zwei unabhängigen InMemory-Bussen besitzt das Journal. Beide stellen Nachrichten zu; nur der Besitzer produziert Send- und Consume-Einträge. Der bisherige Einbus-Test kann diese Grenze nicht belegen. T48-Zweibuskontrollen und bestehende Bus-Spezifikations-/Limits-Tests nicht wiederholen. |
| Qualitätsgates für ein größeres Paket | Bestehende Testharnesses und Requirement-Bindungen nutzen; enge Build-/Testzyklen je Familie, einmal adversariales Read-only-Auswahlreview und Schlussreview, isolierte kausale Gegenproben, dann genau eine vollständige 33-Profil-Messung des eingefrorenen Pakets samt unabhängigem Audit, Changelog und Push. |

Die Reihenfolge ist Consumer-Registrierung → Scope → Fehlergrenzen → Bus-Isolation. Nach
jeder Familie werden nur betroffene Projekte gebaut und gezielt ausgeführt;
der produktweite Prüfaufwand fällt erst nach dem gesamten Paket an. Die
Akzeptanz hängt an beobachteten Produktwirkungen, nicht an einer vorgegebenen
Anzahl neuer Tests oder zusätzlicher Coverage-Zeilen.

## Status

Recherche, bestehende Testkontrollen und read-only Red-Team-Auswahlreview sind
abgeschlossen. Dessen Grenzen zu Scanner-Verkabelung, gemeinsamem Host-Start
und Retry-/Rescue-Auslösung sind oben eingearbeitet. Vier integrierte
Testfamilien sind implementiert. Registrierungs-Discovery (1/1), Scope bei
Kompensation (2/2), Retry/Rescue (2/2 nach Entfernung einer doppelt geprüften
Folgefehler-Route) und Zweibus-Journal (2/2) sind einzeln grün gewesen; die
letzte Retry-Änderung ist gezielt mit 2/2 geprüft. Keine Produktquelle wurde
geändert. Ein erneuter gemeinsamer Core-Lauf einschließlich
Requirement-Projektion bestand 6.894/6.894. Qualitäts-Schlussreview und die einzige
produktweite Vollmessung dieses Pakets sind abgeschlossen. Die eingefrorene
[T58-Vollmessung](product-wide-profile-5e9509367.md) umfasst alle 33 Profile,
13.257 grüne Tests, 91,68462 % Zeilen, 84,29095 % konservative Branches und
null Methoden mit CRAP > 30. Ein unabhängiger XML-/Hash-/Fixture-Audit stimmt
vollständig überein; Line/Branch A+ bleibt offen.

Die Retry/Rescue-Implementierung legte eine wichtige Pipeline-Grenze offen:
`bus.UseMessageRetry(...)` registriert außen und lässt einen äußeren Rescue
jeden einzelnen Versuch sehen. Die explizite Consume-Pipe-Überladung nach
`UseRescue` legt Retry innen, sodass erst drei Versuche und dann genau ein
Rescue auftreten. Der Consumer-Fault wird bereits vor dem Rescue veröffentlicht;
der Test behauptet daher keine Fault-Unterdrückung. Das Schlussreview deckte
eine unbewiesene Rescue-Folgefehler-Behauptung auf: der Bus-Receive-Observer
beobachtet den betroffenen Consumer-Endpunkt nicht. Die schon vorhandenen
`RescueFilterTests` prüfen die genaue propagierte Folgeausnahme. Die doppelte
integrierte Route wurde entfernt. Das Journal-Orakel wurde auf exakte
Besitzer-Korrelations-ID in beiden Einträgen verstärkt, der Scope-Test auf
tatsächliches Dispose aller vier Scopes. Eine frühere Probe mit
gemeinsamen Activity-Argumenttypen konfigurierte denselben Courier-Endpunkt
zweimal; mit eigenem Fehler-Argumenttyp läuft die Scope-Journey. Diese
Autorenkorrekturen sind keine Produktfehler. Die Journal-Eigentümerprobe wurde
mit invertiertem Produktfilter kausal geprüft: beide Varianten scheiterten an
falschen Korrelations-IDs. Das Original ist mit SHA
`a8f404199c098ad977add5c81823f2d23d6c6e51` bytegleich wiederhergestellt.

## Qualitätsprüfung vor dem Freeze

Die vier neuen Testmethoden ergeben sieben parametrische Fälle. Jede prüft
konkrete Produktwirkung über Registrierung, reale InMemory-Zustellung,
Routing-Slip-Kompensation oder Journal-Observer. Discovery prüft den
ausgeschlossenen Nachbarn und die Consumer-Definition; die Scope-Fälle prüfen
Identität, Besitzer und genau einmalige Freigabe aller vier Scopes; die
Retry-Fälle prüfen drei Versuche vor einem Rescue, originale Fault-Ursache,
Recovery-Nachricht und Zustellung des gesunden Nachbarn; die Journal-Fälle
prüfen beide Besitzer mit exakter Korrelations-ID auf Send und Consume.
Kein Fall besteht nur aus einem Null- oder Erfolgscheck. Die vorhandenen
`RescueFilterTests` belegen den Folgefehler des Rescue-Filters direkt.

Pseudo-Mutationen gegen ignorierten Discovery-Filter, vertauschten
Compensation-Scope, vorgezogenen Rescue und fehlende Fault-/Recovery-Wirkung
werden durch konkrete Assertions erfasst; hierfür wird kein ausgeführter
Mutationsscore behauptet. Die Besitzerfilter-Inversion in
`TransportRegistrationBusFactory` wurde wirklich ausgeführt: 2/2 erwartete
Journal-Testfehler, beide mit falscher Korrelations-ID, nach bytegleicher
Wiederherstellung 6.894/6.894 Core-Tests grün. `dotnet format` im Verify-Modus
für die vier Testdateien und `git diff --check` bestehen. Die Microsoft-Skills
`code-testing-agent`, `find-untested-sources`, `coverage-analysis`, `run-tests`,
`assertion-quality` und `test-gap-analysis` wurden für Recherche,
Implementierung und Review genutzt; das statische Pairing und die manuelle
Pseudo-Mutationsanalyse sind keine zusätzliche Coverage-Messung.
