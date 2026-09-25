# Frische Einzel-Coverage-Nachweise vom 25.09.2026

Die sechs unten genannten Testprojekte wurden mit `tools/ci/coverage_receipt.py`
in getrennten, neuen Verzeichnissen unter `artifacts/` im Release-Modus gebaut
und mit MTP-Cobertura ausgeführt. Alle Builds hatten null Warnungen und Fehler;
alle Läufe bestanden ohne fehlgeschlagene oder übersprungene Tests. Der Runner
speichert pro Lauf die Hashes von Runner, Settings, Build-/Test-DLLs, Test-PDB,
Logs und XML sowie Commit und Git-Trees in `receipt.json`. Er prüft vor und nach
dem Lauf den sauberen verfolgten Arbeitsbaum, unveränderte Build-Artefakte,
verfolgte `src`-Pfade und die Anwesenheit der getesteten Produktassembly im
Coverage-Bericht.

| Testprojekt | Commit | Tests | Receipt-Verzeichnis |
| --- | --- | ---: | --- |
| SqlTransport.Tests | `020545cd5` | 217/217 | `artifacts/coverage-receipt-sql-020545cd5/` |
| Abstractions.Tests | `020545cd5` | 821/821 | `artifacts/coverage-receipt-abstractions-020545cd5/` |
| AmazonS3.Tests | `020545cd5` | 26/26 | `artifacts/coverage-receipt-amazons3-020545cd5/` |
| Azure.Storage.Tests | `020545cd5` | 25/25 | `artifacts/coverage-receipt-azure-storage-020545cd5/` |
| DynamoDb.Tests | `da6364bf6` | 28/28 | `artifacts/coverage-receipt-dynamodb-da6364bf6/` |
| Azure.Table.Tests | `da6364bf6` | 69/69 | `artifacts/coverage-receipt-azure-table-da6364bf6/` |

Alle sechs Receipts haben denselben Produkt-Tree
`6d7eefa0ca97f4521fee383690ae5ec2816de5bb` und Test-Tree
`1a9ef6265bd21d8bd7d26527f42e8910bbc59f9c`. Zwischen den Commits wurde
nur der Runner korrigiert. Der erste DynamoDB-Runner-Versuch wurde verworfen:
Er verlangte irrtümlich, dass auch nicht geladene, bloß kopierte Produkt-DLLs
im Cobertura-Bericht stehen. Der Test selbst bestand 28/28; erst der korrigierte
Lauf auf `da6364bf6` ist ein gültiger Receipt. Die Gegenprobe mit aus dem
XML entfernter DynamoDB-Assembly wurde erwartungsgemäß abgewiesen. Das
adversariale Read-only-Review fand nach der Korrektur keinen offenen P1/P2-Befund
für den begrenzten Runner-Scope.

Diese sechs Berichte sind noch **kein produktweites Coverage- oder CRAP-Profil**.
Der große Unit-Build hängt bei `Grpc.Tools` `ProtoCompile`; selbst ein
separates `protoc --version` schloss in der aktuellen Umgebung nicht ab. Die
lokalen PostgreSQL-, SQL-Server-, RabbitMQ- und Azure-Service-Bus-Berichte
fehlen weiterhin, weil die Colima-/Docker-Umgebung eingefroren ist. Der
angefragte harte Neustart der Standard-VM wurde ohne Freigabe nicht ausgeführt.
Erst nach den übrigen frischen Berichten und der konservativen
Branch-/Methodenaggregation lässt sich A+ für das ganze Produkt bewerten.
