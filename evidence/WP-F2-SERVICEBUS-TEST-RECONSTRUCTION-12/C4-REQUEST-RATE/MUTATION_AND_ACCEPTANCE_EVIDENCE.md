# C4 Request rate — mutation and acceptance evidence

## Gegenstand

Der technische Kandidat ist Commit `520fe7028f09fc68e8357397cee984541cd7ce95` mit Tree
`378199531adffe2f97c90365eb981837525824a3`. Er ersetzt sieben R0-Verpflichtungen durch sechs
native xUnit-/MTP-Methoden mit acht materialisierten Fällen unter dem Source Owner
`ViciOne.ServiceBus.Abstractions.Tests/Util`. Produktcode wurde nicht geändert.

## Determinismuskorrektur vor Acceptance

Ein erster Mutationslauf zeigte, dass der Gruppentest seine Request-Parallelität versehentlich mit
der noch asynchron auslaufenden Ergebnis-Kapazität gekoppelt hatte. Dadurch konnte der nächste Pass
weniger Ergebnisse anfordern, obwohl die geprüfte Request-Skalierung korrekt war. Der Test setzt
deshalb für genau diesen Fall `ConcurrentResultLimit` deutlich oberhalb des Requestfensters. Das
entkoppelt die zwei Produktgrenzen und lässt den Test ausschließlich die benannte Request-
Parallelität messen. Der fehlerhafte lokale Kandidat wurde vor jeder Veröffentlichung ersetzt.

Die korrigierte Assembly lief anschließend dreimal unmittelbar hintereinander mit jeweils 107
gesamt, 107 erfolgreich, 0 fehlgeschlagen und 0 übersprungen. Der Test verwendet weiterhin weder
Delay noch Polling noch Zufall; sein Timeout ist nur eine fail-closed Abbruchgrenze.

## Positive Ausführung

```bash
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode --disable-parallel \
  -m:1 -p:BuildInParallel=false -p:NuGetAudit=false \
  /bl:artifacts/c4-request-rate/unit-locked-restore.binlog

dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-restore \
  --no-incremental --disable-build-servers -m:1 -p:BuildInParallel=false \
  -p:UseSharedCompilation=false /bl:artifacts/c4-request-rate/unit-release-build.binlog

dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release \
  --no-build --no-restore --results-directory artifacts/test-results/unit \
  --minimum-expected-tests 660 --max-parallel-test-modules 1
```

Ergebnis: Restore Exitcode 0; Build Exitcode 0 mit 0 Warnungen und 0 Fehlern; Test Exitcode 0 mit
660 gesamt, 660 erfolgreich, 0 fehlgeschlagen und 0 übersprungen.

```bash
dotnet restore ViciOne.ServiceBus.Tests.LocalIntegration.slnx --locked-mode --disable-parallel \
  -m:1 -p:BuildInParallel=false -p:NuGetAudit=false \
  /bl:artifacts/c4-request-rate/local-locked-restore.binlog

dotnet build ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release --no-restore \
  --no-incremental --disable-build-servers -m:1 -p:BuildInParallel=false \
  -p:UseSharedCompilation=false /bl:artifacts/c4-request-rate/local-release-build.binlog

VICIONE_TESTS__Profile=LocalIntegration dotnet test \
  --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release \
  --no-build --no-restore --results-directory artifacts/test-results/local-integration \
  --minimum-expected-tests 3 --max-parallel-test-modules 1
```

Ergebnis: Restore Exitcode 0; Build Exitcode 0 mit 0 Warnungen und 0 Fehlern; Test Exitcode 0 mit
3 gesamt, 3 erfolgreich, 0 fehlgeschlagen und 0 übersprungen. Der abschließende direkte
Abstractions-Lauf erzeugte 107/107 grüne Fälle im CTRF-Rohbericht.

## Ein-Ursachen-Mutationen

Jede Mutation wurde einzeln auf den technischen Kandidaten angewandt, mit Release neu gebaut und
mit allen 107 Abstractions-Fällen ungefiltert ausgeführt. Jede Mutation wurde erkannt; kein Lauf
enthielt einen Skip. Mehrere fehlschlagende Tests bei einer Mutation sind zulässig, wenn sie dieselbe
Produktursache aus verschiedenen öffentlichen Verträgen beobachten.

