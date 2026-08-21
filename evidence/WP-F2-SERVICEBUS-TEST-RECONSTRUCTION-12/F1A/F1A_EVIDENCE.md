# F1a-Beleg — native xUnit-4-/MTP-v2-Grundlage

Der erste Team-1-Kandidat `5996d10a` wurde nicht angenommen. Der Product Owner übertrug die
begrenzte Korrektur direkt an den Lead Architect. Produktverhalten unter `src/**` blieb unverändert.

## 1. Ergebnis

- xUnit 4 läuft ausschließlich über den Microsoft-Testing-Platform-v2-Einstiegspunkt;
- `tests2/testconfig.json` ist die einzige MTP-Konfiguration und wird als
  `<AssemblyName>.testconfig.json` in das Testartefakt übernommen;
- Skips und Testwarnungen sind echte Fehler;
- die aktuelle F1a-Untergrenze von 84 Fällen wird vom CI-Aufruf fail-closed erzwungen;
- native Testpakete und deren Versionen bleiben auf Projekte unter `tests2/**` begrenzt;
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
| unfiltrierter `dotnet test --solution ... --minimum-expected-tests 84` | Exit 0; 84 gesamt; 84 grün; 0 rot; 0 übersprungen |
| `dotnet format ... --verify-no-changes --include tests2` | Exit 0 |
| verbleibende Übergangs-Runner-Selbsttests | 206/206 grün |

Der native Lauf erzeugte zusätzlich einen maschinenlesbaren CTRF-Rohnachweis. Dessen Summary enthält
`tests=84`, `passed=84`, `failed=0`, `pending=0`, `skipped=0` und `other=0`.

## 3. Rohhashes

| Artefakt | SHA-256 |
|---|---|
| `artifacts/f1a-lead-correction/00-product-locked-restore.binlog` | `38bb202b08240e69b6163f6dd6ad2ffdd4d5ddb157786ec1518646fa4ccedca0` |
| `artifacts/f1a-lead-correction/01-engineering-locked-restore.binlog` | `3b1f6c60cf04dc59aadaaa5c3a3b1abd52698377997066cfded0077bb51e8a9c` |
| `artifacts/f1a-lead-correction/02-unit-locked-restore.binlog` | `6bed47896cc51ab9a2522c16f050b30eb7524932e09ddc272d211a10e28da930` |
| `artifacts/f1a-lead-correction/20-product-release-build.binlog` | `22fa144282f8dc72ce219ee6a6eac8269c06638dc0513a0724bb938b97fc23d6` |
| `artifacts/f1a-lead-correction/21-engineering-release-build.binlog` | `a5b5d55010c9745059e8f603fad1de1a914d80f4eeac967f8314ec37e6212724` |
| `artifacts/f1a-lead-correction/22-unit-release-build.binlog` | `bff190a8f8101295b83cb994ffa39a141708a445d59d331dab86eaa11bae9e0c` |
| `artifacts/f1a-lead-correction/30-unit-native-test.ctrf.json` | `1b2bd84a31c6fc89e9b52680e1b378123fef67edac6c0782b71f65aae85af6cc` |
| `tests2/testconfig.json` | `3d0eec5f58c6d4dc6aba60fe8c39ac2864a1a5b8cce1c1d69d1b9b750ca4342f` |
| `tests2/testsettings.json` | `5976ece7528420ac54abafd5e9f18ad378174e78a11412bc0545fbf374b55a23` |
| `tests2/Directory.Build.props` | `7ec422b68e85620c259ff11152784237ab5c97110174ced2f8cc5b3f8ca84de8` |
| `tests2/Directory.Build.targets` | `085abce4579e6be5b35fc441c7b3e2e4b952b8f0c28e358ca6e961ea2b489522` |
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

Zwei temporäre C#-Mutationen wurden jeweils gebaut, ausgeführt und danach vollständig entfernt:

| Mutation | Gemessenes Ergebnis |
|---|---|
| ein `[Fact(Skip=...)]` | `FAIL_SKIP`; 83 grün, 1 fehlgeschlagen; MTP Exit 2 |
| `TestContext.Current.AddWarning(...)` | `FAIL_WARN`; 83 grün, 1 fehlgeschlagen; MTP Exit 2 |

Der abschließende frische Build nach Entfernung beider Mutationen enthält wieder genau 84 reguläre
Fälle. MSBuild- und Architekturregeln sichern außerdem Elternimports, Testprojektklassifikation,
MTP-only-Einstieg, zentrale Konfiguration, verbotene Runner/Adapter, Paketclosure,
Solutionmitgliedschaft, Produkt→Test-Richtung und SDK-Sprachversion ab.

## 6. Testqualität

- 67 `[Fact]`-Methoden plus 17 ausgeführte Datenzeilen aus vier `[Theory]`-Methoden ergeben 84 Fälle;
- 125 statische xUnit-Assert-Aufrufstellen; delegierte ArchUnitNET-Regeln werden durch gewöhnliche
  xUnit-Assertions ausgewertet;
- der gesamte C#-Bestand unter `tests2/**` wurde gelesen;
- keine assertionfreien Testkörper, Skips, Sleeps, Zufalls-/Uhrzeitabhängigkeit, `.Result`, `.Wait()`,
  breite Exception-Fänger oder VSTest-/NUnit-Verdrängung im neuen Bestand;
- Test- und Supportstruktur folgen den geprüften Produkt- und Architekturbereichen; F1a selbst
  beansprucht noch keine vollständige Migration der geerbten Verhaltenskohorten.

## 7. Noch ausstehend

F1a ist technisch grün. Vor formeller Annahme folgen ein sauberer Commit und zwei unabhängige
read-only Reviews genau dieses eingefrorenen Commits. F1b, Verhaltenskohorten, Löschung ersetzter
Alt-Tests, Push und Cloudausführung wurden nicht begonnen.
