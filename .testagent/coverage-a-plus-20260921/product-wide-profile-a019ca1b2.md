# Vollständige Produktmessung bei a019ca1b2

Gemessener Commit: `a019ca1b287c5745def01ca0ddae42d2e55877db`.
Source-Tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Tests-Tree: `8d0561effcf616c4f394ff12c331b8bf7020b33b`.
Dieser Bericht ist ein Dokumentationsnachfolger, kein erneut gemessener Commit.

## Messergebnis

Alle 33 Profile wurden frisch am selben Commit ausgeführt: 20 Unit-/CPU-Profile
und 13 lokale Integrationsprofile. Alle vier Providergruppen endeten mit
Exitcode 0 einschließlich Aufräumen. SQL Server bestand 75/75 Tests in 5m46s;
kein laufender Prozess wurde neu gestartet. Das strikte Aggregat ist vollständig.
Der unabhängige Read-only-Review bestätigt 487 Datei-Hashes, die Commit-/Tree-,
Profil-/CPU-/Provider-/Assembly-Vollständigkeit sowie alle Kennzahlen und Deltas.

| Kennzahl | Ergebnis |
| --- | --- |
| Erfolgreiche Testausführungen einschließlich CPU-Wiederholungen | 12.695 |
| Produktassemblies | 32 |
| Zeilen | 85.267 / 93.762 = 90,9398264 % |
| Konservativ gezählte Verzweigungen | 30.749 / 36.841 = 83,4640754 % |
| Methodeneinträge einschließlich compilererzeugter Einträge | 26.061 |
| Methoden mit CRAP strikt über 30 | 0 |
| Methodeneinträge mit Zeilenlücken | 4.464 |
| Keine / teilweise Zeilenabdeckung | 2.784 / 1.680 |
| Zusätzliche Einträge mit ausschließlich Verzweigungslücken | 1.505 |
| Vereinigung beider Lückenlisten | 5.969 |

Keine fehlgeschlagenen oder übersprungenen Läufe fließen ein. Die konservative
Verzweigungszählung verwendet maximale beobachtete Anzahlen pro Zeile und
Identität, keine Vereinigung individueller Branch-IDs. Methodeneinträge sind
keine unabhängigen Produktverträge. A+ ist weiterhin nicht belegt.

## Ziel und Vergleich mit T33

Die lokale EF-Saga-Funktion RollbackAsync steigt von 3/5 auf 5/5 Zeilen,
CRAP 1. Die [Verhaltensnachweise](t34-ef-rollback.md) prüfen die unveränderte
ursprüngliche Exception trotz sekundärem Rollback-Fehler, Transaktionsidentität,
Abbruchtoken, fehlenden Commit, Kontextfreigabe und erfolgreiche spätere Reads.
Drei gültige Mutationen schlagen an; die restaurierte Kontrolle besteht 2/2.
Der fehlgeschlagene SELECT schrieb nichts: kein Nachweis für die Rücknahme
vorheriger Schreiboperationen oder einen echten Providerausfall.

Gegenüber T33 sinkt die physische Zeilenabdeckung netto um vier Zeilen, während
die konservative Branch-Anzahl unverändert bleibt. Zwei Zeilenlücken-Einträge
schließen und einer erscheint neu; die Lückenvereinigung sinkt von 5.971 auf
5.969. Der Produktcode blieb unverändert.

Neben dem gezielten Rollback-Nachweis erscheint SagaInstance.MarkInUseAsync
wieder vollständig (16/16 Zeilen, 4/4 Branches), ohne neue gezielte Assertions
für dessen kritisches Interleaving. Das ist keine belastbare Verhaltensschließung.
Neu ohne Abdeckung ist Event Hubs ProcessorLockContext.Canceled (0/3; Zeilen
98,100,101). Weitere gesunkene Methodenzeilen-Beobachtungen: ResourceCache.
CompleteCreationAsync 43 auf 42, JobService.CancelJobAsync 10 auf 9 und
BatchCheckpointer.ReadBatchAsync 19 auf 15. Die Nettozuwächse je Methode summieren
sich auf fünf, die Nettoabnahmen auf neun. Der unabhängige Vergleich aller 66
Coverage-Reports präzisiert die physischen Mengen: sechs Zeilen kommen hinzu,
zehn fallen weg. BatchCheckpointer gewinnt Zeile 92 und verliert gleichzeitig
98,104,107,109,111. Weitere Gewinne: Rollback 269,272 und Saga 109,110,112;
weitere Verluste: ProcessorCanceled 98,100,101, JobService 203 und ResourceCache
193. Die Ursache dieser
Änderungen außerhalb der gezielten Tests ist nicht geklärt; weder Regression
noch erfolgreiche Fehlerbehebung wird daraus behauptet.

## Restarbeit und nächste Auswahl

Die vollständigen Listen liegen lokal in t34-method-gaps.json und
t34-branch-only-gaps.json. Keine Lücke wird durch diese Dokumentation ausgeschlossen.
Wiederkehrendes Scheduling ist als nächster zusammenhängender Bereich vorbereitet:
Nachrichtentyp und Zieladresse, initialisierte Metadaten, tatsächliche Pipe-
Ausführung sowie kontrolliert verzögerter Abschluss mit Erfolg, Fehler und Abbruch.
Der bestehende Race-Nachweis für Saga-Entfernung bleibt gesondert offen.
Der Roslyn-API-/Kommentar-Audit folgt erst nach tatsächlichem A+-Coverage-Abschluss.

## Lokale Nachweisdateien

Rohdaten bleiben lokal ignoriert; Hashes veröffentlichen die Dateien nicht.

| artifacts/-Datei | SHA-256 |
| --- | --- |
| t34-aggregate.json | 1607644ae80daf7250b297980651cf325c9303b9695358aef6c8f94ab7f93076 |
| t34-all-methods.json | 27733e0cea84de7c35f355de0cf4b3e54755228ae5cd0288deafb1ad7069e242 |
| t34-method-gaps.json | 367a3db102dce36fc5a30745802b3ae157575a6a1cfea5b0525d037538c5fe76 |
| t34-branch-only-gaps.json | 47637bc07fe7e9de5af7d9c038eed3dcac24016dd61c498b86e34c19984985d9 |
| t34-profile-progress.json | fd72b233f0c6a4b8794138e0665129ae55f4335029b9c5a27652da040f338f89 |
| t34-gap-delta.json | ff8f0bcf154fb9b03938bd4bca411322057b6a208be359b4748b50461e940090 |

Orchestrierung: artifacts/t34-measure-all.py. Auswertung:
artifacts/t34-analyze-complete.py und artifacts/t34-compare-gaps.py.
Alle Mess- und Auswertungsprozesse sind beendet.
