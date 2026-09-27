# Vollständige Produktmessung bei d286a336a

Gemessener Commit: `d286a336a5be47c12362b66b5527aeb349ba4aa5`.
Source-Tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Tests-Tree: `46656b14f1dfdf15ab1442d102c0c232ff017c3d`.
Dieser Bericht ist ein Dokumentationsnachfolger, kein erneut gemessener Commit.

## Messung und dokumentierter Fixture-Startfehler

Alle 33 Profile sind am selben unveränderten Commit verifiziert: 20 Basis-/CPU-
und 13 lokale Integrationsprofile. Drei ursprüngliche Fixturegruppen bestanden
einschließlich Cleanup. Die vierte (SQL Server) scheiterte vor dem Teststart:
SQL Server meldete einen fatalen Startfehler mit errno 11, Resource temporarily
unavailable. Die genaue Ressourcenursache ist nicht bewiesen.

Der ursprüngliche Orchestrierungslauf endete mit Exit 1 und bleibt erhalten.
Profil 32 war pending, ohne Testlauf oder Receipt. Der kanonische Runner hatte
den Container entfernt. Nur dieses fehlende Profil wurde mit neuer SQL-Fixture
am gleichen Commit nachgeholt: Testlauf und Cleanup Exit 0. Kein fehlgeschlagener
Testlauf wurde ersetzt. Die ursprünglichen Fixturecodes [0,0,0,1] bleiben in
fixtureRuns erhalten, Retry 0 separat in fixtureRetryRuns; fixtureGroupsVerified
und allProfilesVerified sind erst nach erfolgreicher Nachholung true.

Der unabhängige Receipt-Audit bestätigt 487 Datei-Hashes, 66 Runner-/Settings-
Bindungen, 32 Assemblies, CPU-Varianten und sämtliche Profile. Die Auswertung
endete mit Exit 0. Der gesonderte Resume-Review bestätigt die Fortsetzung ohne
Buchführungsblocker.

| Kennzahl | Ergebnis |
| --- | --- |
| Erfolgreiche Testausführungen einschließlich CPU-Wiederholungen | 12.902 |
| Produktassemblies | 32 |
| Zeilen | 85.407 / 93.762 = 91,0891406 % |
| Konservativ gezählte Verzweigungen | 30.814 / 36.841 = 83,6405092 % |
| Methodeneinträge einschließlich compilererzeugter Einträge | 26.061 |
| Methoden mit CRAP strikt über 30 | 0 |
| Methodeneinträge mit Zeilenlücken | 4.446 |
| Keine / teilweise Zeilenabdeckung | 2.771 / 1.675 |
| Zusätzliche Einträge mit ausschließlich Verzweigungslücken | 1.511 |
| Vereinigung beider Lückenlisten | 5.957 |

Keine fehlgeschlagenen oder übersprungenen Tests fließen ein. Die konservative
Branch-Zählung verwendet Maxima pro Zeile und Identität, keine Vereinigung
individueller Branch-IDs. Methodeneinträge sind keine unabhängigen Produktverträge.
A+ bleibt unbelegt; CRAP höchstens 30 ist keine A+-Freigabe.

## RabbitMQ-Ziel und Vergleich mit T39

Die [T40-Nachweise](t40-rabbitmq-factory-options.md) enthalten acht neue Fälle:
unabhängige Limits, Authentifizierungspriorität und tatsächliche Clusterauflösung.
Vier Gegenproben erkannt, restaurierte Kontrolle 482/482. Produktcode unverändert.

| Zielmethode | Zeilen vorher / jetzt | Branches vorher / jetzt | CRAP vorher / jetzt |
| --- | --- | --- | --- |
| RabbitMqAddressExtensions.GetConnectionFactory | 43/46 → 46/46 | 22/28 → 25/28 | 28,217473 → 28 |

