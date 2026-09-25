# Frische ServiceBus-Coverage: Zwischenstand vom 25.09.2026

## Geltungsbereich

- Ausgangscommit: `c0325cb922802425885767c28fc080785e330c5a`.
- Git-Tree `src`: `4c2b86f8cca89026298cc57e74b6857a0e3f935c`.
- Git-Tree `tests`: `5f81a6c295ebca30bb78fd13504747990b52f04c`.
- Alle Messungen wurden aus frischen, getrennten Release-Buildverzeichnissen unter `artifacts/coverage-a-plus-20260925-fresh-c0325cb92/` gestartet. Die locked Restores und Release-Builds für Unit, gemeinsame Provider, SQL Server, RabbitMQ und Azure Service Bus sind abgeschlossen; die erfolgreichen Builds melden null Warnungen und Fehler.
- `git status --porcelain -- src tests` ist leer. Die nutzereigenen unversionierten Verzeichnisse `TestResults/` und `review/` wurden nicht verändert.

## Bestätigte Testläufe und Berichte

| Lauf | Ergebnis | Cobertura-Berichte |
| --- | ---: | ---: |
| 22 Unit-/Infrastrukturmodule | 9.984 bestanden, 0 Fehler/Skips | 22 |
| Abstractions ohne AVX2 | 810 bestanden, 0 Fehler/Skips | 1 |
| 9 gemeinsame lokale Providermodule | 338 bestanden, 0 Fehler/Skips | 9 |
| PostgreSQL ohne Coverage, frischer Einzel-Fixture | 79 bestanden, 0 Fehler/Skips | 0 |

Die **32 gültigen Cobertura-Berichte** sind parsebar, nennen alle 32 Produkt-Assemblies und enthalten keine gelöschte oder unversionierte `src`-Datei. Sie reichen **nicht** für eine vollständige A+-Aussage: PostgreSQL, SQL Server, RabbitMQ und Azure Service Bus fehlen als lokale Coverage-Berichte. Der konservative Branch-Wert ist der einzige Entscheidungswert für Branch Coverage; die Summe mehrfach beobachteter Branches wäre lediglich eine Obergrenze.

## PostgreSQL-Diagnose und Infrastruktur

- PostgreSQL lief ohne Coverage vollständig grün (79/79). Drei MTP-Coverage-Versuche mit separatem Fixture blieben vor Abschluss der Tests stehen, auch bei serieller Ausführung und ohne den neu hinzugefügten Wartungstest. Die fehlgeschlagenen Versuche wurden beendet; ihre Logs liegen unter `artifacts/coverage-a-plus-20260925-fresh-c0325cb92/attempts/`, außerhalb der gültigen Berichte.
- Der alternative Microsoft-Collector `dotnet-coverage 18.10.0` bestand einen Kontrolllauf mit 96/96 Benchmark-Tests. Sein Cobertura-Bericht hat gegenüber dem MTP-Bericht für dieselbe Assembly **identische 47.255 Zeilenpositionen, 140 getroffene Zeilen und 7.822 Branch-Positionen mit identischen Branch-Werten**. Ein PostgreSQL-Lauf mit diesem Collector ist vorbereitet.
- Beim Aufräumen des letzten PostgreSQL-Fixtures blockierten Docker-/Colima-Systemaufrufe. Auch das zweite Colima-Profil antwortet nicht. Ein harter Neustart des Standardprofils ist zur Freigabe angefragt, da fremde Container betroffen sein könnten. Bis dahin werden keine weiteren Container gestartet.
- SQL Server, RabbitMQ und Azure Service Bus sind im separaten `remaining-sdk` bereits frisch gebaut. Ihre lokalen Fixture-Tests sind noch offen.

## Nachweisgrenzen und nächste Schritte

Der Auswerter ordnet jedem gültigen XML einen projektspezifischen grünen Log-Block mit positiver Testzahl, null Fehlern/Skips und genauem Berichtspfad zu; fehlgeschlagene Läufe bleiben außerhalb `raw/`. Er prüft die aktuellen `src`-/`tests`-Git-Trees und den sauberen Status. Die DLL-Hashes werden jedoch erst bei der Analyse gelesen, nicht beim Teststart. Die vorliegenden Daten beweisen daher keine lückenlose Byteidentität von Build, ausgeführter DLL und Git-Commit; dies bleibt eine explizite Evidenzgrenze aus dem adversarialen Review.

Nach Wiederherstellung von Docker: PostgreSQL über den separat kontrollierten Collector ausführen, die drei übrigen Provider-Fixtures abschließen, alle 36 Berichte mit dem geprüften Auswerter zusammenführen und erst dann Line-, konservative Branch- und CRAP-Werte als frisches Produktprofil bewerten. Etwaige neue Tests müssen reales Produktverhalten, Fehlerfälle und Grenzen unabhängig prüfen.
