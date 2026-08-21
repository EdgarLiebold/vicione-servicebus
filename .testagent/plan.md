# Plan — F1a native xUnit-4-/MTP-v2-Testgrundlage

Ausführungsplan zu `.testagent/research.md`. Er plant nur die Ausführung; Struktur, Profile,
Proof-Grenzen und Abnahmebedingungen stammen aus dem Lead-Vertrag und aus Direktive
`DIR-A0071-START-F1A-03` und werden hier nicht neu entworfen.

## 1. Zieldateien

### Zentrale Konfiguration — Eigentümer: Integrator

| Datei | Art | Zweck |
|---|---|---|
| `Directory.Packages.props` | Änderung | `xunit.v3.mtp-v2` `4.0.0` und `TngTech.ArchUnitNET` `0.13.4` aufnehmen; `GitHubActionsTestLogger` an eine Bedingung binden, die `tests2` ausschließt |
| `tests2/Directory.Build.props` | neu | Elternimport ohne `Exists`; `net10.0`, nicht packbar, Testbaummarker, zentrale User-Secrets-ID. **Kein** Inhaltselement: diese Datei wird vor dem Projektinhalt ausgewertet und kann ein Projekt nicht klassifizieren |
| `tests2/Directory.Build.targets` | neu | Elternimport ohne `Exists`; zwei getrennte Importnachweise als `InitialTargets`; Klassifikationsabgleich gegen die Paketreferenz; Einbindung der einen `xunit.runner.json` und `testsettings.json` in jedes ausführbare Testartefakt |
| `tests2/xunit.runner.json` | neu | einzige xUnit-Konfiguration: `failSkips`, `failWarns` |
| `tests2/testsettings.json` | neu | eingecheckte, secret-freie Vorgaben |
| `ViciOne.ServiceBus.Tests.Unit.slnx` | neu | Profil `UnitArchitecture` |
| `ViciOne.ServiceBus.Tests.LocalIntegration.slnx` | neu | Profilgrenze, in F1a bewusst ohne Projekt |
| `ViciOne.ServiceBus.Tests.External.slnx` | neu | Profilgrenze, in F1a bewusst ohne Projekt |
| `ViciOne.ServiceBus.Engineering.slnx` | Änderung | neue Projekte für die Werkbank sichtbar machen |
| `docs/build.md` | Änderung | native Profilbefehle dokumentieren |

`ViciOne.ServiceBus.slnx` bleibt unberührt: dort liegen Produkt und geerbter Testbaum.

### `tests2/Testing/ViciOne.ServiceBus.Testing` — frameworkneutral

| Datei | Zweck |
|---|---|
| `ViciOne.ServiceBus.Testing.csproj` | Bibliothek, nicht ausführbar, nicht packbar, **ohne** xUnit-Paket |
| `Configuration/ViciOneTestOptions.cs` | typisiertes Optionsobjekt |
| `Configuration/TestConfigurationProvider.cs` | einziger Owner über `testsettings.json`, den **einen** User-Secrets-Store und `VICIONE_TESTS__`; übersetzt `__` in den Konfigurationspfadtrenner wie der Microsoft-Provider |

Kein Produkt-Assembly-Katalog. Assembly- und Projektmengen werden nie hier gepflegt.

### `tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests` — ausführbar

| Datei | Prüfgegenstand |
|---|---|
| `ViciOne.ServiceBus.Architecture.Tests.csproj` | `xunit.v3.mtp-v2` `4.0.0` als einziger Testeinstieg, ArchUnitNET-Kern |
| `ProductAssemblyFacts.cs` | Auflösung der **realen kompilierten** Assemblies über direkte Typanker; die zwei Produktassemblies sind ein bewusst kleiner F1a-Ankersatz, nicht „alle Produktassemblies“ |
| `Smoke/ProductAssemblySmokeTests.cs` | reale Assemblies: Identität, Zielframework, öffentliche Oberfläche vorhanden |
| `Architecture/TestTreeIsolationTests.cs` | ArchUnitNET-Kern über reale kompilierte Assemblies: Abhängigkeitsrichtung, Namensraumregel der Supportbibliothek, Referenztabellenprüfung |
| `Architecture/EvaluatedBuildGraphTests.cs` | ausgewertete MSBuild-Abfragen für Testprojekt **und** Supportbibliothek: Klassifikation, Testeinstieg, Elternimportanker, TFM, Packbarkeit, C#-14-Wert, gemeinsamer Secrets-Store |
| `Architecture/ResolvedPackageGraphTests.cs` | vollständige aufgelöste Paketclosure aus `packages.lock.json`: keine verbotene Identität, Plattform nicht mitgesperrt |
| `Architecture/RunnerConfigurationTests.cs` | kanonischer Inhalt und bytegenaue, genau einmalige Übernahme ins gebaute Artefakt |
| `Architecture/TestConfigurationTests.cs` | typisierter Konfigurationsowner, kein Zugriff auf Prozessumgebung |

