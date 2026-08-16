# Developer-Bericht — WP-F2-SERVICEBUS-IDENTITY

## Ergebnis und Freeze

Der freigegebene Identity-Slice ist im autorisierten Repository vollständig umgesetzt. Der Arbeitsstand ist absichtlich **nicht committet und nicht gepusht**. Die technische Identität ist repositoryweit auf `ViciOne.ServiceBus`, `ViciOneServiceBus`, `vicione-servicebus`, `ViciOne-ServiceBus` und `vnd.vicione.servicebus` vereinheitlicht; Aliasassembly, Type-Forwarder oder parallele Altidentität wurden nicht eingeführt.

Der erlaubte Ausgangsstand und die Slice-Bindung sind:

- Baseline-HEAD: `1de4bf6eb45c406da3cd6f26bdab6ed6d5aeefbc`
- Baseline-Tree: `2b09d4e2b2e14289f06ba112ce1ae52e326a0307`
- Slice-Datei-SHA-256: `d876460e9cb78c8b8d008dc994c867715c775195b53758b20a3d0ba525e4ca4b`
- Slice-`contentSha256`: `9516436fbe3dac4755a60ca2e7808c6c2d15b38fd0b99fd101af8cf9cb315181`
- Scope-Autorisierung: `WSA-PO-2026-08-06-05`

Da der Auftrag einen Commit ausdrücklich verbietet, gibt es für den Implementierungsstand noch keinen Git-Commit und keinen Git-Tree-Objektnamen. [IMPLEMENTATION_FREEZE.json](IMPLEMENTATION_FREEZE.json) enthält stattdessen den unveränderten Baseline-HEAD/-Tree, den vollständigen nicht ignorierten Deliverable-Census und einen deterministischen SHA-256-Fingerprint des uncommitteten Zielarbeitsstands.

Gesamtstatus: **`INCOMPLETE_NOT_GREEN`**. Alle Identity-, Rechts-, Build-, Artefakt-, Paket-, API-, Proof-Contract- und Werkzeug-Gates bestehen. Die vorhandene Produkttestsuite ist nicht grün: Externe Dienste fehlen beziehungsweise sind inkompatibel, unveränderte Baselinefehler bestehen fort, und sechs langlaufende Providerprojekte erreichten vor der kontrollierten SIGINT-Grenze keinen finalen TRX. Es wird ausdrücklich weder ein grüner Vollbeweis noch Developer-Red-Team- oder Gesamtfreigabe behauptet.

## Umfang und Identitätsbeweise

- 5.654 Baselinepfade sind jeweils exakt einmal auf Ziel- oder unveränderten Pfad abgebildet.
- Der kanonische Census partitioniert diese Pfade in 5.645 geänderte Baselinepfade (5.631 `R` + 14 `M`) und 9 unveränderte Pfade. Zusammen mit 83 neuen, nicht ignorierten Deliverables enthält der Freeze 5.737 Pfade.
- 35.055 öffentliche Deklarationen besitzen eine bijektive Baseline→Ziel-Abbildung; keine stille API-Entfernung wurde gefunden.
- Die frühere technische Identität ist außerhalb exakt pfad- und kontextgebundener Absätze in den fünf Rechts-/Provenienzdateien nicht vorhanden. Werkzeuge und Evidence besitzen keine Sonderausnahme.
- `LICENSE` ist bytegleich. README, NOTICE, COPYRIGHT und MODIFICATIONS dokumentieren Herkunft, Apache-2.0 und alle acht tatsächlich geänderten Binär-/kommentarlosen Änderungsformate.
- Das alte Produktlogo wurde an seiner Funktionsstelle durch ein ViciOne-ServiceBus-Asset ersetzt.
- Der Source-Gate prüft Text, Pfade, ASCII-/UTF-16-Binärstrings, Aliasse, Type-Forwarder, Wire-/Telemetry-Identitäten, Baselinevollständigkeit und Testabschwächung fail-closed.

Primäre Belege: [BASELINE_TO_TARGET_PATHS.json](BASELINE_TO_TARGET_PATHS.json), [IDENTITY_DISPOSITION.json](IDENTITY_DISPOSITION.json), [PUBLIC_API_MAPPING.json](PUBLIC_API_MAPPING.json), [CHANGE_NOTICES.json](CHANGE_NOTICES.json), [PACKAGE_INVENTORY.json](PACKAGE_INVENTORY.json), [SOURCE_IDENTITY_GATE.json](SOURCE_IDENTITY_GATE.json), [ARTIFACT_GATE.json](ARTIFACT_GATE.json).

## Ausgeführte Befehle und Ergebnisse

Die wesentlichen reproduzierbaren Befehle waren:

