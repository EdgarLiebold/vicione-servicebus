# Azure Table: statisches manuelles A+-API-Prüfpaket

## Ergebnis und Bindung

Nur Assembly `ViciOne.ServiceBus.Azure.Table`, `src/Persistence/ViciOne.ServiceBus.Azure.Table/**`; keine DynamoDB-, AmazonS3- oder Initializers-API. Die 39 getrackten eigenen Source-/Projekt-/Lockdateien wurden dateibezogen gelesen und unter dem sortierten SHA-256-Zeilenstrom `0d0bf0b0b75c5faf3bd38b03f47a830b60ad597fcfbe1b59c37cb9bfdb418907` gebunden; Einzelhashes stehen im [Read-Manifest](azure-table-api-read-manifest.tsv). Dieses ist ein **Working-Tree-Scope-Hash**, kein Commit-Freeze.

Die vorhandene Paketreflexion wurde nur als zu validierender Snapshot verwendet. Gegen die aktuellen Deklarationen ergeben sich lokal genau **11 extern sichtbare Typen, 29 deklarierte Member und 55 explizite Parameterslots**; jeder Member und jeder Slot ist im [Member-Mapping](azure-table-api-member-mapping.tsv) einzeln benannt. Die 65 Einträge der beiden Azure-Table-Requirement-Projektionen sind in der [Gegenrichtung](azure-table-api-test-reverse-mapping.tsv) erfasst. Der globale `src/tests`-Hash des ursprünglichen Reflektionslaufs ist zwischenzeitlich invalidiert; diese lokale statische Prüfung behauptet keinen neu gepackten API-Freeze, Consumer-Compile oder Gesamtquotienten.

Normbezug: `PO-2026-09-08-01` Nr. 2, 7–9 (jede Datei/API/Parameter und Testbesitz), `PO-2026-09-04-01` Nr. 6–9 (Start-, Schicht-, Consumer- und Providerbeweise), `PO-2026-09-04-02` Nr. 1, 5–6 (Fähigkeitserhalt und API-Schichten), `PO-2026-08-25-02` (optionales, bounded MessageJournal). Der aktuelle ServiceBus-Auftrag `A-0071` betrifft primär Testrekonstruktion; dieses Paket ist nur eine disjunkte API-Zwischenprüfung, keine freigegebene neue Produkt- oder Teständerung.

## Konkrete statische Befunde

1. **Öffentliche Komposition exponiert einen Advanced-Typ:** Beide `UseAzureTableMessageJournal`-Overloads sind im Provider-Namespace eine Application-Einstiegsfläche, liefern aber `ViciOne.ServiceBus.Advanced.ConnectHandle` zurück (M03/M04). Das ist eine nachweisbare Schichtkreuzung der Signatur. Ob hier eine andere öffentliche Handle-Abstraktion verlangt wird, ist eine Architektur-/Consumer-Disposition, kein aus diesem Paket ableitbarer Produktfehler; Funktionalität und Disconnect-Semantik dürfen nicht verloren gehen.
2. **Keine statisch belegte API-Regression im lokalen Scope:** Projekt-, Assembly-, Rootnamespace- und PackageId-Identität stimmen überein; alle 11 reflektierten Typen und 29 Member sind im gelesenen Source vorhanden. Es gibt hier keinen belegten `Obsolete`-Alias oder sichtbaren `Async`-Namenswiderspruch. Der positive Saga-, Job-, Future-, Courier- und Journal-Pfad hat starke lokale Tests, aber diese Aussage ersetzt keinen Lauf.
3. **Providergrenze ist bewusst enger als ein Cloudbeweis:** `TODO.md` Abschnitt „Complete Azure Table persistence validation against the real cloud service“ verlangt weiterhin echte Azure-Table-, Cosmos-Table-, Entra-ID- und Service-Limit-Akzeptanz. Die vorhandenen LocalIntegration-Tests nutzen Azurite und sind dafür kein Ersatz. Keine Cloud-Ausführung wurde hier behauptet.

## Unbestätigte Testlücken (statische Pseudo-Mutation; keine Mutation injiziert)

