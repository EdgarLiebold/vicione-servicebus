# Ausstehende Aufgaben nach abgebrochenem Warten, 25.09.2026

## Produktfehler und Verhalten

`PendingTaskCollection.CompletedAsync` leerte die Sammlung vor dem Warten.
Brachen Aufrufer das Warten ab, meldete ein späterer Aufruf sofort Abschluss,
obwohl noch eine Empfangs-, Mediator-, Queue- oder Jobaufgabe lief. Ein
fokussierter Test reproduzierte dies vor der Korrektur: 0/1, weil der zweite
Wait bereits abgeschlossen war.

Die Korrektur behält jede Momentaufnahme bis zu ihrem tatsächlichen Abschluss.
Ein Abbruch durch den übergebenen Token lässt sie im Bestand; nach Erfolg oder
Fehler werden ausschließlich die erfassten Aufgaben-IDs entfernt. Später
hinzugefügte Aufgaben bleiben im Bestand und werden im nächsten Durchlauf
erwartet. Die Tests prüfen:

- `CanceledWait_DoesNotLoseUnfinishedWorkForTheNextWaitAsync`: Ein abgebrochener
  Wait verliert eine noch laufende Aufgabe nicht; der nächste Wait endet erst
  nach deren Abschluss.
- `CompletedAsync_DrainsWorkAddedWhileWaitingAsync`: Eine während des ersten
  Waits hinzugefügte Aufgabe hält den Gesamtwait offen.
- `CompletedAsync_ReportsFaultOnlyAfterEveryCapturedTaskSettlesAsync`: Ein
  Fehler wird erst nach Abschluss aller erfassten Aufgaben gemeldet, mit
  unveränderter Exception-Identität; danach ist der Bestand leer.

Das Read-only-Red-Team-Review fand zunächst eine zu frühe Assertion im
zweiten Test. Die abschließende Version beobachtet den Nichtabschluss über
250 ms bei noch offener zweiter Aufgabe. Das erneute Review fand keinen
weiteren konkreten P1/P2-Befund. Diese Zeitgrenze bleibt von Scheduling
abhängig. Eine gezielte Ein-Snapshot-Gegenprobe scheiterte mit 1/3 Tests;
die unveränderte Fassung bestand mit 3/3. Die erste Entwicklungsprobe wurde
später auf dem exakten Mess-Commit wiederholt. Unter
`artifacts/pending-task-snapshot-mutant-e420daff5/` liegen der Quell-Patch
der ersten Gegenprobe `mutant-71867c7d8.patch` (SHA-256
`55e57f917b61d5364194bd20dd130b6e6d03388f3fd0c51c6f70467e0844df9c`),
das Mutantenlog (SHA-256
`cf60fa09f5ec1b3aa3dd0ddeb7f78d158139648b9c57df0ad19f6cf8c8a6a29c`,
Exitcode 2, 1/3 rot), das Kontrolllog (SHA-256
`fd6d31a2bce79fe8469244b82b6df6e4f175db0cf5b88e49ae1605f0d969e30d`,
Exitcode 0, 3/3 grün) und separate Exitcode-Dateien. Nach dem Kontrolllauf
entsprechen `src` und `tests` wieder bytegenau dem Mess-Commit. Eine zweite
Gegenprobe fügte das frühere `_tasks.Clear()` vor dem Wait wieder ein:
`cancel-mutant-71867c7d8.patch` (SHA-256
`4ee1d668cfddd64cd2709488271d02f849e6f3257dcddc5eacb5e3fd2c3216b2`)
führte zu 1/3 roten Tests (Log-SHA-256
`6bbd62db63a63f9a3a5a435beda76a4b799ea12fea1e2b2db93073260125aa38`,
Exitcode 2). Nach Wiederherstellung bestanden 3/3 (Kontrolllog-SHA-256
`cf3a60d29e8e0fb9d9e540456b8f55894a2f247584c6866a3f075daeb05f95b4`,
Exitcode 0). Auch dafür liegen separate Exitcode-Dateien vor.

## Messung und Grenze

Mess-Commit `71867c7d8209cd519e56da49d85509585e20f9bc`, `src`-Tree
`73d2f8937dbb1f950495e596f448d73b4b78b6bf`, `tests`-Tree
`7a5250003c0925bfe6a29c9492d3e95d5f867c70`. Die vier formalen
`coverage_receipt.py`-Läufe unter
`artifacts/coverage-receipt-{abstractions-normal,abstractions-noavx2,abstractions-scalar,amazonsqs}-71867c7d8/`
bestanden mit 913, 913, 913 und 215 Tests, null Fehlern und Skips. Das
Teilaggregat `artifacts/coverage-pending-tasks-71867c7d8-partial.json`
meldet denselben Commit und drei Assemblies mit 2.954 Testausführungen,
11.064/43.761 Zeilen, konservativ 4.074/17.555 Branches und 704 Methoden
über CRAP 30. Es fehlen 16 Produkt-Unit-, 13 lokale Provider-Receipts und
29 Assemblies; die Teilzahlen belegen keinen produktweiten A+-Stand.
