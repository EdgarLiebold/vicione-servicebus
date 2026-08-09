# PROGRESS — Fälligkeit der 28 Explicit-Fälle gemessen, Policy und ActiveMQ nachgezogen

## Korrektur zu Record 0011

`byDueness.DUE_NESS_OPEN` und die Fallliste sagten 28; der Schlusssatz von `records/0011/REQ-CI-002_STATUS.json` nannte irrtümlich 35. Der korrekte Wert ist **28**. Die Korrektur erfolgt hier append-only, `0011` bleibt unverändert.

## Fälligkeit — gemessen, nicht geschätzt

Ein Batch-Lauf über alle 28 war unbrauchbar: Ein einziger Fall, der nie fertig wird, verdeckt das Ergebnis aller anderen. Der erste Versuch hing **39 Minuten** und lieferte kein einziges Resultat. Deshalb jetzt: eigene Fixture, eigener Lauf und harte Zeitschranke von 150 Sekunden je Fall — jeder Fall endet als `PASSED`, `FAILED` oder `TIMEOUT`, Stille gibt es nicht mehr.

| Klasse | Anzahl |
|---|---|
| `DUE_AND_GREEN` | **18** |
| `DUE_OPEN_DEFECT` | **5** |
| `NOT_DUE_EMPTY_ASSERTION` | 2 |
| `NOT_DUE_MANUAL_OBSERVATION` | 2 |
| `NOT_DUE_UNBOUNDED_LOAD` | 1 |

Jeder `NOT_DUE`-Fall trägt einen konkreten, prüfbaren Grund:

- **Leere Assertion (2):** `Configuring_the_publish_topology_at_startup.Should_create_the_exchanges` und die Namespace-Variante haben einen **leeren Methodenrumpf**. Sie melden `PASSED` und behaupten nichts. Genau deshalb reicht das Laufergebnis allein nicht — das musste am Quelltext auffallen, nicht am Outcome.
- **Unbegrenzte Last (1):** `Pounding_the_crap_out_of_the_send_endpoint` lief über 400 Sekunden ohne Abschluss und musste abgebrochen werden. Das ist der Fall, der den ersten Batch zum Hängen brachte.
- **Manuelle Beobachtung (2):** beide `Should_take_time_to_watch_channel_use`. Der Name nennt die Absicht — ein Mensch beobachtet die Kanalzahl. Unbeaufsichtigt laufen sie in den Harness-Timeout.

Eine eigene Fehlannahme ist damit widerlegt: Ich hatte `Should_recover_from_a_crashed_server` als Hänger benannt. Der Fall läuft in 59 Sekunden **grün** durch.

## Die fünf offenen Defekte

| Fall | Befund |
|---|---|
| `Should_properly_fail_on_exclusive_launch` | erwartete `RabbitMqConnectionException` beim zweiten Anspruch auf eine exklusive Queue; keine Ausnahme geworfen |
| `Should_cancel_on_shutdown_and_then_restart_the_job` | `TimeoutException`, dieselbe Job-Service-Familie wie das offene Turnout-Paar |
| `Should_fault_with_operation_cancelled_on_publish` | Assertion prüft `Throws.TypeOf<OperationCanceledException>`, tatsächlich kommt `TaskCanceledException` — die davon **erbt** |
| `Should_source_address_from_the_endpoint` | `TaskCanceledException`, Ursache offen |
| `Should_properly_handle_message_redelivery` | `Consumed.Any<TextMessage>(x => x.Exception == null)` lieferte `False` |

Der dritte Fall ist bemerkenswert: `TypeOf` prüft exakt, `InstanceOf` würde passen. Diese Assertion zu lockern wäre eine Abschwächung und damit ausdrücklich keine Entwicklerentscheidung. Ich behandle ihn als offenen Defekt und nicht als Anpassungskandidaten.

## Policyvalidator nach Direktive 0007 und 0009

Vier neue Regeln, **30 Selbsttests grün**: ephemere Ports erzwungen statt fester, umgangener kanonischer Runner, wirksame bekannte Zugangsdaten, Testmaskierung im Pflichtpfad (`--filter Category!=`, `--blame-hang`, Skip-Schalter).

Die Hostbindungsregel wurde dabei umgedreht: Bisher verlangte sie `127.0.0.1:PORT:PORT`, jetzt `127.0.0.1::PORT`. Ein fester Port ist genau der Zustand, den `0007` beseitigt hat.

**Der Validator hat sofort einen realen Verstoß bei mir gefunden:** Die Workflows riefen `run_test_category.py` direkt auf und starteten die Fixture selbst, umgingen also den kanonischen Runner. Korrigiert.

## ActiveMQ

Derselbe Defekt wie auf der RabbitMQ-Seite gefunden und entfernt:

```csharp
public override async Task Clean()
{
    if (AdminPort != 8161)
        return;
```

Das Aufräumen tat nichts, sobald der Adminport vom Default abwich — mit ephemeren Ports also nie. Eine Bedingung, die Isolation still abschaltet.

Harness und Specs lesen jetzt Host, OpenWire-, AMQP- und Jolokia-Port sowie das Laufkonto über einen gemeinsamen `RunScopedBroker`. Der **Artemis**-Zweig bleibt bewusst hartkodiert: separater Broker auf 61618, den die gepinnte Fixture nicht bereitstellt.

## Nächster Schritt

Die fünf offenen Defekte und das Turnout-Paar ursächlich schließen, danach die ActiveMQ-Kategorie einmal real fahren.

## Blocker

Keiner.
