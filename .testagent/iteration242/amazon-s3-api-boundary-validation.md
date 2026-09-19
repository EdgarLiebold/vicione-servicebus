# Amazon-S3-API-Grenzen · Unit-Validierung · Iteration 242

## Ergebnis und Schreibscope

Vier neue, verhaltensunterscheidende Testmethoden schließen die lokal belegbaren API-Grenzen aus `amazon-s3-api-manual-review.md`: direkte Konstruktor-Nullguards, beobachtbare Client-/Bucket-/Lifecycle-Weitergabe beider Factories, getrennte Port-/Fragment-/Relative-URI-Ablehnung vor SDK-Nutzung sowie positive Weitergabe eines zulässigen Schlüssels mit `-` und `_`. Nur folgende getrackte Dateien wurden geändert:

| Pfad | vor Änderung SHA-256 | final SHA-256 |
|---|---|---|
| `tests/Persistence/ViciOne.ServiceBus.AmazonS3.Tests/MessageData/AmazonS3MessageDataConfigurationTests.cs` | `0c9b775f8b2f25ade26786bbe3e4876810f24fd00a3cabc70814d4246f0b002f` | `bd1b9180b67237a1340e15caf388472d52a7ada6f0c4d6114cfff588da61bff8` |
| `tests/Persistence/ViciOne.ServiceBus.AmazonS3.Tests/Requirements/AmazonS3Requirements.json` | `8236fdd1bf6c86af3bea159281ab1f03a6663750b020fcd07a995480cba514d8` | `fae0e3186667ae91b0ed7811ffc58b8afe677b042b9c84856e8b2a11b0b5e758` |

Der Amazon-S3-Produktpfad blieb unverändert. Kein Staging/Commit; kein LocalStack, echtes AWS oder externer Netzwerkendpunkt wurde für diese Unit-Ergänzung verwendet. Die neue `DispatchProxy`-Testhilfe hält die SDK-Grenze im Prozess an und zeichnet Requests auf; der vorhandene Non-Network-Client wird in den neuen Konstruktor-Assertions nicht aufgerufen.

## Vollständiger Owner-Testprojekt-Lesegate

`git ls-files tests/Persistence/ViciOne.ServiceBus.AmazonS3.Tests` ergibt genau sechs getrackte Dateien. Fünf davon waren im vorherigen [Lesemanifest](amazon-s3-api-read-manifest.tsv) bereits vollständig gelesen und vor diesem Edit hashgleich; der zuvor nicht inventarisierte `packages.lock.json` wurde nun vollständig gelesen. Damit `Git-Dateimenge = gelesene Dateimenge = 6`; nach dem Edit wurden die beiden geänderten Dateien erneut vollständig gelesen. Die unveränderten vier Projektdateien bleiben:

| Datei innerhalb des Owner-Testprojekts | SHA-256 |
|---|---|
| `MessageData/AmazonS3MessageDataObserverTests.cs` | `d9b1c7481f894b59c134cd5906eaf0f386c14f6338df885caa121aa150ba3a2f` |
| `Requirements/RequirementCoverageProjectionTests.cs` | `ae34f127c0423bfdbfd505a4e025ea1c0597f75d53831e920ed9043d0e7daadb` |
| `ViciOne.ServiceBus.AmazonS3.Tests.csproj` | `3808c5366244f1ca5f258b99d58581343495e773ac48dde7ea5185244c6260ed` |
| `packages.lock.json` | `46df372db0f9225e84a1a7b4e7ba7e925a61eab95824334a2cf4b8a122914a4b` |

Wirksame zentrale Eingaben wurden vollständig gelesen: `global.json` (`9384d5f0…f93`), Root-`Directory.Build.props` (`194d42a2…2cf`), Root-`Directory.Build.targets` (`6afa8938…826`), `Directory.Packages.props` (`c942e3b9…432`), `tests/Directory.Build.props` (`0ab460a2…234`), `tests/Directory.Build.targets` (`3a866a9a…db1`), `tests/testconfig.json` (`3d0eec5f…342f`) und `tests/testsettings.json` (`d23bf4c3…9ac7`). Die autoritative Plattform ist .NET SDK `10.0.302` mit `Microsoft.Testing.Platform`, xUnit v3/MTP-v2; die zentrale Testkonfiguration setzt `failSkips=true` und `failWarns=true`. Der Projektgraph referenziert das Amazon-S3-Produkt und die bestehende Testinfrastruktur, ohne neue Pakete.

## Ausführung und anfänglicher Testhilfenfehler

