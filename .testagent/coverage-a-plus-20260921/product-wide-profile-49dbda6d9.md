# Vollständige Produktmessung bei 49dbda6d9

Gemessener Commit: `49dbda6d9b0ec1390daab820ead550258eddbe83`.
Source-Tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Tests-Tree: `22264dc36ea855c047bd3d51a7ecab71a145fe91`.
Dieser Bericht ist ein Dokumentationsnachfolger, kein erneut gemessener Commit.

## Messergebnis

Alle 33 Profile wurden frisch am selben Commit ausgeführt: 20 Basis-/CPU-Profile
und 13 lokale Integrationsprofile. Alle vier Fixturegruppen endeten einschließlich
Cleanup mit Exit 0. Messung und Auswertung endeten mit Exit 0.
Der unabhängige Receipt-Review bestätigt 487 Datei-Hashes, 66 Runner-/Settings-
Bindungen, CPU-Varianten, 32 Assemblies und 12.894 Ausführungen.

| Kennzahl | Ergebnis |
| --- | --- |
| Erfolgreiche Testausführungen einschließlich CPU-Wiederholungen | 12.894 |
| Produktassemblies | 32 |
| Zeilen | 85.402 / 93.762 = 91,0838079 % |
| Konservativ gezählte Verzweigungen | 30.813 / 36.841 = 83,6377948 % |
| Methodeneinträge einschließlich compilererzeugter Einträge | 26.061 |
| Methoden mit CRAP strikt über 30 | 0 |
| Methodeneinträge mit Zeilenlücken | 4.453 |
| Keine / teilweise Zeilenabdeckung | 2.778 / 1.675 |
| Zusätzliche Einträge mit ausschließlich Verzweigungslücken | 1.508 |
| Vereinigung beider Lückenlisten | 5.961 |

Keine fehlgeschlagenen oder übersprungenen Läufe fließen ein. Die konservative
Branch-Zählung verwendet Maxima pro Zeile und Identität, keine Vereinigung
individueller Branch-IDs. Methodeneinträge sind keine unabhängigen Produktverträge.
A+ bleibt unbelegt; CRAP höchstens 30 ist keine A+-Freigabe.

## Ziel und Vergleich mit T38b

Die [T39-Nachweise](t39-dynamodb-validation.md) enthalten 16 neue Verhaltensfälle
für Konfigurationsdiagnosen, Ablehnung vor Registrierung, gültige Grenzwerte und
unveränderliche DI-Optionen. Drei Gegenproben erkannt, restaurierte Kontrolle
44/44. Produktcode blieb unverändert.

| Zielmethode | Zeilen vorher / jetzt | Branches vorher / jetzt | CRAP vorher / jetzt |
| --- | --- | --- | --- |
| DynamoDbSagaRepositoryConfigurator.Validate | 8/14 → 14/14 | 9/14 → 14/14 | 29,428571 → 14 |

Validate und set_TimeToLive schließen ihre Zeilenlücken. Produktweit schließen
vier Einträge, einer erscheint neu. Die beiden weiteren geschlossenen Einträge
sind TaskExtensions.OrCanceledAsync und ReliableMessagingDeliveryService.ExecuteAsync.
SagaInstance.MarkInUseAsync wird neu mit 13/16 Zeilen beobachtet. Diese Änderungen
außerhalb des Ziels werden den neuen Tests nicht kausal zugeschrieben.

Der unabhängige physische OR-Abgleich aller 66 XML-Berichte ergibt +10/−4:
Zielquelle +6 (69,71,73,79,80,81); Nebenpfade +4 (TaskExtensions52/53,
Instrumentation356, ReliableMessagingDeliveryService83) und −4 (SagaInstance
109/110/112, ResourceCache.Creation193). Netto +6 physische Zeilen.
Konservative Branches: Ziel +5, Nebenpfade netto −1, zusammen +4.
Die Nebenpfadschwankungen bleiben kausal ungeklärt und als Restarbeit sichtbar.

## Restrisiken und Restarbeit

Alle Lücken stehen lokal in t39-method-gaps.json und t39-branch-only-gaps.json;
kein Eintrag wird durch diesen Bericht ausgeschlossen.

| Hoher CRAP mit offenen Zeilen | Komplexität | Zeilen | CRAP |
| --- | --- | --- | --- |
| CronExpression.StoreExpressionValues | 26 | 15/18 | 29,129630 |
| CronExpression.ProgressNextFireTimeDay | 26 | 16/19 | 28,661029 |
| RabbitMqAddressExtensions.GetConnectionFactory | 28 | 43/46 | 28,217473 |
| OutboxMessagePipe.DeliverOutboxMessagesAsync | 28 | 31/33 | 28,174528 |

Nächster vorbereiteter Kandidat: RabbitMQ-Limits, CredentialsProvider und
EndpointResolver an der öffentlichen ConnectionFactory-Grenze. Der Roslyn-API-
und Kommentar-Audit folgt erst nach dem A+-Coverage-Ziel.

## Lokale Nachweise

Rohdaten bleiben lokal ignoriert; Hashes veröffentlichen die Dateien nicht.

| artifacts/-Datei | SHA-256 |
| --- | --- |
| t39-aggregate.json | 1d3b6b5afad5fdf900514b23cd8188ba047d66e28fdd25715810e6b094df7b1e |
| t39-all-methods.json | 29d38ead95d62ac59f7693aa131a4bb0f5922856e2d25865e875fd6feb867b78 |
| t39-method-gaps.json | f409df59691ead7682018b82714f049271dcf5d0c99f9acf24b2b5f3aaf39a42 |
| t39-branch-only-gaps.json | a091de94330186b8ee852aaa3609781ccda300c600ee902438fdbe879e1ee62c |
| t39-profile-progress.json | 195baf6f3e26cb7782e9eee48e91dd26d2062e144cf7cbc4cd5fe3f5cc27ce6a |
| t39-gap-delta.json | 6bec41931e27396af7e0756b3ab20d25049d5701c2dc51f3406a5b79d5b0bb95 |

Orchestrierung: artifacts/t39-measure-all.py. Auswertung:
artifacts/t39-analyze-complete.py und artifacts/t39-compare-gaps.py.
Der unabhängige Abschlussaudit bestätigt auch Methodenlisten und Delta ohne
Accounting-Blocker. Remote-Publikation folgt dem Dokumentationscommit und wird
separat gegen den Remote-Branch geprüft.
