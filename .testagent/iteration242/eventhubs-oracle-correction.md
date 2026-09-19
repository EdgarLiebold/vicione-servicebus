# EventHubs-Orakelkorrektur: fokussierter Nachweis

Stand: 19.09.2026. Die Korrektur der drei in `eventhubs-failure-triage.md` benannten Fälle liegt bereits im getrackten Commit `a2e9995a3690469e6431fbbe269fab3d56d71bfd` (Tree `c11a03fed066dfbc3e6b140f5ba581a768ea849d`). In dieser Fortsetzung wurde keine Produkt- oder Testdatei erneut geändert. Geprüft wurden die drei geänderten Tests, ihre Requirement-Projektion, das vollständige getrackte EventHubs-LocalIntegration-Testprojekt (18 Dateien) und dessen gemeinsam wirksame Build-, Paket-, Konfigurations-, Fixture- und CI-Eingaben.

## Kausale Disposition

| Requirement / Test | Rot im früheren Lauf | Wirksame Korrektur im Commit |
|---|---|---|
| `OBL-R0-CLOUD-0157` / `RawSdkEnvelope_WithoutContentType_UsesDefaultReceiveSerializerAsync` | Die vorzeitige Assertion auf `MessageSendContext.Serializer` war mit dessen nicht-nullbarem, bei fehlender Konfiguration werfendem Getter unvereinbar. | Das rohe SDK-Event behält `ContentType == null`; nach Empfang werden der konfigurierte Default-ContentType und beide Vertragsfelder geprüft. Die SendContext-Getter-Assertion wurde entfernt, nicht die Empfangsprüfung. |
| `OBL-R0-CLOUD-0156` / `RawSdkEvent_IsConsumedAndPublishesOntoTheBusWithTransportConversationMetadataAsync` | Der erwartete Source-URI behandelte den einzelnen InMemory-Queue-Namen fälschlich als drei URI-Segmente. | Erwarteter Queue-Name wird vollständig URI-escaped; sowohl die tatsächliche Receive-`InputAddress` als auch die veröffentlichte `SourceAddress` müssen exakt übereinstimmen. Payload-, Korrelations- und Initiatorprüfungen bleiben. |
| `OBL-R0-CLOUD-0155` / `BusSendObserver_SeesRiderProduceBeforeAndAfterTheProviderSendAsync` | Ein retained/fremdes Event konnte das erste Consume-Signal erfüllen, bevor die Observer-Assertions erreicht wurden. | Ein absichtlich fremder Marker wird zuerst gesendet und konsumiert, ohne das Zielsignal abzuschließen. Ziel-Consume und Observer-Einträge werden an den versuchseigenen Marker gebunden; exakt `pre, post`, explizite `MessageId` und Destination bleiben Pflicht. |

## Dynamische Ausführung

Der erste Sandbox-Versuch konnte vor Testausführung nicht auf den Colima-Docker-Socket zugreifen (`permission denied`, Exit 1). Nach genehmigtem Docker-Zugriff enthielt der erste Fixture-Aufruf nur `--broker eventhubs`; dadurch erhielt der Testprozess keinen dynamischen Azurite-Port und griff erfolglos auf den Default `localhost:10000` zu. Dieser umgebungsbedingte Teilversuch wurde nach dem ersten Fehlsignal abgebrochen (Exit 130). Sein eigener Runner-Teardown meldete `findings: []` in `artifacts/run-output/vicione-eed612b60a10/fixture-findings.json`. Er ist kein Urteil über die Korrektur.

Der kausal korrigierte Aufruf nannte beide benötigten Dienste:

```text
python3 tools/ci/run_broker_category.py --broker azurite --broker eventhubs --command -- dotnet test --project tests/Transports/ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests/ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.csproj -c Release --no-build --no-restore --filter-method ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration.EventHubInteropAndContextTests.RawSdkEvent_IsConsumedAndPublishesOntoTheBusWithTransportConversationMetadataAsync ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration.EventHubInteropAndContextTests.RawSdkEnvelope_WithoutContentType_UsesDefaultReceiveSerializerAsync ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration.EventHubProducerDeliveryTests.BusSendObserver_SeesRiderProduceBeforeAndAfterTheProviderSendAsync --minimum-expected-tests 3 --results-directory artifacts/test-results/eventhubs-oracle-correction
```

Ergebnis: **3/3 erfolgreich, 0 fehlgeschlagen, 0 übersprungen, Exit 0** (10,686 s). Run-Identität `vicione-db2f00fd621f`; Azurite und EventHubs wurden auf dynamischen Loopback-Ports bereitgestellt. Das zugehörige `artifacts/run-output/vicione-db2f00fd621f/fixture-findings.json` enthält `findings: []`, mit Log-Digests für beide Dienste. Die Runner-Ausgabe bestätigt den eigenen Teardown ohne Finding.

## Grenze des Befunds

Dies ist ein gefilterter Drei-Fälle-Nachweis auf einem **nicht eingefrorenen gemeinsamen Arbeitsbaum**: andere Teams hatten außerhalb dieses Testprojekts uncommittete Produkt- und Teständerungen. Der `--no-build`-Lauf verwendete das zuvor im selben Arbeitsbaum erstellte Release-Testartefakt. Weder das unfiltrierte EventHubs-Projekt noch die LocalIntegration-Gesamtsuite, ein realer Azure-Dienst oder die gesamte A+-Matrix wurden hier ausgeführt oder als grün behauptet. Der frühere Wholefork-Log ist nur Red-Evidence für die ursprünglichen drei Orakel, keine aktuelle Gesamtbaseline.