Neun Methodeneinträge schließen ihre Zeilenlücken, zwei werden neu beobachtet.
Acht Abschlüsse liegen im RabbitMQ-Zielpfad: Factory, Resolvercallback, drei
Settings-Setter und die drei zugehörigen Configurator-Methoden. Hinzu kommt
InMemoryDelayProvider.RearmTimer. Neu beobachtet werden TaskExtensions.OrCanceledAsync
mit 5/7 und ReliableMessagingDeliveryService.ExecuteAsync mit 5/6 Zeilen.
Diese Nebenpfadänderungen werden den neuen Tests nicht kausal zugeschrieben.

Unabhängiger physischer OR-Abgleich aller 66 XML-Berichte: +10/−5 = netto +5.
RabbitMQ +8: HostConfigurator90/91/130/154/155 und AddressExtensions62/64/79.
Nebenpfade +2: ResourceCache.Creation193 und InMemoryDelayProvider225.
Verluste −5: TaskExtensions52/53, PublishEndpoint235, Instrumentation356 und
ReliableMessagingDeliveryService83. Konservative Branches: AddressExtensions+3,
SequentialEndpointResolver+1, übrige Nebenpfade netto−3, zusammen+1.
Die Beobachtungsschwankungen bleiben ausdrücklich sichtbar.

## Restrisiken und nächste Arbeit

Vollständige lokale Lückenlisten: t40-method-gaps.json und t40-branch-only-gaps.json.
Kein Eintrag wird durch diesen Bericht ausgeschlossen.

| Hoher CRAP mit offenen Zeilen | Komplexität | Zeilen | CRAP |
| --- | --- | --- | --- |
| CronExpression.StoreExpressionValues | 26 | 15/18 | 29,129630 |
| CronExpression.ProgressNextFireTimeDay | 26 | 16/19 | 28,661029 |
| OutboxMessagePipe.DeliverOutboxMessagesAsync | 28 | 31/33 | 28,174528 |

Nächster vorbereiteter Bereich: Cron-Parser und Tagesauswahl. Read-only-Prüfung
bestätigt unerreichbare interne Zustände; Vereinfachung muss das Verhalten für
ungültige Tokens, Whitespace und kombinierte Kalenderregeln erhalten und durch
gezielte Regressionen belegen. Roslyn-API-/Kommentar-Audit folgt erst nach A+.

## Lokale Nachweise

Rohdaten bleiben lokal ignoriert; Hashes veröffentlichen sie nicht.

| artifacts/-Datei | SHA-256 |
| --- | --- |
| t40-aggregate.json | 2bd0fbac95a9209c0b45185539028be6ed580a825d8cfd23633bf0174d077791 |
| t40-all-methods.json | 4ff258d13f5d81d13d82aec94db9535a07f3f69359b9ffbf45052b959f8fc897 |
| t40-method-gaps.json | c36a19814d0192952fcfb21bb6f073109ce6cd5a5b1971b2a5be325028a781f1 |
| t40-branch-only-gaps.json | c7127d1abfc52052cb13cebb46c851568890ae8078ca22a3ae5c6087f6ebbe22 |
| t40-profile-progress.json | 4caa3a0615b710db2d76089ad38472b866e2a34b5b0c61fc460d354701f362ed |
| t40-gap-delta.json | afbf827559c089864632533277db9959cb97e8c67cacc4425d2cdf891ef3512e |

Orchestrierung: t40-measure-all.py; Nachholung: t40-resume-mssql.py.
Startfehler: t40-fixture-3.log und run-output/vicione-70dd511f05cd/mssql-broker.log.
Nachholung: t40-fixture-3-retry1.log. Auswertung: t40-analyze-complete.py und
t40-compare-gaps.py. Der unabhängige Abschlussaudit bestätigt auch Methodenlisten,
Delta und Fixturehistorie ohne Accounting-Blocker. Remote-Publikation folgt dem
Dokumentationscommit und wird separat gegen den Remote-Branch verifiziert.
