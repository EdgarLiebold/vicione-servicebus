# T38: Azure-Service-Bus-Subscription-Processor — offen

Basis `72849192887ed84d563be12c76635f1e9a473dba`.
Microsoft code-testing-agent und run-tests angewandt; SDK10.0.302, MTP/xUnit v3,
gezielter Klassenfilter. Der zuvor read-only geprüfte Plan liegt lokal unter
artifacts/t38-asb-subscription-plan.md. Produktquelle SubscriptionClientContext,
SubscriptionSettings und benachbarte ServiceBusConnectionContextTests wurden
vollständig gelesen; verwendete SDK-Hooks anhand der installierten 7.20.2-XML-
Dokumentation und erfolgreichem Build geprüft. Kein Produktcode bleibt geändert.

## Nachweiszuordnung

Alle Tests in SubscriptionProcessorBehaviorTests.

| Produktvertrag | Test | Fälle |
| --- | --- | --- |
| Message-/Session- und Fehlercallbacks erhalten Identität/Token und warten auf Erfolg, Fehler oder Abbruch | Dispatch_PreservesCallbackIdentityAndWaitsForItsResultAsync | 12 |
| Vier Konfigurationskombinationen werden abgewiesen, ursprünglicher Callback und einzelne Factory-Erzeugung bleiben erhalten | Reconfiguration_RejectsEveryKindCombinationAndPreservesOriginalCallbackAsync | 4 |
| Start/Stop/Close/Dispose warten; Start propagiert Fehler/Abbruch, Stop/Close/Dispose protokollieren und schlucken sie | Lifecycle_AwaitsSdkAndPreservesFailureContractAsync | 24 |
| Geschlossene Prozessoren überspringen Stop/Close; Dispose leitet Default-Token an Close weiter | ClosedState_ControlsStopCloseAndDisposeUsesDefaultTokenAsync | 4 |

Die Tests verwenden echte SubscriptionClientContext-Registrierung und geschützte
SDK-Dispatch-Hooks. Session-Prozessoren verwenden den vorgesehenen Konstruktor mit
Client, Topic und Subscription; der Test besitzt und disponiert diesen Client.
Lifecycle-Overrides zeichnen Aufrufe auf und liefern kontrollierte Tasks.
Assertions befinden sich außerhalb der vom Produkt abgefangenen Overrides;
Warnungen werden mit Level, Originalformat, ursprünglicher Exception und
InputAddress geprüft. Eintrittssignale gehen Pending-Assertions voraus.
Cleanup löst Gates, beobachtet ursprüngliche Tasks und restauriert Ambient-Logging.

## Ausführung und Gegenproben

- Erstlauf: 19 bestanden, 19 Setupfehler, Exit2. Der parameterlose SDK-Session-
  Processor wirft beim Event-Add NullReferenceException. Kein Produktfehlerbeleg.
- Testaufbau korrigiert, sechs Dispose-Abschlussfälle ergänzt: MAIN44/44, Exit0,
  keine Skips. Finale Verify-only-Formatprüfung Exit0.
- Session-Delivery-Token durch None ersetzt: 3 Fehler/41 Kontrollen, Tokenassertion.
- Session-Error-Registrierung entfernt: 3/41; tatsächlicher SDK-Dispatchfehler
  NullReferenceException, keine behauptete spezifische Registrierungsassertion.
- Session-Start nicht abgewartet: 3/41, Pending-Assertion.
- Message→Session-Guard entfernt: 1/43, erwartete Ausnahme fehlt.
- Alle vier Gegenproben Exit2; jeweils manuell restauriert. Produktdatei ohne
  Diff, finale isolierte Kontrolle44/44, Exit0, keine Skips.
- Unabhängiger Read-only-Abschlussreview ohne verbleibenden Blocker.

Kein Nachweis echten Brokerbetriebs, Session-Ownership, Lock-Renewal oder
Settlement. Factory-Settings-Identität beweist keine SDK-Optionsprojektion;
Error-Wiring beweist keinen Supervisor-Shutdown. Fixture-Cleanup setzt kontrollierte
Prozessoren auf geschlossen; vollständiges SDK-Processor-Disposal wird damit
nicht nachgewiesen. Kein globaler A+-Abschluss.

## Lokale Nachweise

GATE ist /private/tmp/servicebus-reply-investigation. Rohdateien bleiben lokal;
Hashes veröffentlichen sie nicht.

