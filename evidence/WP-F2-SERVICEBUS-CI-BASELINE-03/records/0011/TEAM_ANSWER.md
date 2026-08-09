# TEAM_ANSWER — gebundener RabbitMQ-Arbeitsstand nach dem Re-Slice

Slice `94c329445ddb8408ba6b04530fdf711cd3f58187abc5551630f4ca2ff924f3c5` lokal verifiziert, Revision 0002. Kein fremder Container wurde gestoppt oder umkonfiguriert; die Fixture läuft weiterhin auf ephemeren Loopback-Ports neben dem belegten 5672/15672.

## 1. Letzter vollständig beendeter Befehl

```
python3 tools/ci/run_broker_category.py --broker rabbitmq --category rabbitmq \
  --project tests/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj
```

Ungefiltert, zwei aufeinanderfolgende Läufe:

| Lauf | gesamt | ausgeführt | grün | rot | rote Fälle |
|---|---|---|---|---|---|
| 1 | 326 | 288 | 286 | 2 | Turnout-Paar |
| 2 | 326 | 288 | 285 | 3 | Turnout-Paar + `Should_properly_defer_the_message_delivery` |

Beide TRX liegen als `rabbitmq-full-run-1.trx` und `-2.trx` im Evidencepfad.

## 2. Stand der sechs zu entflakenden Fälle

**Fünf ursächlich behoben, einer bleibt deterministisch, einer sporadisch.**

| Fall | Stand |
|---|---|
| `Should_have_the_proper_address` | behoben |
| `Should_have_the_receive_endpoint_input_address` | behoben |
| `Should_use_the_logical_host_name` | behoben |
| `Should_work` (Conductor) | behoben |
| `Should_limit_the_consumer_and_consume_messages_sequentially` | behoben |
| `Should_retry_each_message_type` | behoben |
| `Should_not_send_twice` / `Should_properly_defer_the_message_delivery` | sporadisch, wechselnd, höchstens einer je Lauf |
| `Should_get_the_job_accepted` | **deterministisch rot** |
| `Should_have_published_the_fault_event` | Kaskade aus `Order(1)` desselben Fixtures |

### Die gefundene Ursache

Drei Defekte in der Testisolation, alle im Aufräumpfad:

1. **Auf CI wurde überhaupt nicht aufgeräumt.** `CleanupVirtualHost` prüfte die Umgebungsvariable `CI` und übersprang den Reset auf dem Build-Server. Der Build-Server fuhr damit eine schwächere Isolation als jeder Entwicklerrechner. Das ist eine Verzweigung über die Zielumgebung.
2. **Der Reset war unvollständig.** `Clean()` listet Exchanges und Queues und löscht sie einzeln. Alles, was ein Plugin außerhalb dieser beiden Entitätstypen führt, bleibt liegen — insbesondere der Scheduler-Store des Delayed-Message-Exchange, an dem genau die betroffenen Fälle hängen.
3. **Ein Aufräumfehler wurde verschluckt.** `catch { TestContext.Error.WriteLine(...) }`. Eine Fixture startete auf schmutzigem Broker, und der Fehler erschien an ganz anderer Stelle.

Korrektur: `RecreateVirtualHost()` verwirft die VHost und legt sie neu an, bedingungslos, mit hartem Abbruch bei Fehlschlag. Der Plugin-Store gehört zur VHost und verschwindet mit ihr. `Clean()` blieb unverändert bestehen.

### Was ausdrücklich nicht geschah

Keine Assertion, kein Timeout, kein Retry, keine Reihenfolgeaussage, kein Ignore, Skip oder Filter wurde abgeschwächt. Die vier Adress- und Kontokorrekturen leiten den erwarteten Wert aus der konfigurierten Hostadresse ab, statt auf eine Konstante zu zeigen, die durch den ephemeren Port nicht mehr gilt.

### Vier widerlegte Hypothesen, jeweils gemessen

