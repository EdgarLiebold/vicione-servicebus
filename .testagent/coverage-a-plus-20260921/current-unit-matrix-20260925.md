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
Der separate Abstractions-Lauf ohne AVX2 für die Portabilitätsmessung fehlt
ebenfalls auf dem aktuellen Tree.

Diese 17 Berichte sind **kein produktweites A+-Profil**. Line-, konservative
Branch- und methodische CRAP-Werte für das ganze Produkt dürfen erst nach
den fehlenden aktuellen Berichten und deren gemeinsamer Auswertung als
gültige Gesamtwerte veröffentlicht werden.
