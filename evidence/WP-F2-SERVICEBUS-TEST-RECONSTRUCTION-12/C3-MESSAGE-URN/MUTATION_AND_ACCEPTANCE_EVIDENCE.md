# C3 Message URN — Mutation and acceptance evidence

## Gegenstand

Der technische Kandidat ist Commit `d1b472d42b52d58951a4c2fafb1171cdbdeac02c` mit Tree
`191e80573d9431b4f3c8a2bb6f97c4e4f8f839e9`. Er ersetzt 15 geerbte beziehungsweise in R0
identifizierte Verpflichtungen durch 17 native xUnit-/MTP-Fälle im Source-Owner-Projekt
`ViciOne.ServiceBus.Abstractions.Tests`. `MessageType_Specs.cs` bleibt erhalten, weil seine
unabhängige Array-Transportverpflichtung nicht zu diesem Slice gehört.

## Produktbefund und Orakelentscheidung

Der erste fokussierte Lauf fand einen echten Produktfehler: `MessageUrn.ForType(Type)` und
`MessageUrn.ForTypeString(Type)` behandelten null und offene generische Typen unterschiedlich. Beide
öffentlichen Runtime-Overloads verwenden nun denselben fail-closed Validator. Der Regressionstest
verlangt für beide Überladungen denselben Exception-Typ und den Parameter `type`.

Die erwartete Schreibweise des Prozentzeichens wurde im Test von `%` auf `%25` korrigiert. Das ist
keine Anpassung an einen Produktfehler: `System.Uri` kanonisiert das Zeichen so, und auch das
geerbte Orakel erwartete ausdrücklich `%25`. Die Korrektur entfernt ausschließlich ein falsches
neues Testorakel.

## Positive Ausführung

Alle Befehle liefen aus der Repositorywurzel mit dem unveränderten technischen Kandidaten. Restore
und Build waren seriell und verwendeten die vorhandenen Lockfiles.

```bash
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode --disable-parallel \
  -m:1 -p:BuildInParallel=false -p:NuGetAudit=false \
  /bl:artifacts/c3-message-urn/unit-locked-restore.binlog

dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-restore \
  --no-incremental --disable-build-servers -m:1 -p:BuildInParallel=false \
  -p:UseSharedCompilation=false /bl:artifacts/c3-message-urn/unit-release-build.binlog

dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release \
  --no-build --no-restore --results-directory artifacts/test-results/unit \
  --minimum-expected-tests 652 --max-parallel-test-modules 1
```

Ergebnis: Restore Exitcode 0; Build Exitcode 0 mit 0 Warnungen und 0 Fehlern; Test Exitcode 0 mit
652 gesamt, 652 erfolgreich, 0 fehlgeschlagen und 0 übersprungen.

```bash
dotnet restore ViciOne.ServiceBus.Tests.LocalIntegration.slnx --locked-mode --disable-parallel \
  -m:1 -p:BuildInParallel=false -p:NuGetAudit=false \
  /bl:artifacts/c3-message-urn/local-locked-restore.binlog

dotnet build ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release --no-restore \
  --no-incremental --disable-build-servers -m:1 -p:BuildInParallel=false \
  -p:UseSharedCompilation=false /bl:artifacts/c3-message-urn/local-release-build.binlog

VICIONE_TESTS__Profile=LocalIntegration dotnet test \
  --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release \
  --no-build --no-restore --results-directory artifacts/test-results/local-integration \
  --minimum-expected-tests 3 --max-parallel-test-modules 1
```

Ergebnis: Restore Exitcode 0; Build Exitcode 0 mit 0 Warnungen und 0 Fehlern; Test Exitcode 0 mit
3 gesamt, 3 erfolgreich, 0 fehlgeschlagen und 0 übersprungen.

Der kohortenspezifische direkte MTP-Lauf erzeugte `final-abstractions.ctrf.json`: 99 gesamt,
99 erfolgreich, 0 fehlgeschlagen und 0 übersprungen.