```text
dotnet restore ViciOne.ServiceBus.sln
dotnet build ViciOne.ServiceBus.sln -c Release --no-restore --disable-build-servers
dotnet msbuild <jedes der 33 src-Projekte> /t:Pack /p:NoBuild=true /p:BuildProjectReferences=false /m:1 /nr:false
dotnet test ViciOne.ServiceBus.sln -c Release --no-build --no-restore
python3 -m unittest discover -s tools/identity -p 'test_*.py' -v
python3 tools/identity/identity_gate.py evidence --root . --output evidence/WP-F2-SERVICEBUS-IDENTITY
python3 tools/identity/artifact_gate.py --root . --output evidence/WP-F2-SERVICEBUS-IDENTITY/ARTIFACT_GATE.json
python3 tools/identity/proof_contract_gate.py --slice ../../vicione-architecture/work/delivery/WP-F2-SERVICEBUS-IDENTITY/DEVELOPMENT_SLICE.json --output evidence/WP-F2-SERVICEBUS-IDENTITY
python3 tools/identity/evidence_summary_gate.py --root . --evidence evidence/WP-F2-SERVICEBUS-IDENTITY --build-binlog .testagent/binlogs/target-release-build-final-20260807-010210--48962--IFjmuI.binlog --tool-tests 69
python3 tools/identity/freeze_manifest.py --root . --evidence evidence/WP-F2-SERVICEBUS-IDENTITY --output evidence/WP-F2-SERVICEBUS-IDENTITY/IMPLEMENTATION_FREEZE.json
git diff --check
```

Ergebnisse:

- Restore: PASS nach Zugriff auf den konfigurierten internen NuGet-Feed.
- Release-Build: PASS, 0 Fehler, 201 bereits baselinegebundene Warnungen, 1:07,84 min.
- Pack: 33/33 `src`-Projekte erfolgreich, 33 `.nupkg`. Warnungen: NU1507 sowie die eingefrorenen NU1902/NU1903-Hinweise für MessagePack 3.1.6.
- Artefaktscan: PASS, 178 Artefakte = 145 DLL + 33 nupkg, 145 eingebettete PDBs, 0 externe PDBs, 0 Findings.
- Werkzeugtests: PASS, 69/69. Die Feindfixtures decken neue und bestehende Tool-/Evidence-Dateien, jede der fünf Rechtsdateien, unbegründete Format-Ausnahmen, fehlende oder falsch gebundene Baseline-/API-Zeilen sowie die vollständige F2-RT-04-Matrix ab.
- Proof-Contract-Sabotage: PASS, 24/24 Pflichtmutanten getötet, kein überlebender Mutant.

`dotnet pack` auf Solution- und Einzelprojekt-Frontend hing zweimal. Der erste exakte Prozess wurde per SIGINT beendet; der zweite reagierte nicht auf SIGINT und wurde als exakt aufgelöster PID per SIGTERM beendet. Es gab keinen breiten Prozess-Kill. Der direkte MSBuild-Packpfad lief anschließend für alle 33 Projekte erfolgreich durch. [PACK_GATE.json](PACK_GATE.json) und [ARTIFACT_GATE.json](ARTIFACT_GATE.json) sind die maßgeblichen Paket-/Artefaktbelege.

## Vorhandene Testsuite und exakte Partition

Der unfiltrierte Solution-Lauf wurde nach ungefähr 25 Minuten an einer kontrollierten SIGINT-Grenze beendet, weil serielle externe Providersuiten wiederholt in Infrastruktur-Timeouts liefen. Der Rootprozess und alle zugehörigen Testhost-Kinder wurden einzeln als beendet verifiziert. 17 finale TRX enthalten 2.656 Resultate:

- 2.160 Passed
- 413 Failed
- 83 NotExecuted

Alle 413 Fehler sind ohne Rest partitioniert:

- 330 `BLOCKED_EXTERNAL_INFRASTRUCTURE`
- 78 `PRE_EXISTING_BASELINE_FAILURE` (72 Analyzer-Harness, 2 Cron, 1 Hangfire, 3 Quartz)
- 4 `IDENTITY_REGRESSION_FIXED_TARGET_RERUN_PASS`
- 1 `NON_REPRODUCIBLE_FULL_RUN_FAILURE_TARGET_RERUN_PASS`

Die vier konkreten Endpoint-Altfehler sind:

- `EndpointName_Specs.Should_include_the_namespace_and_prefix`
- `EndpointName_Specs.Should_include_the_namespace_and_prefix_with_generic_consumer`
- `EndpointName_Specs.Should_include_the_namespace_and_prefix_with_message_name`
- `EndpointName_Specs.Should_include_the_namespace`

Der korrigierte Endpoint-Naming-Rerun umfasst diese vier und 14 weitere einschlägige Fälle: 18/18 Passed. `KillSwitch_Specs.Should_be_degraded_after_too_many_exceptions` bestand im isolierten Target-Rerun.

Target und unveränderte Baseline wurden mit identischen Filtern verglichen:

- Core Cron/KillSwitch: Target und Baseline jeweils 114 Passed, 2 identische Cron-Fehler, 1 deklarierter Plattformskip; KillSwitch bestand.
- Hangfire: Target und Baseline jeweils derselbe Duplicate-Key-Fehler für `...+Stop`.
- Quartz: Target und Baseline jeweils 2 Passed und dieselben 3 Duplicate-Key-/Turnout-Fehler.
- Analyzer: Target und Baseline jeweils 108 Resultate, 36 Passed/72 Failed. Beide Minimalproben zeigen denselben vorhandenen net9-Testharnessfehler: die Helper-Referenzen laden im Nicht-NET6-Zweig nicht die benötigte `System.Runtime, Version=9.0.0.0`-Facade. Der Identity-Slice ändert diesen Baseline-Testharnessfehler gemäß Architect-Anweisung nicht und schwächt keine Assertion.

Die 83 `NotExecuted` sind ebenfalls vollständig erklärt: 82 besitzen jeweils genau eine passende `[Explicit]`-Deklaration auf Methode oder Fixture; 1 ist `CronExpressionTest.TestDaylightSaving_QRTZNETZ186` mit dem exakten Grund `Only supported on WIN`. Kein `NotExecuted` wurde durch die SIGINT-Grenze verursacht und kein Skip wurde hinzugefügt oder abgeschwächt.

Maschinenlesbare Belege: [TEST_GATE.json](TEST_GATE.json), [TEST_FAILURE_PARTITION_GATE.json](TEST_FAILURE_PARTITION_GATE.json), [NOT_EXECUTED_SOURCE_GATE.json](NOT_EXECUTED_SOURCE_GATE.json), [ENDPOINT_IDENTITY_RERUN_GATE.json](ENDPOINT_IDENTITY_RERUN_GATE.json), [ANALYZER_BASELINE_COMPARISON.json](ANALYZER_BASELINE_COMPARISON.json), [TEST_EXECUTION_BOUNDARY.json](TEST_EXECUTION_BOUNDARY.json).

Sechs Projekte besitzen wegen der Laufgrenze keinen finalen TRX:

- `ViciOne.ServiceBus.RedisIntegration.Tests`
- `ViciOne.ServiceBus.MongoDbIntegration.Tests`
- `ViciOne.ServiceBus.RabbitMqTransport.Tests`
- `ViciOne.ServiceBus.ActiveMqTransport.Tests`
- `ViciOne.ServiceBus.AmazonSqsTransport.Tests`
- `ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests`

## Bewusste Grenzen und Blockade

Die externen Fehler sind unter anderem durch fehlende/inkompatible SQL-Server-, PostgreSQL-, Cosmos-, DynamoDB-, EventHub-, Kafka- und weitere Providerumgebungen belegt. Zwei gezielte Beispiele:

- Marten: PostgreSQL `28P01`, Passwortauthentifizierung für Benutzer `postgres` fehlgeschlagen.
- EventHub HealthCheck: Providerendpoint bleibt `Unhealthy - not ready`; andere EventHub-Fälle belegen eine inkompatible SASL-Mechanismenmenge.

A+-Empfehlung an die Architect AI: Diesen korrigierten Identity-Freeze unverändert an das Developer Red Team geben; den vollständigen Produktsuite-Grünbeweis separat in einer reproduzierbaren Provider-Testumgebung mit allen erforderlichen Diensten und ohne 25-Minuten-Grenze ausführen. Die 78 vorhandenen Baselinefehler nur in einem eigenen freigegebenen Slice beheben, nicht durch Assertion-/Skip-Abschwächung in diesem Identity-Slice.

Die sechs frozen Proof-Contract-Subjects werden durch diesen Slice nicht fachlich implementiert oder als Architekturverhalten bewiesen. Belegt sind ausschließlich Sliceform, Evidencevollständigkeit und Mutationsempfindlichkeit; die Architekturbindings bleiben ehrlich `ARCHITECTURE_CONCERN` beziehungsweise `NOT_PROVEN_BY_IDENTITY_SLICE`.

## Lead-Korrektur 01 und Revalidierung

Die lokale `.testagent/`-Ablage bleibt für eine ausschließlich lesende Prüfung physisch erhalten, ist nun aber repositoryweit ignoriert. Ihre 74 zuvor Git-untracked Dateien beziehungsweise ungefähr 105 MiB Rohcaptures sind weder Commit-Kandidaten noch Bestandteil des Deliverable-Census oder seines Fingerprints. Dauerhafte Gatebelege enthalten nur kleine, abgeleitete JSON-Zusammenfassungen; insbesondere enthält `BUILD_GATE.json` weder Scratch-Pfad noch Binlog-Hash. [SCRATCH_BOUNDARY_GATE.json](SCRATCH_BOUNDARY_GATE.json) bindet die Grenze mit 0 verbleibenden nicht ignorierten Scratch-Dateien.

