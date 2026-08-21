# F1a-Beleg — native xUnit-4-/MTP-v2-Grundlage

Der erste Team-1-Kandidat `5996d10a` wurde nicht angenommen. Der Product Owner übertrug die
begrenzte Korrektur direkt an den Lead Architect. Produktverhalten unter `src/**` blieb unverändert.

Technischer Prüfgegenstand ist ausschließlich Commit
`99c7e5a373bc3e56861e302675f8cc5fc34cb31d` mit Tree
`e7e055d3b889a6be53baaf3dd0136aa79a1e5dfc`. Dieser Bericht wird nachgelagert committed und
attestiert deshalb seinen technischen Parent; er behauptet keinen selbstreferenziellen Test des
späteren Belegcommits.

## 1. Ergebnis

- xUnit 4 läuft ausschließlich über den Microsoft-Testing-Platform-v2-Einstiegspunkt;
- `tests2/testconfig.json` ist die einzige MTP-Konfiguration und wird als
  `<AssemblyName>.testconfig.json` in das Testartefakt übernommen;
- Skips und Testwarnungen sind echte Fehler;
- die aktuelle F1a-Untergrenze von 89 Fällen wird vom CI-Aufruf fail-closed erzwungen;
- native Testpakete und deren Versionen bleiben auf Projekte unter `tests2/**` begrenzt;
- ein zentraler MSBuild-Guard lehnt verbotene Runner, Adapter und Testlogger anhand ihrer exakten
  Paketidentität ordinal-ignore-case ab und kann von einem Projekt nicht geleert werden;
- das gemeinsame Testkonfigurationsmodell ist typisiert, secret-frei und ausschließlich über die
  zentrale JSON-Datei konfiguriert; C# enthält Schema und Validierung, keine fachlichen Defaults;
- das geerbte `ViciOne.ServiceBus.TestFramework` ist nicht paketierbare Migrationsquelle;
- der fehlgeleitete Python-Policy-Validator von Team 1 ist vollständig entfernt und darf nicht
  rekonstruiert werden.

## 2. Vollständige Ausführung

Alle .NET-Befehle liefen am 22. August 2026 mit SDK `10.0.302`. Restore und Build verwendeten
deaktivierte Build-Server-Wiederverwendung und Locked Restore; der MTP-Testlauf lief ohne Filter.

| Gegenstand | Ergebnis |
|---|---|
| Produkt-Locked-Restore | Exit 0 |
| Engineering-Locked-Restore | Exit 0 |
| Unit-Locked-Restore | Exit 0 |
| Produkt-Release-Build, `--no-restore --no-incremental` | Exit 0; 0 Warnungen; 0 Fehler |
| Engineering-Release-Build, `--no-restore --no-incremental` | Exit 0; 0 Warnungen; 0 Fehler |
| Unit-Release-Build, `--no-restore --no-incremental` | Exit 0; 0 Warnungen; 0 Fehler |
| unfiltrierter `dotnet test --solution ... --minimum-expected-tests 89` | Exit 0; 89 gesamt; 89 grün; 0 rot; 0 übersprungen |
| `dotnet format ... --verify-no-changes --include tests2` | Exit 0 |

Der native Lauf erzeugte zusätzlich einen maschinenlesbaren CTRF-Rohnachweis. Dessen Summary enthält
`tests=89`, `passed=89`, `failed=0`, `pending=0`, `skipped=0` und `other=0`.

Restore und Build liefen lokal seriell (`-m:1`), weil ein fremder, über Tage hängender
VS-Code-Restore die parallelen MSBuild-Knoten blockiert hatte. Das ist ausschließlich eine lokale
Werkzeugmaßnahme; CI und Repositoryarchitektur bleiben parallelisierbar.

## 3. Rohhashes