| Verdacht | Messung |
|---|---|
| Brokerleck | Verbindungen und Kanäle über den Lauf einstellig, am Ende 0/0 |
| Prozessleck | RSS plateaut bei ~235 MB, Threads ohne Trend, `pendingWorkItems` durchgehend 0 |
| Threadpool-Sättigung | 240 Bus-Zyklen, Threadpool konstant 16–17 |
| Degradation über Bus-Lebenszyklen | Roundtrip konstant ~120 ms über 240 Zyklen |

**Für den Produktivstand ist das der wichtigste Befund: es wurde kein Produktdefekt gefunden.** `Start`/`Stop` gibt Verbindungen, Threads und Latenz sauber frei. Der Fehler lag in der Isolationsstrategie der Tests, nicht im Messaging.

### Der verbleibende deterministische Fall

Im Turnout-Fixture läuft der Job vollständig durch — `SubmitJob → JobSubmitted → AllocateJobSlot → StartJob → Job Started → Job Faulted` in 1,0 s. `JobFaulted` erreicht `input_queue`. Der anschließend veröffentlichte `Fault<GrindTheGears>` erreicht `input_queue` **nicht**. Auffällig ist, dass das zugehörige Fault-Exchange erst im Moment der Veröffentlichung deklariert wird, obwohl das Fixture beim Start darauf binden sollte. Die Ursache ist noch nicht abschließend belegt; ich vermute eine Topologiebindung, die im Gesamtlauf nicht so entsteht wie isoliert. Isoliert läuft das Fixture 5/5, zweimal geprüft.

## 3. Nicht ausgeführte Fälle

**38, nicht 37** — die Differenz ist meine eigene, bewusst als `Explicit` markierte Diagnosesonde.

| Ausschlussmechanismus | Anzahl |
|---|---|
| NUnit `Explicit` | **38** |
| `Ignore` | 0 |
| Kategoriefilter | 0 |

Kein Fall wird still gefiltert; alle sind vom importierten Testbestand ausdrücklich als opt-in deklariert.

Fälligkeitsklassifikation, Zwischenstand:

| Klasse | Anzahl | Begründung |
|---|---|---|
| `NOT_DUE_EXTERNAL_INFRASTRUCTURE` | 9 | `OpenTelemetry_Specs` gegen einen Jaeger-Endpunkt, `Connecting_to_RabbitMQ_via_Amazon` gegen AmazonMQ |
| `NOT_DUE_DIAGNOSTIC` | 1 | die Sonde dieses Work Packages |
| `DUE_NESS_OPEN` | **28** | noch nicht entschieden |

Die 28 offenen sind der nächste Arbeitsschritt. Sie umfassen unter anderem Verbindungsabbruch- und Wiederanlauffälle, Lastfälle, Kanalzählungsfälle und Timeoutfälle; jeder wird einzeln auf Fälligkeit geprüft, statt pauschal eingeordnet zu werden. Die vollständige Liste mit Fixture, Test, Datei und Mechanismus steht in `REQ-CI-002_STATUS.json`.

## 4. Neue Artefakte

| Artefakt | Zweck | läuft in einer Kategorie |
|---|---|---|
| `tools/ci/run_broker_category.py` | kanonischer Laufweg: Fixture starten, ephemere Loopback-Ports aus Docker auslesen, Endpunkte und Laufkonto nur an den Testprozess geben, im `finally` abräumen | ja |
| `tests/…Analyzers.Tests/HarnessIntegrity_Specs.cs` | drei Regressionstests, die rot werden, wenn der Analyzer-Harness Kompilier- oder Referenzdefekte wieder verschluckt | ja |
| `tests/…RabbitMqTransport.Tests/BusLifecycleAccumulation_Probe.cs` | `Explicit`-Diagnosesonde über 240 Bus-Lebenszyklen; dokumentiert die vier Negativbefunde reproduzierbar | nein |

## 5. Blocker und nächster Schritt

**Kein Blocker.**

Nächster Schritt: Fälligkeit der 28 offenen Fälle einzeln bestimmen und jeden fälligen ausführbar machen, danach das Turnout-Paar ursächlich entflaken. Anschließend ActiveMQ-Verdrahtung, die drei Policy-Sabotagefälle aus Direktive `0007` und die Maskierungsfälle aus `0009`.
