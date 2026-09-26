# T35: Wiederkehrendes Scheduling und Endpoint-Abschluss — offen

Basis: `306e1f93b6ba792c586520dca3c018ad17c18b8e`.
Die vollständige T34-Messung bei `a019ca1b2` bleibt maßgeblich.

`RecurringSchedulingCompletionTests.Scheduling_PreservesCommandAndWaitsForEndpointCompletionAsync`
prüft 132 Kombinationen: zwei Schedulerimplementierungen, explizites Send-Ziel
oder Publish-Ziel, elf Nachrichten-/Pipeformen und drei Endpoint-Ausgänge.
Die Formen umfassen konkrete, runtime-typisierte, ausdrücklich als Interface
deklarierte und initialisierte Nachrichten sowie typisierte, untypisierte und
leere Pipes. Die beiden Schedulerquellen und die benachbarten Vertragstests
wurden vollständig gelesen; Microsoft code-testing-agent und run-tests wurden
angewandt. Der vorab geprüfte Plan liegt lokal in artifacts/t35-recurring-scheduling-plan.md.

## Verhaltensnachweise

Der Endpoint führt die echte Command-Pipe während des Aufrufs auf einem
MessageSendContext aus. Ein Completion-Gate hält anschließend den Abschluss
zurück. Die Assertions prüfen Command-Vertrag, unabhängig angegebene URNs,
unterschiedliche Interface-/Klassen-Zieladressen, Payloadwerte und Identität,
Initialisierungsheader, typisierte Pipe-Payloads, Header/CorrelationId-Effekte,
alle acht Scheduleeigenschaften und den separaten abbrechbaren Aufrufertoken.

Vor Freigabe darf kein Handle vorliegen. Nach Freigabe werden entweder exakte
Handleinhalte, dieselbe Exception oder der separate Provider-Abbruchtoken
verlangt. Das Gate wird im finally auch bei einem Assertionfehler freigegeben.
WhenAny beobachtet frühzeitige Fehler direkt, statt sie als Eintrittstimeout
zu verdecken. Die Anforderungszuordnung lautet REQ-VSB-RECURRING-SCHEDULER,
Variante command-contract-and-pipes-await-endpoint-completion.

## Versuche und Gegenproben

- Der erste Versuch verdeckte Fehler im Topologie-Test-Doppel durch Timeouts.
  Er wurde gezielt beendet (Exit 143), nicht als Nachweis gezählt.
- Nach verbesserter Fehlerbeobachtung: 90/132 bestanden, 42 scheiterten an der
  fehlenden generischen Topologievariante des Test-Doppels (Exit 2).
- Test-Doppel manuell korrigiert: generisches out und Type-plus-out werden
  getrennt behandelt. MAIN-Kontrolle: 132/132, Exit 0, keine Skips.
- Verify-only-Formatprüfung fand vier Einrückungen. Manuell korrigiert;
  erneute Verify-only-Prüfung Exit 0.
- Isolierte Publish-Pipe ausgelassen: 24 Fehler / 108 Kontrollen bestanden;
  Initialisierungsheader beziehungsweise typisierte Pipe-Assertions schlagen an.
- Isolierte falsche Publish-Zieladresse: 12 Fehler / 120 Kontrollen bestanden;
  Zieladressassertion schlägt an.
- Isoliertes fehlendes Await beim Publish-Schedule ohne Pipe: 18 Fehler /
  114 Kontrollen bestanden; verfrühter Taskabschluss wird erkannt.
- Alle drei Produktmutationen manuell restauriert; Scheduler-Source-Diff sauber.
  Finale isolierte Kontrolle mit finaler Testdatei: 132/132, Exit 0, keine Skips.
- Unabhängiges Read-only-Review bestätigte die Diagnose-/Topologiekorrekturen,
  Anforderungszuordnung, Assertions und Gatefreigabe ohne weiteren Blocker.

Die Gegenproben betreffen die Publish-Implementierung. Sie belegen keine
gesonderte Mutation der Endpoint-Schedulerimplementierung. Getestet werden
Command-/Pipeverarbeitung und Endpoint-Abschluss, weder Brokerpersistierung
noch tatsächliche wiederkehrende Zustellung. Der Pending-Check allein ist kein
vollständiger Await-Nachweis; die später freigegebenen Fehler-/Abbruchfälle
gehören wesentlich dazu. Kein Produktcode wurde für diese Tests geändert.

## Lokale Hashbelege

MAIN bezeichnet das Produktrepository, GATE /private/tmp/servicebus-reply-investigation.
Die Rohdateien bleiben lokal. Der Quellhash gilt für die finale Testdatei.

| Datei | SHA-256 |
| --- | --- |
| MAIN artifacts/t35-first.log | 53c8c8a6f0a10834b58397e64900f1ae3a945f11a8ef038f4f3f0ca8bd6c4e39 |
| MAIN artifacts/t35-diagnostic.log | 3c324eaa00b5a582958dd991098e3dc8525f3609ff59c7f2257ab085ec879867 |
| MAIN artifacts/t35-corrected.log | b6058e95f2d7a40a1c0a73e61660c96174ac3b58f61a81cc5d62ffc265e3e292 |
| MAIN artifacts/t35-format-final.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| RecurringSchedulingCompletionTests.cs | 41f3e91bb3b72e724f010c8564b83f3cdcc387331f8a70ec7091ba9beea9aeef |
| GATE artifacts/t35-drop-pipe-mutant.log | 7f554819f9b8bcea26415536e48993b4f45c7d530f180317c0e362e101c7fadd |
| GATE artifacts/t35-wrong-destination-mutant.log | 4cf0f6756615fb5423fa777977d437f35e4c3de1865ae45dcfcf26ed60d9dbd5 |
| GATE artifacts/t35-missing-await-mutant.log | 945451c422e1d90c2fb9140db8908cb378e7a5e8ebd172bcb96ca56e5a3a29d2 |
| GATE artifacts/t35-restored.log | 236e8266c11b6be4ba59688bb7ee89523aa1b316875d378d2dfdac7409586a3f |

Offen: kanonische CHANGELIST, Commit, alle 33 frischen Profile am exakten Commit,
unabhängiges Review des Aggregats und autorisierter Push. Kein A+-Abschluss.
