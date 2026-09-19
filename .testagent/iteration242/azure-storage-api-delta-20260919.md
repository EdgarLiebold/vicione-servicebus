# Azure Storage — statischer API-Delta zum Iteration-242-Paket

Stand 2026-09-19; Basis ist `.testagent/iteration242/azure-storage-api-static-packet.md` (dortiger Source-Stand mit Git-HEAD `413d845a0ba6377a52584389ffb8979846a5fedf`). Hier wird ausschließlich die **aktuelle Working Copy** von `src/Persistence/ViciOne.ServiceBus.Azure.Storage` und den zwei zugehörigen Azure-Storage-Testprojekten samt Requirements bewertet. Beim Lesen war Repository-HEAD `730b119bd04bee3f29d2995325ecd278bb6b09c3`; die unten genannten Produkt-/Test-Deltas sind uncommitted und deshalb kein eingefrorener Commit-Beleg. Kein .NET-Prozess, Build, Test, frischer Reflection-/Paketlauf, Coverage- oder Mutationslauf durch diesen Agenten. `review/**` und `TestResults/**` wurden weder enumeriert noch gelesen.

## Quellen- und Hashbindung

Alle sechs aktuellen C#-Quelldateien, das Produktprojekt und sein Lock wurden von mir vollständig gelesen; ebenso die aktuellen sieben C#-Test-/Fixture-/Projektionsdateien, zwei Testprojekte und beide Requirement-JSONs. Die folgenden Hashes beziehen sich auf gelesene Working-Copy-Bytes, nicht auf `HEAD`. Ein Scanner-Treffer, eine Dateiliste oder ein Hash ist **keine** manuelle Volllektüre.

Präfixe: `S` = `src/Persistence/ViciOne.ServiceBus.Azure.Storage/`; `U` = `tests/Persistence/ViciOne.ServiceBus.Azure.Storage.Tests/`; `L` = `tests/Persistence/ViciOne.ServiceBus.Azure.Storage.LocalIntegration.Tests/`.

