# Research — F1a native xUnit-4-/MTP-v2-Testgrundlage

Arbeitspaket `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12`, Direktive `DIR-A0071-START-F1A-03`,
wirksamer Slice `revisions/0002/DEVELOPMENT_SLICE.json` (SHA-256 `4d3e3ebe…7d75`, 92 Schreibscopes).
Ausgangscommit `09512522157898afe4eef2f5708fcb1a9a5e9b1d`, Nachkomme der R0-Baseline
`a6b205c9fc2a29d81968069936148f1ee0a6e1d1`. Arbeitsbaum sauber, `tests2` abwesend.

Vorgehen nach dem Microsoft-Skill `dotnet-test:code-testing-agent`, Kette Research → Plan →
Implement. Umfang ist **broad**, deshalb sind `research.md`, `plan.md` und `status.md` Pflichtbelege.

## 1. Umgebung und zentrale Buildwahrheit

| Gegenstand | Feststellung |
|---|---|
| SDK | `10.0.302`, identisch mit dem Pin in `global.json` |
| Testrunner-Auswahl | `global.json` enthält `"test": { "runner": "Microsoft.Testing.Platform" }` |
| Aufrufform | SDK 10 ⇒ `dotnet test --solution …`, MTP-Argumente direkt, **kein** `--`-Trenner |
| Buildausgabe | `UseArtifactsOutput=true`, `ArtifactsPath=artifacts/sdk`; `/artifacts/` ist ignoriert |
| Restore | `RestorePackagesWithLockFile=true`, `RestoreLockedMode=true` als Wurzelvorgabe |
| Wurzel-`Directory.Build.targets` | erzwingt Lockfile, Lockmode, Paketnotice und TFM-Grenze (`VOSB0001`–`VOSB0008`) |

Die Wurzel-`Directory.Build.targets` setzt `ViciOneProjectIdentity`; die Wurzel-`Directory.Build.props`
setzt `ArtifactsPath`. Beide Eigenschaften eignen sich als Nachweisanker dafür, dass der jeweilige
Elternimport tatsächlich stattgefunden hat.

**Wichtig:** MSBuild importiert nur die *erste* `Directory.Build.props`/`.targets` auf dem Weg nach
oben. Für Projekte unter `tests2/**` ist das die Datei in `tests2/`, nicht die der Wurzel. Der
Elternimport ist deshalb Pflicht und kein Komfort.

## 2. Fallstrick aus dem Bestand — repositoryweiter Testlogger

`Directory.Packages.props` enthält

```xml
<GlobalPackageReference Include="GitHubActionsTestLogger" Version="3.0.5" />
```

ohne Bedingung. Ein `GlobalPackageReference` gilt für **jedes** Projekt des Repositorys. Ohne
Gegenmaßnahme gelangte damit ein nach `REQ-TEST-203` verbotenes Paket automatisch in jedes neue
`tests2`-Projekt. Der geerbte Baum unter `tests/**` benötigt es weiterhin, es darf also nicht
ersatzlos entfallen. Lösung: Die Itemgruppe wird an eine Eigenschaft gebunden, die ausschließlich
`tests2/Directory.Build.props` setzt. Itembedingungen werten spät aus und sehen diese Eigenschaft
(Skill `dotnet-msbuild:directory-build-organization`, Abschnitt AP-21).

## 3. Geerbter Testbestand — nur Kontext, keine Vorlage

`tests/**` verwendet NUnit 4 mit `Microsoft.NET.Test.Sdk` und `NUnit3TestAdapter`, also VSTest.
Das ist genau die Kombination, die im neuen Baum verboten ist. Der Bestand bleibt in F1a
**unverändert**; er dient nur als Verhaltensevidenz für spätere Kohorten, niemals als Bauform.

Bestehende Quellprojekte: 22 unter `src/**`. Bestehende Testprojekte: 16 unter `tests/**`.

## 4. Paketprüfung am Feed

| Paket | Feststellung |
|---|---|
| `xunit.v3.mtp-v2` `4.0.0` | stabil vorhanden; Metapaket über `xunit.v3.core.mtp-v2` `[4.0.0]`, `xunit.v3.assert` `[4.0.0]`, `xunit.analyzers` `2.0.0`; Zielgruppe `net8.0`, kompatibel zu `net10.0` |
| `TngTech.ArchUnitNET` | letzte **stabile** Version `0.13.4` mit `lib/netstandard2.0`; `2.1.0-draft` ist Prerelease und scheidet wegen des Verbots offener/instabiler Versionen aus |

Damit ist genau ein Testeinstiegspaket je ausführbarem Projekt erfüllbar. Ein xUnit-Adapter für
ArchUnitNET wird nicht referenziert.

## 5. Abgrenzung des F1a-Umfangs

Enthalten: zentrale CPM-/MSBuild-/MTP-Konfiguration, eine zentrale `tests2/xunit.runner.json`,
`tests2/Testing/ViciOne.ServiceBus.Testing` (frameworkneutral, nicht ausführbar, nicht packbar, ohne
xUnit), `tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests` (ausführbar, xUnit 4 auf MTP v2,
ArchUnitNET-Kern, kleiner struktureller Smoke-Satz), drei Profil-Solutions, Engineering-Solution,
secret-freie Testkonfiguration, Builddokumentation und F1a-Evidence.