Der Source-Scanner prüft exakt die 5.737 vorhandenen Git-Commitkandidaten aus `tracked` plus `others --exclude-standard`; es gibt keine pauschale Tool-, Evidence- oder Rechtsdateiausnahme. Zulässige Provenienzvorkommen werden ausschließlich in ihren exakten Pfad-/Kontextspannen maskiert, sodass jedes zusätzliche Vorkommen in README, LICENSE, NOTICE, COPYRIGHT oder MODIFICATIONS rot wird. Die frühere Proof-only-Sonderkategorie und gleichbedeutende Ausnahmen wurden vollständig entfernt.

Persistierte Baselinebelege enthalten keine Klartext-Baselinepfade oder -deklarationen. Jeder Baselineeintrag ist stattdessen an Baselinecommit, `baselinePathSha256`, Git-Blob-OID, Git-Mode und `baselineKey` gebunden; API-Einträge binden zusätzlich Baselinezeile, Deklarationshash, Zielzeile und Zieldeklarationshash. Das Gate rekonstruiert alle 5.654 Pfade und 35.055 öffentlichen Deklarationen direkt aus dem echten Baselinecommit und vergleicht die persistierten Records vollständig und bijektiv.

`.devcontainer/devcontainer.json` ist aus Code-Allowlist, NOTICE und MODIFICATIONS entfernt. Mapping und Freeze führen sie mit identischen Baseline-/Ziel-SHA-256-Werten als `UNCHANGED`. Der Legal-Validator weist erfundene, unveränderte oder kommentierbare Format-Ausnahmen zurück.

Der Freeze führt alle 5.654 Baselinebindungen einschließlich der neun unveränderten Bindungen sowie alle 83 neuen nicht ignorierten Commit-Kandidaten exakt einmal auf. `baselineKey` ist für `R`, `M` und `UNCHANGED` immer nicht-null; nur `A` verwendet `null`. Die Manifestintegrität heißt eindeutig `manifestIntegrityStatus: PASS`, während der an `GATE_SUMMARY.json` gebundene Produktgesamtstatus unverändert `overallStatus: INCOMPLETE_NOT_GREEN` lautet. Rohe Git-Delete-Diffstats sind ausdrücklich keine Deliverable-Statistik.

Für Lead-Korrektur 01 wurden ausschließlich Prüfwerkzeuge, Werkzeugtests, Evidence und die zwei autorisierten Legaltexte NOTICE/MODIFICATIONS geändert. Build, Pack und Produkttests wurden deshalb nicht erneut ausgeführt; Produktquelle, öffentliche Ziel-API und Produktsemantik blieben unverändert. [CORRECTION_SCOPE_REUSE_GATE.json](CORRECTION_SCOPE_REUSE_GATE.json) belegt maschinell null Abweichungen von der deterministischen Produktabbildung und bindet alle 178 vorhandenen Build-/Paketartefakte, 33 Pakete sowie 19 TRX-Captures per SHA-256. Erneut ausgeführt wurden 64 Werkzeugtests, Source-/Text-Pfad-/API-/Legal-Census, vollständige Proof-Mutationen, Evidence-Rekonstruktion, Freeze-Validierung und `git diff --check`.

## Lead-Korrektur 02 und F2-RT-04

Die Korrektur begann nach unveränderter Verifikation des Ausgangsfreeze `bd0f232313912fefe215c309cb8b5339a3276b1cf5381410362248e5567adf88`, des oben gebundenen HEAD/Tree und des freigegebenen Slice. Geändert wurden ausschließlich `tools/identity/**`, deterministisch davon abhängige Evidence und dieser Bericht; Produkt-, Projekt-, öffentliche API-, .NET-Test-, Legal-, Paket- und Laufzeitdateien blieben unverändert. Es wurde weder committet noch gepusht.

Eine einzige deklarative Registry mit 18 Altidentitätsfamilien enthält nun Scannergrammatik und sämtliche Pfad-/Text-Mappingregeln. Mapper, Pfadscan, UTF-8-Scan sowie ASCII-/UTF-16-Binärscan werden daraus abgeleitet; das frühere separate Binärinventar existiert nicht mehr. Die Registry umfasst vollständige, getrennte und Bindestrichnamen, Vendor-/MIME-, URL-, Endpoint-, Analyzer-, Benchmark-, Queue-, Logo-, Header-, Environment-, lokale Assembly-/Admin-/Test-/URL-, Trade-/Trades- und produktbezogene Kurzformen.

Die Erkennung aller Buchstabenformen ist case-insensitive. Konkrete bekannte Header-Roots und Tokenbegrenzungen verhindern Kollisionen mit realen Drittbegriffen; insbesondere bleibt der vorhandene Dritt-Token `mt-bench` unbelegt. Der vollständige reale Commitkandidatenbaum bestand mit 0 Findings. Ein während der Entwicklung absichtlich ähnlich benannter Python-Test kollidierte korrekt mit der Environmentgrammatik und wurde neutral umbenannt; es wurde keine Ausnahme ergänzt.

