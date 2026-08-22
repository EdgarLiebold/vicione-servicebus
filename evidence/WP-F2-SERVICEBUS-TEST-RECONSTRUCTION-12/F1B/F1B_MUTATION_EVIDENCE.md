# F1b-Mutationsnachweis

Alle sechs Mutanten wurden am 22. August 2026 einzeln aus
`87dd0fa3682dbfdc0fe1bf626d72747948eb0be1` erzeugt. Fünf Kopien lagen unter
`/private/tmp/vsb-f1b-evidence.3atzA4/<case>`. Der präzisierte
`missing-projected-method`-Fall lag unter `/private/tmp/vsb-f1b-missing-method-final`. Jede Kopie
wurde nach dem Lauf vollständig gelöscht. Nur die ignorierten Rohlogs und Binlogs unter
`artifacts/f1b-final/mutations/` blieben erhalten.

## Gemeinsame Befehle

Die fünf gemeinsamen Kopien entstanden durch:

```bash
git archive --format=tar --output=/private/tmp/vsb-f1b-evidence.3atzA4/source.tar \
  87dd0fa3682dbfdc0fe1bf626d72747948eb0be1
mkdir -p /private/tmp/vsb-f1b-evidence.3atzA4/<case>
tar -xf /private/tmp/vsb-f1b-evidence.3atzA4/source.tar \
  -C /private/tmp/vsb-f1b-evidence.3atzA4/<case>
```

Die präzisierte Methodenentfernung entstand separat durch:

```bash
git archive --format=tar \
  --output=/private/tmp/vsb-f1b-missing-method-final.tar \
  87dd0fa3682dbfdc0fe1bf626d72747948eb0be1
mkdir -p /private/tmp/vsb-f1b-missing-method-final
tar -xf /private/tmp/vsb-f1b-missing-method-final.tar \
  -C /private/tmp/vsb-f1b-missing-method-final
```

In jeder Kopie liefen anschließend exakt diese Restore- und Buildformen; `<case>` wurde durch den
jeweiligen Fallnamen ersetzt. Der absolute Binlogpfad schrieb bewusst in den kanonischen ignorierten
Artefaktbaum, bevor die Kopie gelöscht wurde. Die gezeigte Shellumleitung erfasste stdout und stderr
in demselben Baum:

```bash
cd <root>/<case>
env DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 \
  dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode \
  -p:NuGetAudit=false -m:1 -p:BuildInParallel=false \
  /bl:"/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/artifacts/f1b-final/mutations/<case>-restore.binlog" \
  > "/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/artifacts/f1b-final/mutations/<case>-restore.log" 2>&1

env DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 \
  dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release \
  --no-restore --no-incremental --disable-build-servers -m:1 \
  -p:BuildInParallel=false -p:UseSharedCompilation=false \
  /bl:"/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/artifacts/f1b-final/mutations/<case>-build.binlog" \
  > "/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/artifacts/f1b-final/mutations/<case>-build.log" 2>&1
```

`NuGetAudit=false` galt nur für die isolierten Wegwerfkopien, weil ihr Audit keinen Netzzugang
hatte. Locked Restore und Paketclosure blieben aktiv; die kanonischen positiven Restores liefen
zuvor ohne diese Ausnahme.

`<root>/<case>` war für die fünf gemeinsamen Fälle
`/private/tmp/vsb-f1b-evidence.3atzA4/<case>` und für den präzisierten Methodenfall exakt
`/private/tmp/vsb-f1b-missing-method-final`.

Wenn der Build erwartungsgemäß grün war, lief ausschließlich der Comparator:

```bash
./artifacts/sdk/bin/ViciOne.ServiceBus.Architecture.Tests/release/\
ViciOne.ServiceBus.Architecture.Tests \
  --filter-method \
ViciOne.ServiceBus.Architecture.Tests.Requirements.\
RequirementCoverageProjectionTests.LeadBoundProjection_MatchesCompiledRequirementMetadata \
  --minimum-expected-tests 1 \
  > "/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/artifacts/f1b-final/mutations/<case>-test.log" 2>&1
```

## Exakte Mutationen und Ergebnisse