1. `dotnet build tests/Persistence/ViciOne.ServiceBus.AmazonS3.Tests/ViciOne.ServiceBus.AmazonS3.Tests.csproj --configuration Release --no-incremental --disable-build-servers -v:minimal` endete zunächst erfolgreich mit **0 Warnungen und 0 Fehlern**.
2. Der erste fokussierte Klassenlauf über `dotnet test --project tests/Persistence/ViciOne.ServiceBus.AmazonS3.Tests/ViciOne.ServiceBus.AmazonS3.Tests.csproj --configuration Release --no-build --filter-class ViciOne.ServiceBus.AmazonS3.Tests.MessageData.AmazonS3MessageDataConfigurationTests` endete **4/9 bestanden, 5/9 fehlgeschlagen, 0 Skips**. Ursache war ausschließlich die neu angelegte, versehentlich `sealed` deklarierte `DispatchProxy`-Testbasisklasse (`ArgumentException: The base type ... cannot be sealed`). **Dies war kein Buildfehler und kein Produktfehler.** Die Testhilfe wurde auf nicht-`sealed` korrigiert; weder Produktcode noch Testorakel wurden abgeschwächt.
3. Derselbe frische Release-Build mit `--no-incremental` endete nach der Korrektur erneut mit **0 Warnungen und 0 Fehlern**.
4. Derselbe fokussierte Klassenlauf endete danach **9/9 bestanden, 0 fehlgeschlagen, 0 Skips**.
5. `dotnet test --project tests/Persistence/ViciOne.ServiceBus.AmazonS3.Tests/ViciOne.ServiceBus.AmazonS3.Tests.csproj --configuration Release --no-build` endete für das eigene Unit-Projekt ungefiltert **17/17 bestanden, 0 fehlgeschlagen, 0 Skips**. Darin wurde auch `AmazonS3Requirements_MatchCompiledRequirementMetadata` ausgeführt; die Projektionsprüfung ist grün. Zusätzlich validiert `jq` die JSON-Datei als 14 eindeutige Requirement-/Variant-Zuordnungen. `git diff --check` ist sauber.

## Requirement → Testorakel

| Lokal belegbare Grenze | Neue Testmethode und unterscheidende Assertion |
|---|---|
| direkter Konstruktor mit `null`-Client bzw. `null`-Options | `RepositoryConstructor_RejectsNullClientAndOptions`: genaue `ArgumentNullException.ParamName`-Werte `client`/`options` |
| Factory-Weitergabe | `Factories_UseTheSuppliedClientAndBucketLifecycleSettingsAsync`: je Factory eigener SDK-Proxy; `HeadBucketRequest.BucketName`, kein fremder Proxy-Aufruf, nur beim Selector konfigurierte Lifecycle-Regel mit exakt sieben Tagen |
| Port, Fragment und relative URI | `GetAsync_RejectsPortFragmentAndRelativeAddressBeforeClientUseAsync`: drei Theory-Fälle, `ArgumentException.ParamName=address`, kein `GetObjectRequest` |
| gültiger Schlüssel mit Bindestrich und Unterstrich | `GetAsync_AcceptsSafeHyphenAndUnderscoreKeyAsync`: SDK-Grenze erreicht, Bucket und `key_A-9` exakt im `GetObjectRequest`; Sentinel-Exception beweist keinen echten Transporterfolg |

Die Identität des unveränderlichen Optionsobjekts als Referenz wird bewusst nicht per Reflection behauptet; geprüft sind seine beobachtbaren Bucket- und Lifecycle-Wirkungen. Diese Testgruppe beweist keine positiven `PutAsync`-TTL-, Mehrfach-Upload- oder öffentlichen Bus-Startup-Fälle aus dem größeren manuellen Review.

## Separater Root-Nachweis: LocalStack

Der Root-Agent meldete nach diesem Unit-Lauf einen eigenen ungefilterten Release-Lauf des `AmazonS3.LocalIntegration`-Projekts mit frischem Build gegen seine eigene `--broker localstack`-Fixture: **5/5 bestanden, 0 fehlgeschlagen, 0 Skips, Exit 0**. Run-ID: `vicione-f2fdff43df9a`; `fixture-findings.json` enthielt `[]`. Dies ist ein separater, vom Root-Agenten ausgeführter Nachweis und kein von diesem Amazon-S3-Unit-Paket gestarteter Lauf. Seine Fixture- und Testresultate belegen die lokale Providerintegration, nicht das Verhalten eines echten AWS-Kontos.

## Offene Providergrenzen

Der hier grüne Unit-Lauf verwendet weder LocalStack noch echtes AWS; der separate Root-Lauf deckt LocalStack aktuell ab. Reale AWS-IAM-/Quota-/Throttling-/Restart-Grenzen und servicegesteuerte Lifecycle-Langzeitlöschung bleiben `EXTERNAL_PENDING`: **LocalStack ≠ echtes AWS**, und ein Unit- oder LocalStack-PASS ist dafür kein Ersatz. Der repositoryweite API-Nenner aus `current-api-reflected.txt` bleibt wegen paralleler Änderungen am Gesamtbaum global stale; dieser Test-Edit ist keine neue API-Reflexion oder Gesamtfreigabe.
