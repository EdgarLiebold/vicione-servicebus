# Azure Storage — statisches, dateigenaues API-Prüfpaket

Stand: 2026-09-19. Scope: ausschließlich Assembly `ViciOne.ServiceBus.Azure.Storage`, `src/Persistence/ViciOne.ServiceBus.Azure.Storage/**`. Read-only-Prüfung von Produkt- und Testcode; nur dieser Bericht wurde geschrieben. Keine .NET-Prozesse, kein Testlauf, kein Stage/Commit/Push. Keine Aussage über ein A+-PASS.

## Freeze und vollständig gelesene Assembly-Dateien

Git `HEAD` `413d845a0ba6377a52584389ffb8979846a5fedf`, Tree `2b3cddb7d2e74ebde6989bcb2ec70b07e462449e`. `git ls-files` im Assembly-Pfad: 8; gelesen: 8. Die SHA-256-Werte beziehen sich auf die aktuelle Working Copy beim Lesen, nicht auf eine Behauptung der HEAD-Gleichheit.

| Pfad (relativ zum ServiceBus-Repository) | SHA-256 |
|---|---|
| `src/Persistence/ViciOne.ServiceBus.Azure.Storage/BlobServiceClientExtensions.cs` | `30373137a4d80fdcbd64692e0b7a3b197e74e2c50fc00c7d513a5ef5d8edaa2c` |
| `src/Persistence/ViciOne.ServiceBus.Azure.Storage/MessageDataRepositorySelectorExtensions.cs` | `6c22cee052406708dc2154ea201fb3686ebfdce446ef473a3dabc7312de51fa6` |
| `src/Persistence/ViciOne.ServiceBus.Azure.Storage/MessageData/AzureBlobMessageDataRepository.cs` | `85922991b5712f53c05b47a948733f61dbc89b0b70d3a5c57a4542878d92793d` |
| `src/Persistence/ViciOne.ServiceBus.Azure.Storage/MessageData/BlockBlobUploadStream.cs` | `e8379b97996619728e4dc6dc8c4fdf9fcba27868523b46d238c908761fd057e6` |
| `src/Persistence/ViciOne.ServiceBus.Azure.Storage/MessageData/IBlobNameGenerator.cs` | `bf559bae4fa804bc8b0b8ffe21ac6449363aa92c10f45a971a490f376e818d99` |
| `src/Persistence/ViciOne.ServiceBus.Azure.Storage/MessageData/NewIdBlobNameGenerator.cs` | `d8e01fbf1cae52f78160699967adb67d804a977dc79b675bec20b4377cf25904` |
| `src/Persistence/ViciOne.ServiceBus.Azure.Storage/ViciOne.ServiceBus.Azure.Storage.csproj` | `4776aaada984349918a9aed57806d50c273842dcb9a3d035bafc688c03dca435` |
| `src/Persistence/ViciOne.ServiceBus.Azure.Storage/packages.lock.json` | `93d08a1ab692aa63435447ae0ae1fc7e1cd36057b7d0a2005d1bc3f2118c73a3` |

Projektbefund: `net10.0`, Nullable aktiviert, `Azure.Storage.Blobs` und das Kernprojekt referenziert; Lock löst `Azure.Storage.Blobs` auf `12.29.2` auf. Der interne `BlockBlobUploadStream` gehört nicht zur exportierten API, wurde aber für den öffentlichen komprimierten `PutAsync`-Pfad vollständig gelesen. Die vorhandene `docs/api/packed-public-api.txt`-Sektion 5573–5595 stimmt in der sichtbaren Signaturform mit der Quellinventur überein; sie ist hier kein frischer Compile- oder Paketbeweis.

## Öffentliche Oberfläche — aus Quelle inventarisiert

Fünf exportierte Typen, 16 explizit deklarierte öffentliche Member, 1 impliziter öffentlicher parameterloser Konstruktor (`NewIdBlobNameGenerator`), 31 explizite Parameterslots. Keine öffentlichen Properties oder Events in diesen Typen. Abkürzungen: `C` = `AzureBlobStorageConfigurationTests`; `U` = Unit-`AzureBlobMessageDataRepositoryTests`; `T` = `AzureStorageMessageDataTimeProviderTests`; `L` = LocalIntegration-`AzureBlobMessageDataRepositoryTests`. Rechts stehen konkrete Methodennamen/Kürzel, nicht Ausführungsergebnisse.

