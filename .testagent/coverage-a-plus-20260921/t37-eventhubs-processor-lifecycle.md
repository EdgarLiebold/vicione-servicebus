# T37: Event-Hubs-Processor-Cancellation und Partition-Shutdown — offen

Basis `8e741200848fa1496628bf2db81b02d023d18c79`.
Microsoft code-testing-agent und run-tests wurden angewandt; SDK 10.0.302,
MTP und xUnit v3, gezielter Klassenfilter ohne Argumentseparator.
Der zuvor read-only geprüfte Plan liegt lokal unter
artifacts/t37-eventhubs-processor-cancellation-plan.md. Die Produktquellen,
benachbarten Tests und der verwendete LogContext wurden vollständig gelesen.
Kein Produktcode bleibt geändert.

## Nachweiszuordnung

| Produktvertrag | Test in ProcessorCheckpointLifecycleTests |
| --- | --- |
| Event-Abbruch erlaubt Shutdown ohne eigenen Checkpoint und lässt den gleichen Offset einer anderen Partition unbeeinflusst | CanceledEvent_AllowsShutdownWithoutCheckpointAndPreservesOtherPartitionAsync |
| Shutdown wartet auf den bereits betretenen Checkpoint-Callback und dessen erfolgreichen Abschluss | Shutdown_WaitsForInFlightCheckpointBeforeCompletingAsync |

Die Tests verbinden echten EventHubProcessorContext und ProcessorLockContext mit
unterstützten geschützten Partition-Callbacks eines Azure-SDK-Testclients.
Checkpoint-Aufrufe protokollieren Partition und Offset. Die Negativassertion
für das abgebrochene Event erfolgt nach Abschluss beider ursprünglicher
Shutdown-Tasks. Die gemeinsame Lifetime bleibt während der Assertions aktiv.
Der positive Shutdown-Fall hält einen tatsächlich betretenen Callback mit einem
Signal offen und verlangt vor Freigabe ausstehenden Shutdown, danach exakten
gespeicherten Offset. Beide Fälle belegen anschließende erneute Client-Ausleihe.

Cleanup hält die ursprünglichen Shutdown-Tasks fest, gibt Callback-Gates frei,
bricht offene Bestätigungen ab und wartet begrenzt vor Freigabe der Lease.
Ambient-Logging wird restauriert. Keine privaten Felder, Sleeps oder zufälligen
Wiederholungen. Der Host liefert einen echten NullLogger-LogContext.

## Ausführung und Gegenproben

- MAIN: 2/2 bestanden, Exit 0, keine Skips; Verify-only-Format Exit 0.
- Isolierter Prüfbaum: Cancellation-Weiterleitung entfernt; genau der
  Cancellation-Fall scheitert am begrenzten Shutdown-Wait, positive Kontrolle
  besteht (1 Fehler, 1 Erfolg, Exit 2).
- Restauriert, dann Checkpointer-Dispose nicht abgewartet: beide Tests scheitern,
  Shutdown ist vor Freigabe abgeschlossen und der Nachbar-Checkpoint fehlt in
  diesem Lauf. Das kontrollierte Pending-Orakel ist der gezielte Drain-Nachweis;
  die zusätzliche Nachbar-Assertion ist kein deterministischer Mutationszähler.
- Beide Mutationen manuell restauriert; beide Produktdateien ohne Diff.
  Finale isolierte Kontrolle 2/2, Exit 0, keine Skips.
- Unabhängiger Read-only-Review ohne verbleibenden Blocker.

Beim absichtlich beschädigten Drain-Mutanten ist kein vollständiger Worker-Join
nachgewiesen. Callback-Freigabe und Tokenabbruch werden nicht als Ersatz dafür
ausgegeben. Für den restaurierten Produktpfad wird der ursprüngliche Shutdown
abgewartet. Kein Nachweis echter Azure-Ausfälle, privater Cancellation-Tokenidentität
oder der Zulässigkeit eines Checkpoints nach fehlgeschlagener Consumption.

## Lokale Nachweise

GATE ist /private/tmp/servicebus-reply-investigation. Rohdateien bleiben lokal;
Hashes veröffentlichen sie nicht.

| Datei | SHA-256 |
| --- | --- |
| MAIN artifacts/t37-first.log | e42fd0b6a8b491abad9b1d9d2aaa4cc8736da0ce19e07bc54a1a14e352a386f5 |
| MAIN artifacts/t37-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| ProcessorCheckpointLifecycleTests.cs | 3b48a18526d22c7ff0f19d0a5fea973c4c455e4bc4c34a95d15922d08b3c02d5 |
| GATE artifacts/t37-missing-cancellation-mutant.log | 1fbd1e3d9f03e3bf147c32ff0abf48ca05f55feb22e4bf7c87eabf5b52d21b51 |
| GATE artifacts/t37-missing-drain-mutant.log | 3de4a8a3bbcca5f8701153bec5caf0224b5ae48fbe5baed3df2f64b1ab9a201c |
| GATE artifacts/t37-restored.log | 7b1a6e5ae359b0d7627117c6d4c5117c974be2583ffc92aaad54fe8f9aeed962 |

Offen: kanonische CHANGELIST, Commit, vollständige frische 33-Profil-Messung,
Aggregatreview und autorisierter Push. T36 bleibt die maßgebliche vollständige
Messung. Kein A+-Abschluss.