Die tabellengetriebene Strukturprüfung `test_registry_drives_every_mapping_rule_and_casefolded_scanner_channel` führt für jede Familie kanonische, kleine, große und deterministisch gemischte Schreibweise durch Pfad, UTF-8, ungültiges UTF-8 mit Binärsicht und UTF-16. Sie vergleicht außerdem die aktiven Mapper-Regeln vollständig mit der Registry und wird rot, sobald eine Mappingregel keine Scannerfamilie besitzt. `test_red_team_short_form_examples_hit_their_required_gates` weist alle neun in F2-RT-04 vorgegebenen Gegenbeispiele im jeweils zuständigen Gate nach. `test_compiled_registry_union_preserves_all_four_scanner_channels` bindet die speicherschonende, einmal kompilierte Registry-Union an dieselben vier Trefferkanäle. Der vollständige Werkzeuglauf bestand mit 69/69; alle 20 zuvor bestätigten Red-Team-Mutanten und die neun neuen Gegenbeispiele wurden getötet. Die Proof-Contract-Prüfung bestand erneut mit 24/24 getöteten Pflichtmutanten.

Das veraltete Feld wurde durch die ausschließlich auf aktuelle, pfad- und kontextgebundene Legalvorkommen bezogene Aussage `legalRetentionIsPathAndContextExact` ersetzt. Eine Proof-only-Sonderkategorie wurde nicht wieder eingeführt.

Build, Pack und Produkttests wurden im autorisierten engen Scope nicht erneut ausgeführt. Der Reuse-Nachweis bindet 5.633 aktuelle .NET-Inputs unter `src/**`, `tests/**` und den Root-Build-/Paketinputs exakt an denselben Start-/Ist-Aggregathash. Zusätzlich wurden aktuelle Produktsemantik, alle 178 Artefakthashes und 33 Pakete erneut geprüft. Der TRX-Aggregathash beschreibt ausdrücklich nur den derzeit vorhandenen Satz von 19 Captures und behauptet keine historische Bytegleichheit mit einem früheren Freeze. [CORRECTION_SCOPE_REUSE_GATE.json](CORRECTION_SCOPE_REUSE_GATE.json) ist grün.

Nach der Korrektur wurden Source-/Pfad-/API-/Legal-Census, der geschlossene Commitkandidatenscan, alle Werkzeugtests, die 24 Proof-Contract-Mutanten, die 29 adversarialen Identity-/Evidence-/Legal-/Binding-Mutanten, Evidence-Zusammenfassung, Freeze-Integrität und `git diff --check` erneut ausgeführt. Der Gesamtstatus bleibt wegen der unveränderten Produkttestgrenzen `INCOMPLETE_NOT_GREEN`; eine grüne Produktsuite wird nicht behauptet.

## Lead-Korrektur 03 und F2-RT-05

Vor jedem Schreibschritt wurde der Ausgangsfreeze `bdb80401540142c9d27203db5b1b5837eb823c0b99ff589936db6fdeae21a6d8` unabhängig reproduziert. HEAD, Tree sowie Slice-Datei- und Content-Hash blieben unverändert. Der Korrekturscope umfasst ausschließlich `tools/identity/identity_rules.py`, `tools/identity/test_identity_rules.py`, deterministisch abhängige Evidence und diesen Bericht. Es wurde weder committet noch gepusht.

Die unveränderte einzige Registry wird für eine nicht autoritative Developer-Schnellprüfung auf 18 Familien, 40 Mappingregeln und den lokalen Digest `992c1fea0b89c5cc4a4e7d9202d479f8522e436031e06da350822575862e6cf3` kanonisiert. Diese lokale Momentaufnahme verwirft schnelle innere Digestabweichungen, ist aber gemeinsam mit der Registry im Developer-Scope änderbar und beweist deshalb keinen Architect-Contract. Es entstand kein zweites Klartextformenregister.

`test_each_of_40_original_mapping_rules_has_non_equivalent_isolated_effect_mutant` leitet für jede Regel Probe, erwartete Wirkung, Kanal und Strategie aus dem unveränderten Originaleintrag ab, bevor eine isolierte Registryansicht exakt diese Regel deaktiviert. Alle 40 Mutanten verloren die konkrete Mappingwirkung; 0 waren redundant oder äquivalent. `test_developer_quickcheck_rejects_each_of_40_mapping_rule_removals` tötete zusätzlich jede der 40 Entfernungen in der lokalen Schnellprüfung. `test_developer_quickcheck_rejects_exact_f2_rt_05_and_required_strategy_removals` tötete den exakten F2-RT-05-Mutanten sowie Path-, Literal-, Regex-, Pascal-, Lower-, Path-Context- und semantisch kontextspezifische Stichproben. Der vollständige Werkzeuglauf bestand mit 74/74.

