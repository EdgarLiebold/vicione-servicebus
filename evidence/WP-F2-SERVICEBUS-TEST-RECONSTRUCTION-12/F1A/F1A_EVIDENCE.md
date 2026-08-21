# F1a-Beleg — native xUnit-4-/MTP-v2-Testgrundlage

Arbeitspaket `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12`, wirksamer Slice `revisions/0002`
(SHA-256 `4d3e3ebe55a2a6cfa07e647754723ecfc3436db2f91baf3b3a6d059328ea7d75`).
Umgesetzte Direktiven: `DIR-A0071-START-F1A-03`, `-LIVE-CORRECTION-01` bis `-04`.

## 1. Belegkette in der angeordneten Reihenfolge

| # | Befehl | Exitcode |
|---|---|---|
| 1 | `dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx /bl:artifacts/f1a-evidence/10-locked-restore.binlog` | `0` |
| 2 | `dotnet build ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-restore --no-incremental /bl:artifacts/f1a-evidence/11-release-build.binlog` | `0` |
| 3 | `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore` | `0` |

Restore läuft im Locked Mode (Repositoryvorgabe). Der Build meldet **0 Warnungen, 0 Fehler**.
Der Testlauf ist unfiltriert: **52 gesamt, 52 erfolgreich, 0 fehlgeschlagen, 0 übersprungen**.

Genau ein normaler Restore hat zuvor die neuen Lockfiles erzeugt
(`-p:RestoreLockedMode=false`); die Belegkette selbst verwendet ausschließlich den Locked Restore.

## 2. Rohe Artefakthashes

| Artefakt | SHA-256 |
|---|---|
| `artifacts/f1a-evidence/10-locked-restore.binlog` | `a1aea2971f0881b0e2ee34762dea31f6a9b3e5d3e8c989a9b2a166b9f598e047` |
| `artifacts/f1a-evidence/11-release-build.binlog` | `33f8eb6816090a931e82c503b5b57cb1742ee21db1e832d8a7288affb7c49558` |
| `tests2/xunit.runner.json` | `c95b68567892731e43a8f3b70d160cb945f215e18a0d6fb27f250497e58a609e` |
| `tests2/testsettings.json` | `156697be7cadb5c6a0463732bd70605cf241c4fdfdf3279dc601af1cb2d0dfd2` |
| `tests2/Directory.Build.props` | `3fc9d92a50dbe913798e862d0f4ba64beed4ef4f6ee12e7113f671841261da2a` |
| `tests2/Directory.Build.targets` | `42239d265f9ae2a199a0839532ec7295b9c8e4c66c8a3e4dd209be1be9f40f75` |

Binlogs liegen unter der ignorierten Ausgabefläche `artifacts/` und sind nicht getrackt; ihre
Hashes stehen hier, damit der Lauf identifizierbar bleibt.

## 3. Aufgelöste Paketclosure der neuen Projekte

| Projekt | Pakete in der Closure | verbotene Identitäten | xUnit-Pakete |
|---|---|---|---|
| `ViciOne.ServiceBus.Architecture.Tests` | 45 | keine | `xunit.v3.mtp-v2` und dessen Kette |
| `ViciOne.ServiceBus.Testing` | 11 | keine | **keine** |

Für MTP v2 erforderlich und bewusst **nicht** gesperrt: `Microsoft.Testing.Platform`,
`Microsoft.Testing.Platform.MSBuild`, `Microsoft.Testing.Extensions.Telemetry`,
`Microsoft.Testing.Extensions.TrxReport.Abstractions`. Gesperrt ist der VSTest-Bridge
`Microsoft.Testing.Extensions.VSTestBridge`, nicht die Plattform.

## 4. Neutralität der geänderten Lockfiles

Betroffen ist genau eine bestehende Datei: `src/ViciOne.ServiceBus/packages.lock.json`.

```
net10.0: Closure gleich = True (16 vorher, 16 nachher)
         geaenderte aufgeloeste Version = 0
         geaenderter contentHash        = 0
         geaenderte Klassifikation      = Microsoft.Extensions.Configuration       Transitive -> CentralTransitive
                                          Microsoft.Extensions.Configuration.Binder Transitive -> CentralTransitive
```

Damit ist ausschließlich die erwartete zentrale CPM-Klassifikation gewechselt.

## 5. Sabotageproben — jede scheitert aus ihrem eigenen Grund

| ID | Eingriff | Ergebnis |
|---|---|---|
| M1a | Elternimport aus `tests2/Directory.Build.props` entfernt | `VOSBT001`, allein |
| M1b | Elternimport aus `tests2/Directory.Build.targets` entfernt | `VOSBT002`, allein |
| M2 | echte Paketidentität `TngTech.ArchUnitNET.xUnit` aufgenommen | `VOSBT006` |
| M3 | kanonische Runner-Konfiguration auf `failSkips: false` geändert | `CanonicalConfiguration_TurnsSkipsAndWarningsIntoFailures` rot |
| M4 | zweite Runner-Konfiguration im Projekt verdoppelt | `VOSBT008` |
| M5 | einen Test mit `Skip` versehen | Lauf rot, **0 übersprungen**, 1 fehlgeschlagen |

Zu M1a: Ohne besondere Reihenfolge scheiterte der Build zunächst mit `VOSB0001`
(„RestorePackagesWithLockFile off") — einer *Folge* des fehlenden Imports, nicht seiner Ursache.
Die Importprüfung läuft deshalb als `InitialTargets` vor jedem anderen Target, damit die
spezifische Diagnose gewinnt. Eine erste Messung meldete zusätzlich `VOSB0001`/`VOSB0008` bei M1b;
das war ein Fehler der Messung, nicht des Tors: Der Suchausdruck traf die Zeichenkette
`VOSB0001-VOSB0008` im Meldungstext von `VOSBT002`.

Jede Probe wurde einzeln angewendet und vollständig zurückgenommen; der Kontrolllauf danach ist
grün (52/52, 0 Warnungen, 0 Fehler).

## 6. Befund während der Umsetzung — Konfiguration im Solution-Build

Ein Projekt, das eine Solution nur über eine `ProjectReference` erreicht, nimmt nicht an der
Konfigurationszuordnung der Solution teil. `dotnet build <Profil> -c Release` erzeugte dadurch ein
**Release**-Testartefakt mit **Debug**-Produktassemblies — das Profil hätte Release behauptet und
gegen Debug-Code gemessen.

Kausalnachweis: Der Projektbuild propagierte Release korrekt, der Solutionbuild nicht; nach Aufnahme
der referenzierten Produktprojekte als Solution-Mitglieder ist die Ausgabe bytegleich mit der
Release-Ausgabe. Regressionsschutz ist `ProductAssemblies_AreBuiltInTheSameConfigurationAsTheTests`,
das die `AssemblyConfigurationAttribute` der geladenen Assemblies mit der Testassembly vergleicht.

## 7. Nicht erbrachte Nachweise

- Die zwei unabhängigen read-only Reviews des vollständigen integrierten Diffs stehen aus.
- Die Profile `LocalIntegration` und `External` sind bewusst leer; ein Lauf gegen sie endet mit dem
  MTP-Null-Test-Exitcode 8. Das bleibt sichtbar offen und wird nicht in Grün umgedeutet.
- Dass die Einträge der Sperrliste die real veröffentlichten NuGet-Identitäten sind, ist offline
  nicht beweisbar und wird nicht behauptet; belegt ist die Wirksamkeit gegen die echte Identität
  `TngTech.ArchUnitNET.xUnit` durch Probe M2.
