# Vollständige Produktmessung bei 3aea04fae

Gemessener Commit: `3aea04fae7f291d5c219f87b9a07f2df4935429e`.
Source-Tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Tests-Tree: `f33a09bb10b2ce8f7bd063f0945569f487ee261a`.
Dieser Bericht ist ein Dokumentationsnachfolger, kein erneut gemessener Commit.

## Messergebnis

Alle 33 Profile wurden frisch am selben Commit ausgeführt: 20 Unit-/CPU-Profile
und 13 lokale Integrationsprofile. Alle vier Providergruppen endeten mit Exit 0
einschließlich Aufräumen. SQL Server bestand 75/75 ohne Skips; der Testlauf dauerte
5m34s. Messung und Auswertung endeten mit Exit 0. Kein laufender Messprozess
wurde neu gestartet. Das strikte Aggregat ist vollständig.
Der unabhängige Read-only-Abschlussreview bestätigt 487 Datei-Hashes und
66 zusätzliche Runner-/Settings-Hashbindungen, alle 33 Profile, beide
CPU-Sonderprofile, 32 Assemblies und sämtliche Summen ohne Accounting-Blocker.

| Kennzahl | Ergebnis |
| --- | --- |
| Erfolgreiche Testausführungen einschließlich CPU-Wiederholungen | 12.832 |
| Produktassemblies | 32 |
| Zeilen | 85.367 / 93.762 = 91,0464794 % |
| Konservativ gezählte Verzweigungen | 30.790 / 36.841 = 83,5753644 % |
| Methodeneinträge einschließlich compilererzeugter Einträge | 26.061 |
| Methoden mit CRAP strikt über 30 | 0 |
| Methodeneinträge mit Zeilenlücken | 4.467 |
| Keine / teilweise Zeilenabdeckung | 2.785 / 1.682 |
| Zusätzliche Einträge mit ausschließlich Verzweigungslücken | 1.509 |
| Vereinigung beider Lückenlisten | 5.976 |

Keine fehlgeschlagenen oder übersprungenen Läufe fließen ein. Die konservative
Branch-Zählung verwendet Maxima pro Zeile und Identität, keine Vereinigung
individueller Branch-IDs. Methodeneinträge sind keine unabhängigen Produktverträge.
A+ bleibt unbelegt; CRAP höchstens 30 ist keine A+-Freigabe.

## Ziel und Vergleich mit T35

Die [fünf neuen Verhaltensfälle](t36-eventhubs-checkpoint.md) prüfen neuesten
akzeptierten Checkpoint, geordneten Rückfall, Fortsetzung nach abgelehnten Updates
und abbrechbare Admission an einer vollen Queue. Drei kompilierbare Mutationen
werden erkannt; die restaurierte isolierte Kontrolle besteht 5/5. Die Speicherung
wird durch SDK-Callbacks simuliert. Weder echte Azure-Ausfälle noch die
ProcessorLockContext-Weiterleitung sind dadurch bewiesen.

BatchCheckpointer bleibt bei 58/68 Methodenzeilen und 9/14 konservativen Branches.
Die beiden Methodeneinträge mit Zeilenlücken bleiben offen: ReadBatchAsync
(100,106,110) und WaitForBatchAsync (63,65,66,68,69,71,72).
TryCheckpointAsync erreicht alle elf Zeilen, aber nur zwei von vier Branches.
Die Tests stärken den Verhaltensnachweis; ein Coverage-Gewinn im Ziel wird nicht
behauptet. Methodenzeilen können dieselbe physische Zeile mehrfach zählen.

Produktweit bleibt die Zahl abgedeckter physischer Zeilen gleich; die konservative
Branch-Zahl steigt um zwei. Eine Zeilenlücke schließt in BatchConsumer.TimeLimitExpired
(jetzt 4/4); eine erscheint neu in FutureExtensions.<AddSubscription>b__1_0
(jetzt 0/2, Zeilen 38,39). Die drei in T35 neu beobachteten Lücken bleiben offen:
ClientRequestHandle.SendAsync 155, generisches TaskExtensions.OrCanceledAsync
52,53 sowie SagaInstance.MarkInUseAsync 109,110,112. Der Produktcode ist unverändert.
Nebenänderungen werden den neuen Tests nicht zugeschrieben; ihre Ursachen bleiben
offen. Sie belegen weder eine Produktregression noch deren Behebung.

Der unabhängige physische OR-Abgleich sämtlicher 66 XML-Berichte bestätigt
zwei hinzugekommene Zeilen (BatchConsumer 91, ServiceBusInstrumentation 356)
und zwei weggefallene Zeilen (FutureExtensions 38,39), netto null.
Die konservative Branch-Differenz besteht aus jeweils +1 bei
PlanStateMachineRegistrations, EnsureDurableMetrics und TimeLimitExpired
sowie -1 bei PlanSagaRegistrations. Auch diese Nebenbeobachtungen werden
den neuen Tests nicht kausal zugeschrieben.

## Restarbeit

Die vollständigen lokalen Arbeitslisten sind t36-method-gaps.json und
t36-branch-only-gaps.json. Kein Eintrag wird durch diesen Bericht ausgeschlossen.
Der vorbereitete Folgeplan behandelt Event-Hubs-Processor-Cancellation und
Partition-Shutdown über unterstützte SDK-Callbacks, einschließlich unabhängiger
Partitionen und vollständigem Worker-Cleanup. Er ist noch nicht implementiert.
Der Roslyn-API-/Kommentar-Audit folgt erst nach dem A+-Coverage-Ziel.

## Lokale Nachweisdateien

Rohdaten bleiben lokal ignoriert; Hashes veröffentlichen die Dateien nicht.

| artifacts/-Datei | SHA-256 |
| --- | --- |
| t36-aggregate.json | 331fbebc5516b23eebd8b5d5d695a11b7a7fdebaab8007c8ff64fabb50918521 |
| t36-all-methods.json | 7725d1a4eb698a9d49446aaf12438d12c690f633447958c8cc646acc7308c037 |
| t36-method-gaps.json | a4625027a167ee8b0294b03a81b3b390abd41b1387e555a9615c50729bea93a2 |
| t36-branch-only-gaps.json | a7df965806a916a1d2451f5dde3e09e99e03f2cfb12b51ae72c6492938367d99 |
| t36-profile-progress.json | 6e07eb5fdeff9c6ab2709e4e5825d1cc2519c09ee82fdc13ac0352c29848f197 |
| t36-gap-delta.json | a497f382dad42a1e088f17c79b3667b8979917105120673a78335ccd29379203 |

Orchestrierung: artifacts/t36-measure-all.py. Auswertung:
artifacts/t36-analyze-complete.py und artifacts/t36-compare-gaps.py.
Alle Mess- und Auswertungsprozesse sowie der unabhängige Abschlussreview sind
beendet. Die Remote-Publikation wird nach dem Dokumentationscommit separat
gegen den Remote-HEAD geprüft.