Der angeordnete kurze Census rekonstruierte 5.654 Baselineabbildungen und 35.055 öffentliche Deklarationen mit jeweils 0 Mapping-, API-, Konformitäts-, Legal- oder geänderten Tooldatei-Findings. Die Proof-Contract-Prüfung bestand frisch mit 24/24 getöteten Pflichtmutanten. Weil Registry-, Scanner- und Mappingsemantik digestgleich blieben, wurden der vollständige Commitkandidatenscan und der 178-Artefaktscan nicht wiederholt; stattdessen bindet [CORRECTION_SCOPE_REUSE_GATE.json](CORRECTION_SCOPE_REUSE_GATE.json) deren vorhandene grüne Evidence unverändert per SHA-256.

Die 5.633 .NET-Inputs bleiben bitidentisch mit Start-/Ist-Hash `0ad79cefb9395a51ec1be6fb1c36f16aa2c86c4a1001df3821da0d0f07888f5b`. Build, Pack und Produkttests wurden nicht erneut ausgeführt. Der TRX-Hash beschreibt weiterhin ausschließlich den aktuellen Capture-Bestand und behauptet keine historische Bytegleichheit. [REGISTRY_SEMANTICS_GATE.json](REGISTRY_SEMANTICS_GATE.json) und [TOOL_TEST_GATE.json](TOOL_TEST_GATE.json) sind die maßgeblichen neuen Belege. Der Gesamtstatus bleibt `INCOMPLETE_NOT_GREEN`.

## Lead-Korrektur 04 und F2-RT-06

Vor dem ersten und jedem weiteren Schreibschritt wurde der Ausgangsfreeze `639f153323e2af4dcf442b01c402e419bf96f294e04fed9ecefcb5b01e45185f` unabhängig reproduziert. Geändert wurden ausschließlich die zwei autorisierten Tooldateien, deterministisch abhängige Evidence und dieser Bericht. Registry, Mappingwirkungen und Scannergrenzen blieben unverändert; es gab weder Commit noch Push.

Die repo-lokalen Namen und Beschreibungen lauten nun eindeutig `DEVELOPER_REGISTRY_QUICKCHECK_*` beziehungsweise `developer_registry_quickcheck_*` und tragen die Einstufung `NON_AUTHORITATIVE_LOCAL_DEVELOPER_CHECK`. Diese Schnellprüfung hält alle 40 Kantenwirkungs- und Digestmutanten wirksam, besitzt aber keine Autorität über die Developer-Grenze hinaus. Das lokale PASS wird nicht als alleiniger Contractbeweis oder externe Freigabe ausgegeben.

Der externe Vertrauensanker wird ausschließlich als Referenz gebunden: `vicione-architecture/work/delivery/WP-F2-SERVICEBUS-IDENTITY/ARCHITECT_REGISTRY_SEMANTICS_CONTRACT.json`, SHA-256 `7f1ddc060e60821cd4609e07ea3aca25653dce375a4ff165e4d0a502e5a0bd5a`, Entscheidungscommit `aada559bf524705097bbd740b304945016be95b1`. Es wurde keine Contractkopie und kein developer-wählbarer externer Pfad angelegt. Sein Status in [REGISTRY_SEMANTICS_GATE.json](REGISTRY_SEMANTICS_GATE.json), [TOOL_TEST_GATE.json](TOOL_TEST_GATE.json) und [GATE_SUMMARY.json](GATE_SUMMARY.json) bleibt ausdrücklich `PENDING_ARCHITECT_GATE`; die Developer AI behauptet keine externe Ausführung oder Freigabe.

Der vollständige lokale Werkzeuglauf bestand mit 74/74. Der kurze Census bestand für 5.654 Baselineabbildungen und 35.055 öffentliche Deklarationen mit 0 Mapping-, API-, Konformitäts-, Legal- oder Tooldatei-Findings. Proof bestand mit 24/24 getöteten Pflichtmutanten. Vollständiger Baum- und 178-Artefaktscan sowie Restore, Build, Pack, Provider- und .NET-Produkttests wurden nicht wiederholt; ihre vorhandenen PASS-Hashes bleiben gebunden. Die 5.633 .NET-Inputs sind weiter bitidentisch, und der TRX-Hash beschreibt nur den aktuellen Capture-Bestand. Der Gesamtstatus bleibt `INCOMPLETE_NOT_GREEN`.

## Lead-Korrektur 05 und F2-RT-07

