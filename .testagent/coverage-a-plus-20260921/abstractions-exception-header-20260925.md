# Abstractions: Ausnahmefilter und Transport-Header, 25.09.2026

## Verhalten und Gegenproben

- `ExceptionSpecification.Match<T>` prüft nach einem negativen Predicate-Ergebnis auf der äußeren Exception weiterhin innere Aggregate-Exceptions. Für jedes Inner prüft es zuerst das direkte Objekt und danach eine davon verschiedene Basis-Exception. Include- und Exclude-Regeln verwenden denselben Pfad. Das vorherige Verhalten übersprang innere Matches, wenn die äußere Exception bereits `T` war, und übersprang direkte innere Matches mit eigener Ursache.
- `BroadPredicate_ExaminesAggregateInnersWhenTheAggregateDoesNotPass`, `IgnoredAggregateInner_VetoesAnOtherwiseHandledException` und `TypedPredicate_ExaminesDirectAggregateInnerBeforeItsBaseException` schlugen vor den jeweiligen Produktkorrekturen fehl. Diese roten Vor-Fix-Ergebnisse wurden während der Entwicklung in der Konsole beobachtet; ihre Logs wurden nicht archiviert. `Predicate_ExaminesDistinctRootCauseWhenDirectAggregateInnerDoesNotPass` schützt den erhaltenen Root-Cause-Pfad.
- Die sieben `DictionaryTransportSetHeaderAdapterTests`-Fälle prüfen gespeicherte skalare Werte, Textkürzung, selektives Entfernen leerer Werte, Ablehnung nicht unterstützter Werte, Host-/Fault-Optionen und den nicht generischen Eingang. Die Assertions vergleichen konkrete Dictionary-Werte und -Anzahlen.
- Das adversariale Read-only-Red-Team fand den zweiten Fehler im direkten Aggregate-Inner. Nach Test und Korrektur erteilte es ein PASS für den aktuellen Produkt-Diff und die Adapter-Tests.

## Geprüfte Messung

- `ViciOne.ServiceBus.Abstractions.Tests`: **821/821 bestanden**, 0 Fehler, 0 Skips, mit Microsoft Testing Platform und Microsoft CodeCoverage. Bericht: `artifacts/coverage-a-plus-20260925-header-tests/raw/abstractions/coverage-evidence.cobertura.xml`, SHA-256 `f6068db1d7d81081aa708ffec823ecdb742857f6f2639bf6bf3e195246e6e9eb`. Der archivierte Testlog `artifacts/coverage-a-plus-20260925-header-tests/abstractions-coverage-evidence.log` hat SHA-256 `0b85168a1f2e826b1fd03aceed7a9f11d7491bbf03e7c35ae3acc7db50d01c7c` und enthält die Testzahl, null Fehler/Skips und den genauen Berichtspfad.
- In diesem Bericht: `DictionaryTransportSetHeaderAdapter.Set<T>` 8/8 Zeilen, CRAP **12,00** (vorher 0/8, 156,00); nicht generisches `Set` 4/7, CRAP **13,04** (vorher 0/7, 72,00); `ExceptionSpecification.Match<T>` 13/13, CRAP **22,00** (vorher 2/10, 85,73). Die Werte gelten für diesen einzelnen Abstractions-Bericht, nicht als produktweites Profil.
- Verwendet: Microsoft-Skills `code-testing-agent` für fokussierte Teständerungen, `run-tests` für MTP-Aufrufe und `coverage-analysis` für die methodische Auswertung. Assertions wurden gegen die Produktpfade und durch ein unabhängiges Red-Team geprüft.

## Offene Gates

Ein frischer vollständiger Unit-/Architecture-Aufbau wurde begonnen, hing aber beim externen `Grpc.Tools`-Schritt `ProtoCompile`. Der anschließende Lauf mit `--no-build` startete die bereits vorhandenen Testmodule, konnte jedoch wegen des noch nicht erzeugten Core-Testmoduls nicht vollständig grün werden. Die Logs und Buildartefakte liegen unter `artifacts/coverage-a-plus-20260925-abstractions-iteration/`; dieser Versuch zählt ausdrücklich **nicht** als bestandenes Gesamt-Gate. Der angeforderte Colima-Neustart für die vier fehlenden Provider-Coverage-Berichte ist weiterhin offen. Nach Wiederherstellung der Umgebung sind vollständiger Unit-Gate-Lauf und frische produktweite Line-, Branch- und CRAP-Messung erneut erforderlich.
