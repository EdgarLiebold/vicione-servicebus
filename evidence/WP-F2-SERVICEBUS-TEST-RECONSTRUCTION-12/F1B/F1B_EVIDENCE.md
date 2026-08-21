# F1b-Beleg — native Anforderungsbindung der Architektur-Referenzkohorte

Technischer Prüfgegenstand ist ausschließlich Commit
`87dd0fa3682dbfdc0fe1bf626d72747948eb0be1` mit Tree
`e4e375303c0017cae1c10c1dd0dbea16c5ba3475`. Dieser Bericht wird nachgelagert committed und
attestiert seinen technischen Parent; er behauptet keinen selbstreferenziellen Test des späteren
Belegcommits.

## 1. Ergebnis und Grenze

- sechs Lead-freigegebene Architekturvarianten sind in einer unveränderlichen, eingebetteten
  Projektion gebunden;
- passive Metadaten auf kompilierten xUnit-Testmethoden werden ordinal und ohne Normalisierung mit
  genau dieser Projektion verglichen;
- xUnit bleibt Eigentümer von Discovery und Einzelurteilen, Microsoft Testing Platform bleibt
  Eigentümer des Prozessurteils;
- es gibt keinen Receipt-, Interceptor-, Sentinel-, Ergebnisparser- oder zweiten Verdict-Pfad;
- eine neue ArchUnitNET-Core-Regel prüft die reale Abhängigkeitsrichtung
  `ViciOne.ServiceBus.Abstractions` ↛ `ViciOne.ServiceBus`;
- Produktcode, geerbte Tests, Pakete, Lockfiles und Root-Buildvertrag sind unverändert.

F1b ist nur die erste kleine Architektur-Referenzkohorte. Der Nachweis behauptet weder die Migration
aller Verhaltenskohorten noch die Vollständigkeit aller Produktfeatures. Kein geerbter Test wurde
gelöscht.

## 2. Projektion

Die Projektion enthält genau sechs Einträge und je Eintrag ausschließlich:

1. `requirementId`;
2. `variantKey`;
3. einfachen Assemblynamen;
4. `Type.FullName`;
5. `MethodInfo.Name`.

Der Vergleich verwirft fehlende, zusätzliche oder doppelte Eigenschaften, nichtkanonische Werte,
doppelte Requirement-/Variantenschlüssel, fehlende oder überladene Methoden, mehrere
Coverage-Attribute und Methoden ohne genau ein xUnit-Testattribut. R0 und
`VERIFICATION_MODEL.json` werden weder geladen noch nachgebildet.

SHA-256 der eingebetteten Projektion:
`86757d5cf08009e94f2860bd35bfb952a1e8d247266d6bec9e08896d11d8d198`.

## 3. Vollständige positive Ausführung

Alle .NET-Befehle liefen am 22. August 2026 mit SDK `10.0.302`. Restore war locked; Builds liefen in
Release ohne Restore und ohne inkrementelle Wiederverwendung. Der native Testlauf war ungefiltert.

| Gegenstand | Ergebnis |
|---|---|
| Produkt-Locked-Restore | Exit 0 |
| Engineering-Locked-Restore | Exit 0 |
| Unit-Locked-Restore | Exit 0 |
| Produkt-Release-Build | Exit 0; 0 Warnungen; 0 Fehler |
| Engineering-Release-Build | Exit 0; 0 Warnungen; 0 Fehler |
| Unit-Release-Build | Exit 0; 0 Warnungen; 0 Fehler |
| unfiltrierter nativer Unit-Lauf mit Mindestzahl 91 | Exit 0; 91 gesamt; 91 grün; 0 rot; 0 übersprungen |
| `dotnet format --verify-no-changes --include tests2` | Exit 0; 0 Dateien geändert |

Restore lief je Ziel in dieser exakten Form, wobei `<target>` und `<name>` nacheinander Product,
Engineering und Unit bezeichneten:

```bash
env DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 \
  dotnet restore <target>.slnx --locked-mode --disable-build-servers \
  -m:1 -p:BuildInParallel=false /bl:artifacts/f1b-final/<name>-locked-restore.binlog
```

Release-Builds liefen je Ziel in dieser exakten Form:

```bash
env DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 \
  dotnet build <target>.slnx --configuration Release --no-restore --no-incremental \
  --disable-build-servers -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false \
  /bl:artifacts/f1b-final/<name>-release-build.binlog
```

Der abschließende Unit-Acceptance-Build verwendete denselben Befehl mit dem Binlognamen
`unit-release-build-acceptance.binlog`. Die zwei nativen Testaufrufe waren:

```bash
env DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release \
  --no-build --no-restore --minimum-expected-tests 91 --max-parallel-test-modules 1

./artifacts/sdk/bin/ViciOne.ServiceBus.Architecture.Tests/release/\
ViciOne.ServiceBus.Architecture.Tests \
  --minimum-expected-tests 91 --results-directory artifacts/f1b-final \
  --report-xunit-ctrf \
  --report-xunit-ctrf-filename final-unit-native-test-after-acceptance-build.ctrf.json
```

Die Formatprüfung lief exakt als:

```bash
env DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 \
  dotnet format ViciOne.ServiceBus.Tests.Unit.slnx --verify-no-changes --no-restore \
  --include tests2 --binarylog artifacts/f1b-final/format.binlog --verbosity diagnostic
```