| Fall | Exakte mechanische Mutation | Restore | Build | Test | Einziger Zielbefund |
|---|---|---:|---:|---:|---|
| `missing-projected-method` | gesamten zusammenhängenden Quellblock aus `[Fact]`, `RequirementCoverage`, `ExecutableTestProject_ReferencesTheSingleTestEntryExactlyOnce` und seinem Testkörper entfernt; Projektion unverändert | 0 | 0 | 2 | unveränderte projizierte Methode löst zu 0 Methoden auf |
| `missing-projection-entry` | erstes JSON-Objekt einschließlich folgendem Komma entfernt | 0 | 0 | 2 | kompilierte Tupelidentität als unprojiziert benannt |
| `unprojected-attributed-method` | `SupportLibrary_IsNotClassifiedAsTestProject` um `[RequirementCoverage("REQ-TEST-999", "unprojected-mutant")]` ergänzt | 0 | 0 | 2 | zusätzliche Tupelidentität benannt |
| `duplicate-requirement-variant` | Variantenschlüssel des zweiten Eintrags durch `architecture-test-project-single-native-test-entry` ersetzt | 0 | 0 | 2 | doppelter Schlüssel `REQ-TEST-203/architecture-test-project-single-native-test-entry` |
| `duplicate-coverage-attribute` | an `AbstractionsTypes_DoNotDependOnCoreTypes` zweites `[RequirementCoverage("REQ-TEST-205", "duplicate-attribute-mutant")]` angefügt | 0 | 1 | nicht ausgeführt | ausschließlich `CS0579`, 0 Warnungen, 1 Fehler |
| `missing-embedded-resource` | `LogicalName` der Projektion von `...ArchitectureFoundation.json` auf `...Missing.json` geändert | 0 | 0 | 2 | erwarteter exakter Ressourcenname fehlt |

Die mechanischen Änderungen wurden mit diesen exakten Befehlen ausgeführt; jeder absolute Pfad
bezeichnet die zugehörige Wegwerfkopie:

```bash
perl -0pi -e 's/\n    \[Fact\]\n    \[RequirementCoverage\("REQ-TEST-203", "architecture-test-project-single-native-test-entry"\)\]\n    public void ExecutableTestProject_ReferencesTheSingleTestEntryExactlyOnce\(\)\n    \{.*?\n    \}\n(?=\n    \[Fact\]\n    public void SupportLibrary_ReferencesNoXunitPackageAtAll)//s' \
  /private/tmp/vsb-f1b-missing-method-final/tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests/Architecture/EvaluatedBuildGraphTests.cs

perl -0pi -e 's/\A\[\n  \{.*?\n  \},\n/[\n/s' \
  /private/tmp/vsb-f1b-evidence.3atzA4/missing-projection-entry/evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12/F1B/projections/architecture-foundation.json

perl -0pi -e 's/    \[Fact\]\n    public void SupportLibrary_IsNotClassifiedAsTestProject/    [Fact]\n    [RequirementCoverage("REQ-TEST-999", "unprojected-mutant")]\n    public void SupportLibrary_IsNotClassifiedAsTestProject/' \
  /private/tmp/vsb-f1b-evidence.3atzA4/unprojected-attributed-method/tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests/Architecture/EvaluatedBuildGraphTests.cs

perl -0pi -e 's/architecture-test-project-single-canonical-platform-config-item/architecture-test-project-single-native-test-entry/' \
  /private/tmp/vsb-f1b-evidence.3atzA4/duplicate-requirement-variant/evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12/F1B/projections/architecture-foundation.json

perl -0pi -e 's/    \[RequirementCoverage\("REQ-TEST-205", "abstractions-do-not-depend-on-core-types"\)\]/    [RequirementCoverage("REQ-TEST-205", "abstractions-do-not-depend-on-core-types")]\n    [RequirementCoverage("REQ-TEST-205", "duplicate-attribute-mutant")]/' \
  /private/tmp/vsb-f1b-evidence.3atzA4/duplicate-coverage-attribute/tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests/Architecture/TestTreeIsolationTests.cs

perl -0pi -e 's/ViciOne\.ServiceBus\.Architecture\.Tests\.Requirements\.F1B\.ArchitectureFoundation\.json/ViciOne.ServiceBus.Architecture.Tests.Requirements.F1B.Missing.json/' \
  /private/tmp/vsb-f1b-evidence.3atzA4/missing-embedded-resource/tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj
```

Die Shell-Exitcodes stehen maschinenlesbar in `RESULTS.tsv`; die SHA-256 aller 29 Rohlogs/Binlogs
und dieser Ergebnistabelle stehen in `F1B_MUTATION_ARTIFACTS.sha256`. Die Hashliste enthält relative
Artefaktpfade und wird mit diesem Nachweis committed; die großen Rohartefakte bleiben generiert und
ignoriert.

Der präzisierte Methodenfall entfernte exakt den Block zwischen dem Coverage-Attribut und dem
abschließenden `Assert.Single(testEntries)` samt Methodengrenzen. Vor Restore wurde mit `rg` belegt,
dass die Methodenidentität in der mutierten C#-Datei nullmal vorkam; die eingebettete Projektion
blieb bytegleich zum technischen Commit.
