# Vollständige Produktmessung bei 7c617f34c

Gemessener Commit: `7c617f34cd7fadf6a6fdab32b4178b1008da5a9d`.
Source-Tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Tests-Tree: `d43e7dcbe0c42805009887e002087a0659306729`.
Dieser Bericht ist ein Dokumentationsnachfolger, kein erneut gemessener Commit.

## Messergebnis

Alle33 Profile wurden frisch am selben Commit ausgeführt:20 Unit-/CPU-Profile
und13 lokale Integrationsprofile. Alle vier Fixturegruppen endeten einschließlich
Cleanup mit Exit0. SQL Server bestand75/75 ohne Skips in6m01s.
Messung und Auswertung endeten mit Exit0. Das strikte Aggregat ist vollständig.
Der frühere fehlgeschlagene T38-Lauf wurde nicht eingemischt oder überschrieben.
Der unabhängige Read-only-Abschlussreview bestätigt487 Datei-Hashes und66
Runner-/Settings-Bindungen, beide CPU-Sonderprofile,32 Assemblies und sämtliche
Summen ohne Accounting-Blocker.

| Kennzahl | Ergebnis |
| --- | --- |
| Erfolgreiche Testausführungen einschließlich CPU-Wiederholungen | 12.878 |
| Produktassemblies | 32 |
| Zeilen | 85.396 / 93.762 = 91,0774088 % |
| Konservativ gezählte Verzweigungen | 30.809 / 36.841 = 83,6269374 % |
| Methodeneinträge einschließlich compilererzeugter Einträge | 26.061 |
| Methoden mit CRAP strikt über30 | 0 |
| Methodeneinträge mit Zeilenlücken | 4.456 |
| Keine / teilweise Zeilenabdeckung | 2.779 / 1.677 |
| Zusätzliche Einträge mit ausschließlich Verzweigungslücken | 1.506 |
| Vereinigung beider Lückenlisten | 5.962 |

Keine fehlgeschlagenen oder übersprungenen Läufe fließen ein. Die konservative
Branch-Zählung verwendet Maxima pro Zeile und Identität, keine Vereinigung
individueller Branch-IDs. Methodeneinträge sind keine unabhängigen Produktverträge.
A+ bleibt unbelegt; CRAP höchstens30 ist keine A+-Freigabe.

## Ziel und Vergleich mit T37

Die [T38-Nachweise](t38-asb-subscription.md) enthalten44 neue Verhaltensfälle
für Subscription-Processor sowie vier erkannte Mutationen und44/44 restaurierte
Kontrollen. Der ursprüngliche vollständige Messversuch scheiterte an einem
ActiveMQ-Quartz-Test: TriggerFinalized lag vor dem Store-Abschluss. Die korrigierte
Zustandsbeobachtung besteht beide Protokolle, erkennt einen am Callback kontrolliert
zurückgehaltenen Trigger und besteht die restaurierte Kontrolle2/2. Im frischen
Gesamtlauf besteht ActiveMQ lokal100/100. Produktcode blieb unverändert.

| Zielquelle | Methodenzeilen vorher / jetzt | Branches vorher / jetzt |
| --- | --- | --- |
| SubscriptionClientContext | 47/69 → 69/69 | 24/44 → 41/44 |

Acht Zielmethodeneinträge schließen ihre Zeilenlücke: ConfigureMessageProcessor,
ConfigureSessionProcessor, StartAsync, ShutdownAsync, CloseAsync, EntityPath,
InputAddress und der Session-Message-Callback. Drei Branches bleiben offen.
Methodenzeilen können dieselbe physische Zeile mehrfach zählen.

Produktweit werden netto25 zusätzliche physische Zeilen und21 zusätzliche
konservative Branches beobachtet. Zehn Methodenzeilenlücken schließen, keine
erscheint neu. Die beiden weiteren geschlossenen Einträge liegen in
ClientRequestHandle und TypeConverterCache.GetOrCreateConverter. Diese Beobachtungen
außerhalb der Zielquelle werden den neuen Tests nicht kausal zugeschrieben.

Der unabhängige physische OR-Abgleich aller66 XML-Berichte ergibt +26/-1:
SubscriptionClientContext +21 physische Zeilen; ClientRequestHandle.Responses
+3 (24,26,28) und -1 (33), ClientRequestHandle +1 (155), TypeConverterCache
+1 (54). Die22 zusätzlichen Ziel-Methodenzeilen zählen eine physische Zeile
mehrfach. Branchdelta +21: Ziel +17, Nebenpfade netto +4 (acht Zuwächse und
vier Verluste um jeweils eins). Diese Request-, EF-, Courier-, Retry-, Saga-
und EventHubs-Beobachtungen bleiben kausal ungeklärt. Je ein Zielbranch bleibt
in ShutdownAsync, CloseAsync und StopAfterCallbackAsync offen.

## Restrisiken und Restarbeit

Die vollständigen lokalen Listen sind t38b-method-gaps.json und
t38b-branch-only-gaps.json. Kein Eintrag wird durch diesen Bericht ausgeschlossen.
Höchste teilweise abgedeckte CRAP-Kandidaten bleiben DynamoDB-Konfiguration
Validate (8/14,29,428571), Cron StoreExpressionValues (15/18,29,129630) und
ProgressNextFireTimeDay (16/19,28,661029).
Der Roslyn-API-/Kommentar-Audit folgt erst nach dem A+-Coverage-Ziel.

## Lokale Nachweisdateien

Rohdaten bleiben lokal ignoriert; Hashes veröffentlichen die Dateien nicht.

| artifacts/-Datei | SHA-256 |
| --- | --- |
| t38b-aggregate.json | 50016448acbd5de83053ff6b4a79c572ba2055d1c3e3ef8549e1ad06ca91cf7d |
| t38b-all-methods.json | 8dde800a747a8e729f769674bbd374230d35969f105044a96ed916c29359cee1 |
| t38b-method-gaps.json | 8ab9d453a3d142f9ee7b365120418290d19832ee6fb4a74f6fffaf60fee581f2 |
| t38b-branch-only-gaps.json | bb3cec2ded44c15e046d052ef183bd42c5524d1e20fd4703f5dccb54eb5e8c38 |
| t38b-profile-progress.json | 53a60558b928162021e40afeb51a14af1ae10bdf1a3c82304250fcbe92d31403 |
| t38b-gap-delta.json | f5d8a548cb89c45349c019c91eb07203c693958fba322a8e7595732acc858719 |

Orchestrierung: artifacts/t38b-measure-all.py. Auswertung:
artifacts/t38b-analyze-complete.py und artifacts/t38b-compare-gaps.py.
Messung, Auswertung und unabhängiger Aggregatreview sind abgeschlossen.
Die Remote-Publikation wird nach dem Dokumentationscommit separat verifiziert.
