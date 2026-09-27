# Vollständige Produktmessung bei 364da8b13

Gemessener Commit: `364da8b1380bea366764ed913ad69404dabae022`.
Source-Tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Tests-Tree: `2e846e2ea8c8060845d91296cdda3d6c5c94aaa3`.
Dieser Bericht ist ein Dokumentationsnachfolger, kein erneut gemessener Commit.

## Messergebnis

Alle 33 Profile wurden frisch am selben Commit ausgeführt: 20 Unit-/CPU-Profile
und 13 lokale Integrationsprofile. Alle vier Providergruppen endeten mit Exit 0
einschließlich Aufräumen. SQL Server bestand 75/75 ohne Skips in 5m52s.
Messung und Auswertung endeten mit Exit 0. Kein laufender Messprozess wurde
neu gestartet. Das strikte Aggregat ist vollständig.
Der unabhängige Read-only-Abschlussreview bestätigt 487 Datei-Hashes und
66 zusätzliche Runner-/Settings-Bindungen, beide CPU-Sonderprofile,
32 Assemblies sowie sämtliche Summen ohne Accounting-Blocker.

| Kennzahl | Ergebnis |
| --- | --- |
| Erfolgreiche Testausführungen einschließlich CPU-Wiederholungen | 12.834 |
| Produktassemblies | 32 |
| Zeilen | 85.371 / 93.762 = 91,0507455 % |
| Konservativ gezählte Verzweigungen | 30.788 / 36.841 = 83,5699357 % |
| Methodeneinträge einschließlich compilererzeugter Einträge | 26.061 |
| Methoden mit CRAP strikt über 30 | 0 |
| Methodeneinträge mit Zeilenlücken | 4.466 |
| Keine / teilweise Zeilenabdeckung | 2.783 / 1.683 |
| Zusätzliche Einträge mit ausschließlich Verzweigungslücken | 1.508 |
| Vereinigung beider Lückenlisten | 5.974 |

Keine fehlgeschlagenen oder übersprungenen Läufe fließen ein. Die konservative
Branch-Zählung verwendet Maxima pro Zeile und Identität, keine Vereinigung
individueller Branch-IDs. Methodeneinträge sind keine unabhängigen Produktverträge.
A+ bleibt unbelegt; CRAP höchstens 30 ist keine A+-Freigabe.

## Ziel und Vergleich mit T36

Die [zwei neuen Verhaltensfälle](t37-eventhubs-processor-lifecycle.md) prüfen
Cancellation-Weiterleitung, Isolation gleicher Offsets in verschiedenen Partitionen,
Shutdown eines bereits laufenden Checkpoints und erneute Client-Ausleihe.
Beide gezielten Mutationen werden erkannt; restaurierte Kontrolle 2/2.
Callbacks simulieren Speicherung; weder echter Azure-Betrieb noch private
Tokenidentität oder Checkpoint-Zulässigkeit nach Consumerfehlern werden bewiesen.

| Zielquelle | Methodenzeilen vorher / jetzt | Branches vorher / jetzt |
| --- | --- | --- |
| ProcessorLockContext | 25/35 → 28/35 | 7/14 → 7/14 |
| PartitionCheckpointData | 14/15 → 14/15 | 2/4 → 2/4 |

ProcessorLockContext.Canceled schließt die gezielte Zeilenlücke (0/3 → 3/3).
Die übrigen Pfade dieser Quellen sind damit nicht vollständig abgedeckt.
Methodenzeilen können dieselbe physische Zeile mehrfach zählen.

Produktweit werden netto vier zusätzliche physische Zeilen und zwei weniger
konservative Branches beobachtet. Drei Zeilenlücken schließen: Canceled,
SagaInstance.MarkInUseAsync (16/16) und der FutureExtensions-Subscription-Callback
(2/2). Zwei erscheinen neu: TypeConverterCache.GetOrCreateConverter (Zeile54,
11/12) und BatchConsumer.TimeLimitExpired (Zeile91,3/4). Die Nebenbeobachtungen
außerhalb Canceled werden den neuen Tests nicht zugeschrieben. Der Produktcode
ist unverändert; diese Wechsel belegen weder eine Produktregression noch deren
Behebung. Insbesondere wird kein deterministischer Saga-Nachweis behauptet.

Der unabhängige physische OR-Abgleich aller 66 XML-Berichte ergibt +8/-4:
ProcessorLockContext +3 (98,100,101), FutureExtensions +2 (38,39), SagaInstance
+3 (109,110,112); jeweils -1 bei EventHubDataReceiver85, BatchConsumer91,
TypeConverterCache54 und ServiceBusInstrumentation356. Netto +4 stimmt mit
dem Gesamtwert überein. Die Branch-Differenz -2 setzt sich zusammen aus +2
im PublishEndpoint.PublishInternalAsync-Callback, jeweils +1 bei
AddActivitiesFromNamespaceContaining und MarkInUseAsync sowie jeweils -1 bei
EF SendMessageAsync, EnsureDurableMetrics, AddActivities, EventHub HandleAsync,
GetOrCreateConverter und TimeLimitExpired. Diese Nebenpfadwechsel bleiben
kausal ungeklärt.

## Restarbeit

Die vollständigen lokalen Arbeitslisten sind t37-method-gaps.json und
t37-branch-only-gaps.json. Kein Eintrag wird durch diesen Bericht ausgeschlossen.
Der read-only geprüfte Folgeplan behandelt Azure-Service-Bus-Subscription-
Processor: Callback-Identität und Abschluss, Konfigurationsschutz, Lifecycle
und dokumentierte Fehlerbehandlung. Er ist noch nicht implementiert.
Der Roslyn-API-/Kommentar-Audit folgt erst nach dem A+-Coverage-Ziel.

## Lokale Nachweisdateien

Rohdaten bleiben lokal ignoriert; Hashes veröffentlichen die Dateien nicht.

| artifacts/-Datei | SHA-256 |
| --- | --- |
| t37-aggregate.json | 5dde8f444638ff6d9369d03bb17e28bb0a38d80d1dd09eefb73acf096f99927d |
| t37-all-methods.json | 93ba79bf64f170a122a2e845b0caa41b929fd00766bfd810f44641cacdec3adf |
| t37-method-gaps.json | 23dc90ac1192bd852a7c3a41b567f8f526539ab870cb0f93ef17cb498d5662db |
| t37-branch-only-gaps.json | 7f9773c58c26054fce06794c199fd9e63b10b0225efe6e8ce342f58d3e5711f9 |
| t37-profile-progress.json | df44b0a804de76d12668c57c6b7f9efe1a67a27683dee7694a85449423f69133 |
| t37-gap-delta.json | 53cf00161de34e237d2d9d8994f6a84328f849af8bb9a8ac21b48a8904787b8e |

Orchestrierung: artifacts/t37-measure-all.py. Auswertung:
artifacts/t37-analyze-complete.py und artifacts/t37-compare-gaps.py.
Alle Mess- und Auswertungsprozesse sowie der unabhängige Abschlussreview sind
beendet. Die Remote-Publikation wird nach dem Dokumentationscommit separat
gegen den Remote-HEAD geprüft.