| Artefakt | SHA-256 |
|---|---|
| `artifacts/f1a-final3/00-product-locked-restore.binlog` | `b6e96194209f5563d91f59d183548084f9dedc6b81aac0380f8bbe393a64e169` |
| `artifacts/f1a-final3/01-engineering-locked-restore.binlog` | `1796277c34e6db6f29933df8f1b75b1843e84ca327d675d1bb297e4550f4b7b6` |
| `artifacts/f1a-final3/02-unit-locked-restore.binlog` | `c242d26012610c846b40c16b9e8ea271d0c86a4bce7e7cde0b598c30faabf448` |
| `artifacts/f1a-final3/20-product-release-build.binlog` | `2284c67bf771dc240b5fcb5a5404c57223d3a8fb085f5291a40f76071d1b0bb7` |
| `artifacts/f1a-final3/21-engineering-release-build.binlog` | `c98685ade9b49af449b0920b9bd0f3a0a9f0156a1a78f92bfb079c76889370c6` |
| `artifacts/f1a-final3/22-unit-release-build.binlog` | `cb9fafaa87f96c096ab9483a049f52e84710e6d8f0379320e4b999e6ccd504d7` |
| `artifacts/f1a-final3/30-unit-native-test.ctrf.json` | `2f9c8444c23e9db237e6168de834e656c1df1a2122d553a7000b26ea271afd40` |
| `tests2/testconfig.json` | `3d0eec5f58c6d4dc6aba60fe8c39ac2864a1a5b8cce1c1d69d1b9b750ca4342f` |
| `tests2/testsettings.json` | `5976ece7528420ac54abafd5e9f18ad378174e78a11412bc0545fbf374b55a23` |
| `tests2/Directory.Build.props` | `7ec422b68e85620c259ff11152784237ab5c97110174ced2f8cc5b3f8ca84de8` |
| `tests2/Directory.Build.targets` | `c925885377d337e7ab8446e85c508517ab9babfff0d22383e4ae32967e22f6f4` |
| Architektur-Lockfile | `1e0b30183aaed716fe8a5d2d7db2d57affad6dbab67f894dadcb81dbe5fde92b` |
| Infrastruktur-Lockfile | `7c24c052dca12ec4b4ac0e3582eb9a68af0beaf2282f6ac296716d7442e1c14b` |

Binlogs und CTRF liegen absichtlich im ignorierten Artefaktbaum; ihre Hashes binden die ausgeführten
Läufe, ohne generierte Dateien als Quellcode einzuchecken.

## 4. Paket- und Projektgraph

| Projekt | aufgelöste Paketknoten | verbotene Identitäten |
|---|---:|---:|
| `ViciOne.ServiceBus.Architecture.Tests` | 41 | 0 |
| `ViciOne.ServiceBus.Tests.Infrastructure` | 10 | 0 |

Die Architekturtests prüfen zusätzlich alle aktuellen Projekte unter `src/**` auf eine unerlaubte
Referenz nach `tests2/**` und auf den nativen Testpaketmarker. Der Supportcode enthält transitiv kein
xUnit-, NUnit-, MTP- oder ArchUnitNET-Paket.

## 5. Fail-closed-Gegenproben

Zwei temporäre C#-Mutationen wurden vor dem letzten Paketguard-Fix jeweils gebaut, ausgeführt und
danach vollständig entfernt. Die betroffenen Runner- und Konfigurationsdateien sind zwischen diesem
Nachweis und dem Prüfgegenstand bytegleich:

| Mutation | Gemessenes Ergebnis |
|---|---|
| ein `[Fact(Skip=...)]` | `FAIL_SKIP`; MTP Exit 2 |
| `TestContext.Current.AddWarning(...)` | `FAIL_WARN`; MTP Exit 2 |
| projektseitig geleerte Paketpolicy plus `NUnit` | ausschließlich `VOSBT006`; MSBuild Exit 1 |
| abweichend geschriebenes `nunit` | ausschließlich `VOSBT006`; MSBuild Exit 1 |
| je ein entfernter Pflichtschlüssel `Profile`, `OperationTimeout` oder `LocalInfrastructure` | Validierung rot für den jeweils fehlenden Schlüssel |

Der abschließende frische Build nach Entfernung der Mutationen enthält genau 89 reguläre Fälle.
MSBuild- und Architekturregeln sichern außerdem Elternimports, Testprojektklassifikation,
MTP-only-Einstieg, zentrale Konfiguration, verbotene Runner/Adapter, Paketclosure,
Solutionmitgliedschaft, Produkt→Test-Richtung und SDK-Sprachversion ab. Der Produktgraph enthält
null Referenzen auf `GitHubActionsTestLogger`, `Microsoft.NET.Test.Sdk` oder
`xunit.runner.visualstudio`.

## 6. Testqualität

- 67 `[Fact]`-Methoden plus 22 ausgeführte Datenzeilen aus sechs `[Theory]`-Methoden ergeben 89 Fälle;
- 132 statische xUnit-Assert-Aufrufstellen; delegierte ArchUnitNET-Regeln werden durch gewöhnliche
  xUnit-Assertions ausgewertet;
- der gesamte C#-Bestand unter `tests2/**` wurde gelesen;
- keine assertionfreien Testkörper, Skips, Sleeps, Zufalls-/Uhrzeitabhängigkeit, `.Result`, `.Wait()`,
  breite Exception-Fänger oder VSTest-/NUnit-Verdrängung im neuen Bestand;
- Test- und Supportstruktur folgen den geprüften Produkt- und Architekturbereichen; F1a selbst
  beansprucht noch keine vollständige Migration der geerbten Verhaltenskohorten.

## 7. Noch ausstehend

F1a ist auf dem technischen Prüfcommit dynamisch grün. Vor formeller Annahme folgen die
Belegattestierung und zwei unabhängige read-only Reviews genau dieses eingefrorenen Commits. F1b,
Verhaltenskohorten, Löschung ersetzter Alt-Tests, Push und Cloudausführung wurden nicht begonnen.
