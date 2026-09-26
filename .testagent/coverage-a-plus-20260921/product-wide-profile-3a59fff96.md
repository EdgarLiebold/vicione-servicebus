# Vollständige Produktmessung bei 3a59fff96

Gemessener Commit: `3a59fff96f220f54e56467293641ff12be3607d4`.
Source-Tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Tests-Tree: `9d24231dc6f4399e9ef8ff25682d23440c35ac69`.
Dieser Bericht ist ein Dokumentationsnachfolger, kein erneut gemessener Commit.

## Messergebnis

Alle 33 Profile wurden frisch am selben Commit ausgeführt: 20 Unit-/CPU-Profile
und 13 lokale Integrationsprofile. Alle vier Providergruppen endeten mit Exit 0
einschließlich Aufräumen. Core bestand 6.725/6.725, SQL Server 75/75 in 6m01s.
Kein laufender Messprozess wurde neu gestartet. Das strikte Aggregat ist
vollständig. Der unabhängige Read-only-Review bestätigt 487 Datei-Hashes,
HEAD/Source-/Tests-Tree, alle Profile, CPU-Modi, Provider und 32 Assemblies sowie
sämtliche Summen und Deltas ohne Accounting-Blocker.

| Kennzahl | Ergebnis |
| --- | --- |
| Erfolgreiche Testausführungen einschließlich CPU-Wiederholungen | 12.827 |
| Produktassemblies | 32 |
| Zeilen | 85.367 / 93.762 = 91,0464794 % |
| Konservativ gezählte Verzweigungen | 30.788 / 36.841 = 83,5699357 % |
| Methodeneinträge einschließlich compilererzeugter Einträge | 26.061 |
| Methoden mit CRAP strikt über 30 | 0 |
| Methodeneinträge mit Zeilenlücken | 4.467 |
| Keine / teilweise Zeilenabdeckung | 2.784 / 1.683 |
| Zusätzliche Einträge mit ausschließlich Verzweigungslücken | 1.508 |
| Vereinigung beider Lückenlisten | 5.975 |

Keine fehlgeschlagenen oder übersprungenen Läufe fließen ein. Die konservative
Branch-Zählung verwendet Maxima pro Zeile und Identität, keine Vereinigung
individueller Branch-IDs. Methodeneinträge sind keine unabhängigen Produktverträge.
A+ bleibt unbelegt; CRAP höchstens 30 ist keine A+-Freigabe.

## Ziel und Vergleich mit T34

Die [132 neuen Verhaltensfälle](t35-recurring-completion.md) prüfen echte
Command-/Pipeverarbeitung beider Recurring-Scheduler, Vertragsauswahl, Metadaten
und kontrolliert verzögerten Endpoint-Abschluss mit Erfolg, Fehler und Abbruch.
Drei Mutationen im Publish-Scheduler schlagen an; die restaurierte Kontrolle
besteht 132/132. Weder Brokerpersistierung noch wiederkehrende Zustellung wird
damit bewiesen. Eine Mutation des anderen Schedulers wird nicht behauptet.

| Zielquelle | Methodenzeilen vorher / jetzt | Branches vorher / jetzt | Zeilenlücken-Einträge vorher / jetzt |
| --- | --- | --- | --- |
| PublishRecurringMessageScheduler | 110/216 → 164/216 | 33/108 → 56/108 | 24 → 24 |
| EndpointRecurringMessageScheduler | 134/235 → 182/235 | 37/110 → 57/110 | 25 → 25 |
| ScheduleRecurringMessageCommand | 21/23 → 21/23 | 0/0 → 0/0 | 1 → 1 |

Methodenzeilen können dieselbe physische Zeile mehrfach zählen. Die Zielmethoden
haben weiterhin unter anderem Eingabeprüfungen, fehlende Topologie/Zielauflösung,
Probe-Aufrufe und beim Endpoint die Provider-Auflösung als offene Beobachtungen.
Keine vollständige Schließung dieser Quellen wird behauptet.

Produktweit: netto 100 weitere physische Zeilen und 39 weitere konservativ
gezählte Branches gegenüber T34. Der direkte Vergleich aller 66 XML-Reports
bestätigt 110 hinzugekommene und zehn weggefallene physische Zeilen. Gewinne:
54 im Publish-Scheduler, 48 im Endpoint-Scheduler, fünf im BatchCheckpointer
und jeweils eine in ResourceCache, EventHubDataReceiver und
ClientRequestHandle.Responses. Verluste: TaskExtensions 52,53; SagaInstance
109,110,112; ClientRequestHandle.Responses 24,26,28; ClientRequestHandle 155;
ServiceBusInstrumentation 356. Die Nebenänderungen werden den neuen Tests
nicht zugeschrieben. Kein Zeilenlücken-Eintrag schließt vollständig,
drei erscheinen neu: ClientRequestHandle.SendAsync Zeile 155 (18/19), generisches
TaskExtensions.OrCanceledAsync Zeilen 52,53 (5/7) und SagaInstance.MarkInUseAsync
Zeilen 109,110,112 (13/16). Dadurch steigt die Lückenvereinigung von 5.969 auf
5.975 trotz höherer Zeilen-/Branch-Abdeckung. Der Produktcode ist unverändert;
die Ursachen wechselnder Beobachtungen außerhalb der gezielten Tests bleiben
offen. Aus ihnen wird weder Regression noch erfolgreiche Behebung abgeleitet.

## Restarbeit

Die vollständigen lokalen Arbeitslisten sind t35-method-gaps.json und
t35-branch-only-gaps.json. Kein Eintrag wird durch diesen Bericht ausgeschlossen.
Der nächste geprüfte Plan behandelt Event-Hubs-Checkpoint-Reihenfolge, Rückfall
bei Providerfehlern und abgebrochene Admission an einer vollen Queue. Er nutzt
kontrollierte Signale und unterstützte SDK-Callbacks. Die separate Frage nach
Checkpoints jenseits einer fehlgeschlagenen Consumption wird dadurch nicht
entschieden. Der Roslyn-API-/Kommentar-Audit folgt erst nach dem A+-Coverage-Ziel.

## Lokale Nachweisdateien

Rohdaten bleiben lokal ignoriert; Hashes veröffentlichen die Dateien nicht.

| artifacts/-Datei | SHA-256 |
| --- | --- |
| t35-aggregate.json | a3833673e6965ad416ec06aa43d3cb1c0eed6cc3fdd9368f69f46bf4275b06e7 |
| t35-all-methods.json | a5964f0e2a07bc6be9b97f46632f9d338e2d974891a3824c77cf37c510b60abc |
| t35-method-gaps.json | 61b900ade8a482b60d800b77c93f4005944e6f6d9aeff85ac57a1eebd352c3d2 |
| t35-branch-only-gaps.json | 179f959349ccdaab9ca798195a4075d9f5077527f9817ed0e33721b19a0ab106 |
| t35-profile-progress.json | a96f857b7fcacee81017295098a9b4f4b260643f839ed42fd922c865bdb1f9b9 |
| t35-gap-delta.json | 226cb0b94d38d9ac04e8ff6e763a15bb6d5717b1f592705d2bc30b1e7cfd957a |

Orchestrierung: artifacts/t35-measure-all.py. Auswertung:
artifacts/t35-analyze-complete.py und artifacts/t35-compare-gaps.py.
Alle Mess- und Auswertungsprozesse sind beendet.
