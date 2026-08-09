# PROGRESS — REQ-CI-001 Brokerfixtures stehen und sind funktional belegt

## Ziel

Offiziell gepflegte, unveränderlich gebundene Basen für RabbitMQ und ActiveMQ mit reproduzierbarer, sicherer Laufkonfiguration — ohne Repository- oder Registry-Publikation.

## Ausgeführte Arbeit

`build/test-infrastructure/` angelegt: `images.lock.json` als einzige Pinquelle, je ein Dockerfile für RabbitMQ und ActiveMQ, `enabled_plugins`, eine aus der Originalkonfiguration des Images abgeleitete `activemq.xml`, `groups.properties`, `compose.yaml` als einzige Fixturedefinition für lokal und CI sowie `new-run-credentials.sh` für das laufbezogene Konto.

## Beobachtete Evidence

**Bindung.** Basisimages ausschließlich per Digest, kein bewegliches Tag. Die Pluginbytes hängen an SHA-256 `f168b2c0…` über `ADD --checksum`. Ein Build mit absichtlich falscher Prüfsumme bricht mit `ERROR: digest mismatch` ab — die Durchsetzung ist bewiesen, nicht behauptet.

**RabbitMQ.** `rabbitmq_delayed_message_exchange 4.2.0` und `rabbitmq_management 4.2.9` aktiv. Management-API mit dem Laufkonto `HTTP 200`, `guest` wird mit `HTTP 401` abgewiesen. Der vhost `test` wird angelegt und das Konto erhält dabei automatisch `configure/write/read = .*`. Ein echter Exchange vom Typ `x-delayed-message` mit `x-delayed-type: direct` liegt an und liest sich zurück — das Plugin ist nicht nur installiert, sondern wirksam.

**ActiveMQ.** Jolokia auf genau dem Pfad, den `ActiveMqTestHarness` nutzt: `HTTP 200` mit Laufkonto, `HTTP 401` ohne Credentials und bei falschem Passwort. Connectors `openwire`, `amqp`, `stomp`, `mqtt`, `ws` vorhanden; der Testbestand nutzt davon OpenWire 61616, AMQP 5672 und Jolokia 8161. Die `JobScheduler`-MBean existiert — auf dem unveränderten Image fehlt sie, weil ActiveMQ `schedulerSupport` per Default auf `false` lässt.

## Technische Entscheidungen

**`activemq.xml` aus dem Image abgeleitet, nicht neu geschrieben.** Einziges Delta ist `schedulerSupport="true"`. Eine selbst formulierte Brokerkonfiguration wäre eine zweite Wahrheit neben der offiziellen und würde bei jedem Basisupdate stillschweigend divergieren.

**Kontoname fest, Geheimnis pro Lauf.** ActiveMQ autorisiert die Webkonsole über eine Rolle, deren Bindung in einer Konfigurationsdatei steht. Mit zufälligem Namen authentifiziert das Konto zwar, wird aber mit `HTTP 403` abgewiesen — das war der erste Messbefund. Ein Name allein gewährt keinen Zugang; das Geheimnis wird bei jedem Lauf neu erzeugt, nie gespeichert und unter Actions vor dem Export maskiert.

**Profile in `compose.yaml`.** RabbitMQ und ActiveMQ beanspruchen beide AMQP 5672. Genau ein Broker läuft je Lauf, was zugleich dem Zuschnitt der CI-Jobs entspricht.

**Harter Abbruch statt Rückfall.** Fehlen die Laufcredentials, scheitert Compose mit `required variable VICIONE_SERVICEBUS_RMQ_USER is missing a value`. Ein stiller Rückfall auf ein bekanntes Konto ist damit ausgeschlossen.

## Nächster Schritt

`REQ-CI-007`: Policyvalidator unter `tools/ci/**` mit positiven, negativen und sabotierenden Fällen, der die hier gesetzten Invarianten dauerhaft absichert — bewegliche Tags, fehlende Digests oder Prüfsummen, Remote-`guest` und `loopback_users=none`.

## Blocker

Keiner.
