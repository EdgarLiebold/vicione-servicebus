# NewId-Batch: Bereichsvalidierung, 25.09.2026

## Produktfehler und Korrektur

Die drei Array-Batchmethoden `Next`, `NextGuid` und `NextSequentialGuid`
prüften nur `index + count > ids.Length`. Bei negativem Index oder negativer
Länge, einem Index hinter dem Array oder einem `int`-Überlauf konnte diese
Prüfung ungültige Bereiche passieren lassen. Bei einem negativen Index
konnte der spätere Arrayzugriff während der gehaltenen `SpinLock` eine
Ausnahme auslösen. Die Methoden prüfen jetzt Null, negative Werte und den
Index vor der Tick-Abfrage und vor der Sperre; `count > ids.Length - index`
vermeidet dabei einen Additionsüberlauf.

Die Tests prüfen für alle drei Methoden die Exception samt Parameternamen,
unveränderte Arrayelemente und die nächste ID gegen einen Referenzgenerator.
Sie enthalten negative Werte, Bereiche hinter dem Array und einen
Additionsüberlauf. Der gültige Grenzfall `(ids, ids.Length, 0)` prüft
Arrayidentität, Offset, Count, unveränderte Elemente und Sequenzzustand.
Das adversariale Review fand diese gültige Grenze als fehlenden Fall;
nach Ergänzung wurde der Teständerung ein PASS erteilt.

## Verifizierter Messstand

Commit `848bd1c9674fd4cf766960a9e0989f5dfb115950`, `src`-Tree
`a2333260323a0429596d148bfe2aa087e4fd0a55`, `tests`-Tree
`39f0948acf39e04393c445d3ad5119b739d31307`.

| Modus | Receipt | Tests |
| --- | --- | ---: |
| Normal | `artifacts/coverage-receipt-abstractions-normal-848bd1c96/receipt.json` | 849/849 |
| Ohne AVX2 | `artifacts/coverage-receipt-abstractions-noavx2-848bd1c96/receipt.json` | 849/849 |
| Skalar | `artifacts/coverage-receipt-abstractions-scalar-848bd1c96/receipt.json` | 849/849 |

Jeder Lauf meldet null Fehler und Skips; der Receipt-Runner bestätigt vier
unveränderte Binärdateien und 322 verfolgte Produktquellen. Die Vereinigung
der drei Cobertura-Berichte erreicht für `NextGuid(Guid[], int, int)` 49/49
und für `NextSequentialGuid(Guid[], int, int)` 43/43 Zeilen. Das ist keine
volle Branch-Abdeckung: die konservativen Branch-Vereinigungen sind 18/20
beziehungsweise 14/16. Das unabhängige Receipt-Review prüfte alle
referenzierten Hashes, Logs und diese XML-Zahlen mit PASS. Ein produktweiter
A+-Wert folgt daraus nicht.

Der partielle Aggregator akzeptiert die drei Receipts mit identischem Commit
und 2.547 Testausführungen. Weiter offen sind 17 Produkt-Unit-Receipts,
13 lokale Provider-Receipts und 31 Produktassemblies; vier Support-Projekte
haben separate Gates. Die vorigen `ee9da5791`-Receipts bleiben historische
Vergleichswerte und sind wegen der geänderten `src`- und `tests`-Trees kein
Teil des aktuellen Messprofils.
