# T59 — Saga-Request-Lebenszyklus als größeres Paket

## Ausgangspunkt und Zuschnitt

T58 ist mit 33 Profilen und unabhängigem Audit abgeschlossen. Die Messung am
Implementierungscommit `5e9509367` ergibt 13.257 bestandene Fälle,
91,68462 % physische Zeilen, 84,29095 % konservative Zweige und keinen
CRAP-Wert über 30. Globale Line-/Branch-A+ ist offen. T59 bündelt die
mehrteiligen Saga-Requests mit gespeicherter Request-ID und realem Quartz-Timeout.
Es gibt **eine** neue vollständige 33-Profil-Messung nach Implementierung,
adversarialem Schlussreview und kausalen Gegenproben des ganzen Pakets.
Während der Entwicklung laufen nur betroffene Testklassen.

Der bisherige Microsoft-Roslyn-Pairinglauf aus T58 (`/private/tmp/vsb-t58-pairing.json`)
ist der statische Einstieg; die gemessenen T58-Gaps bleiben Auswahlbasis.
Statische Zuordnung ist kein Nachweis fehlender Ausführung. Die Produktpfade sind
`ViciOneServiceBusStateMachine.Request` (zwei/drei Antworten),
`StateMachineRequestExtensions`, `StateMachineRequest` und Quartz-Scheduling.
Vorhandene echte Kontrollfälle: eine Antwort mit Property-ID, drei Antworten
ohne Property-ID, Quartz-Saga-ID mit Antwort/Fault/Timeout/Callback, einantwortige
Request-Folgegeneration und asynchrone Callback-Journeys. Diese Verträge werden
nicht als neue Abdeckung gezählt.

## Akzeptanzmatrix

| Familie | Neues Verhalten und harte Orakel | Status |
| --- | --- | --- |
| Property-ID und drei Antworttypen | Request-ID unterscheidet sich von Saga-/Body-ID; ResponseAddress und Accept-URNs stimmen; nur richtiger Owner erhält Antwort; exakter Quartz-Trigger wird gelöscht. | Fünf Varianten in `PropertyRequestId_RoutesTheExactOwnerAndSettlesItsQuartzTriggerAsync` und vier Zwei-Owner-Varianten in `PropertyRequestId_ReplySettlesOnlyItsOwnerWhileNeighborTimesOutAsync` grün. |
| Property-ID, Fault und Timeout | Originaldiagnose und Fehlerklasse erhalten; echter Quartz-Trigger liefert Timeout; Zustand und Trigger des Nachbarn bleiben erhalten. | Fault/Timeout in der ersten Matrix und Fault plus nachfolgender echter Nachbar-Timeout im Zwei-Owner-Test grün. |
| Überschriebene Korrelation | Für Completed/Completed2/Completed3/Faulted/TimeoutExpired gilt bewusst die konfigurierte Body-ID; die Request-Owner-Saga, ihre gespeicherte Request-ID und ihr Quartz-Trigger bleiben unverändert. | Fünf Varianten in `PropertyRequestId_CustomCorrelationSelectsBodyOwnerWithoutCancelingRequestOwnersTriggerAsync` grün. |
| Fehlende/falsche Request-ID | Kein Zustandswechsel, keine fremde Triggerlöschung; Empfangsfehler nur mit tatsächlich beobachtetem Receive-Pfad behaupten. | Vier fehlende Header in `PropertyRequestId_MissingHeaderCannotSelectUnownedSagaOrCancelItsTriggerAsync` und sieben falsche IDs im Zwei-Owner-Test grün. |
| Folgegeneration | Späte Antworttypen, Fault und Timeout der alten Generation ändern weder neuen Request noch neuen Trigger; aktuelle Antwort schließt genau einmal. | Drei bestehende Einantwort-Kontrollen und neun neue Zwei-/Dreiantwort-Varianten laufen zusammen 12/12 in `PreviousRequestMessages_CannotCompleteOrCancelTheNextRequestOfTheSameSagaAsync`. |
| Zweiantwort-Variante | Dieselben Owner-/Fault-/Timeout-Grenzen auf der separaten zweiantwortigen Request-Registrierung. | Drei Zwei-Owner-Fälle mit echter Antwort/Fault und Nachbar-Timeout in `PropertyRequestId_ReplySettlesOnlyItsOwnerWhileNeighborTimesOutAsync` plus vier Folgegenerationen grün. |
| Asynchrone Request-Bindung | Nur falls nach Quellenprüfung eigenständig: Pending-Barriere, dynamische Zieladresse und Nachbarsaga mit exaktem Transportziel. | Nicht Teil von T59: Die vorhandene T47-Callback-Journey deckt diesen Pfad bereits ab; das Auswahlreview fand keinen eigenständigen neuen Vertrag. |

