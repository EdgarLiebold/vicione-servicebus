# Generischer Headeradapter und SQS-Sendpfad, 25.09.2026

## Produktbefund und Korrektur

`TransportSetHeaderAdapter<TValueType>` nahm einen null-Konverter an und
scheiterte erst bei der späteren Headerverarbeitung. Der Konstruktor lehnt ihn
jetzt mit `ArgumentNullException` für `converter` ab. Die Abstractions-Tests
prüfen beide `Set`-Overloads mit sechs Optionskombinationen, normale Header,
je zwei Host- und Fault-Detail-Schlüssel, `FaultInputAddress`, `FaultMessage`,
Konverterablehnung und das Entfernen eines leeren typisierten Werts.

Die SQS-Queue benutzt den untypisierten Overload mit
`IncludeFaultMessage`. Ein zusätzlicher Test sendet über
`SendTransport<ClientContext>` und `QueueSendTransportContext` bis zum
abgefangenen `SendMessageBatchRequestEntry`. Er prüft die Werte der drei
erlaubten Header und das Fehlen von je zwei Host- und Fault-Detail-Attributen
im tatsächlichen Provider-Request. Das adversariale Review fand keinen
P1/P2-Befund; ein entfernter untypisierter Filter würde diesen Test brechen.

## Gegenprobe und formale Messung

- Auf `1be9fe390` entfernte ein archivierter Mutant ausschließlich den
  untypisierten `IsHeaderIncluded`-Aufruf. Der fokussierte Lauf scheiterte
  mit 5/15; nach Wiederherstellung bestand er mit 15/15. Patch, Logs und
  Exitcodes liegen unter `artifacts/generic-header-filter-mutant-1be9fe390/`.
  Das Red Team prüfte die Artefakte und Hashes unabhängig.
- Der zusätzliche SQS-Sendtest liegt auf Commit
  `06a7f5dc727c0243467d853c01375ce47dcd506a`, `src`-Tree
  `9787719c937c89ea1e9be0cf8cbf7b5028d891b8`, `tests`-Tree
  `7fdb48a1ab7e4fee75b1c5d7600078dd05adbcd5`.
- Die vier `coverage_receipt.py`-Läufe unter
  `artifacts/coverage-receipt-{abstractions-normal,abstractions-noavx2,abstractions-scalar,amazonsqs}-06a7f5dc7/`
  bestanden mit 910, 910, 910 und 215 Tests; Fehler und Skips jeweils null.
  Alle Receipts verifizieren unveränderte Binärdateien und ihren Quellbaum.
- Im SQS-Cobertura-Bericht stehen für den untypisierten Adapter-Overload
  nun 4/7 Zeilen und 5/8 Branches; zuvor waren es 0/7 Zeilen. Die fehlenden
  Pfade betreffen die Null-Entfernung eines `HeaderValue`, dessen öffentlicher
  Konstruktor Nullwerte bereits ablehnt. Der typisierte Overload erreicht
  im Abstractions-Bericht 8/8 Zeilen und alle Branches.

Das geprüfte Teilaggregat
`artifacts/coverage-generic-header-06a7f5dc7-partial.json` enthält drei
Produktassemblies und 2.945 Testausführungen. Es beobachtet 11.030/43.753
Zeilen und konservativ 4.059/17.549 Branches; 705 Methoden liegen über
CRAP 30. Es fehlen 16 Produkt-Unit-Receipts, 13 lokale Provider-Receipts und
29 Assemblies. Diese Werte beschreiben nur den gemessenen Ausschnitt.