| Datei unter Präfix | SHA-256 | Delta gegen älteres Paket |
| --- | --- | --- |
| S `BlobServiceClientExtensions.cs` | `30373137a4d80fdcbd64692e0b7a3b197e74e2c50fc00c7d513a5ef5d8edaa2c` | unverändert |
| S `MessageDataRepositorySelectorExtensions.cs` | `6c22cee052406708dc2154ea201fb3686ebfdce446ef473a3dabc7312de51fa6` | unverändert |
| S `MessageData/AzureBlobMessageDataRepository.cs` | `2384e9b330245971cf5fa0907300eb0966f5add101639c7f9e0045b8c5cef3e6` | geändert |
| S `MessageData/BlockBlobUploadStream.cs` | `c9e0e27f7d1d18f2cb966f960a785f57b4d3078c08e2859dd6d3b125b705d6e3` | geändert; interner Typ |
| S `MessageData/IBlobNameGenerator.cs` | `bf559bae4fa804bc8b0b8ffe21ac6449363aa92c10f45a971a490f376e818d99` | unverändert |
| S `MessageData/NewIdBlobNameGenerator.cs` | `d8e01fbf1cae52f78160699967adb67d804a977dc79b675bec20b4377cf25904` | unverändert |
| S `ViciOne.ServiceBus.Azure.Storage.csproj` | `4776aaada984349918a9aed57806d50c273842dcb9a3d035bafc688c03dca435` | unverändert |
| S `packages.lock.json` | `93d08a1ab692aa63435447ae0ae1fc7e1cd36057b7d0a2005d1bc3f2118c73a3` | unverändert |
| U `Configuration/AzureBlobStorageConfigurationTests.cs` | `3ec6aace2cee347ccde667c3d14e3101056a34669c8ec352c2864a95bf6298e0` | unverändert im scoped Worktree-Diff |
| U `MessageData/AzureBlobMessageDataRepositoryTests.cs` | `793d31a41ec3f0de0ffc3140b67e0e5b3175dddac56e8eb562e4db6a5b1bbdfe` | geändert; während der Lesung kamen drei weitere negative Adressfälle hinzu; dieser Hash ist der nachgelesene Stand |
| U `MessageData/AzureStorageMessageDataTimeProviderTests.cs` | `c5c5daeca4b0a1cc85eb94084637385e6f1d520d01b62958e720d5e248ef78e7` | geändert |
| U `Requirements/RequirementCoverageProjectionTests.cs` | `b5aff7206c60ecee60d9ec430f38c331a7872bbc4da930030feb5f6375500f41` | unverändert |
| U `Requirements/AzureStorageRequirements.json` | `f3f5959403f7e9d8810a209862357c41c5fbbb93b9470e945bcc8b2dd2ba12df` | geändert: 20 → 24 Einträge |
| U `ViciOne.ServiceBus.Azure.Storage.Tests.csproj` | `f50d98f8bf3a2e693b3b2dc297c9b4c85451b55fbeb96f2ba8fc89e0fd45ba72` | unverändert im scoped Worktree-Diff |
| L `Infrastructure/AzureBlobTestContainer.cs` | `89bd98800859bbfbf92dcd8a48f6f51c178ddae412f71465ac76f2e75683a9a7` | unverändert |
| L `MessageData/AzureBlobMessageDataRepositoryTests.cs` | `2aaa63d5ceb51eb92a4458ced178d557b805780e7e332cfef1c285dee840200b` | geändert |
| L `Requirements/RequirementCoverageProjectionTests.cs` | `bb0066c5b05df5e33a82830983a23b8b2a196835b2b81a373fc838d261114e2d` | unverändert |
| L `Requirements/AzureStorageLocalIntegrationRequirements.json` | `fb5b2a587d72e16c151a71087dfc80e548ba1fa6b765a27534ec44d673404f27` | geändert: 5 → 6 Einträge |
| L `ViciOne.ServiceBus.Azure.Storage.LocalIntegration.Tests.csproj` | `e259171718150a65f797865790b5c88acbeddc4f1d2597ed471b65890839b536` | unverändert im scoped Worktree-Diff |

## Öffentliche Signaturen: 5 / 17 / 31 bleiben aktuell

Die frühere Zählung bleibt **vollständig** gültig: fünf exportierte Quelltypen, 17 öffentliche Member einschließlich des impliziten parameterlosen Konstruktors von `NewIdBlobNameGenerator`, und 31 explizite Parameterslots. Kein öffentlicher Typ, Membername, Parametertyp, Parametername, Reihenfolge oder Default wurde in der aktuellen Source geändert. `BlockBlobUploadStream` bleibt `internal`. Die Typen sind `BlobServiceClientExtensions`, `MessageDataRepositorySelectorExtensions`, `AzureBlobMessageDataRepository`, `IBlobNameGenerator` und `NewIdBlobNameGenerator`.

