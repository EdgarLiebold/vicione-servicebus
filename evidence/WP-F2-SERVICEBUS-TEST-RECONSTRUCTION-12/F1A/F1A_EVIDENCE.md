# F1a-Beleg — korrigierte native xUnit-4-/MTP-v2-Grundlage

Der erste Team-1-Kandidat `5996d10a` wurde nach vollständiger Lead-Prüfung nicht angenommen. Der PO
hat die begrenzte F1a-Korrektur direkt dem Lead Architect übertragen. Produktverhalten unter
`src/**` blieb unverändert.

## 1. Behobene Ablehnungsgründe

- Test-only CPM-Versionen sind auf `ViciOneNativeTestTree` begrenzt. Der bestehende Produkt-Lockfile
  ist byteinhaltlich wieder identisch mit dem Zustand vor F1a; der vollständige Produkt-Restore ist
  im Locked Mode grün.
- Die ungültigen leeren Profil-Solutions wurden entfernt. Ein Profil wird erst mit seinem ersten
  ausführbaren Testprojekt materialisiert.
- Die frameworkneutrale Bibliothek heißt eindeutig
  `ViciOne.ServiceBus.Tests.Infrastructure`; sie kollidiert nicht mehr mit der ausgelieferten
  Produktoberfläche `ViciOne.ServiceBus.Testing`.
- Profil, lokale Endpunkte und externe Provider werden fail-closed typisiert und validiert.
- Alle ArchUnitNET-Frameworkadapter sind verboten; nur der Kern ist erlaubt.
- Paketclosure und Projektreferenzen werden getrennt gezählt.
- Projekt-, Solution-, Sprachversions- und Produkt→Test-Grenzen werden aus dem echten Graph geprüft.
- Aktive Build-, Test- und Arbeitsdokumente beschreiben nur den gemessenen aktuellen Stand.

## 2. Vollständige Belegkette

Alle Befehle liefen mit SDK `10.0.302`, deaktivierter Build-Server-Wiederverwendung und ohne
Testfilter.

| Gegenstand | Ergebnis |
|---|---|
| `dotnet restore ViciOne.ServiceBus.slnx --locked-mode --disable-build-servers` | Exit 0 |
| `dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode --disable-build-servers` | Exit 0 |
| `dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode --disable-build-servers` | Exit 0 |
| Produkt-Release-Build, `--no-restore --no-incremental` | Exit 0, 0 Warnungen, 0 Fehler |
| Engineering-Release-Build, `--no-restore --no-incremental` | Exit 0, 0 Warnungen, 0 Fehler |
| Unit-Release-Build, `--no-restore --no-incremental` | Exit 0, 0 Warnungen, 0 Fehler |
| `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore` | Exit 0; 76 gesamt, 76 grün, 0 rot, 0 übersprungen |
| `dotnet format ... --verify-no-changes --include tests2` | Exit 0 |
| `git diff --check` | Exit 0 |

Der nicht auf `tests2` begrenzte Formatlauf meldet geerbte Produktformatierung; er ist kein Befund
gegen den neuen Bestand und wurde nicht durch F1a verändert.

## 3. Rohhashes

| Artefakt | SHA-256 |
|---|---|
| `artifacts/f1a-lead-correction/10-unit-locked-restore.binlog` | `929fa7acdd0515bb5fd3f3a1317d2fcccc231c489580b488a226c05c262faf6e` |
| `artifacts/f1a-lead-correction/20-product-release-build.binlog` | `181bf0c40175a26f7920f228a9ad3acdf7b1a8352e3437a1431ae5ad0e368be9` |
| `artifacts/f1a-lead-correction/21-engineering-release-build.binlog` | `bd651ac9e87fc40006745f60002af4626ba53ea5ea94bdd28c16ebfd99adc32b` |
| `artifacts/f1a-lead-correction/22-unit-release-build.binlog` | `2f2a47ff51c61caba87835726534193c90fdb7d035e031f6f3c54fa8a9025553` |
| `tests2/xunit.runner.json` | `c95b68567892731e43a8f3b70d160cb945f215e18a0d6fb27f250497e58a609e` |
| `tests2/testsettings.json` | `5976ece7528420ac54abafd5e9f18ad378174e78a11412bc0545fbf374b55a23` |
| `tests2/Directory.Build.props` | `3fc9d92a50dbe913798e862d0f4ba64beed4ef4f6ee12e7113f671841261da2a` |
| `tests2/Directory.Build.targets` | `b0de3c87bdb8e110fd8772cb6e124b77a5987acacbfafd2c9eab11e53056d07a` |
| Architektur-Lockfile | `1e0b30183aaed716fe8a5d2d7db2d57affad6dbab67f894dadcb81dbe5fde92b` |
| Infrastruktur-Lockfile | `7c24c052dca12ec4b4ac0e3582eb9a68af0beaf2282f6ac296716d7442e1c14b` |

