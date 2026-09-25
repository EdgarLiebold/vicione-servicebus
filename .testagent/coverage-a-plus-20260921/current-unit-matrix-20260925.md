# Aktuelle Unit-Coverage-Matrix vom 25.09.2026

## Gültige Produktbereichs-Receipts

Alle 17 folgenden `tools/ci/coverage_receipt.py`-Läufe wurden auf Commit
`0fcee3a1cf8df24274412b9659c26740a1ed3b4c` mit identischem `src`-Tree
`6d7eefa0ca97f4521fee383690ae5ec2816de5bb` und `tests`-Tree
`fe688c658a68b75191fe90a2de51537e8bdf8c36` abgeschlossen. Jeder
Receipt unter `artifacts/coverage-receipt-<Name>-0fcee3a1c/receipt.json`
bindet den positiven Testlauf an einen frischen Release-Build, unveränderte
DLL-/PDB-Bytes, null Buildwarnungen/-fehler, den Cobertura-Bericht und die
verfolgten Quellpfade. Fehler und Skips sind jeweils null.

| Name | Bestandene Tests |
| --- | ---: |
| abstractions | 824 |
| sql | 217 |
| amazons3 | 26 |
| azure-storage | 25 |
| dynamodb | 28 |
| azure-table | 69 |
| messagepack | 118 |
| signalr | 97 |
| amazonsqs | 214 |
| activemq | 174 |
| rabbitmq | 395 |
| quartz | 267 |
| efcore | 276 |
| azureservicebus | 336 |
| visualizer | 29 |
| analyzers | 164 |
| codefixes | 36 |
| **Summe** | **3.295** |

Die Cobertura-Berichte enthalten zusammen 28 der 32 erwarteten
Produktassemblies unter `src`. Die fehlenden vier sind
`ViciOne.ServiceBus.EventHubs`, `.EventHubs.Testing`, `.Futures` und
`.Mediator`. Das adversariale Read-only-Review hat alle 17 Receipts und die
Sammelzahl unabhängig gegen Git-Trees, Dateien, Logs und XML geprüft.

## Weitere Unit-Tests ohne Produkt-Coverage-Receipt

Die vier Projekte `Benchmark.Tests`, `Diagnostics.Tests`,
`Roslyn.Tests.Infrastructure.Tests` und `Tests.Infrastructure.Tests` liegen
außerhalb des Produkt-`src`-Zielassembly-Schemas. Ihre MTP-Logs unter
`artifacts/unit-support-<Name>-0fcee3a1c/test.log` zeigen 96, 45, 4 und 135
bestandene Tests, insgesamt 280, jeweils null Fehler/Skips. Das Red Team
hat die Summaries geprüft. Für diese vier Läufe gibt es keinen separaten
Buildlog, keinen Cobertura-Bericht und keinen Binary-/Commit-Receipt; daraus
wird keine Produkt-Coverage oder Buildwarnungsfreiheit abgeleitet.

Damit wurden 21 der 22 Unit-/Infrastruktur-Testprojekte ausgeführt. Das
große `ViciOne.ServiceBus.Tests`-Projekt bleibt offen: Sein Build hängt bei
`Grpc.Tools` `ProtoCompile`; auch ein isolierter `protoc --version`-Aufruf
schloss in dieser Umgebung nicht ab. Alle 13 lokalen Provider-Berichte müssen
noch auf dem aktuellen Tree erhoben werden. Insbesondere die Läufe für
PostgreSQL, SQL Server, RabbitMQ, Azure Service Bus und EventHubs warten auf
eine funktionsfähige Colima-/Docker-Umgebung. Ein harter Neustart der
Standard-VM wurde angefragt, aber ohne Freigabe nicht ausgeführt.

## Ergänzende Portabilitätsmessung ohne AVX2

Der separate Abstractions-Lauf liegt unter
`artifacts/coverage-receipt-abstractions-noavx2-4d6be6c08/receipt.json`.
Er wurde auf Commit `4d6be6c08bb544715e6b9279c2a2fca92bc46f7f`
mit denselben `src`- und `tests`-Trees wie die 17 obigen Läufe erhoben.
Der Runner-Receipt deklariert für Restore, Build und Test
`DOTNET_EnableAVX2=0` unter `dotnetEnvironment`. 824 von 824
Tests bestanden, ohne Fehler, Skips, Buildwarnungen oder Buildfehler. Die
Binärdateien und der Cobertura-Bericht sind im Receipt gehasht.

Das Red Team verglich die beiden Abstractions-Cobertura-Berichte unabhängig:
Unter den 16 `src/.../NewId/`-Dateien stehen in beiden Berichten dieselben
756 eindeutigen Quellpositionen. Der normale Lauf deckt 487 ab, der Lauf
ohne AVX2 452; die Schnittmenge umfasst 376. Nur im normalen Lauf sind
111 Positionen gedeckt, nur im Lauf ohne AVX2 76, zusammen 563. Dies ist
ein gemessener Coverage-Unterschied. Die deklarierte Laufzeitvariable wurde
nicht unabhängig im Prozess gemessen; auch ein Test auf Hardware ohne AVX2
liegt nicht vor.

## Receipt-Aggregation und nächstes Mess-Gate

`tools/ci/aggregate_coverage_receipts.py` prüft Hashes, Logs, Binärinventar,
Quellpfade und Git-Trees und vereint Zeilen, beobachtete Branches und
methodische CRAP-Werte. Im strengen Modus akzeptiert er nur Receipts aller
18 Produkt-Unit-Projekte, aller 13 lokalen Provider-Projekte und des
Abstractions-Laufs ohne AVX2. Alle müssen vom selben Mess-Commit stammen und
alle 32 Produktassemblies abdecken. Die vier oben genannten Support-Projekte
haben separate Test-Gates; sie referenzieren keine `src`-Produktprojekte direkt.

Die Probe mit den vorhandenen 18 Receipts meldet ausdrücklich `partial`:
17 Produkt-Unit-Läufe und ein zusätzlicher Portabilitätslauf, 4.119
Testausführungen einschließlich der zweimal gelaufenen 824 Abstractions-Tests,
ein fehlender Core-Unit-Receipt, 13 fehlende lokale Provider-Receipts und
vier noch nicht beobachtete Produktassemblies. Die Receipt-Commits sind
`0fcee3a1c` und `4d6be6c08`, daher `sameCommit=false`. Diese Teildaten
sind kein produktweiter Line-, Branch- oder CRAP-Wert. Eine spätere
vollständige Messung benötigt einen einheitlichen Mess-Commit.

Adversariale Gegenproben werden abgewiesen: gemischte Git-Trees, geänderte
Binärhashes, fehlende PDBs oder Restore-Logs, doppelte Projekte,
unvollständige Profile im strengen Modus und das Überschreiben eines
vorhandenen Receipts. Die AVX2-Einstellung ist eine vom Runner deklarierte
Eingabe. Eine unabhängige In-Process-ISA-Messung oder manipulationsfeste
Invocation liegt weiterhin nicht vor.

Diese 17 Berichte sind **kein produktweites A+-Profil**. Line-, konservative
Branch- und methodische CRAP-Werte für das ganze Produkt dürfen erst nach
den fehlenden aktuellen Berichten und deren gemeinsamer Auswertung als
gültige Gesamtwerte veröffentlicht werden.