## Ein-Ursachen-Mutationen

Jede Mutation wurde einzeln auf den technischen Kandidaten angewandt, mit Release neu gebaut und
mit allen 99 Abstractions-Fällen ungefiltert ausgeführt. Jeder Lauf endete aus genau dem erwarteten
Grund mit Exitcode 2 und 98 erfolgreichen sowie einem fehlgeschlagenen Test. Nach jeder Ausführung
wurde der Quelltext exakt zurückgeführt; `git diff --exit-code` war vor der Acceptance-Ausführung
grün.

| Mutation | Erwarteter und gemessener Fehler |
|---|---|
| Arraypfad gibt den Element-URN ohne `[]` zurück | `AttributedArray_UsesElementUrnWithArraySuffix` |
| `ForTypeString(Type)` umgeht den gemeinsamen Validator | `RuntimeTypeOverloads_RejectNullAndOpenGenericTypes` |
| Präfixprüfung verwendet `EndsWith` statt `StartsWith` | `Constructor_DefaultPrefixValue_IsRejected` |
| Deconstruction verwirft den Assemblynamen | `Deconstruct_ReturnsExpectedComponents` für `AssemblyQualified` |
| Verschachtelter Typ verwendet `.` statt `+` | `NestedType_IncludesDeclaringType` |

## Rohartefakte

Die generierten Artefakte bleiben im ignorierten `artifacts/`-Baum; ihre Hashes binden die hier
ausgewerteten Bytes, ohne Buildausgaben als Quellcode einzuchecken.

| Artefakt | SHA-256 |
|---|---|
| `artifacts/c3-message-urn/unit-locked-restore.binlog` | `3f047c7757c08e58cc04bc392cb4074063a8f0805e18f7ca5206db4303671172` |
| `artifacts/c3-message-urn/unit-release-build.binlog` | `ed5eff0c240208f051d34fef6165ea716bb14661da1f176ad138ddd0046e8a72` |
| `artifacts/c3-message-urn/local-locked-restore.binlog` | `43323e36e63fcc0b13a6e8e5d3f2708c1d7284ade8679b0e535ab47ea7ef69ba` |
| `artifacts/c3-message-urn/local-release-build.binlog` | `bc3537c21ef872e73ab04a1802e93e00ae01a41ab21166a5d2875ad32bda8894` |
| `artifacts/c3-message-urn/final-abstractions.ctrf.json` | `e7979fed5a7efa3e4a0755b8a7da5a11629009656685ecd4b562bf8ba9bd7422` |
| `artifacts/c3-message-urn/mutation-array.ctrf.json` | `e9f330d664bacfbb159c56f5fafd5ed351bc78be85ba671d681e7d177bd82565` |
| `artifacts/c3-message-urn/mutation-validation.ctrf.json` | `f1b400576237b1c7907af565fc19d88457a71862f575d89cdf2cbcc9e80e0fb8` |
| `artifacts/c3-message-urn/mutation-attribute.ctrf.json` | `baa34392067e80dc187ebcd3403e1ac27a983eb822bd77ef0cac751d8ffe45a1` |
| `artifacts/c3-message-urn/mutation-deconstruct.ctrf.json` | `e05651c1c578af2afe0e72d994ae062eeb14d04a2454ec77eed614eff9331c96` |
| `artifacts/c3-message-urn/mutation-derived-name.ctrf.json` | `3257b0f1c7a38de5544d73a8516c335095daf4025345de81e952f079fa38a296` |

## Urteil

PASS. Der neue Testcode spiegelt den Produktowner, besitzt direkte externe Orakel, enthält keine
Skips oder Umgehungen und erkennt jede angegriffene Produktsemantik. Der einzige gefundene
Produktfehler wurde minimal im Produkt behoben und durch eine negative Mutation abgesichert.