| ID | Typ / deklarierter Member | Parameterslots in Reihenfolge | Statischer Testanker |
|---|---|---|---|
| A01 | `BlobServiceClientExtensions.CreateMessageDataRepository` | `client`, `containerName`, `compress=false`, `timeProvider=null` | C.`PublicApi_UsesOneAzureBlobVocabularyWithoutLegacyAliases`; C.`Construction_UsesCallerOwnedClientsAndRejectsInvalidArguments`; T.`TimeToLiveMetadata_IsWrittenAtomicallyUsingTheInjectedClockAsync` |
| A02 | `MessageDataRepositorySelectorExtensions.UseAzureBlobStorage` | `selector`, `connectionString`, `containerName="message-data"`, `compress=false` | C.`PublicApi_UsesOneAzureBlobVocabularyWithoutLegacyAliases`; C.`Selector_UsesExplicitVerbDefaultContainerAndStrictValidation` |
| A03 | `AzureBlobMessageDataRepository` constructor 1 | `containerClient`, `compress=false`, `timeProvider=null` | C.`PublicApi_UsesOneAzureBlobVocabularyWithoutLegacyAliases`; C.`Construction_UsesCallerOwnedClientsAndRejectsInvalidArguments`; T.`TimeToLiveMetadata_IsWrittenAtomicallyUsingTheInjectedClockAsync`; L.`PutAndGetAsync_RoundTripExactBytesThroughAzuriteAsync` |
| A04 | `AzureBlobMessageDataRepository` constructor 2 | `containerClient`, `blobNameGenerator`, `compress=false`, `timeProvider=null` | C.`PublicApi_UsesOneAzureBlobVocabularyWithoutLegacyAliases`; C.`Construction_UsesCallerOwnedClientsAndRejectsInvalidArguments`; U.`PutAsync_RejectsInvalidStreamsAndGeneratedNamesBeforeTransportUseAsync`; L.`PutAsync_CommitsExpirationAndContentEncodingWithTheBlobAsync` |
| A05 | `AzureBlobMessageDataRepository.PostCreate` | `bus` | U.`BusObserverLifecycle_ValidatesArgumentsAndPropagatesAzureStartupFailuresAsync` |
| A06 | `AzureBlobMessageDataRepository.CreateFaulted` | `exception` | U.`BusObserverLifecycle_ValidatesArgumentsAndPropagatesAzureStartupFailuresAsync` |
| A07 | `AzureBlobMessageDataRepository.PreStartAsync` | `bus` | U.`PreStartAsync_CreatesOnlyAMissingContainerAsync`; U.`BusObserverLifecycle_ValidatesArgumentsAndPropagatesAzureStartupFailuresAsync`; L.`PreStartAsync_CreatesTheMissingContainerBeforeReportingReadyAsync` |
| A08 | `AzureBlobMessageDataRepository.PostStartAsync` | `bus`, `busReady` | U.`BusObserverLifecycle_ValidatesArgumentsAndPropagatesAzureStartupFailuresAsync` |
| A09 | `AzureBlobMessageDataRepository.StartFaultedAsync` | `bus`, `exception` | U.`BusObserverLifecycle_ValidatesArgumentsAndPropagatesAzureStartupFailuresAsync` |
| A10 | `AzureBlobMessageDataRepository.PreStopAsync` | `bus` | U.`BusObserverLifecycle_ValidatesArgumentsAndPropagatesAzureStartupFailuresAsync` |
| A11 | `AzureBlobMessageDataRepository.PostStopAsync` | `bus` | U.`BusObserverLifecycle_ValidatesArgumentsAndPropagatesAzureStartupFailuresAsync` |
| A12 | `AzureBlobMessageDataRepository.StopFaultedAsync` | `bus`, `exception` | U.`BusObserverLifecycle_ValidatesArgumentsAndPropagatesAzureStartupFailuresAsync` |
| A13 | `AzureBlobMessageDataRepository.GetAsync` | `address`, `cancellationToken=default` | U.`GetAsync_DerivesCompressionFromPersistedContentEncodingAsync`; U.`GetAsync_RejectsAddressesOutsideTheConfiguredContainerBeforeTransportUseAsync`; U.`GetAsync_RejectsUnsupportedContentEncodingAsync`; U.`GetAsync_TranslatesAzureRequestFailuresToMessageDataExceptionAsync`; L.`PutAndGetAsync_RoundTripExactBytesThroughAzuriteAsync` |
| A14 | `AzureBlobMessageDataRepository.PutAsync` | `stream`, `timeToLive=null`, `cancellationToken=default` | U.`PutAsync_UploadsPlainPayloadAndForwardsCancellationAsync`; U.`PutAsync_StreamsCompressedPayloadInBoundedBlocksAndForwardsCancellationAsync`; U.`PutAsync_WhenCompressionSourceFails_DoesNotCommitPartialBlobAsync`; U.`PutAsync_RejectsInvalidStreamsAndGeneratedNamesBeforeTransportUseAsync`; T.`TimeToLiveMetadata_IsWrittenAtomicallyUsingTheInjectedClockAsync`; T.`NonPositiveTimeToLive_IsRejectedBeforeUploadAsync`; T.`ExpirationOutsideDateTimeOffsetRange_IsRejectedBeforeUploadAsync`; T.`MissingTimeToLive_DoesNotIssueAMetadataWriteAsync`; T.`TimeToLiveUpload_HonorsCallerCancellationAsync`; L.`PutAndGetAsync_RoundTripExactBytesThroughAzuriteAsync`; L.`PutAsync_CommitsExpirationAndContentEncodingWithTheBlobAsync`; L.`PutAsync_DoesNotOverwriteAnExistingBlobAsync` |
| A15 | `IBlobNameGenerator.GenerateBlobName` | — | C.`DefaultBlobNameGenerator_ProducesNonEmptyDistinctNames`; U.`PutAsync_RejectsInvalidStreamsAndGeneratedNamesBeforeTransportUseAsync` |
| A16 | `NewIdBlobNameGenerator.GenerateBlobName` | — | C.`DefaultBlobNameGenerator_ProducesNonEmptyDistinctNames` |
| A17 | `NewIdBlobNameGenerator()` | —; implizit, nicht im C#-Quelltext deklariert | C.`DefaultBlobNameGenerator_ProducesNonEmptyDistinctNames`; packed-API-Sektion 5591–5593 |