| Priorität | Member | Hypothetische Einzeländerung und Testoracle |
|---|---|---|
| hoch | M21, `UseAzureTableForRegisteredSagas` | `SetSagaRepositoryProvider(...)` weglassen. Der sichtbare Azure-Table-Test prüft hier nur Nullargumente; ein positiver dynamisch entdeckter Saga-Consumer samt Auflösung und Persistenz wäre das differenzierende Oracle. |
| hoch | M18, `Create<TSaga>(factory, keyFormatter)` | Den übergebenen `keyFormatter` durch den Standardformatter ersetzen. Der vorhandene Test prüft Nullguards und Repositoryobjekte, aber keinen erfolgreichen Custom-Key-Roundtrip. Ein Clienttest mit `FixedRowSagaKeyFormatter` und exakter Table-Key-Prüfung würde ihn töten. |
| mittel | M01/M02, Job-Service-`UseAzureTable` | Im Zweiparameter-Overload einen der drei Typnamen vertauschen oder im Fünfparameter-Overload zwei Formatter vertauschen. Die Unit-Test-Orakel prüfen nicht die drei resultierenden Persistenzschlüssel; der LocalIntegration-Jobpfad nutzt den anderen DI-Registrierungseinstieg. Eine drei-Saga-Key-Matrix für beide Overloads fehlt statisch. |
| mittel | M06, Journal-`UseAzureTable(IMessageJournalConfigurator, TableServiceClient, tableName, storeOptions)` | Einen anderen gültigen `tableName` an `GetTableClient` geben. Negativ-Inputtests bleiben grün; ein positiver Store-Identitäts-/Persistenztest über genau diesen Overload wäre nötig. Der Bus-Overload M04 wird bereits positiv getestet, ist aber ein anderer öffentlicher Einstieg. |
| mittel | M08, `AppendAsync` | Bei bestehender Partition mit 99 Journal-Einträgen die Überfüllungsablehnung entfernen oder nach initialem 404/konkurrierendem 409 die Lease nicht erneut lesen. Die aktuellen Tests prüfen Kapazitätsabsenkung, 8 parallele Appends und atomare Action-Reihenfolge, jedoch nicht deterministisch diese beiden Fehlerpfade. Je ein präparierter Table-Client/Realprovider-Fall würde die Grenze unterscheiden. |
| niedrig | M15, dreiparametriger Concurrency-Konstruktor | Identitätswerte vertauschen. Dessen Guards sind geprüft; die positiven `SagaType`-/`CorrelationId`-Assertions verwenden den vierparametrigen Konstruktor M16. Ein positives Assertion-Triple reicht. |

Das sind **keine bestätigten Survivor und kein Mutationsscore**: Der Auftrag verbietet Produkt-/Testedits und breite Testläufe. Der `test-gap-analysis`-Skill fordert für einen echten Survivor eine isolierte Mutation mit schmalem Testlauf; daher bleiben diese Kandidaten ausdrücklich unbestätigt. Auch das Vorhandensein eines Requirement-Projektionseintrags belegt nur die Metadatenzuordnung, nicht die dynamische Ausführung.

## Gegenrichtung und Beweisgrenzen

Das Memberledger verweist auf konkrete Testmethoden. Das Reverse-Ledger führt jede Requirement-Projektion zurück auf Member-IDs; Future-, Courier-, Job- und Teile der Saga-Tests werden als **indirekte** API-Wirkung gekennzeichnet, da sie Providerverhalten über die Registrierungs-/Repositorygrenze prüfen. Die zwei Requirement-Metadaten-Selbsttests tragen bewusst keine Produkt-Member-ID. Triviale Konstanten/Getter sind als solche markiert, nicht als pseudo-mutationspflichtige Logik gezählt.

Keine Produkt- oder Testdatei geändert, keine Tests ausgeführt, keine Cloud-/Azurite-/Consumer-/Coverage-/CRAP-/Mutation-/Remote-/unabhängigen Red-Team-Gates als bestanden dargestellt. Ein Gesamt-A+-Urteil bleibt offen.