Nicht enthalten: jede Verhaltenskohorte, `tests2/Core/**`-Bebauung, Coveragebindung,
`RequirementCoverageAttribute`, Löschung von `tests/**` oder `src/ViciOne.ServiceBus.TestFramework/**`,
F1b- und C1-Arbeit, Push.

**Disposition zu `dotnet-test:find-untested-sources`:** Das Werkzeug beantwortet, welche Quelldatei
als Nächstes einen Test braucht. F1a erzeugt bewusst **keinen** quellbezogenen Verhaltenstest; sein
Ergebnis hätte in diesem Checkpoint keinen zulässigen Abnehmer, und danach zu handeln wäre die in
F1a verbotene Kohortenmigration. Der Lauf gehört deshalb an den Beginn der ersten Kohortenwelle und
ist dort Pflicht. Das ist eine begründete Terminierung, kein Auslassen.

## 5a. Befund während der Umsetzung — Konfiguration im Solution-Build

Ein Projekt, das eine Solution nur über eine `ProjectReference` erreicht, nimmt nicht an der
Konfigurationszuordnung der Solution teil. Der Build der Profil-Solution mit `-c Release` erzeugte
dadurch ein Release-Testartefakt mit **Debug**-Produktassemblies: Das Profil hätte Release behauptet
und gegen Debug-Code gemessen. Reparaturpunkt ist die Mitgliedschaft der referenzierten
Produktprojekte in der Profil-Solution; der Regressionsschutz liest die
`AssemblyConfigurationAttribute` der geladenen Assemblies und vergleicht sie mit der Testassembly.

## 6. Akzeptanzcheckliste

| # | Anforderung | Quelle |
|---|---|---|
| A1 | Jedes ausführbare Testprojekt referenziert direkt und ausschließlich `xunit.v3.mtp-v2` `4.0.0` als Testeinstieg | 0007, `REQ-TEST-203` |
| A2 | `GitHubActionsTestLogger`, `Microsoft.NET.Test.Sdk`, NUnit, VSTest, ArchUnitNET-xUnit-Adapter fehlen im **ausgewerteten** `tests2`-Graph | 0007, `REQ-TEST-203` |
| A3 | Genau eine zentrale `tests2/xunit.runner.json` gelangt über die verschachtelte MSBuild-Konfiguration in jedes ausführbare Testartefakt; keine Projektkopie | 0007 Nr. 2, `REQ-TEST-203` |
| A4 | Übersprungene Tests und Warnungen sind Fehler | Vertrag §2, `REQ-TEST-203` |
| A5 | Verschachtelte `Directory.Build.props`/`.targets` importieren die Eltern ohne stillen `Exists`-Fallback; Entfernen jedes Imports scheitert aus **eigenem** Grund | 0007, Vertrag §5 |
| A6 | Paketversionen ausschließlich in der Wurzel-`Directory.Packages.props` | Vertrag §5, `REQ-TEST-205` |
| A7 | Testprojekte sind `net10.0`, nicht packbar; `LangVersion` kommt als `14.0` aus dem SDK und wird nirgends gepinnt | Vertrag §5, `REQ-TEST-205`, 0009 Nr. 1 |
| A8 | `ViciOne.ServiceBus.Testing` ist nicht ausführbar, nicht packbar, ohne xUnit und ohne handgepflegten Produkt-Assembly-Katalog | 0004 Nr. 6, 0007 Nr. 3 |
| A9 | Kein Testartefakt gelangt in einen Produktgraph (Pack/Publish) | `REQ-TEST-205` |
| A10 | Ein typisierter Konfigurationsowner, **ein** zentral deklarierter User-Secrets-Store, `__` wird in den Konfigurationspfad übersetzt, Anforderungen über typisierte Gruppen statt freier Schlüssel; kein Test mutiert Prozessumgebung | `REQ-TEST-206`, 0009 Nr. 2/3, 0010 Nr. 1 |
| A11 | Jede Architektur-/Smokeprüfung untersucht eine reale kompilierte Assembly oder einen tatsächlich ausgewerteten MSBuild-Graphen | 0004 Nr. 5, 0007 Nr. 4 |
| A12 | Keine zweite Bestands-, Runner-, Zähl- oder Verdictwahrheit; MTP-Exitcode und xUnit sind alleinige Urteilsquelle | 0007, `REQ-TEST-202/203` |
| A13 | Belegkette: locked Restore → Release-Build `--no-restore --no-incremental` → unfiltrierter Test `--no-build --no-restore`, je mit eindeutigem Binlogpfad und Exitcode | 0007 |
| A14 | Graphauswertung über `-graphBuild` beziehungsweise gezielte `-getItem`/`-getProperty`, nicht über `-graph` | 0007 |
| A15 | Kein `tests/**`, kein `src/**`-Produktverhalten, keine Kohorte, kein F1b/C1, kein Push | 0007 |