Die lokale MSBuild-Ausführung verwendete `-m:1` und `BuildInParallel=false`, nachdem ein vollständiger
Binlog ausschließlich einen lokalen Mehrknoten-Handshake von mehr als zehn Minuten ohne Compiler-
oder Testfehler gezeigt hatte. Der identische serielle Gesamtgraph baute in 17 Sekunden. Das ist eine
lokale Ausführungsmaßnahme, keine Repository- oder CI-Architekturentscheidung.

Das native .NET-10-/MTP-`dotnet test` akzeptiert keinen MSBuild-`/bl`-Schalter: Er wird als
Testanwendungsargument interpretiert und führt fail-closed zu null ausgewählten Tests. Deshalb sind
Restore und Build durch Binlogs belegt; der eigentliche Testlauf durch den nativen MTP-Exitcode und
den direkt vom gebauten Testprogramm erzeugten CTRF-Rohnachweis. Es gibt keinen alternativen Runner.

## 4. Rohhashes

| Artefakt | SHA-256 |
|---|---|
| `artifacts/f1b-final/product-locked-restore.binlog` | `2d90a1d274b214c88f2ac1a577d9970d3bc702ee3907a136e30ea75de266d0da` |
| `artifacts/f1b-final/engineering-locked-restore.binlog` | `8ab85b773c8de2897cf54940302f9556738aaf16109240ab888e5a4861d9b62c` |
| `artifacts/f1b-final/unit-locked-restore.binlog` | `2faa9cc6c9adf04d84e551294e9f86ca961c5ffc4978c30d8685c36042cb43d0` |
| `artifacts/f1b-final/product-release-build.binlog` | `d5b677bc4eb1728b23afc9eca6a96c65f19b0ac222c6c80d08e758aab07eefc6` |
| `artifacts/f1b-final/engineering-release-build.binlog` | `c6288e2dc1cddbd2377783cddc7dd50bcc6ee595557c8e4a7709518e19e5fd55` |
| `artifacts/f1b-final/unit-release-build-acceptance.binlog` | `3b715403d058a1fd80b63398abc6565cc72ea80da3cd3eaed56db8581e67e99e` |
| `artifacts/f1b-final/format.binlog` | `c693712267ae16d31f5faf2ea48d4ad2ed9c92363ea1eda8f1d056322ea20860` |
| `artifacts/f1b-final/final-unit-native-test-after-acceptance-build.ctrf.json` | `92c66b809286a882a29965eab330b6d9835c5d582c779195db032b13810d4538` |

Die generierten Dateien liegen absichtlich im ignorierten Artefaktbaum. Ihre Hashes binden die
Ausführung, ohne Buildausgaben als Quellcode einzuchecken.

## 5. Ein-Ursachen-Sabotagen

Jede Mutation lief in einer eigenen, aus dem technischen Commit erzeugten Wegwerfkopie. Die
kanonische Arbeitskopie blieb sauber.

| Mutation | Gemessenes Ergebnis |
|---|---|
| projizierte Methode fehlt | Comparator rot; Identität löst zu null Methoden auf |
| Projektionseintrag fehlt | Comparator rot; unprojizierte kompilierte Tupelidentität benannt |
| zusätzliche attribuierte Methode | Comparator rot; zusätzliche Tupelidentität benannt |
| doppelter Requirement-/Variantenschlüssel | Comparator rot; doppelter Schlüssel benannt |
| doppeltes Coverage-Attribut | Build rot; ausschließlich Compilerfehler `CS0579` |
| eingebettete Projektion fehlt/ist umbenannt | Comparator rot; exakter Ressourcenname benannt |

Alle sechs Gegenproben scheiterten aus ihrem jeweils vorgesehenen Grund; keine Mutation wurde in
den technischen Commit übernommen. Exakte Mutationen, Befehlsformen, Exitcodes und Zielbefunde sind
in `F1B_MUTATION_EVIDENCE.md` gebunden. `F1B_MUTATION_ARTIFACTS.sha256` bindet die Ergebnistabelle
und alle 29 Rohlogs und Binlogs.

## 6. Statische Qualitätsprüfung

- der vollständige Elf-Dateien-Delta und sein fachliches Umfeld wurden gelesen;
- 69 `[Fact]`-Methoden und sechs `[Theory]`-Methoden ergeben im nativen Lauf 91 Fälle;
- keine Skips, Sleeps, Zufalls-/Uhrzeitabhängigkeit, Umgebungsänderung, `.Result`, `.Wait()`, breite
  Exception-Fänger oder assertionfreien neuen Testkörper;
- keine Python-, Receipt-, Interceptor-, Sentinel-, `VERIFICATION_MODEL`- oder R0-Laufzeitkopplung;
- die passive Infrastruktur kennt weder xUnit noch MTP; der eigentliche Abgleich ist ein normaler
  xUnit-Test in der Architektur-Testassembly;
- die Teststruktur bleibt nach Produkt-/Architekturbereichen gegliedert.

## 7. Unabhängige Prüfung

Zwei voneinander unabhängige, read-only Red-Teams prüfen diesen eingefrorenen technischen Commit,
seinen vollständigen Delta, die Nachweise und die Manipulationsgrenzen. Bis beide ohne BLOCKER oder
MAJOR enden, ist F1b noch nicht angenommen und es beginnt keine Verhaltenskohorte.