| Mutation | Gemessenes Ergebnis |
|---|---|
| generischer Ergebnis-Callback liefert Rückgabecount `0` | 1 Fehler: `Run_RequestsConfiguredLimitAndProcessesEveryResult` |
| Maximum aktiver Requests wird nicht fortgeschrieben | 1 Fehler: `GroupedRun_RepeatedFullBatchesReachConfiguredRequestConcurrency` |
| Full-batch-Wachstumsdivisor `2` wird `3` | 2 Fehler: Wachstumskurve und gruppierte Endkonkurrenz |
| Resultlimit ignoriert den kleineren Prefetchwert | 1 Fehler: `ResultLimit_IsClampedToPrefetchCount` |
| Requestlimit verwendet eine falsche Aufrundungsformel | 3 Fehler: Single-Request-Full-Row, Wachstumskurve und gruppierte Endkonkurrenz |
| Nullguard für `PrefetchCount` wird unwirksam | 1 Fehler: zugehörige Konstruktor-Theory-Zeile |

Nach der letzten Mutation bestätigten `git diff --exit-code` und `git status --short` den
unveränderten, sauberen Kandidaten vor Restore, Build und Acceptance.

## Rohartefakte

| Artefakt | SHA-256 |
|---|---|
| `artifacts/c4-request-rate/unit-locked-restore.binlog` | `8f51f3003f4568344c67499d91e30a7ff94a5c4a759b5ae63988ce1c513f376b` |
| `artifacts/c4-request-rate/unit-release-build.binlog` | `e740af503d879f375dc7da44b2309c5ebb70eff5f4eae4cf6be45dc016f21f39` |
| `artifacts/c4-request-rate/local-locked-restore.binlog` | `7cc2b80be1a5c320547f809e9d926ab082cf64647036fc9a60a45cab2d8e8ac6` |
| `artifacts/c4-request-rate/local-release-build.binlog` | `f87e8e1c4cc64ec22e0b051003c72c1d56793c9fc5e59da0232f3a50faed740f` |
| `artifacts/c4-request-rate/final-abstractions.ctrf.json` | `d489d3456b6a18a30f5be14a7053f220efdc84f0834d4a334d425555e4dda618` |
| `artifacts/c4-request-rate/corrected-stability-1.ctrf.json` | `fa899a9ea00735ab87c007936c102d181fd5fff5b32faa2accc3048b690e21d6` |
| `artifacts/c4-request-rate/corrected-stability-2.ctrf.json` | `7c21439d3fd50f891f00fc34987a58bfa2ae1feca3f15acb225975d84a374088` |
| `artifacts/c4-request-rate/corrected-stability-3.ctrf.json` | `026bb66763590b6168c7a2af3fdacbaa8d9f08af3f030594725556a9e6654fbd` |
| `artifacts/c4-request-rate/mutation-callback-final.ctrf.json` | `714096bfacbb19b8fe37c9ae5c79d83155efc53320081170793fc89da2b92843` |
| `artifacts/c4-request-rate/mutation-concurrency.ctrf.json` | `5c9f9c046e7da660f9cbe154ffb834508daae3192c49e9998847b216c854319e` |
| `artifacts/c4-request-rate/mutation-scaling.ctrf.json` | `3b43c1341856c3bbf04d3a4808d794ff7b51ec49437dde5e8238c1c9ad0e432f` |
| `artifacts/c4-request-rate/mutation-clamp.ctrf.json` | `b6a7b091858a5f09b994702e2bd703490629aa86b6cfc49249bf6d0f0aa7b939` |
| `artifacts/c4-request-rate/mutation-single-request.ctrf.json` | `188d37d873bcb3b96aad95ac5634ba1c944afd7079507c29e85365f6d02d3d19` |
| `artifacts/c4-request-rate/mutation-validation.ctrf.json` | `9a8f9e05ee34e1ea41767bb3e12d8f4ac373987d33270143903df2918b9d2dd0` |

Die generierten Rohartefakte bleiben im ignorierten `artifacts/`-Baum. Ihre Hashes binden die
ausgewerteten Bytes, ohne Buildausgaben als Quellcode einzuchecken.

## Urteil

PASS. Die neue Kohorte besitzt direkte externe Orakel, deterministische Synchronisation, vollständige
R0-Disposition und wirksame negative Nachweise. Sie enthält keine Produktfehleranpassung und keine
Produktänderung.
