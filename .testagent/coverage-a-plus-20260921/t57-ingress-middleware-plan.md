# T57 — zusammenhängendes Ingress-Middleware-Paket

## Research

- Ausgangspunkt: veröffentlichter Stand `9bc78539d`; T56 misst 91,6110 % Zeilen,
  84,2340 % konservative Branches und keine Methode mit CRAP > 30.
- Zielbereich: Consumer-, Handler- und Instance-Filter, Retry- und
  Circuit-Breaker-Ausführung. Retry und Circuit Breaker haben bereits tiefe
  Verträge für Budgets, Zustandsübergänge, Abbruch und Observer-Fehler. Direkte
  Tests der drei Consumer-Einstiegsfilter fehlen weitgehend.
- Microsoft-Roslyn-Paarung auf einer bytegleichen, auf acht Produktdateien und
  fünf repräsentative Testdateien begrenzten Momentaufnahme: 2/8 gekoppelt,
  6/8 statisch ungekoppelt. Das ist nur eine Heuristik für Typreferenzen und
  keine Zeilen-, Branch- oder Qualitätsmessung; insbesondere vorhandene
  indirekte Pipeline-Tests werden nicht zuverlässig zugeordnet.

## Acceptance checklist und Plan

| Vertrag | Konkreter Testplan |
| --- | --- |
| Consumer, Handler und Instance melden Erfolg erst nach ausgeführter Arbeit und vor `next` | Ein parametrisierter, gesperrter asynchroner Notification-Test je Form mit exakter Reihenfolge und Kontext-/Consumer-Identität. |
| Fehler erhalten die ursprüngliche Exception und überspringen `next` | Parametrisierter echter Business-Fehler je Form mit exakter Fehlerbenachrichtigung. |
| Asynchroner Fehler der Consume-Benachrichtigung darf Arbeit nicht wiederholen oder `next` ausführen | Parametrisierter Notification-Fehler je Form mit exakter Exception-Identität und Aufrufspur. |
| Ein Fehler in `next` nach erfolgreicher Consumer-Meldung darf nicht als zweiter Fehler desselben Consumers gelten | Roter Test in allen drei Formen; anschließend wird `next` außerhalb der Consumer-Fault- und Telemetrie-Lebensdauer ausgeführt. |
| Fremder Abbruch wird als Consumer-Abbruch gemeldet, angeforderter Abbruch bleibt unverändert | Je Form beide Token-Situationen und die ursprüngliche Exception in der Fehlerbenachrichtigung. |
| Kein Test dient bloß einer Coverage-Zeile | Pseudo-Mutationen gegen ausgelassene Notifications, vorgezogene Fortsetzung, erneuten Business-Aufruf und falsche Exception-Identität; anschließend Assertion-Review und read-only Red Team. |
| Ein größeres Paket statt Mikro-Iterationen | Alle obigen Filterverträge plus eine gemeinsame Retry/Circuit-Breaker-Integration, die Zustellung statt Einzelversuche zählt und den offenen Consumer nicht aufruft; enge Build-/Testzyklen, danach eine vollständige 33-Receipt-Messung und unabhängige Prüfung. |

Die statische Paarung gibt als neue Testorte
`tests/ViciOne.ServiceBus.Tests/Middleware/{ConsumerMessageFilter,HandlerMessageFilter,InstanceMessageFilter}Tests.cs`
an. Ein gemeinsamer Vertragstest im selben Middleware-Verzeichnis ist für die
drei gleichartigen Ausführungsformen besser nachvollziehbar.

## Read-only Red-Team-Review

- P2: Arbeitspfad muss den richtigen Nachrichten-/Receive-Kontext und beim
  Handler genau das ursprüngliche Context-Objekt erhalten. Assertions ergänzt.
- P2: Retry/Circuit-Breaker-Versuche waren nur global gezählt. Exakt zwei
  Aufrufe je erster/zweiter Zustellung sowie null für die abgewiesene Zustellung
  und eins für die Erholung werden jetzt nach Objektidentität geprüft.
- P3: `next`-Fehler nach einer Consume-Meldung erzeugten zusätzlich eine
  Fault-Meldung für denselben Consumer. Der neue Vertragstest reproduzierte
  den doppelten Abschluss auf unverändertem Produktcode (3/3 rot). Die drei
  Filter rufen `next` jetzt nach ihrem Consumer-Fault-/Telemetry-Block auf;
  alle drei Fälle sind grün.
- Der Produkt-Nachreview fand keinen Blocker. Seine zwei Grenzfälle sind
  nachgezogen: sechs Downstream-Abbruchfälle mit beiden Caller-Token-Zuständen
  und drei gesampelte Activity-Lebensdauerfälle. Der finale read-only Nachcheck
  sieht keine konkrete Restlücke im Paket.

## Qualitäts- und Ausführungsnachweis vor der Vollmessung

Die acht neuen Testmethoden liefern 28 parametrisierte Ausführungen. Alle
haben konkrete Ergebnis-, Identitäts-, Zustands- oder Reihenfolge-Assertions;
keine ist assertionfrei oder besteht nur aus einem Null-/Wahrheitscheck.
`SuccessfulWork` trennt die Notification-Barriere von `next`, Business-,
Notification- und Downstream-Fehler prüfen die exakte Exception-Identität und
verbieten falsche Folgeschritte. Die beiden Abbruchmatrizen unterscheiden
Consumer- und Downstream-Eigentum für denselben Tokenzustand. Die Activity-
Matrix prüft den tatsächlichen gesampelten Process-Span, und der
Retry/Circuit-Breaker-Fall zählt Versuche je Eingangskontext. Das erfüllt die
A-Qualitätsmerkmale der Microsoft-Grade-Tests-Rubrik für die acht Methoden;
es ist keine Aussage über die noch offene globale A+-Coverage.

Pseudo-Mutationskontrolle: Weglassen/Vorziehen der Consume-Meldung,
falscher Arbeitskontext, falsche Exception, 3/1-Retry-Verteilung, erneuter
Aufruf bei offenem Breaker und ein über `next` hinaus laufender Process-Span
scheitern jeweils an benannten Assertions. Die `next`-Catch-Mutation wurde
als echter red-first Altcode-Gegenlauf ausgeführt (3/3 rot). Die übrigen
Mutationen sind statisch geprüft; für sie wird kein ausgeführter Mutationsscore
behauptet.

Der finale eng gefilterte Lauf besteht 28/28. Der vollständige Core-Testlauf
auf denselben Produkt- und Testbytes besteht 6.887/6.887 ohne Skip. Zwei
frühere Testhüllenfehler (fehlender ReceiveContext und fehlende
SupportedMessageTypes beim gesampelten Activity-Test) wurden im Testaufbau
behoben; sie sind keine Produktfehler. Die anfänglichen Fehlläufe bleiben
separate Diagnoseartefakte unter `/private/tmp/servicebus-t57-*`.
