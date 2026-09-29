# T153 — Azure Table runtime saga registration and custom keys

The historical Azure Table review identified two public-path risks: automatic saga registration might omit the Azure Table repository provider, and the public repository factory might ignore its caller-supplied key formatter. Existing coverage proved only direct registration and the default key layout.

Commit `80ffead2f` adds `RuntimeProvider_PersistsWithCustomKeysAndPublicRepositoryReloadsAsync`. It registers a real saga through `UseAzureTableForRegisteredSagas`, sends an initiating message, observes its committed publication, reads the physical Azure Table row at `(correlationId, state)`, and reloads the state through `AzureTableSagaRepository.Create(factory, formatter)`. A missing runtime provider cannot produce that row. A factory that substitutes the default fixed-partition formatter cannot reload it. The test asserts the saga identity, value, revision and stage, not only the presence of a row.

The focused saga integration class passed 3/3 and the complete Azure Table LocalIntegration project passed 45/45 against Azurite. Independent read-only Red Team review found no concrete P1/P2 and confirmed that the physical row and reload are separate behavioral oracles for the two risks. No product source changed; these were unverified risks, not confirmed defects.

The exact-commit receipt `artifacts/t153-azure-table-runtime-keys/receipt.json` (SHA-256 `34498d56e562a40fc0b44e37e7a4a584f3aee305a8509f638291e5708caec5af`) verifies 45/45 tests, 14 unchanged binaries and 1,928 tracked product sources at `80ffead2f`. It does not establish a new product-wide Coverage/CRAP result. Azure production account authorization, throttling and regional recovery remain outside this local acceptance test.