| Alte ID | Aktuelle öffentliche Signatur; alle unverändert | Verhaltensdelta |
| --- | --- | --- |
| A01 | `CreateMessageDataRepository(this BlobServiceClient client, string containerName, bool compress = false, TimeProvider? timeProvider = null)` | keines im Member |
| A02 | `UseAzureBlobStorage(this IMessageDataRepositorySelector selector, string connectionString, string containerName = "message-data", bool compress = false)` | keines im Member |
| A03 | `AzureBlobMessageDataRepository(BlobContainerClient containerClient, bool compress = false, TimeProvider? timeProvider = null)` | Konstruktion unverändert; nachfolgendes Repository-Verhalten geändert |
| A04 | `AzureBlobMessageDataRepository(BlobContainerClient containerClient, IBlobNameGenerator blobNameGenerator, bool compress = false, TimeProvider? timeProvider = null)` | Konstruktion unverändert; nachfolgendes Repository-Verhalten geändert |
| A05–A12 | `PostCreate(IBus bus)`; `CreateFaulted(Exception exception)`; `PreStartAsync(IBus bus)`; `PostStartAsync(IBus bus, Task<BusReady> busReady)`; `StartFaultedAsync(IBus bus, Exception exception)`; `PreStopAsync(IBus bus)`; `PostStopAsync(IBus bus)`; `StopFaultedAsync(IBus bus, Exception exception)` | keine Sourceänderung |
| A13 | `Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default)` | akzeptiert jetzt eng erkannte ältere SAS-Adressen, verwirft deren Credentials zugunsten des aktuellen Clients; loggt eine queryfreie Adresse |
| A14 | `Task<Uri> PutAsync(Stream stream, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)` | gibt eine queryfreie Blob-URI zurück; `TimeSpan.MaxValue` bedeutet nun keine Ablaufmetadaten; komprimierte Block-IDs haben einen zufälligen Prefix pro Upload-Versuch |
| A15 | `string IBlobNameGenerator.GenerateBlobName()` | keine Sourceänderung |
| A16 | `string NewIdBlobNameGenerator.GenerateBlobName()` | keine Sourceänderung |
| A17 | implizites `NewIdBlobNameGenerator()` | keine Sourceänderung |

Die drei geänderten Parameter-Verhaltensgrenzen sind A13.`address` (SAS-Altadresse/Query-Filter), A14.`timeToLive` (`TimeSpan.MaxValue`) und mittelbar A14.`stream`/A04.`compress` (Block-ID-Isolation bei komprimierten Uploads). A13.`cancellationToken`, A14.`cancellationToken` und alle anderen Slots behalten ihre bisherige statische Zuordnung. Die Namen `GetAsync` und `PutAsync` bleiben bei tatsächlicher `Task`-Rückgabe konventionsgemäß; die Observer-Async-Member bleiben unverändert.

## Bidirektionales Test-/Requirement-Delta

Die älteren 25 Varianten (20 Unit, 5 LocalIntegration) und ihre Testmethoden bleiben in den vollständig gelesenen Projektionen erhalten. Hinzugekommen sind **fünf** Varianten mit jeweils einer gleichlautend annotierten Methode, jetzt 24 Unit + 6 LocalIntegration = **30 Projektionseinträge**; 28 sind Verhaltenstests und zwei Metadaten-/Projektionsprüfungen. Diese Aussage ist ein manueller statischer Abgleich, kein ausgeführter Projektionsgate.

| Neue Requirement-Variante → aktuelle Testmethode | Rückrichtung zum Public Member und statisches Orakel |
| --- | --- |
| `REQ-VSB-AZURE-STORAGE-UPLOAD` / `compressed-colliding-uploads-use-disjoint-block-ids` → U.`PutAsync_UsesDisjointBlockIdsForSeparateAttemptsAtTheSameNameAsync` | A14 mit A04.`compress=true`, interner Blockstream: zwei Versuche am gleichen Blobnamen, nichtleere und disjunkte gestagte Block-ID-Mengen; Recording-Handler simuliert **keine** echte Blob-Kollision. |
| `REQ-VSB-AZURE-STORAGE-UPLOAD` / `sas-credentials-stay-out-of-claim-check-address` → U.`PutAsync_DoesNotPublishSasCredentialsAndAddressSurvivesRotationAsync` | A14→A13: zurückgegebene URI ohne SAS-Query, Lesen über neu erstellten Client mit anderer Signatur; Handler beobachtet die neue Signatur im GET. Keine reale SAS-Autorisierung. |
| `REQ-VSB-AZURE-STORAGE-DOWNLOAD` / `legacy-sas-address-uses-current-client-credentials` → U.`GetAsync_ReadsLegacySasAddressWithCurrentCredentialsAsync` | A13.`address`: ältere vollständige SAS-URI wird akzeptiert, GET verwendet aktuelle statt eingebetteter Signatur. Die bestehende `repository-address-boundary`-Variante wurde zusätzlich um negative `comp`, `snapshot` und `versionid`-Queryformen erweitert; sie ist **keine** sechste neue Variante. |
| `REQ-VSB-AZURE-STORAGE-MESSAGE-DATA-TIME` / `maximum-ttl-means-unbounded-storage` → U.`MaximumTimeToLive_DoesNotWriteExpirationMetadataAsync` | A14.`timeToLive`: `TimeSpan.MaxValue` erzeugt beim unkomprimierten Upload kein `ValidUntilUtc`; `null`, negative/Null-Dauer, positive Dauer und Überlauf waren bereits gebunden. |
| `REQ-VSB-AZURE-STORAGE-LOCAL-PERSISTENCE` / `remaining-bytes-upload-preserves-caller-stream` → L.`PutAsync_UploadsRemainingBytesAndLeavesCallerStreamOpenAsync` | A14.`stream` bei A03.`compress=false/true`: lädt ab `Position=2` nur Restbytes hoch, lässt Caller-Stream lesbar und liest Bytes über A13 zurück; lokaler konfigurierter Blob-Endpunkt, hier **nicht** ausgeführt. |