Vor der Evidence-Korrektur wurde der Ausgangsfreeze `fb8bca4c48f7b55c7f5a97ae308a263f16d19004da5c3b899291aa3d3a3f6445` unabhängig und mit `manifestIntegrityStatus: PASS` reproduziert. Der aktualisierte Architect-owned Contract aus Architecture-Commit `c48083ddea964e984458c24629e10dbf30bd00af` wurde ausschließlich als externer Vertrauensanker referenziert; seine Bytes ergeben exakt SHA-256 `7f1ddc060e60821cd4609e07ea3aca25653dce375a4ff165e4d0a502e5a0bd5a`. Entscheidungscommit `aada559bf524705097bbd740b304945016be95b1` und Entscheidungsbeleg `8f90b2548c7d9bb420224222fa76c914e714ec996343fea7379dac410068c038` bleiben unverändert gebunden.

Die beiden nun vollständig eingefrorenen Trägerdateien wurden ausschließlich read-only geprüft und nicht geändert: `tools/identity/identity_rules.py` besitzt SHA-256 `ef347e144161891b217ca08b9bf9d9d42214a57eaeea0f15075222d6d883d7b6`, `tools/identity/test_identity_rules.py` SHA-256 `999a1ef7a22ed6ab1ccabaed6b2ce5f7f79bb38619bf593375dd49dced3b7ef3`. Der unveränderte lokale Semantikdigest lautet `992c1fea0b89c5cc4a4e7d9202d479f8522e436031e06da350822575862e6cf3`; der lokale Status bleibt `PASS` mit Autorität `NON_AUTHORITATIVE_LOCAL_DEVELOPER_CHECK`. Das externe Gate bleibt `PENDING_ARCHITECT_GATE` und behauptet weder Developer-Ausführung noch Developer-Freigabe.

Die 5.633 .NET-Inputs wurden read-only erneut an `0ad79cefb9395a51ec1be6fb1c36f16aa2c86c4a1001df3821da0d0f07888f5b` gebunden. Die vorhandenen PASS-Belege blieben bytegenau: Source `ded0957b952a58f06b915510173b8be50719fd1c552848a5e6cbe9fc4b916661`, Artifact `4ec062c91efbbab9beec8ba12fa1f81f860daf15aaad46d59ab4a1d674cd6626`, Test `b3fbec8fcbe6e486e6fd0af33fa938d7b8a3a4849b8c54ab6ef18f1aa049dbe4` und Proof `2f8493aa1b9a8ec7fea77f6bf5018e6a81621d3d55625a8afd77b6be88148c1e`. Es wurden keine Werkzeugtests, Vollbaum-/Artefaktscans, Restores, Builds, Packs, Provider- oder .NET-Produktläufe wiederholt. Geändert wurden ausschließlich deterministisch abhängige Evidence, dieser Bericht und der anschließend regenerierte Freeze; Produkt-, Projekt-, Testcode-, Legal-, Paket-, Laufzeit- und Architekturdateien blieben unverändert. Es gab weder Commit noch Push. Der Gesamtstatus bleibt `INCOMPLETE_NOT_GREEN`.

## Lead-Korrektur 07: EOL-Kanonisierung

Die freigegebene EOL-Korrektur normalisierte exakt 211 zuvor durch Raw-/Clean-Abweichungen belegte Textpfade auf LF. Für jeden Pfad bindet [EOL_CANONICALIZATION_GATE.json](EOL_CANONICALIZATION_GATE.json) Vorher-/Nachher-Hashes, EOL-Zähler, Git-OIDs sowie die Gleichheit von Worktree, Index und `git check-attr`/Clean-Ausgabe. Der sortierte Pfadsatz besitzt den Digest `b080d0f974835490177f29db1eb8064ed3928db0997dce5b1ae011b6a7be30cc`; es gibt 0 Binär-/NUL-Pfade, 0 Nicht-EOL-Änderungen und 0 verbleibende Raw-/Clean-Abweichungen.

`.gitattributes` erhielt ausschließlich den freigegebenen Kommentar und `LICENSE -text`. Die Apache-2.0-Lizenz blieb in Baseline, Worktree, Index und Clean-Ausgabe byteidentisch mit SHA-256 `f634e0b04b32e05da1915e2d58e89c63e9f358dcd65cc5218a6152ec0e3d65a1`; `git check-attr text -- LICENSE` liefert `unset`.

Der anschließende Release-Build bestand mit 0 Fehlern und 30 Warnungen. Der paketweise Release-Pack bestand 33/33; alle Binlog- und Rohcaptures verbleiben ausschließlich in ignoriertem `.testagent/`-Scratch. Diese Artefakte wurden in Lead-Korrektur 08 weder neu gebaut noch neu gepackt.

## Lead-Korrektur 08: containerbewusster NuGet-Scan

Der Artefaktscanner behandelt `.nupkg` nun als ZIP-Container. Er prüft fail-closed den äußeren Paketpfad, ZIP-Gültigkeit und Lesbarkeit, Archivkommentar, jeden Entry-Pfad, jeden Entry-Kommentar und den dekomprimierten Entry-Inhalt. Die frühere technische Identität wird nicht mehr generisch im rohen komprimierten Containerstrom gesucht. Die Rechtsausnahme gilt nur für exakt benannte logische Root-Legalentries; ein verschachtelter gleicher Basename bleibt rot. Der bestehende rohe DLL-/PDB-Scan ist unverändert.

