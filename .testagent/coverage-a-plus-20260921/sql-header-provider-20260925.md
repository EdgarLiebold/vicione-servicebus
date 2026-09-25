# SQL-Header-Projektion: fokussierte Gegenproben vom 25.09.2026

## Geprüftes Produktverhalten

- `HeaderProvider_ProjectsDistinctNativeMetadataAndDerivedRedeliveryCount` prüft die getrennte Projektion von Transport-, Message-, Request-, Correlation-, Conversation- und Initiator-ID, Adressen, Typ, Content-Type, Routing-/Partition-Key sowie `DeliveryCount=3 → RedeliveryCount=2` mit unterscheidbaren Erwartungswerten.
- `HeaderProvider_ExplicitRedeliveryCountOverridesNativeDeliveryCountIncludingZero` beweist, dass ein ausdrücklich gespeicherter Redelivery-Wert `0` gegenüber `DeliveryCount=5` erhalten bleibt.
- `HeaderProvider_UnsetMetadataAndFirstDeliveryDoNotInventValues` prüft fehlende optionale Werte, fehlende freie Header und `DeliveryCount=1` ohne erfundenen Redelivery-Header.
- `HeaderProvider_PreservesApplicationOnlyHeadersBesideTransportHeaders` prüft Lookup und vollständige Enumeration zweier disjunkter Headerquellen mit konkreten Werten und gemischter Groß-/Kleinschreibung. Der bisherige Transport-Vorrang bei gleichem Namen bleibt durch `HeaderProvider_MergesDuplicateNamesWithTransportPrecedence` geprüft.

## Evidenz und Grenzen

- Microsoft Testing Platform und Microsoft CodeCoverage: `ViciOne.ServiceBus.SqlTransport.Tests` **217/217 bestanden**, 0 Fehler, 0 Skips. Bericht `artifacts/coverage-a-plus-20260925-sql-header-iteration/raw/sql-transport/coverage-evidence.cobertura.xml` SHA-256 `14ea62d4c3627c57fdf2e26b1dc1626d18984d06f4b9940cf541d13f39522e15`; Testlog `artifacts/coverage-a-plus-20260925-sql-header-iteration/sql-transport-coverage-evidence.log` SHA-256 `4bb7b631b8b86fbeaf04739a1dfbbfe3bae9d95930e952ec9133b020cb279884`.
- `SqlHeaderProvider.TryGetHeader`: 47/47 Zeilen, Komplexität 8, methodisches CRAP **8,00** in diesem Bericht. Der zuvor vorhandene Teilbericht mit 5/47 Zeilen diente nur zur Auswahl der Testlücke; er ist nach den inzwischen geänderten `src`-/`tests`-Trees keine gültige produktweite Vergleichsmessung.
- Ein adversariales Read-only-Red-Team fand den fehlenden Anwendungsheader-Fallback als überlebenden Mutanten. Der zusätzliche Test tötet ihn; das finale Review ist **PASS**. Die Test-Assertions wurden gegen die beiden Produktklassen und die unabhängigen Headerwerte geprüft.
- Die Microsoft-Skills `code-testing-agent`, `run-tests` und `coverage-analysis` wurden für fokussierte Testgestaltung, MTP-Ausführung und methodische Auswertung verwendet. Das produktweite A+-Gate einschließlich vier ausstehender Provider-Berichte und des vollständigen Unit-/Architecture-Gates bleibt offen.