## Bisherige Umsetzung und Qualitätsgrenzen

Die bestehende Quartz-Integration wurde um einen Property-ID-Modus erweitert.
Die Saga-ID-Kontrollgruppe und 21 neue Property-ID-Fälle laufen zusammen
35/35 grün. Weitere neun Zwei-/Dreiantwort-Folgegenerationsfälle plus drei erhaltene
Einantwort-Kontrollen laufen 12/12 grün. Das gesamte Quartz-Projekt besteht
318/318 einschließlich der aktualisierten Requirement-Projektion.
Ein erster Lauf zeigte zwei Timeout-Fehler im neuen
Callback/Fault- und Callback/Second-Orakel: Die Body-Owner-Saga hatte keine
gespeicherte Request-ID und kann folglich keinen Request-Trigger stornieren.
Das Orakel prüft jetzt das Ausbleiben dieser Stornierung und den fortbestehenden
Trigger des ursprünglichen Request-Owners. Der Zwei-Owner-Test musste zunächst
auf beide vollständig angelegten Quartz-Trigger warten; die ersten 2/4 Fälle
hatten eine Testaufbau-Race. Der erste fehlende-ID-Versuch erwartete fälschlich
eine Receive-Exception und später null PostConsume-Ereignisse. Der beobachtete
Property-ID-Pfad schließt die Zustellung ohne Saga-Treffer ab; Zustand, Ausgabe
und Trigger bleiben unverändert. Ein zunächst fehlender Assertion auf die
gespeicherte ID des Request-Owners wurde nach Red-Team-Befund ergänzt.
Die Folgegenerationsmaschinen für zwei und drei Antworten sind vom ursprünglichen
Einantwort-Kontrollmodell getrennt. Beide neuen Zwei-Owner-Korrelationen wurden
isoliert auf `Guid.Empty` mutiert: `Completed2` machte genau den zweiten
Zweiantwort-Fall rot, `Completed3` genau den dritten Dreiantwort-Fall.
Die Produktdatei ist jeweils bytegenau auf SHA-256
`b34154afbdcca7bef514d35fd589519f2ae14c7b9d2bdb1f940cb87f5856eeae`
zurückgestellt. Verify-only-Formatprüfung und `git diff --check` bestehen.
Kein Produktcode geändert, noch kein Vollprofil und keine A+-Behauptung.

Das read-only Red Team hat die Auswahl gegen T18/T20/T47 und den fertigen
Testcode geprüft. Zwei konkrete Orakellücken wurden geschlossen; die
Nachprüfung fand keinen weiteren konkreten Blocker. Die Microsoft-Skills
`code-testing-agent`, `find-untested-sources`, `run-tests`, `test-gap-analysis`
und `assertion-quality` wurden angewandt; die neuen Matrizen prüfen exakte
Ergebnisse, Saga-Zustände, gespeicherte IDs, Transportheader, Trigger und
negative Nachbarwirkungen. Das Paket ist bereit für den einmaligen frozen
full33-Lauf samt Audit, Changelog, CHANGELIST und autorisiertem Push.