Binlogs liegen absichtlich im ignorierten Artefaktbaum; ihre Hashes binden den ausgeführten Lauf.

## 4. Aufgelöste Graphen

| Projekt | echte Paketknoten | Projektknoten | verbotene Identitäten |
|---|---:|---:|---:|
| `ViciOne.ServiceBus.Architecture.Tests` | 41 | 3 | 0 |
| `ViciOne.ServiceBus.Tests.Infrastructure` | 10 | 0 | 0 |

Die drei Projektknoten sind Core, Abstractions und Testinfrastruktur; sie werden nicht mehr als
NuGet-Pakete ausgegeben. Alle 22 aktuellen Projekte unter `src/**` werden auf fehlende Referenzen in
`tests2/**` geprüft.

## 5. Isolierte Gegenproben

Die Proben liefen ausschließlich in `/private/tmp/vsb-f1a-correction.HzLUUz`; der kanonische
Arbeitsbaum wurde nicht für Sabotage verändert.

| Eingriff | Erwartetes und gemessenes Scheitern |
|---|---|
| `LangVersion` in Root-Props pinnen | `LanguageVersion_IsPinnedOnlyForTheTwoRoslynComponents`, MTP Exit 2 |
| leeres `ViciOne.ServiceBus.Tests.External.slnx` hinzufügen | `EveryMaterializedProfileSolution_HasAnExecutableTestProject`, MTP Exit 2 |
| Produktprojekt auf Testinfrastruktur referenzieren | `EveryProductProject_StaysIndependentOfTheNativeTestTree`, MTP Exit 2 |
| `TngTech.ArchUnitNET.xUnitV3` direkt aufnehmen | `VOSBT006` |
| Elternimport aus `tests2/Directory.Build.props` entfernen | `VOSBT001` |
| Elternimport aus `tests2/Directory.Build.targets` entfernen | `VOSBT002` |
| `failSkips` auf `false` setzen | zwei Runner-Konfigurationsprüfungen rot, MTP Exit 2 |
| zweite Runnerkonfiguration aufnehmen | `VOSBT008` |
| einen Test mit `Skip` markieren | `FAIL_SKIP`, 1 fehlgeschlagen, 0 übersprungen, MTP Exit 2 |

Konfigurationssabotage ist als gewöhnlicher xUnit-Test ausgeführt: unbekanntes Profil, nichtpositiver
Timeout, leere Hosts, Ports 0/65536, externer Selektor im Unitprofil, External ohne Selektor,
External mit Emulator, unbekannte oder doppelte Providerselektion sowie eine fehlende ausgewählte
Providergruppe sind rot auf Vertragsebene.

## 6. Testqualitätsprüfung

- 63 Fakten plus 13 ausgeführte Theory-Zeilen = 76 Fälle;
- 103 explizite Assertions;
- keine assertionfreien Tests, Skips, Sleeps, Zufalls-/Uhrzeitabhängigkeit, `.Result`/`.Wait()` oder
  breite Exception-Fänger im neuen Bestand;
- `find-untested-sources` wurde wie vom Microsoft-Skill verlangt ausgeführt. Sein lexikalischer
  Repositoryscan meldete 3.888 Quellen, 1.083 Testdateien, 591 Paarungen und 3.297 Kandidaten. Diese
  Werte enthalten geerbte/generierte Struktur und sind keine Coverage- oder Vollständigkeitswahrheit;
  R0 bleibt die verbindliche Verhaltenspflichtmenge für die Kohortenmigration.

## 7. Noch ausstehend

F1a bleibt bis zu zwei unabhängigen read-only Reviews des eingefrorenen korrigierten Commits
technisch grün, aber formal nicht angenommen. F1b, Verhaltenskohorten, Altbestandslöschung, Push und
Cloudausführung wurden nicht begonnen.
