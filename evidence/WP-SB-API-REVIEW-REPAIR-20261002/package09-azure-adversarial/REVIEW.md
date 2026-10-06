Package09: unabhängige interne Reviewempfehlung für F036/F037

Ergebnis: **F036/F037 im eingefrorenen Paket akzeptieren; keine neuen Blocker gefunden.** Das ist eine eng begrenzte interne Reviewempfehlung. Ich bin nicht Autor dieses Produktpatches, beanspruche keine externe Produktrolle und erteile keine Gesamtproduktfreigabe. Freeze-Bindung: `PACKAGE09_AZURE_FREEZE.json`, SHA256 `0f52f76e6882ca9fff256b4aeca5f965727aea68402c30a90e3e292526955025`.

F038 bleibt unverändert rot. Der tatsächliche vollständige Azure-Ownerlauf zeigt **430 gesamt, 429 erfolgreich, 1 fehlgeschlagen, 0 übersprungen, Exit 2**. Ein Whole-PASS, eine Naming-/Migrationentscheidung, frische NuGet-Verifikation, integrierte Profile und die abschließende vollständige API-Closure werden hier ausdrücklich nicht bestätigt.

Alle sechs Freeze-Deltas wurden geprüft; Snapshot, aktuelle Datei und gegebenenfalls Vergleichsbaseline passen zu ihren Hashes. Fixture und 15 unmittelbar relevante Sourceabhängigkeiten wurden vollständig gelesen. Für die vollständige Azure-Testowner-/Shared-Input-Lektüre wird ausschließlich mein eigener, erneut hashgeprüfter `R/repair-research/azure-lease-regression/READ_CLOSURE.json` verwendet: 38 Originaldateien, 35 SharedInputs, drei neue Fixtures. Keine fremde FULL-Lektüre ersetzt die eigene. Die Requirement-Datei war dort bereits vollständig mit exakt dem Freeze-Hash gelesen; der vollständige semantische Delta hält alle 188 bisherigen Zeilen unverändert in derselben Reihenfolge und hängt genau vier Zeilen an. Die historische Changelog-Datei wurde nicht als vollständiger neuer Read ausgegeben: ihr unveränderter Bytepräfix und die vollständigen sechs angehängten Zeilen wurden geprüft. Details und 19 vollständig gelesene tatsächliche Logdateien stehen in `READ_MANIFEST.json`.

| Änderung | Unabhängig geprüfter Sourceeffekt und Kontrolle |
| --- | --- |
| `SessionIdSendTopologyConvention.Factory(this)` | Die Factory liest `owner.DefaultFormatter` beim erstmaligen Erzeugen der Typkonvention; das bisherige konstante `null` entfällt. `TopologyConventionCache` behält je Typ die einmal erstellte Instanz. Der vorhandene Adapter führt den Default über den realen `SetSessionIdFilter` aus; `SessionId` hält im realen SendContext den `PartitionKey` konsistent. |
| Expliziter Formatter / leeres Resultat | `SessionIdMessageSendTopologyConvention.SetFormatter` ersetzt weiterhin den geerbten Default. Die Probe prüft `message-session` für Session und Partition, null Defaultcalls und tatsächlich denselben SendContext am Terminal. Ein Nullresultat erhält `already-explicit`; beim Defaultresultat werden genau ein Aufruf und dieselbe Nachrichteninstanz geprüft. Kein API-/Signaturwechsel und keine Credentialsänderung. |
| `SubscriptionConsumeTopologySpecification.Validate` | Exakt das bestehende Rule/Filter-XOR-Vertragsproblem wird korrigiert: ein Failure nur bei zwei nicht-null Argumenten. Rule-only und Filter-only bleiben erlaubt. Named und typed Subscribe speichern dieselbe Spezifikation; ihre Validateketten erreichen die neue Failure vor dem normalen Busbau. Apply und SDK-Optionsprojektion sind unverändert. |
| PublicConsumer-Routen | Konsumierende Topologie wird über named Subscribe und `GetMessageTopology<T>().Subscribe` aufgebaut. Jeweils Rule-only und Filter-only werden positiv geprüft; der Konflikt produziert exakt den Failure-Key und über normales öffentliches `Bus.Factory.CreateUsingAzureServiceBus` einen spezifischen ConfigurationException. Keine Produkt-Testseam und kein privater Reflection-Oracle. |
| Fixture / Requirement / Docs | Vier attribuierte Methoden, sechs Fälle insgesamt: 1 F036, 2 Topologie-F037, 2 Busbau-F037 und 1 weiterhin roter F038-Fall. Genau vier passende Fünffeld-Projektionszeilen mit existenten IDs. Die zwei Changelog-Bullets behaupten nur die tatsächlich korrigierten Verträge. |
| Mutationsrunner | Der ganze eingefrorene Runner und sein `run_operation.py`-Receiptpfad wurden gelesen. Jeder Mutant ersetzt genau eine eindeutig gefundene Stelle; Testbytes bleiben unverändert, Aufrufe ohne `--no-build` kompilieren die geänderte Source. Die Assertionlogs, Quellenhash-Rekonstruktion, tatsächlichen Exits und eindeutigen Ledger-Receipts wurden geprüft. |

