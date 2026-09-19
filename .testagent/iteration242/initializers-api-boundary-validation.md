# Initializers API · Nachprüfung der ersten Parametergrenzen (2026-09-19)

Dies ist ein Addendum zum bereits versionierten statischen `initializers-api-review.md` und `initializers-api-member-review.tsv`, keine Neuzählung des API-Nenners. Deren damaliger Befund 2 ist für die unten genannten Overloads durch direkte negative Testaufrufe geschlossen. Der manuelle API-Numerator bleibt **8/8 Typen, 33/33 Member, 107/107 Parameterslots**; er ist keine gemessene Code-Coverage oder A+-Freigabe.

| Member-IDs | Direkter neuer Gegenbeleg | RequirementCoverage-Projektion |
| --- | --- | --- |
| INI-05/06/08/09 | Je typed/untyped Send-/Publish-Pipe: Null-`endpoint` mit exaktem `ParamName` und einfacher, nicht-Advanced Endpoint mit `NotSupportedException`. | `REQ-VSB-ADVANCED-MESSAGE-INITIALIZER/capability-validation` |
| INI-12–16 | Je Callback-/Mehrfachantwort-Form: Null-`client` mit exaktem `ParamName` und einfacher, nicht-Advanced Request-Client mit `NotSupportedException`. | `REQ-VSB-ADVANCED-REQUEST-INITIALIZER/capability-validation` |
| INI-18/19/21/22 | Je typed/untyped ScheduleSend-/SchedulePublish-Pipe: Null-`scheduler` mit exaktem `ParamName` und einfacher, nicht-Advanced Scheduler mit `NotSupportedException`. | `REQ-VSB-ADVANCED-SCHEDULE-INITIALIZER/capability-validation` |
| INI-24/26 | Je Runtime-Selected-Pipe: Null-`endpoint` mit exaktem `ParamName`. Ein einfacher Endpoint ist bei diesen Dispatcher-basierten Overloads unterstützt, also kein `NotSupportedException`-Fall. | `REQ-VSB-RUNTIME-MESSAGE-INITIALIZER/required-inputs` |

Nur drei zuvor saubere Core-Testdateien wurden geändert (jeweils rein additive Assertions in bestehenden `[RequirementCoverage]`-Methoden); `CoreRequirements.json`, Produktcode und andere Packages blieben durch dieses Paket unberührt. Die JSON-Requirement-Projektion verwendet diese bereits vorhandenen Methoden. Der Worktree enthält parallel fremde Änderungen, weshalb der unfiltrierte Lauf ein Grünstand des gesamten damaligen Core-Worktrees, kein isolierter Kausalbeweis ausschließlich für diese drei Dateien ist.

| Geänderte Datei | SHA-256 nach Testlauf | Diff gegen HEAD |
| --- | --- | --- |
| `tests/ViciOne.ServiceBus.Tests/Initializers/AdvancedMessageInitializerExtensionsTests.cs` | `0a11eea8809af157b3fb12da536b2ed4b815cc46d23a23dbb13f05abff86f5f6` | +30/−0 Zeilen |
| `tests/ViciOne.ServiceBus.Tests/Initializers/AdvancedRequestInitializerExtensionsTests.cs` | `7535f9dd64385b0d3338b34cc733e6de2b2e50750116258ccfac953bdab1671d` | +36/−0 Zeilen |
| `tests/ViciOne.ServiceBus.Tests/Initializers/AdvancedScheduleInitializerExtensionsTests.cs` | `83b5690fa71bd01372737ad2612c72c16683a7f8b81195d14a4204a9011dc4e2` | +30/−0 Zeilen |

Verifikation, Microsoft.Testing.Platform/xUnit v3, Release/net10.0/x64:

- `dotnet test --project tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --filter-class ViciOne.ServiceBus.Tests.Initializers.AdvancedMessageInitializerExtensionsTests ViciOne.ServiceBus.Tests.Initializers.AdvancedRequestInitializerExtensionsTests ViciOne.ServiceBus.Tests.Initializers.AdvancedScheduleInitializerExtensionsTests`: **12/12 erfolgreich, 0 fehlgeschlagen, 0 übersprungen**.
- `dotnet test --project tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release`: **6242/6242 erfolgreich, 0 fehlgeschlagen, 0 übersprungen**.
- `git diff --check` für genau die drei Testdateien: ohne Befund.

Der vorherige vollständige Core-Projekt-Leseledger enthielt 702 Dateien; die zwei später hinzugekommenen Core-Testdateien waren separat vom Lead vollständig gelesen und committed. Vor diesem Testedit wurden alle damaligen 704 Core-Testpfade und ihre Hashes gegen beide Belege abgeglichen; einzig `Testing/DependencyInjectionTestHarnessTests.cs` wich ab und wurde vollständig nachgelesen. Dies ist eine **Delta-Closure aus dem Team-Ledger**, keine Behauptung, ich hätte persönlich alle 704 Dateien neu gelesen. Parallel nachfolgende fremde Teständerungen machen diese 704-Hashliste nicht zu einem aktuellen Freeze.

Alle zehn Projekt-/Quelldateien der Initializers-Assembly passen weiterhin SHA-256-genau zum versionierten `initializers-api-read-manifest.tsv`; der globale 30-Assembly-Snapshot bleibt aus den dort beschriebenen Gründen stale. Kein Staging, Commit oder Push durch dieses Paket.
