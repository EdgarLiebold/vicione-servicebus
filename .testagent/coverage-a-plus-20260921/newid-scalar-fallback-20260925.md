# NewId-Batch: skalarer Fallback, 25.09.2026

## Messstand

Commit `ee9da57917e7f4d29f2dadbf2df46eeb5b6ba118`, `src`-Tree
`6d7eefa0ca97f4521fee383690ae5ec2816de5bb`, `tests`-Tree
`71163720aa77639bd0d1be63fd402a1922b55440`.

| Modus | Receipt | Tests | Abstractions-Zeilen | Abstractions-Branches |
| --- | --- | ---: | ---: | ---: |
| Normal | `artifacts/coverage-receipt-abstractions-normal-ee9da5791/receipt.json` | 828/828 | 5633/8225 | 2089/3004 |
| `DOTNET_EnableAVX2=0` | `artifacts/coverage-receipt-abstractions-noavx2-ee9da5791/receipt.json` | 828/828 | 5598/8225 | 2096/3004 |
| `DOTNET_EnableHWIntrinsic=0` | `artifacts/coverage-receipt-abstractions-scalar-ee9da5791/receipt.json` | 828/828 | 5662/8225 | 2084/3004 |

Alle drei Läufe haben null Fehler, Skips, Buildwarnungen und Buildfehler. Der
Receipt-Runner bindet Logs, Binärdateien, Coverage-XML, Commit und Git-Trees
mit Hashes. Das adversariale Read-only-Review prüfte alle drei Receipts und
deren Hashes unabhängig.

Der skalare Bericht trifft die Batch-Fallback-Zeilen 270 sowie 333–336 und
338 in `NewIdGenerator.cs`; die beiden anderen Berichte treffen diese Zeilen
nicht. Das ist ein beobachteter Ausführungspfad unter der deklarierten
Laufzeitvariable, kein unabhängiger Hardware- oder In-Process-ISA-Nachweis.

Die Vereinigung der drei Berichte erreicht für `NextGuid(Guid[], int, int)`
43/44 Zeilen und konservativ 15/18 Branches, für
`NextSequentialGuid(Guid[], int, int)` 37/38 Zeilen und 11/14 Branches.
Jeweils offen ist noch die Invalid-Range-Guard-Zeile 209 beziehungsweise 287.
Das ist keine vollständige Methoden- oder Branch-Abdeckung. Der partielle
Aggregator akzeptiert die drei Receipts als denselben Commit mit 2484
Testausführungen, meldet aber weiterhin 17 fehlende Produkt-Unit-Läufe,
13 lokale Provider-Läufe und 31 weitere Produktassemblies. Ein produktweiter
A+-Wert ist daraus nicht ableitbar.

## Runner und Gate

`coverage_receipt.py` unterstützt nun den exklusiven Schalter
`--disable-hw-intrinsics` zusätzlich zu `--disable-avx2`. Er setzt
`DOTNET_EnableHWIntrinsic=0` für Restore, Build und Test. Der Aggregator
unterscheidet Normal-, No-AVX2- und Scalar-Receipts und verlangt im strengen
Produkt-Gate beide Portabilitätsläufe. Damit sind auf einem einheitlichen
Mess-Commit 18 Produkt-Unit-, 13 lokale Provider- und zwei zusätzliche
Abstractions-Portabilitätsreceipts erforderlich. Vier Support-Testprojekte
haben separate Gates.
