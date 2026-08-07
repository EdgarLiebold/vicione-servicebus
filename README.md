# ViciOne.ServiceBus

ViciOne.ServiceBus ist der Messaging- und Saga-Baustein der ViciOne Suite. Der vollständige Fork stellt Projekte, Assemblies, Pakete, Namespaces, öffentliche Typen, Konfiguration, Wire-Identitäten, Diagnose und Tests unter einer einzigen technischen Produktidentität bereit.

Der Arbeitsstand ist ein privater Entwicklungsstand. Er ist erst nach bestandenen Funktions-, Sicherheits-, Provenienz- und Publikationsgates zur Veröffentlichung vorgesehen.

## Herkunft und Lizenz — Deutsch

Dieses Repository ist ein vollständiger Fork von **MassTransit 8.5.10**, Upstream-Commit `62ab339afa3bac2e9b3fe1769d0d35d7e44778e9`, aus dem Projekt [MassTransit](https://github.com/MassTransit/MassTransit). Der übernommene und geänderte Bestand steht unter der **Apache License 2.0**; siehe [LICENSE](LICENSE), [NOTICE](NOTICE), [COPYRIGHT](COPYRIGHT) und [MODIFICATIONS.md](MODIFICATIONS.md).

ViciOne hat die technische Produktidentität repositoryweit auf `ViciOne.ServiceBus` umgestellt. Dazu gehören insbesondere Pfade, Projekte, Assemblies, Paket-IDs, Namespaces und Typen sowie Konfigurationsschlüssel, Wireheader, MIME-Typen, Topologien, Telemetrie, Logs, Generatoren, Analyzer, Tests, Fixtures, Samples und Buildautomation. Die Umbenennung ändert weder die Herkunft noch die Apache-2.0-Pflichten.

## Origin and license — English

This repository is a complete fork of **MassTransit 8.5.10**, upstream commit `62ab339afa3bac2e9b3fe1769d0d35d7e44778e9`, from the [MassTransit](https://github.com/MassTransit/MassTransit) project. The retained and modified code is licensed under the **Apache License 2.0**; see [LICENSE](LICENSE), [NOTICE](NOTICE), [COPYRIGHT](COPYRIGHT), and [MODIFICATIONS.md](MODIFICATIONS.md).

ViciOne changed the technical product identity throughout the repository to `ViciOne.ServiceBus`. This includes paths, projects, assemblies, package IDs, namespaces and types, configuration keys, wire headers, MIME types, topology names, telemetry, logs, generators, analyzers, tests, fixtures, samples, and build automation. The rename does not change the origin or any Apache-2.0 obligation.

## Build

Requirements:

- .NET SDK 10.0.302 or the compatible repository-approved SDK baseline
- access to the package sources configured for the development environment

```bash
dotnet restore ViciOne.ServiceBus.sln
dotnet build ViciOne.ServiceBus.sln --configuration Release --no-restore
dotnet test ViciOne.ServiceBus.sln --configuration Release --no-build --no-restore
```

The repository contains transport and persistence integration tests that require their unchanged external infrastructure. Missing infrastructure is reported as an explicit incomplete proof; tests are not silently skipped or weakened for the identity refactor.

## Scope

The fork retains the complete upstream source scope, including in-memory messaging, broker transports, riders, saga persistence, scheduling, analyzers, generators, test infrastructure, benchmarks, and samples. ViciOne Suite integration is deliberately outside this identity-only work package.
