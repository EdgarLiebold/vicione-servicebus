# PROGRESS — Hostgrenze gesetzt, Policyvalidator steht

## Ziel

Direktive `0005`: Alle veröffentlichten Broker-, Management- und Jolokia-Ports ausschließlich an `127.0.0.1` binden, und diese Invariante im Policyvalidator positiv wie negativ beweisen. Dazu `REQ-CI-007` insgesamt.

## Ausgeführte Arbeit

`compose.yaml` bindet jeden veröffentlichten Port explizit an `127.0.0.1`. Die drei projektlokalen Compose-Dateien erben das über `extends`, es gibt also weiterhin genau eine Fixturedefinition.

`tools/ci/policy_validator.py` und `tools/ci/test_policy_validator.py` sind neu, beide ausschließlich Standardbibliothek: Der Validator muss vor jedem Restore laufen können und darf nie von dem abhängen, was er prüft.

## Beobachtete Evidence

**Hostgrenze.** Alle fünf veröffentlichten Ports tragen das Präfix `127.0.0.1:`. `docker port` meldet für beide Fixtures ausschließlich `127.0.0.1`-Bindungen; eine `0.0.0.0`-Bindung würde dort als solche erscheinen. Über Loopback erreichbar: RabbitMQ-Management `200`, ActiveMQ-Jolokia `200`. Die Absicherung bleibt scharf: `guest` `401`, Jolokia ohne Credentials `401`.

**Policyvalidator.** Positivlauf gegen den realen Repositoriumsstand: `PASS ci-policy all invariants hold`. Selbsttests: **25 Fälle, alle grün**, davon 22 Sabotagemutationen und 3 Positivfälle.

Die drei von der Direktive verlangten Fälle sind darunter und einzeln benannt: nackte Host-Portabbildung ohne Host-IP, Bindung an `0.0.0.0`, abgeschwächte Laufcredential-Pflicht. Dazu die übrigen Sentinel: Rückkehr beider toter Brokerimages, fehlender Basisdigest, bewegliches `latest`, fehlende Pluginprüfsumme, `guest`, gelockerte `loopback_users`, entfernte Pflichtkategorie, Upstream-Repository- und `master`-Guard, umgangenes Testzahl-Gate, Pack ohne Pflichtgates, ohne Laufartefakt und ohne Pakethashes, `nuget push`, Registry-Push und ungültiger Analyzer-Releaseheader.

Zwei Positivfälle sind bewusst Gegenproben zur Regel selbst: ein verbotenes Image, das nur in einem Kommentar vorkommt, und ein `;`-Herkunftshinweis im Analyzer-Tracking müssen **akzeptiert** werden. Ohne sie wäre der Validator scharf, aber unbrauchbar.

## Technische Entscheidung

Der Validator entfernt Kommentarzeilen, bevor er nach verbotenen Mustern sucht. Ein Dokumentationssatz über ein verbotenes Muster ist kein Verstoß — sonst wäre jede ehrliche Erklärung im Repository ein Gate-Fehler. Genau daran ist mein erster Evidence-Lauf aufgelaufen: Er meldete `allLoopbackBound: false`, weil er die Kommentarzeile mit dem Gegenbeispiel `"5672:5672"` mitgezählt hat. Der Fehler lag im Auswerteskript, nicht in der Konfiguration; der Validator war bereits grün.

## Nächster Schritt

`REQ-CI-005`, die unverfälschte EF-Core-Wiederholung, und `REQ-CI-006`, das Vulnerability-Inventar.

## Blocker

Der RabbitMQ-Pflichtjob kann lokal nicht laufen: Die Ports 5672 und 15672 sind auf diesem Rechner durch einen fremden Entwicklungscontainer belegt, den ich nicht anfasse. Die Fixture selbst ist auf Ausweichports vollständig belegt; der Kategorielauf erfolgt in CI oder nach Freigabe des Ports.
