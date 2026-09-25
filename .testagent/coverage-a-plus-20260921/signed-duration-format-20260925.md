# Signierte Diagnosedauern, 25.09.2026

## Produktfehler und Regressionstest

`TimeSpanExtensions.ToFriendlyString` wird für Diagnoseausgaben des
Message-Fabric-Charts, der SQS-Visibility-Zeit und der Azure-Service-Bus-
Auto-Delete-Zeit verwendet. Bei negativen Dauern fielen die bisherigen
Einheiten weg: `-1 ms` erschien als `-1000000ns`. Der öffentliche
`ChartTable.Add`-Pfad akzeptiert solche Dauern.

Die Formatierung berechnet nun den vorzeichenlosen Betrag aus den Ticks und
setzt ein einziges Minus vor die vollständige Einheitensequenz. Die
Betragsbildung behandelt auch `TimeSpan.MinValue`, dessen positiver Betrag
kein `TimeSpan` ist. Die Tests prüfen exakte positive und negative Einheiten,
ns/µs-Rundung, die 7-/30-/365-Tage-Grenzen, eine zusammengesetzte Dauer in
beiden Vorzeichen und den Minimalwert. Vor dem Fix scheiterten negative
Fälle in einem Entwicklungs-Lauf ohne archiviertes Rohlog; nach dem Fix
bestanden 28/28. Das adversariale Read-only-
Review bestätigte die Arithmetik und die unveränderten positiven Ausgaben.

Eine archivierte Gegenprobe entfernte das Minus nur vorübergehend:
10 von 28 Fällen scheiterten. Nach Rücknahme bestand dieselbe Klasse 28/28,
und `TimeSpanExtensions.cs` entsprach wieder HEAD. Patch, Exitcodes und
Rohlogs liegen unter `artifacts/signed-duration-sign-mutant-9fca34c5a/`.
Der SHA-256 des Mutantenlogs ist
`7396f7aefb64169cc22e47c9a2b8d4dd8826e9db446850d5cdcd6945d0d3c565`,
der des Kontrolllogs
`bdb1572464e5c88d2e8fa3cfe60200b794514549231a90fbdef0e0d5ea4b292a`.

Nach der ersten grünen Messung hatte die monolithische Methode bei voller
Zeilenabdeckung noch Komplexität und CRAP 36. Die Zerlegung in
Vorzeichennormalisierung, Betragsformatierung, Kalender-, Uhr- und
Submillisekunden-Schritte erhielt 28/28 sowie 895/895 Tests und erhielt
ein zweites Review-PASS.

## Verifizierter Messstand

Commit `9fca34c5af93d06301c71c7b0269a9e9f1347b3c`, `src`-Tree
`9f006c046cdf0f04c656aa6401d7e9566c91e7d1`, `tests`-Tree
`7e5af25632f6e0e42b6bc6143131796d92f294c7`.

| Modus | Receipt | Tests |
| --- | --- | ---: |
| Normal | `artifacts/coverage-receipt-abstractions-normal-9fca34c5a/receipt.json` | 895/895 |
| Ohne AVX2 | `artifacts/coverage-receipt-abstractions-noavx2-9fca34c5a/receipt.json` | 895/895 |
| Skalar | `artifacts/coverage-receipt-abstractions-scalar-9fca34c5a/receipt.json` | 895/895 |

Der normale Cobertura-Bericht trifft alle Zeilen und Branches der sechs
Methoden in `TimeSpanExtensions.cs`; ihre Komplexitäten sind 4, 12, 8, 8,
2 und 1. Das partielle Aggregat
`artifacts/coverage-signed-duration-9fca34c5a-partial.json` umfasst
2.685 Testausführungen, 5.838/8.089 eindeutige Zeilen, eine konservative
Branch-Untergrenze von 2.168/3.014 und noch 23 Methoden mit CRAP > 30.
Offen bleiben 17 Produkt-Unit- und 13 lokale Provider-Receipts sowie 31
Produktassemblies; daraus folgt noch kein produktweiter A+-Wert.
