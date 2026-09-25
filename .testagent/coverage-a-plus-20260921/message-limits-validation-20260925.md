# MessageLimits: Konfigurationsgrenzen, 25.09.2026

## Vertrag und Gegenprobe

`MessageLimits.Validate` schützt die verpflichtenden Größen- und Tiefenlimits
eines Busses. Die Tests prüfen gültige inklusive Grenzen, unabhängig
optionale Warn- und Offload-Schwellen sowie 13 ungültige Konfigurationen.
Für die Fehlerfälle wird die `ConfigurationException` samt Busname,
Feldname und Grund geprüft. Ein adversariales Read-only-Review fand eine
fehlende positive Warn-only-Kombination; Warn-only und Offload-only wurden
ergänzt und mit PASS nachgeprüft.

Eine vorübergehende Änderung von `value > MaxBodyBytes` zu
`value >= MaxBodyBytes` wurde durch den gezielten Testlauf erkannt:
2 von 19 Fällen scheiterten an den beiden zulässigen Gleichheitsgrenzen.
Nach Rücknahme war der Quell-Diff leer und dieselbe Klasse bestand 19/19.
Die Produktimplementierung wurde in dieser Etappe nicht geändert.
Patch, Exitcodes und Rohlogs liegen unter
`artifacts/message-limits-boundary-mutant-e1ccf182c/`.
Der SHA-256 des Mutantenlogs ist
`6d20832e207e85fd61e601b09a7c817f425776bd3dac88e7d4acab431102e072`,
der des Kontrolllogs
`833666654046925f8f0511dbbc75e957d7363121b9e5eb84cf04a4be25ca553f`.

## Verifizierter Messstand

Commit `e1ccf182cecf20235c2c7836d8c8a1b708bda3cd`, `src`-Tree
`a2333260323a0429596d148bfe2aa087e4fd0a55`, `tests`-Tree
`5e747c7e0ba78c7cb18bd6726f3d70b215b2c06c`.

| Modus | Receipt | Tests |
| --- | --- | ---: |
| Normal | `artifacts/coverage-receipt-abstractions-normal-e1ccf182c/receipt.json` | 867/867 |
| Ohne AVX2 | `artifacts/coverage-receipt-abstractions-noavx2-e1ccf182c/receipt.json` | 867/867 |
| Skalar | `artifacts/coverage-receipt-abstractions-scalar-e1ccf182c/receipt.json` | 867/867 |

Das unabhängige Receipt-Review bestätigte Commit, Git-Trees, alle
referenzierten Hashes, Logs, 0 Fehler/Skips/Buildwarnungen und Cobertura-XML.
In jedem der drei Berichte erreicht `MessageLimits.Validate` 15/15 Zeilen
und 8/8 Branches, `ValidateOptionalThreshold` 7/7 Zeilen und 4/4 Branches.
Damit entsprechen ihre methodischen CRAP-Werte den Komplexitäten 8 und 4.
Im partiellen Abstractions-Aggregat sind noch 24 Methoden mit CRAP > 30;
vor dieser Testetappe waren es 25.

Das reproduzierbare Aggregat
`artifacts/coverage-message-limits-e1ccf182c-partial.json` enthält
2.601 Testausführungen und eine Produktassembly: 5.777/8.069 eindeutige
Zeilen und eine konservative Branch-Untergrenze von 2.134/3.010.
Es fehlen 17 Produkt-Unit-, 13 lokale Provider-Receipts und 31
Produktassemblies. Diese Teilmessung ist kein produktweiter A+-Wert.
