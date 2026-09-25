# Query-String-Vertrag: Testiteration vom 25.09.2026

## Produktgrund und Testnachweis

`FutureLocation` liest die Korrelations-ID aus der Adresse über
`TryGetValueFromQueryString`; der ActiveMQ-Antwortpfad prüft damit die
Anwesenheit von `temporary`. Drei neue Tests in
`QueryStringExtensionsTests.cs` prüfen unterscheidbare Vertragsfälle:

| Fall | Test und Orakel |
| --- | --- |
| Mehrdeutige `Id`/`id`-Parameter zwischen anderen Parametern | `TryGetValueFromQueryString_RejectsAmbiguousKeysRegardlessOfCase`: genaue `InvalidOperationException` für den angefragten Schlüssel `iD` |
| Fehlender Schlüssel bei vorhandenen anderen Parametern | `TryGetValueFromQueryString_MissingKeyReturnsFalseAndNull`: `false` und `null` |
| Vorhandener Schlüssel ohne `=` | `TryGetValueFromQueryString_ValuelessKeyIsPresentWithEmptyValue`: `true` und leere Zeichenfolge |

Die drei `RequirementCoverage`-Varianten sind in
`Requirements/AbstractionsRequirements.json` eingetragen. Das adversariale
Read-only-Review fand zunächst diese fehlende Projektion; nach der Korrektur
bestätigte es die drei Testorakel und meldete keinen weiteren P1/P2-Befund
für diesen Slice. Der fokussierte Lauf bestand 7/7, die vollständige
Abstractions-Suite vor und nach der Gegenprobe jeweils 824/824.

Eine gezielte Mutation ersetzte beim Schlüsselvergleich
`OrdinalIgnoreCase` durch `Ordinal`. Im Klassenlauf schlug genau der neue
Ambiguitätstest fehl (`Assert.Throws`: keine Exception), 6/7 bestanden.
Die Mutation wurde vollständig zurückgenommen; `git diff -- src` ist leer.
Der erste Gegenprobenversuch mit einem zu engen Methodenfilter entdeckte
null Tests und wurde nicht als Mutationsergebnis gezählt.

## Frischer Commit-Nachweis und Grenzen

Commit `a1cefb77f` wurde mit `tools/ci/coverage_receipt.py` aus einem neuen
Release-Build in `artifacts/coverage-receipt-abstractions-a1cefb77f/` geprüft:
824 bestanden, null Fehler/Skips, null Buildwarnungen/-fehler. `receipt.json`
enthält die Hashes von Build-/Testbytes, Runner, Settings, Logs und Cobertura.
Der Produkt-Tree ist `6d7eefa0ca97f4521fee383690ae5ec2816de5bb`,
der Test-Tree `fe688c658a68b75191fe90a2de51537e8bdf8c36`.

Im frischen Cobertura-Bericht ist
`QueryStringExtensions.TryGetValueFromQueryString` auf 9/9 Zeilen und
Komplexität 8; der methodische CRAP-Wert ist damit 8. Im vorherigen
Abstractions-Receipt waren es 8/9 Zeilen und rechnerisch CRAP 8,088. Die
übrigen Quellbereiche wurden in diesem Commit nicht geändert. Der neue
Test-Tree macht die älteren Einzelnachweise für einen Nachweis am aktuellen
exakten Commit unzureichend; ein vollständiges Produktprofil bleibt offen.
