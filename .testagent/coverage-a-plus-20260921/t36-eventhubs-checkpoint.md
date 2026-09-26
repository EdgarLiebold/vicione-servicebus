# T36: Event-Hubs-Checkpoints und begrenzte Admission — offen

Basis `889c8984c4ebb18b2f6fe84ec4121a71db5be0f2`.
T35 bei `3a59fff96` bleibt die maßgebliche vollständige Messung.
Microsoft code-testing-agent und run-tests wurden angewandt; der zuvor
read-only geprüfte Plan liegt lokal in artifacts/t36-eventhubs-checkpoint-plan.md.
Die dort aufgeführten Produktquellen und benachbarten Cancellation-Tests wurden
vollständig gelesen. Kein Produktcode bleibt geändert.

## Nachweiszuordnung

| Produktvertrag | Test in BatchCheckpointBehaviorTests |
| --- | --- |
| Neuester akzeptierter Offset; Rückfall nach null, einer oder zwei Ablehnungen ohne redundante Updates | CompletedBatch_CheckpointsNewestAcceptedOffsetWithoutRedundantWritesAsync (drei Fälle) |
| Erfolgreiche spätere Verarbeitung nach drei fehlgeschlagenen Checkpoint-Callbacks | FailedCheckpointBatch_AllowsTheNextBatchToPersistAsync |
| Abbruch nur des wartenden Queue-Eintrags; vollständige Verarbeitung angenommener Einträge | FullQueue_CancelsOnlyWaitingAdmissionAndDrainsAcceptedEventsAsync |

Die fünf Fälle verwenden den echten BatchCheckpointer und PendingConfirmation
mit unterstützten Azure-SDK-Modellobjekten und Checkpoint-Callbacks. Alle Events
gehören derselben Partition an. Bei Dreierbatches bleiben Bestätigungen bis zur
vollständigen Aufnahme offen. Provider-Callbacks protokollieren exakte Reihenfolge
und Token; der simulierte gespeicherte Offset ändert sich nur bei Erfolg.
Negativassertions erfolgen nach vollständig beendetem Worker.

Backpressure entsteht kausal: erster Checkpoint blockiert, zweite Aufnahme ist
beendet, dritte muss warten. Deren separater Token wird abgebrochen; exakter
Token, Taskstatus und später ausschließlich die ersten beiden Updates werden
verlangt. Cleanup gibt alle Gates und Bestätigungen frei, bricht nötigenfalls
die Worker-Lifetime ab und wartet begrenzt auf Worker und wartende Admission.
Keine Sleeps, privaten Hooks oder zufälligen Wiederholungsschleifen.

## Ausführung und Gegenproben

- Erster MAIN-Build: Exit 1, xUnit1051 für vier Cleanup-Waits. Keine Testevidence.
  Die zeitlich begrenzten Cleanup-Waits wurden manuell mit ausdrücklich nicht
  abbrechbarem Token versehen; normale Test-Waits beachten Test-Cancellation.
- Korrigierte MAIN-Kontrolle: 5/5, Exit 0, keine Skips. Verify-only-Format Exit 0.
- Erste Fallback-Mutation: Exit 1, CS0162, nicht als Gegenprobe gezählt.
- Kompilierbare vorzeitige Fallback-Beendigung: 3 Fehler, 2 Kontrollen bestanden.
  Zwei Reihenfolgeassertions schlagen an; im Fall vollständiger Ablehnung fehlt
  das Signal des ältesten Versuchs und der begrenzte Wait läuft ab.
- Ältester statt neuester Offset zuerst: 4 Fehler, 1 Kontrolle bestanden;
  Reihenfolge-/Offset-Orakel unterscheiden die falsche Auswahl.
- Admission-Token ignoriert: 1 Fehler, 4 Kontrollen bestanden. Der wartende Task
  liefert Timeout statt der geforderten OperationCanceledException. Das Cleanup
  beendet trotzdem Worker und Admission; Prozess Exit 2.
- Alle Mutationen manuell restauriert, Source-Diff für BatchCheckpointer sauber.
  Finale isolierte Kontrolle: 5/5, Exit 0, keine Skips.
- Unabhängiger Read-only-Review ohne verbleibenden Blocker; Zuordnung,
  Assertiontiefe, Cleanup und Grenzen bestätigt.

Die Consumer-Bestätigungen sind hier erfolgreich; nur Checkpoint-Callbacks werden
abgelehnt. Kein Nachweis echter Azure-Ausfälle, keiner für ProcessorLockContext-
Weiterleitung und keine Aussage über die Zulässigkeit von Checkpoints jenseits
fehlgeschlagener Consumption. Der Test simuliert Persistierung im Callback;
er schreibt keinen Azure-Checkpointstore.

## Lokale Nachweise

MAIN ist das Produktrepository, GATE /private/tmp/servicebus-reply-investigation.
Rohdateien bleiben lokal; Hashes veröffentlichen sie nicht.

| Datei | SHA-256 |
| --- | --- |
| MAIN artifacts/t36-first.log | faece7990e18e53ffae13b77ecb1e2d67e50110650b8f546d39969c049a7112f |
| MAIN artifacts/t36-corrected.log | c9b19ab601049ad939b6f245ae3ee14e0ef0cb9c17e18379bafd590a180ebdf1 |
| MAIN artifacts/t36-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| BatchCheckpointBehaviorTests.cs | b7441ebd5153f5c4cc268b21bd221cf15fab154686c736c4212faf0ddbc1fb4c |
| GATE artifacts/t36-stop-fallback-mutant.log | 7ad26b719bd8dc57caf8d2a2fe354c15194602ad88f57c3663430843bf270ce4 |
| GATE artifacts/t36-stop-fallback-valid-mutant.log | 3ea20a1c95136bea5585d24413f4acd2444e83dbd9508846d974738d87b2ca49 |
| GATE artifacts/t36-oldest-first-mutant.log | fdbbc03a4f86b87198c985a7e0cdce6cd1acd4e548dacfe4efe669a0cda55e44 |
| GATE artifacts/t36-ignore-cancellation-mutant.log | 337c3bfa158c0fbb546b586fc7e7c95c714c454213a7dda10916d9c643ed94f1 |
| GATE artifacts/t36-restored.log | b800818a77f5914a2afe0c0140b60ecb7d08d7d31d6ad941bbfbfedcbcfaada3 |

Offen: kanonische CHANGELIST, Commit, alle 33 frischen Messprofile, unabhängiger
Aggregatreview, Dokumentationsabschluss und autorisierter Push. Kein A+-Abschluss.