| Datei | SHA-256 |
| --- | --- |
| MAIN artifacts/t38-first.log | 815475f1f66e840376931d3aa24e00fb0e96ef1be69aa43fe9233035f80975ad |
| MAIN artifacts/t38-corrected.log | 3a2a66344cc2deb588fe8e40f52ad8944362487bbb28a73042c00a2cfbd3097b |
| MAIN artifacts/t38-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| SubscriptionProcessorBehaviorTests.cs | 25188cfaf1e9e9241ab7293422111668819da11b94b344518268446afe1e7481 |
| GATE artifacts/t38-token-mutant.log | 5608416e8a90525955ee87b7d656fcc92ef74df93557447b10024704c3348e86 |
| GATE artifacts/t38-error-mutant.log | 3b06f3953f9cf042163bdbed8a4f03307d8290e3fd73a3ff59e29154cf740008 |
| GATE artifacts/t38-start-mutant.log | f0ba19a2ed8e61a8a86e29784bfee07dc62d9d545948a5073b1c88bf5b79ee87 |
| GATE artifacts/t38-guard-mutant.log | 4a219e50fad5af99cbe64ee4d0e83a2fb9c0eecf2ee5f1b8e6bf472ffda41b1d |
| GATE artifacts/t38-restored.log | 2de218efc5955009e4c514bb388280e4c19d8db257b3f4011aee4c41810c1331 |

## Gesamtmessung und Quartz-Testkorrektur

Der erste vollständige Lauf am Commit
`11362883c6c0e3399c927f86b75f325a19cd7faa` endete mit Exit1:
28 Profile/12556 erfolgreiche Ausführungen verifiziert, ActiveMQ lokal99/100,
vier weitere Profile nicht ausgeführt. Kein gültiges Gesamtaggregat.
AzureServiceBus.Tests bestand380/380 einschließlich der44 neuen Fälle.

`ActiveMqQuartzSchedulingTests.QuartzScheduledPublish_ReachesConsumerExactlyOnceAsync`
scheiterte für activemq an der Trigger-Abwesenheitsassertion. Quartz4.0.1 meldet
TriggerFinalized vor NotifyJobStoreJobComplete; der Callback beweist noch keine
Store-Entfernung. Reihenfolge anhand der offiziellen JobRunShell/RAMJobStore-
Quellen geprüft. Der Test wartet nun nach der Tokenprüfung mit eigenem Zeitlimit
auf `IScheduler.Exists(finalizedKey) == false`. Alle bisherigen Abwesenheits-,
Zustellungs- und Brokerzählerassertionen bleiben erhalten. Die Zustandsabfrage
verändert den Scheduler nicht. Kein Produktcode wurde geändert.

- Erster Korrekturlauf scheiterte beim Build: CheckExists existiert in der
  installierten Version nicht. API anhand lokaler Quartz-XML auf Exists korrigiert.
- Korrigierte MAIN-Kontrolle:2/2, Exit0, keine Skips; Verify-only-Formatprüfung0.
- Isolierte Lifecycle-Gegenprobe: TriggerFinalized signalisiert den Test und
  wartet anschließend auf ein Gate. Dadurch kann Quartz den echten ursprünglichen
  Trigger noch nicht entfernen. Beide Protokolle scheitern nach30s am Removal-Wait
  mit TaskCanceledException, Exit2. Cleanup gibt das Gate vor Bus-Stop/Lease-Dispose
  frei. Kein Quartz-Produktmutant und kein aus dem Exitcode abgeleiteter Worker-Join.
- Gegenprobe manuell zurückgenommen, MAIN/GATE bytegleich; restaurierte isolierte
  Kontrolle2/2, Exit0, keine Skips. Read-only-Review ohne weiteren Blocker.

| Zusätzlicher lokaler Nachweis | SHA-256 |
| --- | --- |
| MAIN artifacts/t38-profile-29/tests.log | 9ff97c13f813bb245e8bb7c997905d0caf3a8b525ea1e0f3a834ff6c1a621b72 |
| MAIN artifacts/t38-profile-progress.json | 07bab1289e10b778464024c48bc7bc379c7fe6bf9b3914166d981f04736e8a20 |
| MAIN artifacts/t38-quartz-fix.log | cf124368bd8c8901f4f5cb4a1186dfba623ddece92ebc6541d624e7f6df6dc62 |
| MAIN artifacts/t38-quartz-fix-corrected.log | 441147d3a01a80bbb6011e3fc5850f6b2ae2236d0c8897ddce447be5a55544d2 |
| MAIN artifacts/t38-quartz-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| GATE artifacts/t38-quartz-retained.log | 89d07090feeb633a833deb1c91bacfd40223e2686e4abaf4164051c14c02091f |
| GATE artifacts/t38-quartz-restored.log | f66580b1009e4ea7a3f1643a9571f18558683efec09f31e79c535f439c27db86 |

Offen: Korrekturcommit, vollständige frische33-Profil-Messung unter t38b,
Aggregatreview und autorisierter Push. T37 bleibt die maßgebliche vollständige Messung.