`AzureBlobMessageDataRepository` deklariert `IMessageDataRepository` und `IBusObserver`; geerbte oder vom Compiler synthetisierte weitere Member wurden nicht als Quelldeklarationen gezählt. Der interne `BlockBlobUploadStream` und seine Overrides sind keine extern sichtbaren Assembly-Member.

## Jeder öffentliche Parameterslot — Unterscheidung und Grenze

`direkt` bedeutet nur, dass der gelesene Testcode einen positiven/negativen oder zwei beobachtbar verschiedene Fälle formuliert. Es bedeutet nicht, dass er ausgeführt wurde. `Form` bedeutet lediglich Signatur-/Default-Reflexion, `offen` fehlendes verhaltensunterscheidendes Orakel.

| Member | Slot(s) | Statische Zuordnung und Status |
|---|---|---|
| A01 | `client` | C.Construction gültig/null: direkt. |
| A01 | `containerName` | C.Construction gültig/leer/Whitespace: direkt für Validierung; tatsächliche Containerweitergabe nur indirekt. |
| A01 | `compress` | C.PublicApi prüft Typ/Default; **offen**: kein Aufruf des Extension-Members mit `compress:true` und beobachtetem gzip/Plain-Unterschied. |
| A01 | `timeProvider` | T.TimeToLiveMetadata ruft die Extension mit fixierter Uhr und prüft Metadatum: direkt. |
| A02 | `selector` | C.Selector gültig/null: direkt nur für Nullgrenze; Selector wird sonst nicht verwendet. |
| A02 | `connectionString` | C.Selector gültig/leer/Whitespace: direkt für Validierung/Konstruktion; Zielendpoint nicht beobachtet. |
| A02 | `containerName` | C.PublicApi prüft Default, C.Selector prüft leer/Whitespace und akzeptiert `custom-data` nur per Rückgabetyp: **offen für Weitergabe/Containerwahl**. |
| A02 | `compress` | C.PublicApi prüft Default, C.Selector ruft `true` auf und prüft nur Rückgabetyp: **offen für komprimierten Upload über diesen Einstieg**. |
| A03 | `containerClient` | C.Construction gültig/null/namenslos; L.RoundTrip nutzt Container: direkt. |
| A03 | `compress` | L.RoundTrip `false`/`true` mit Bytevergleich; U.PutAsync plain/compressed über A04: direkt nur bei L. |
| A03 | `timeProvider` | T.TimeToLiveMetadata über A01→A03 fixierte Uhr: direkt. Null-/System-Fallback separat nicht beobachtet. |
| A04 | `containerClient` | C.Construction gültig/null/namenslos; L.Properties und L.Collision nutzen konkreten Client: direkt. |
| A04 | `blobNameGenerator` | C.Construction null; U.InvalidStreamsAndGeneratedNames leer/Whitespace; L.Properties und L.Collision fixer Name: direkt. |
| A04 | `compress` | L.Properties `true` prüft `gzip` und L.Collision `true`: direkt. `false` bei A04 zusätzlich über U.PutAsync plain beobachtet. |
| A04 | `timeProvider` | L.Properties fixierte Uhr und exakte Ablaufmetadaten: direkt. Null-/System-Fallback separat nicht beobachtet. |
| A05 | `bus` | U.BusObserverLifecycle gültig/null: direkt für Nullgrenze; Methode arbeitet bei gültigem Bus bewusst nicht weiter. |
| A06 | `exception` | U.BusObserverLifecycle gültig/null: direkt für Nullgrenze; Methode arbeitet bei gültiger Exception bewusst nicht weiter. |
| A07 | `bus` | U.PreStart und U.BusObserverLifecycle gültig/null; L.PreStart realer lokaler Container: direkt. |
| A08 | `bus`, `busReady` | U.BusObserverLifecycle gültig/null pro Slot: direkt für Nullgrenze; keine Prüfung einer noch laufenden/fehlgeschlagenen Ready-Task, obwohl der Parameter nicht verwendet wird. |
| A09 | `bus`, `exception` | U.BusObserverLifecycle gültig/null je Slot: direkt für Nullgrenzen. |
| A10 | `bus` | U.BusObserverLifecycle gültig/null: direkt für Nullgrenze. |
| A11 | `bus` | U.BusObserverLifecycle gültig/null: direkt für Nullgrenze. |
| A12 | `bus`, `exception` | U.BusObserverLifecycle gültig/null je Slot: direkt für Nullgrenzen. |
| A13 | `address` | U.GetAddressBoundary mehrere fremde/ungültige Formen und gültiger Read; direkt. |
| A13 | `cancellationToken` | U.GetAddressBoundary vorab abgebrochener Token; U.GetFormat prüft CanBeCanceled; direkt, aber In-flight-Abbruch nicht beobachtet. |
| A14 | `stream` | U.PutInvalid gültig/null/nicht lesbar und Payload-Bytes; direkt. |
| A14 | `timeToLive` | T fünf Fälle (positiv, 0/negativ, Überlauf, null, Cancellation), L.Properties: direkt. |
| A14 | `cancellationToken` | U.PutPlain/Compressed leiten kündbaren Token weiter; T.Cancellation bricht laufenden Upload ab: direkt. |

