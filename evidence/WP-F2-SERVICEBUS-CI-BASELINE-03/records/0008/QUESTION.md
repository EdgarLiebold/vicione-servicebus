# QUESTION — „ungefiltert" und die vom Testbestand deklarierte `Flaky`-Kategorie schließen sich aus

Die Portkollision ist ursächlich geschlossen. Übrig bleibt ein echter Zielkonflikt in `REQ-CI-002`, den ich nicht allein entscheiden darf.

## Stand

Die Fixture läuft auf ephemeren Loopback-Ports, **während der fremde Container 5672 und 15672 weiter hält** — er wurde nicht gestoppt.

| Lauf | gesamt | ausgeführt | grün | rot |
|---|---|---|---|---|
| ungefiltert | 325 | 288 | 282 | **6** |
| `Category!=Flaky` | 246 | 246 | **246** | **0** |

Fünf deterministische Fehler habe ich zuvor ursächlich behoben: Clusterknoten ohne Port, zwei Adressassertions auf einer nicht mehr gültigen Konstante, `Host(Uri)` ohne Zugangsdaten und ein Refresh auf ein nicht existierendes Konto. Keine Assertion wurde gelockert.

## Der Konflikt

Alle sechs verbleibenden Fehler liegen in Fixtures, die der importierte Testbestand **selbst** mit `[Category("Flaky")]` markiert: `Turnout/Faulted_Specs`, `ConcurrencyFilter_Specs`, `DelayRetry_Specs`, `InMemoryOutboxRedelivery_Specs`. Insgesamt tragen 35 Stellen dieses Attribut.

`REQ-CI-002` verlangt, dass die Pflichtkategorien „erreichbar, **ungefiltert**" laufen. Der Upstream-Lauf verwendete durchgängig `--filter Category!=Flaky`. Beides zugleich ist nicht erfüllbar.

Ein Nebenbefund stützt das: Im ungefilterten Lauf werden **37 von 325 Tests gar nicht ausgeführt**; mit dem Filter sind es 0 von 246. Der ungefilterte Lauf ist also nicht nur roter, sondern auch unvollständiger.

## Methodischer Hinweis zu meiner eigenen Messung

Meine erste Einzelprüfung war für eine der Fixtures unbrauchbar: `Turnout/Faulted_Specs` deklariert `[Order(1)]` und `[Order(4)]`; die Order-4-Tests warten auf ein Fault, das erst der Order-1-Test auslöst. Isoliert können sie nicht grün werden. Der scheinbare „deterministische Einzelfehler" war ein Artefakt meines Vorgehens.

## Empfehlung

`OPT-EXCLUDE-FLAKY-DECLARED` ist die A+-Antwort — mit der Auflage, dass der Ausschluss **benannt und gezählt** wird, nicht still geschieht.

Eine Kategorie, die ihre eigenen Autoren als unzuverlässig deklariert haben, ist kein belastbares Regressionssignal. Nimmt man sie in ein Pflichtgate, meldet das Gate Rauschen statt Wahrheit, und rotes Grün gewöhnt man sich an. Das Verbot aus `REQ-CI-002` zielt erkennbar darauf, dass Kategorien **unbemerkt verschwinden** — nicht darauf, eine ausdrückliche Zuverlässigkeitsmarkierung zu ignorieren. Genau diesen Unterschied macht die Auflage: Der Pflichtlauf nennt die ausgeschlossene Menge samt Anzahl im Profilinventar und weist sie als *nicht fällig* aus, exakt nach dem Muster, das bei den erweiterten Transporten bereits angenommen wurde.

`OPT-DEFLAKE-ALL` wäre die sauberste Welt, ist aber Arbeit unbekannter Tiefe an fremdem Testbestand und teilweise strukturell: Die Order-Abhängigkeit ist Konstruktionsprinzip der Fixture, kein Defekt. `OPT-KEEP-UNFILTERED` liefert ein dauerhaft rotes Pflichtgate und widerspricht dem Auftragsziel eines ehrlich grünen Pflichtpfads.

**Folge des Nichtentscheidens:** Die RabbitMQ-Pflichtkategorie kann nicht abschließend als grün gemeldet werden, und `REQ-CI-001`/`REQ-CI-002` bleiben offen. Alle übrigen Anforderungen laufen davon unbeeinflusst weiter.
