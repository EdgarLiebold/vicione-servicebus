# Azure Service Bus Receiver-Fehlercallback: fokussierte Testergänzung

Stand: 19.09.2026. Statische Implementierung im gemeinsamen Arbeitsbaum; kein .NET-, MSBuild-, Format- oder Testprozess durch diesen Agenten. Dynamische Abnahme bleibt beim Lead.

## Änderung und Grenze

- Neu: `tests/Transports/ViciOne.ServiceBus.AzureServiceBus.Tests/ServiceBusReceiverErrorCallbackTests.cs` mit sechs Testmethoden und acht Theory-Datenzeilen.
- Ergänzt: sechs eindeutige Varianten in `Requirements/AzureServiceBusRequirements.json` desselben Projekts.
- Unverändert: `src/**`, sonstige Testprojekte, gemeinsame `.testagent`-Ledger und der Azure-Receiver. `Receiver.cs` hat weiterhin SHA-256 `783eb2f8fc1b79b87f437ead5d3cf7c418c913d63e09837b989a1b11cc42a71a`.
- Der Test erfasst ausschließlich den von `Receiver.Start()` beziehungsweise `SessionReceiver.Start()` tatsächlich bei `ClientContext` registrierten Fehlerdelegate. Es gibt keinen Private-Method-Reflection-Aufruf und keinen Produkt-Test-Hook. Die Testdoubles bilden nur die benutzte Client-/Endpoint-/Dispatcher-Schnittstelle ab.

## Kausale Orakel

| Fall | Test und diskriminierende Assertion |
|---|---|
| ServiceCommunicationProblem, auch transient; MessagingEntityNotFound; nichttransienter ServiceTimeout als Fallback | `QueueProcessorErrorCallback_RecyclesOnlyTheRequiredServiceBusFailuresAsync`: exakt eine Fault-Benachrichtigung mit identischem Exceptionobjekt und dem SDK-`EntityPath`, der absichtlich vom Exception-Entitynamen und der konfigurierten Adresse abweicht. |
| MessageLockLost, SessionLockLost, MessagingEntityDisabled und sonstiger transienter ServiceTimeout | Dieselbe Theory: keine Fault-Benachrichtigung und kein begonnenes Receiver-Stoppen. |
| MessageTimeToLiveExpiredException und MessageLockExpiredException | `QueueProcessorErrorCallback_ExpiredMessageAndLockDoNotRecycleAsync`: nach jedem Callback keine Fault-Benachrichtigung und kein Stoppen. |
| Unbekannter nichttransienter Fehler | `QueueProcessorErrorCallback_UnknownNonTransientFailureRequestsRecycleAsync`: exakt eine Benachrichtigung mit Originalexception und SDK-Entitypfad. |
| Sessionregistrierung | `SessionProcessorErrorCallback_UsesTheInheritedRecycleHandlerAsync`: Sessiondelegate statt Queuedelegate registriert; geerbte Recyclewirkung mit Originalexception und SDK-Entitypfad. |
| Asynchrone Completion | `QueueProcessorErrorCallback_WaitsForFaultNotificationBeforeCompletingAsync`: kontrolliert unvollständige Benachrichtigung hält den Callback offen; nach Freigabe schließt er ab. |
| Tatsächlicher Receiver-Lifecycle | `QueueProcessorErrorCallback_RecycleStopsReceiverButIgnoredFailureLeavesItRunningAsync`: nach ignoriertem Lockverlust bleiben `Stopping` ungesetzt und `Completed` offen; ein anschließender Kommunikationsfehler am selben Receiver führt über den registrierten Delegate zu `Stopping` und erfolgreichem `Completed`. Beide positiven Wartepunkte haben eine gemeinsame endliche Test-Cancellation. Das trifft insbesondere eine entfernte `TrySetConsumeException`-Weitergabe. |

Die Assertions sollten einen invertierten Reason-Zweig, eine unterdrückte `NotifyFaultedAsync`-Benachrichtigung und einen vertauschten Entitypfad jeweils einzeln treffen. Das ist eine statische Erwartung, kein ausgeführter Mutationsnachweis. Die SDK-Fehlerargs und ServiceBusException-Signaturen wurden aus der lokal aufgelösten Azure.Messaging.ServiceBus-7.20.2-API geprüft; Projektverweise auf den Transport und xUnit/MTP sind bereits vorhanden.

## Statische Prüfung und offene Nachweise

- `jq empty` für die Requirements-Datei: erfolgreich.
- `jq -e 'map(.variantKey) | length == (unique | length)'`: `true`.
- `git diff --check -- tests/Transports/ViciOne.ServiceBus.AzureServiceBus.Tests`: ohne Ausgabe, Exit 0.
- Testdatei SHA-256 `20fcead3d7ed83d7728642d37cf03708d9b217d9d3061dd98c61b55a47308815`; Requirements SHA-256 `452c2352484a965385d4e152998059d2b9091199fab01ba62ce4d7d575cd0371`.
- Fokussierter Projektbuild, Testlauf, unfiltriertes Pflichtprofil und konkrete Mutanten sind **nicht ausgeführt**. Nur der Lead darf sie nach A-0071 starten. Das Projekt ist deshalb nicht als grün oder fertig abgenommen gemeldet.
- `OperationCanceledException` und `ObjectDisposedException("$cbs")` sind absichtlich nicht mit einem Shutdown-Orakel festgeschrieben. Der statische Hotspot-Bericht weist auf die Spannung zwischen Logunterdrückung und Recycleklassifikation hin; SDK-Stop-Semantik und realer Callbackzeitpunkt sind hier nicht bewiesen. Ein etwaiger Produktfehler benötigt Lead-Disposition und getrennten Produktscope.

Die verpflichtende `dotnet-test:code-testing-agent`-Anleitung hat die enge, vorhandene Projekt-/Frameworkkonvention, die API-Signaturprüfung und die verhaltensunterscheidenden Assertions bestimmt. Ihre dynamische Completion-Bedingung bleibt wegen der Lead-exklusiven .NET-Prozessgrenze offen.