Rückrichtung der geänderten Public Member: A13 → neue `legacy-sas-address...`-Variante, erweiterte alte `repository-address-boundary`-Variante und neuer A14→A13-Rotationstest; A14 → neue SAS-, TTL-, Reststream- und Block-ID-Varianten. Die unveränderten A01–A12 und A15–A17 behalten ihre Zuordnungen aus dem früheren Paket; A03/A04 sind nur als gewählte Konstruktoren in neuen Tests zusätzlich beteiligt. Die Projektionsprüfungen sind Metadaten-Gates, keine Verhaltensbeweise.

## Verbleibende A+-Lücken und Aussagegrenze

1. Die alten drei Optionslot-Lücken bleiben: A01.`compress` hat keinen beobachteten gzip/Plain-Unterschied **über diesen Extension-Einstieg**; A02.`containerName` hat keinen beobachteten gewählten Container; A02.`compress` hat keinen beobachteten komprimierten Upload **über diesen Selector-Einstieg**. Form/Default/Rückgabetyp allein unterscheiden Verhalten nicht (`PO-2026-09-08-01` Nr. 7).
2. Der API-Shape-Test prüft nur eine Teilmenge, keine exakte vollständige 5/17/31-Assembly-Surface oder Package-Consumer-Compile. Diese Zählung ist aktuelle manuelle Quellinventur, keine frisch reflektierte oder gebaute Assembly.
3. Die neuen SAS-Rotations-/Altadress-Tests verwenden einen synthetischen HTTP-Handler. Sie belegen die lokale URI- und SDK-Request-Wahl, nicht reale SAS-Autorisierung, Rotation oder Persistenz bei Azurite/Azure. Die bereinigte `GetAsync`-Diagnostikadresse wird nicht durch einen erfassten Logeintrag geprüft.
4. Die `_blockIds`-Liste wächst weiter pro gestagtem Block; der 256-KiB-Puffer begrenzt Einzelblöcke, nicht Anzahl/gesamte Uploadgröße. Der neue Disjunktheits-Test prüft zwei Versuche, keine hohe Blockanzahl oder echte Collision-Semantik. Das ist eine offene Ressourcen-/Providergrenze, nicht allein ein nachgewiesener Produktfehler.
5. Der alte LocalIntegration-Kollisionstest bleibt ausschließlich `compress:true`; ein echter Plain-Overwrite-Versuch fehlt. Der Unsupported-Content-Encoding-Test prüft die Exception, nicht die Dispose-Wirkung des heruntergeladenen Streams. Die neue Reststream-Theory schließt dagegen die frühere Teilstream-/Caller-Ownership-Frage statisch für beide Kompressionswerte.
6. Keine der neuen oder bestehenden Methoden wurde hier ausgeführt; ein nicht ausgeführter Azurite-/Azure-Cloud-, Coverage-, CRAP-, Mutations-, Release- oder Package-Gate bleibt offen. Die statische Paarung ist weder Line-/Branch-Coverage noch ein A+-PASS.