## 2. Zuordnung Checkliste → Umsetzung und Nachweis

| # | Umsetzung | Nachweis |
|---|---|---|
| A1 | genau ein `PackageReference` auf `xunit.v3.mtp-v2` im Architekturprojekt | `EvaluatedBuildGraphTests.SingleTestEntryPackage…`; `-getItem:PackageReference` |
| A2 | Bedingung an `GlobalPackageReference`; keine Aufnahme verbotener Pakete | `EvaluatedBuildGraphTests.ForbiddenTestPackages…`; ausgewerteter Paketgraph |
| A3 | Inhaltselement in `tests2/Directory.Build.props` kopiert die eine Datei | `EvaluatedBuildGraphTests.RunnerConfiguration…`; Datei im Ausgabeverzeichnis |
| A4 | `failSkips`/`failWarns` in `tests2/xunit.runner.json` | Mutationsprobe M4: ein `Skip`-Test macht das Profil rot |
| A5 | zwei unbedingte Elternimporte, zwei getrennte Anker als `InitialTargets` | Mutationsproben M1a → `VOSBT001`, M1b → `VOSBT002` |
| A6 | keine `Version=` in Projektdateien | `EvaluatedBuildGraphTests.PackageVersionsAreCentral…` |
| A7 | `net10.0`, `IsPackable=false`, SDK-abgeleitetes C# 14 | `-getProperty:TargetFramework,IsPackable,LangVersion`; Wert `14.0` und Gleichheit mit einem Projekt außerhalb `tests2` |
| A8 | Bibliothek ohne xUnit, ohne Katalog | `EvaluatedBuildGraphTests.TestingLibraryHasNoXunit…` |
| A9 | Testprojekte nicht packbar, kein Produktprojekt referenziert sie | `EvaluatedBuildGraphTests.NoTestArtifactInProductGraph…`; `-getItem:ProjectReference` je `src/**` |
| A10 | ein Provider, ein Secrets-Store, typisierte Anforderungsgruppen statt freier Schlüssel | `TestConfigurationTests.*`; injizierte Umgebung ohne Prozessmutation |
| A11 | jede Prüfung lädt eine reale Assembly oder liest eine ausgewertete Abfrage | Testnamen und gebundene Rohausgaben |
| A12 | kein Zähler, kein Parser, kein Receipt, kein Interceptor | vollständige Dateiliste im Diff; Reviewbericht |
| A13 | feste Befehlsfolge mit Binlogs | Exitcodes und Binlogpfade in der Evidence |
| A14 | `-getItem`/`-getProperty`, `-graphBuild` | gebundene Rohausgaben |
| A15 | kein Pfad außerhalb der Schreibmenge | `git diff --name-status` gegen die 92 Scopes |

## 3. Mutationsproben — jede scheitert aus eigenem Grund

| ID | Eingriff | Tatsächliches Scheitern |
|---|---|---|
| M1a | Elternimport aus `tests2/Directory.Build.props` entfernen | `VOSBT001` |
| M1b | Elternimport aus `tests2/Directory.Build.targets` entfernen | `VOSBT002` |
| M2 | echte Paketidentität `TngTech.ArchUnitNET.xUnit` aufnehmen | `VOSBT006` |
| M3 | kanonische Runner-Konfiguration inhaltlich ersetzen | `CanonicalConfiguration_TurnsSkipsAndWarningsIntoFailures` |
| M4 | zweite Runner-Konfiguration im Projekt verdoppeln | `VOSBT008` |
| M5 | einen Test mit `Skip` versehen | Lauf rot durch `failSkips`, null übersprungene Tests |

Jede Probe wird einzeln angewendet, gemessen und **vollständig zurückgenommen**; der Kandidat bleibt
danach unverändert.

## 4. Reihenfolge

1. zentrale Konfiguration schreiben;
2. `ViciOne.ServiceBus.Testing` schreiben;
3. Architekturprojekt schreiben;
4. Solutions erzeugen;
5. **einmaliger** normaler Restore erzeugt die neuen Lockfiles;
6. Belegkette: locked Restore → Release-Build `--no-restore --no-incremental` → unfiltrierter Test
   `--no-build --no-restore`, je mit eindeutigem Binlogpfad;
7. Mutationsproben M1a–M5, je einzeln und zurückgenommen;
8. Evidence binden, `docs/build.md` und `status.md` schreiben;
9. zwei unabhängige read-only Reviews des vollständigen integrierten Diffs;
10. genau ein sauberer Commit, danach `PROGRESS` an den Lead.

## 5. Haltegrenzen

Kein Push, kein F1b, keine Coveragebindung, keine Kohorte, keine Änderung unter `tests/**` oder am
Produktverhalten unter `src/**`, keine Löschung des geerbten Bestands. Bei einer Stopklasse wird die
Arbeit angehalten und eine `QUESTION` gestellt.