## Rückrichtung: jede Azure-Requirement-Projektion zu Membern

Gelesene aktuelle Projektionen: 20 Einträge im Unit-Projekt (`AzureStorageRequirements.json`), 5 im LocalIntegration-Projekt (`AzureStorageLocalIntegrationRequirements.json`). Alle 25 referenzierten Methoden sind im jeweils gelesenen C#-Testcode mit gleichlautendem `[RequirementCoverage]` vorhanden. 23 sind Verhaltenstests, 2 prüfen nur die Projektion gegen kompilierte Metadaten. Die beiden Projektdateien betten ihre JSON-Projektion ein und referenzieren die Azure-Storage-Assembly. In der folgenden Tabelle steht `...` jeweils für das gemeinsame Präfix `REQ-VSB-AZURE-STORAGE`. Das ist eine statische Konsistenzbeobachtung; die Verifikation wurde nicht ausgeführt.

| Requirement / Variant (Kurzform) | konkrete Testmethode (Klasse C/U/T/L) | berührte Member |
|---|---|---|
| `REQ-VSB-AZURE-STORAGE-API` / `greenfield-blob-api-shape` | C.`PublicApi_UsesOneAzureBlobVocabularyWithoutLegacyAliases` | A01–A04, A17; nur Teiloberfläche reflektiert |
| `...-CONSTRUCTION` / `caller-owned-client-and-argument-boundaries` | C.`Construction_UsesCallerOwnedClientsAndRejectsInvalidArguments` | A01, A03, A04 |
| `...-CONSTRUCTION` / `selector-default-and-validation` | C.`Selector_UsesExplicitVerbDefaultContainerAndStrictValidation` | A02 |
| `...-BLOB-NAMES` / `default-generator-produces-nonempty-distinct-names` | C.`DefaultBlobNameGenerator_ProducesNonEmptyDistinctNames` | A15–A17 |
| `...-UPLOAD` / `plain-payload-address-and-cancellation` | U.`PutAsync_UploadsPlainPayloadAndForwardsCancellationAsync` | A04, A14 |
| `...-UPLOAD` / `bounded-compressed-payload-and-cancellation` | U.`PutAsync_StreamsCompressedPayloadInBoundedBlocksAndForwardsCancellationAsync` | A04, A14; interner Blockstream |
| `...-UPLOAD` / `compression-failure-does-not-commit-partial-blob` | U.`PutAsync_WhenCompressionSourceFails_DoesNotCommitPartialBlobAsync` | A04, A14; interner Blockstream |
| `...-UPLOAD` / `stream-and-generated-name-validation` | U.`PutAsync_RejectsInvalidStreamsAndGeneratedNamesBeforeTransportUseAsync` | A04, A14, A15 |
| `...-DOWNLOAD` / `format-is-derived-from-persisted-content-encoding` | U.`GetAsync_DerivesCompressionFromPersistedContentEncodingAsync` | A13 |
| `...-DOWNLOAD` / `repository-address-boundary` | U.`GetAsync_RejectsAddressesOutsideTheConfiguredContainerBeforeTransportUseAsync` | A13 |
| `...-DOWNLOAD` / `unsupported-content-encoding-is-rejected` | U.`GetAsync_RejectsUnsupportedContentEncodingAsync` | A13 |
| `...-DOWNLOAD` / `azure-failure-is-translated` | U.`GetAsync_TranslatesAzureRequestFailuresToMessageDataExceptionAsync` | A13 |
| `...-OBSERVER` / `container-existence-and-creation` | U.`PreStartAsync_CreatesOnlyAMissingContainerAsync` | A07 |
| `...-OBSERVER` / `lifecycle-validation-and-fail-closed-startup` | U.`BusObserverLifecycle_ValidatesArgumentsAndPropagatesAzureStartupFailuresAsync` | A05–A12 |
| `...-MESSAGE-DATA-TIME` / `ttl-metadata-is-atomic-and-uses-injected-clock` | T.`TimeToLiveMetadata_IsWrittenAtomicallyUsingTheInjectedClockAsync` | A01, A03, A14 |
| `...-MESSAGE-DATA-TIME` / `nonpositive-ttl-is-rejected-before-upload` | T.`NonPositiveTimeToLive_IsRejectedBeforeUploadAsync` | A14 |
| `...-MESSAGE-DATA-TIME` / `overflowing-expiration-is-rejected-before-upload` | T.`ExpirationOutsideDateTimeOffsetRange_IsRejectedBeforeUploadAsync` | A14 |
| `...-MESSAGE-DATA-TIME` / `missing-ttl-does-not-write-expiration-metadata` | T.`MissingTimeToLive_DoesNotIssueAMetadataWriteAsync` | A14 |
| `...-MESSAGE-DATA-TIME` / `atomic-ttl-upload-honors-cancellation` | T.`TimeToLiveUpload_HonorsCallerCancellationAsync` | A14 |
| `...-REQUIREMENT-PROJECTION` / `compiled-metadata-matches-projection` | Unit `RequirementCoverageProjectionTests.AzureStorageRequirements_MatchCompiledRequirementMetadata` | kein Produktmember; Projektions-Metagate |
| `...-LOCAL-STARTUP` / `missing-container-created-before-ready` | L.`PreStartAsync_CreatesTheMissingContainerBeforeReportingReadyAsync` | A03, A07 |
| `...-LOCAL-PERSISTENCE` / `plain-and-compressed-round-trip` | L.`PutAndGetAsync_RoundTripExactBytesThroughAzuriteAsync` | A03, A13, A14 |
| `...-LOCAL-PROPERTIES` / `ttl-and-content-encoding-committed-atomically` | L.`PutAsync_CommitsExpirationAndContentEncodingWithTheBlobAsync` | A04, A14 |
| `...-LOCAL-COLLISION` / `duplicate-generated-name-cannot-overwrite` | L.`PutAsync_DoesNotOverwriteAnExistingBlobAsync` | A04, A14, A15; nur komprimierter Pfad |
| `...-LOCAL-REQUIREMENT-PROJECTION` / `compiled-metadata-matches-projection` | Local `RequirementCoverageProjectionTests.AzureStorageLocalIntegrationRequirements_MatchCompiledRequirementMetadata` | kein Produktmember; Projektions-Metagate |