13 fokussierte hostile Tests und der vollständige Identity-Werkzeuglauf mit 83/83 Tests bestanden ohne Skip. Alle fünf Pflichtmutanten wurden getötet: Wiedereinführung des Raw-Container-Scans sowie Entfernung der äußeren Paketpfad-, Entry-Pfad-, dekomprimierten Inhalts- und Kommentarprüfungen. Null-Discovery oder Skip ist rot. Maßgeblich sind [ARTIFACT_SCANNER_MUTATION_GATE.json](ARTIFACT_SCANNER_MUTATION_GATE.json) und [TOOL_TEST_GATE.json](TOOL_TEST_GATE.json).

Der korrigierte Vollscan bestand mit 178 Artefakten, darunter 145 DLLs, 0 externe PDBs und 33 NuGet-Pakete; Findings: 0. Vorher und nachher besitzen dieselben 178 Artefakte denselben Inventardigest `ff2edf995e89d7bf61ac60784cb7e1704d680917058c5b43159b94de2618522b` und zusammen 58.353.600 Bytes. Das maßgebliche Regressionspaket `src/ViciOne.ServiceBus/bin/Release/ViciOne.ServiceBus.1.0.0.nupkg` blieb byteidentisch bei SHA-256 `91427c55ff06b29199422505eda1f1576f2af6f865bc501c174ab6934bac9e10`. Es gab keinen Build, Pack oder Artefaktdrift in Korrektur 08.

Die frischen Zieltests bestanden für Endpoint-Naming 18/18 und den exakten KillSwitch-Fall 1/1. Der Analyzer-Rerun lieferte erneut 36 Passed und exakt dieselben 72 vorhandenen `PRE_EXISTING_BASELINE_TEST_HARNESS_DEFECT`-Signaturen; Ergebnisabbildung, Fehlermeldungszähler und Outcomes sind identisch mit dem eingefrorenen Vergleich. Es gibt keine neue funktionale Regression und keine abgeschwächte Assertion oder Skip-Regel. [CORRECTION_08_TARGETED_TEST_GATE.json](CORRECTION_08_TARGETED_TEST_GATE.json) bindet die drei frischen TRX-Dateien per SHA-256.

Der unveränderte rohe Source-Identity-Scanner meldet transparent 212 Findings, weil sein geschlossener Vor-Korrektur-Hashvertrag die freigegebene EOL-Kanonisierung nicht kennt: zwei Findings für `.gitattributes` und 210 deterministische EOL-Pfadabweichungen. Kein Finding liegt außerhalb von `.gitattributes` oder dem EOL-Pfadsatz; `NOTICE` ist der einzige der 211 EOL-Pfade ohne Raw-Finding, da der Rechtspfad bereits im geschlossenen Mapping kanonisch ist. [SOURCE_IDENTITY_RECONCILIATION_GATE.json](SOURCE_IDENTITY_RECONCILIATION_GATE.json) belegt diese exakte Partition mit 0 unautorisierten Findings. `SOURCE_IDENTITY_GATE.json` wird nicht nachträglich grüngefärbt oder unterdrückt.

Der Proof-Contract bestand erneut mit 24/24 getöteten Pflichtmutanten. Der externe Registry-Semantikcontract bleibt unverändert `PENDING_ARCHITECT_GATE`. Der Gesamtstatus bleibt wegen der bereits dokumentierten vollständigen Produktsuite-Grenzen ehrlich `INCOMPLETE_NOT_GREEN`. Es wurde weder committet noch gepusht.

## Gateübersicht

PASS: Build, Restore, Source-Identity-Reconciliation, EOL-Kanonisierung, Text/Pfad, API, Legal, Artifact, Artefaktimmutabilität, Scanner-Mutationen, IL/Metadaten, PDB, NuGet, Binary, Pack, Proof-Mutationen, lokale Developer-Tooltests, Korrekturscope-Bindung, frische C08-Zieltests, Failure-Partition und NotExecuted-Attribution.

PENDING: unabhängiges Architect-Gate für den externen Registry-Semantikcontract (`PENDING_ARCHITECT_GATE`).

RAW FAIL, vollständig autorisiert und reconciled: der unveränderte Source-Identity-Hashvertrag gegen die 211 pfadweise bewiesenen EOL-Kanonisierungen und die exakte `.gitattributes`-Ergänzung.

FAIL/INCOMPLETE: vorhandene Gesamttestsuite aus den oben dokumentierten externen, baselinegebundenen und Laufgrenzen-Gründen. [GATE_SUMMARY.json](GATE_SUMMARY.json) ist die abschließende maschinenlesbare Übersicht.