Die Testprüfung umfasst alle vier Methoden und deren Helfer/Fields, nicht nur die beiden Produktdateien. Ergebnis der gepinnten Microsoft-Test-Antimuster-Prüfung: **0 Critical, 0 High, 0 Medium, 0 Low**. Die Assertions zeigen reale Pipeline- oder Konfigurationsfolgen, werden korrekt abgewartet, enthalten spezifische Exception-/Wertkontrollen und verwenden lokal getrennte Objekte. Es gibt keine Sleep-/Delay-Absenz als PASS, kein Assertion-Swallowing und keine Skip-Entwertung der roten F038-Assertion. Die kombinierte Defaultprobe enthält bewusst ihren Vorrang-Gegenfall, so dass der folgende Default-Assert nicht einen kaputten Vorrang verdecken kann.

Tatsächliche, bereits vom ausführenden Owner erzeugte Evidenz; vom Reviewer gelesen und hashgeprüft, **nicht selbst ausgeführt**:

| Native Phase / Mutant | Tatsächlicher Befund |
| --- | --- |
| Original-Family-6 | 6/6 FAIL, Exit 2; F036 Zeile 55 erwartetes `default-session`, actual null; F037 Zeile 99 erwarteter Rule/Filter-Key, actual leer; öffentlicher Busbau Zeile 115 erwarteter ConfigurationException, keiner geworfen; F038 Zeile 131 2 statt 1. |
| Korrigierte Family-6 | 5 PASS / 1 F038 FAIL / 0 SKIP / Exit 2. Beide Original-/Fix-Buildlogs: 0 Warnungen, 0 Fehler, Exit 0. |
| Isolierte Originalkontrollen | 1 + 2 + 2 relevante Fälle jeweils vollständig rot durch die genannten Vertragsassertions; F038 wurde nicht mitselektiert. |
| Isolierte korrigierte und anschließende Rollbackkontrollen | dieselben 1 + 2 + 2 Fälle jeweils grün und ohne Skip; jeweils Exit 0. |
| `default-formatter-ignored` | Zeile 55 `Assert.Equal`: expected `default-session`, actual null; 1/1 FAIL, Exit 2. |
| `typed-override-ignored` | **Gegenmutant**: Zeile 40 `Assert.Equal`: expected `message-session`, actual `default-session`; 1/1 FAIL, Exit 2. |
| `conflict-validation-removed` | Zeile 99 `Assert.Equal`: expected `[Rule/Filter]`, actual `[]`; beide typed/named Fälle FAIL, Exit 2. |
| `single-rule-overrejected` | **Gegenmutant** `&& → ||`: Zeile 85 `Assert.Empty` scheitert bereits an der erlaubten Einzelform; beide typed/named Fälle FAIL, Exit 2. |
| Ursprünglicher isolierter Sourcebaum | 6.209 originale tracked Datei-SHA256 selbst erneut gegen `R/FILE_COVERAGE.csv` geprüft: 0 Abweichungen; neue Fixture wieder entfernt. Dies beweist tracked Source-Rollback, keinen vollständigen Scratch-/Artifact-Cleanup. |

Damit sind **vier echte Mutanten** durch passende Vertragsassertions erfasst, einschließlich zwei Gegenmutanten gegen gefährliche Überkorrekturen. Die zehn isolierten Kontrollreceipts enthalten einmal Restore sowie drei Original-, drei Korrektur- und drei Rollback-Testaufträge. Zusammen mit fünf Produktreceipts und vier Mutantreceipts sind alle 19 Receiptlogs unverändert und jeweils genau einmal im tatsächlichen `VERIFICATION_LOG.jsonl` enthalten. Das ist kein allgemeiner Mutationsscore für die Codefamilie.

Angrenzende Kontrollen für ein später erweitertes API-Review, keine neuen Patchblocker und keine empirisch behaupteten Survivor:

| Statische Zusatzfrage | Konkrete sinnvolle Kontrolle |
| --- | --- |
| Defaultwechsel nach dem ersten gecachten Typ | Typ A mit Default A materialisieren, Default B setzen, neuen Typ B materialisieren: A behält seine Typkonvention, B erhält B; ein explizit gesetzter Typformatter bleibt vorrangig. Die Source spricht klar von neu konfigurierten Konventionen; die aktuelle Fixture prüft diesen mehrtypigen Zeitablauf nicht direkt. |
| Beide Subscriptionoptionen nicht gesetzt | Named und typed Subscribe ohne Callback sollen validieren und öffentlich bauen. Ein hypothetischer zusätzlicher Both-null-Reject müsste anhand des vollständigen bestehenden Owners isoliert verifiziert werden, bevor daraus eine echte Testlücke abgeleitet wird. |

Die neue Validierung deckt F037s bestätigten Rule/Filter-Konflikt. Sie ist keine Behauptung, dass sämtliche anderen Subscription-Konfigurationsverträge nun vollständig über die Spezifikation laufen. Die Änderung soll nicht beiläufig die spätere AutoDelete-/Forwardingprojektion umbauen.

F038 darf bei Folgeschritten nicht durch Filter, Skip, geänderte Expectation oder Reviewstatus aus dem Gesamtbefund entfernt werden. Seine Assertion bleibt in Fixture und Requirement-Projektion erhalten; die Benennung und Migration bestehender Broker-Subscriptions benötigen weiterhin die getrennte PO-Entscheidung. Die hier akzeptierten F036/F037-Änderungen ändern diese Namen nicht.

Revieweraktionen: ausschließlich Source-/Evidence-Reads, Hash-/JSON-/Textvergleich und die drei Dateien dieses Reviewverzeichnisses. Keine Produktcode-/Testcode-Edits, keine Builds, keine Tests, keine SDK-/Containerprozesse und keine eigenen Mutationen.