## Befunde / offene Orakel

1. **Offen — drei öffentliche Optionslots ohne unterscheidendes Verhaltensorakel:** A01.`compress` sowie A02.`containerName` (Weitergabe) und A02.`compress`. C.PublicApi/C.Selector belegen Form, Default beziehungsweise bloße Rückgabetypen, aber nicht deren Wirkung. Das widerspricht einer Behauptung, `PO-2026-09-08-01` Nr. 7 sei für diese Assembly vollständig bewiesen. Kein Produktdefekt allein daraus abgeleitet.
2. **Offen — Public-API-Inventar als Testorakel ist partiell:** C.PublicApi zählt zwar zwei Repository-Konstruktoren und je eine Extension-Methode, verlangt aber keine exakte exportierte Typ-/Membermenge und keine Signaturen von A05–A16. Die statische Quellinventur und `packed-public-api.txt` ersetzen keinen aktuellen Paket-/Consumer-Compile unter identischem Freeze.
3. **Offen — reale Durchsetzung und Ressourcen:** Die LocalIntegration-Methoden verwenden einen konfigurierten lokalen Azure-Blob-Endpunkt (Azurite-Pfad), wurden hier nicht gestartet; kein Azure-Cloud-Beweis. Der komprimierte Pfad hält einen festen 256-KiB-Datenpuffer, führt aber eine mit jeder Blockzahl wachsende `_blockIds`-Liste; die 700-kB-Unit-Methode zeigt begrenzte Einzelblöcke, nicht eine maximale Gesamtpayload-/Blockzahl. Ob eine solche Grenze geschuldet ist, bleibt ein Owner-Orakel, nicht hier disponierter Produktfehler.
4. **Offen — zusätzliche Negativfälle:** Der reale Kollisionsfall L.Collision verwendet nur `compress:true`; der Plain-Pfad hat statisch `IfNoneMatch = ETag.All` und einen Unit-Headercheck, aber keinen lokalen Überschreibversuch. U.UnsupportedEncoding prüft die Exception, nicht die Dispose-Wirkung auf den empfangenen Stream. Beides sind präzise Testlücken, keine nachgewiesenen Laufzeitfehler.

Belegstatus: **statisches Inventar und statische Zuordnung erstellt; 0 Tests ausgeführt, 0 Builds, 0 aktuelle Paket-Consumer-Compiles, 0 Mutationen, 0 reale Providerläufe. A+-Gate ausdrücklich offen.**
