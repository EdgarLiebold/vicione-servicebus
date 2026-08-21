# F1a-Beleg — native xUnit-4-/MTP-v2-Grundlage

Der erste Team-1-Kandidat `5996d10a` wurde nicht angenommen. Der Product Owner übertrug die
begrenzte Korrektur direkt an den Lead Architect. Produktverhalten unter `src/**` blieb unverändert.

Technischer Prüfgegenstand ist ausschließlich Commit
`5b47c9ba9b9e30de0d48d268ef65856d63a6f724` mit Tree
`781e7b8f805036e4637cc69fe9543bdfa22dee32`. Dieser Bericht wird nachgelagert committed und
attestiert deshalb seinen technischen Parent; er behauptet keinen selbstreferenziellen Test des
späteren Belegcommits.

## 1. Ergebnis

- xUnit 4 läuft ausschließlich über den Microsoft-Testing-Platform-v2-Einstiegspunkt;
- `tests2/testconfig.json` ist die einzige MTP-Konfiguration und wird als
  `<AssemblyName>.testconfig.json` in das Testartefakt übernommen;
- Skips und Testwarnungen sind echte Fehler;
- die aktuelle F1a-Untergrenze von 86 Fällen wird vom CI-Aufruf fail-closed erzwungen;
- native Testpakete und deren Versionen bleiben auf Projekte unter `tests2/**` begrenzt;
- ein zentraler MSBuild-Guard lehnt verbotene Runner, Adapter und Testlogger anhand ihrer exakten
  Paketidentität ab;
- das gemeinsame Testkonfigurationsmodell ist typisiert, secret-frei und zentral änderbar;
- das geerbte `ViciOne.ServiceBus.TestFramework` ist nicht paketierbare Migrationsquelle;
- der fehlgeleitete Python-Policy-Validator von Team 1 ist vollständig entfernt und darf nicht
  rekonstruiert werden.

## 2. Vollständige Ausführung

Alle .NET-Befehle liefen am 21. August 2026 mit SDK `10.0.302`, deaktivierter
Build-Server-Wiederverwendung, Locked Restore und ohne Testfilter.

| Gegenstand | Ergebnis |
|---|---|
| Produkt-Locked-Restore | Exit 0 |
| Engineering-Locked-Restore | Exit 0 |
| Unit-Locked-Restore | Exit 0 |
| Produkt-Release-Build, `--no-restore --no-incremental` | Exit 0; 0 Warnungen; 0 Fehler |
| Engineering-Release-Build, `--no-restore --no-incremental` | Exit 0; 0 Warnungen; 0 Fehler |
| Unit-Release-Build, `--no-restore --no-incremental` | Exit 0; 0 Warnungen; 0 Fehler |
| unfiltrierter `dotnet test --solution ... --minimum-expected-tests 86` | Exit 0; 86 gesamt; 86 grün; 0 rot; 0 übersprungen |
| `dotnet format ... --verify-no-changes --include tests2` | Exit 0 |

Der native Lauf erzeugte zusätzlich einen maschinenlesbaren CTRF-Rohnachweis. Dessen Summary enthält
`tests=86`, `passed=86`, `failed=0`, `pending=0`, `skipped=0` und `other=0`.

Restore und Build liefen lokal seriell (`-m:1`), weil ein fremder, über Tage hängender
VS-Code-Restore die parallelen MSBuild-Knoten blockiert hatte. Das ist ausschließlich eine lokale
Werkzeugmaßnahme; CI und Repositoryarchitektur bleiben parallelisierbar.

## 3. Rohhashes

| Artefakt | SHA-256 |
|---|---|
| `artifacts/f1a-final2/00-product-locked-restore.binlog` | `186b2403523cf97a43a27b14eddc29b8e7ecaeb9bbd94429e29f08c145dce404` |
| `artifacts/f1a-final2/01-engineering-locked-restore.binlog` | `12a35b644cd0d96092e44d576b83f6149bda4c75d75c96dcfe041cc738fabd98` |
| `artifacts/f1a-final2/02-unit-locked-restore.binlog` | `b7a25d33458e093e57039d8dacfe9d3e9ad23cc882c99a1ee071889ee760c342` |
| `artifacts/f1a-final2/20-product-release-build.binlog` | `eb3bf09a2c63f48f2ce18f3eb16fab0ad20b11ea44be59e0746daa4e5f5adc29` |
| `artifacts/f1a-final2/21-engineering-release-build.binlog` | `167e3b0a164967a8a39d5bc75dda04451155301f332a26cf73912038610850bf` |
| `artifacts/f1a-final2/22-unit-release-build.binlog` | `c6c0b406ab5a628080029352748522568f1fb12049da790431fbb7a3f15aea19` |
| `artifacts/f1a-final2/30-unit-native-test.ctrf.json` | `f24d8f1b8d00b0788fa7bdc789d904cbd1217791becac07ba8f1830329a5048e` |
| `tests2/testconfig.json` | `3d0eec5f58c6d4dc6aba60fe8c39ac2864a1a5b8cce1c1d69d1b9b750ca4342f` |
| `tests2/testsettings.json` | `5976ece7528420ac54abafd5e9f18ad378174e78a11412bc0545fbf374b55a23` |
| `tests2/Directory.Build.props` | `fdc34341ebe93440aa6816091d33bb2cf8530c23a3ee0e53b5a5563eb3cbbbc9` |
| `tests2/Directory.Build.targets` | `9ec72614d81fdd7d2335e296a60e08a843679e5ce219e3663205982876c6d610` |
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
| `TngTech.ArchUnitNET.xUnitV3` als direkte Referenz am finalen Commit | ausschließlich `VOSBT006`; MSBuild Exit 1 |

Der abschließende frische Build nach Entfernung der Mutationen enthält genau 86 reguläre Fälle.
MSBuild- und Architekturregeln sichern außerdem Elternimports, Testprojektklassifikation,
MTP-only-Einstieg, zentrale Konfiguration, verbotene Runner/Adapter, Paketclosure,
Solutionmitgliedschaft, Produkt→Test-Richtung und SDK-Sprachversion ab. Der Produktgraph enthält
null Referenzen auf `GitHubActionsTestLogger`, `Microsoft.NET.Test.Sdk` oder
`xunit.runner.visualstudio`.

## 6. Testqualität

- 69 `[Fact]`-Methoden plus 17 ausgeführte Datenzeilen aus vier `[Theory]`-Methoden ergeben 86 Fälle;
- 129 statische xUnit-Assert-Aufrufstellen; delegierte ArchUnitNET-Regeln werden durch gewöhnliche
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
