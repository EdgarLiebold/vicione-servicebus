# R0-PER — Reconciliation

Cohort `R0-PER`, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`.
Baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`, tree `e5897e7632be4f491e01d51221ee59081d4d2aa0`.
`READ_MANIFEST.tsv` sha256 `e5628f5dcbdfb20f4c62f4c2f9e26cdfbfa194d537b4e037beb9c5f404ce0df3`.

## 1. Completeness of reading (TLP-017)

Scope was taken as `git ls-files` over the ten assigned directories. Every tracked file was read; none
was sampled and none is unread.

| Scope | tracked files | of these `.cs` | Lead's number |
|---|---:|---:|---:|
| `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests` | 61 | 58 | 58 |
| `tests/Persistence/ViciOne.ServiceBus.Azure.Table.Tests` | 25 | 22 | 22 |
| `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests` | 7 | 5 | 5 |
| `tests/Scheduling/ViciOne.ServiceBus.QuartzIntegration.Tests` | 28 | 26 | 26 |
| `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration` | 75 | 72 | 72 |
| `src/Persistence/ViciOne.ServiceBus.Azure.Table` | 33 | 30 | 30 |
| `src/Persistence/ViciOne.ServiceBus.DynamoDbIntegration` | 15 | 12 | 12 |
| `src/Scheduling/ViciOne.ServiceBus.QuartzIntegration` | 23 | 20 | 20 |
| `src/Persistence/ViciOne.ServiceBus.AmazonS3` | 6 | 3 | 3 |
| `src/Persistence/ViciOne.ServiceBus.Azure.Storage` | 8 | 5 | 5 |
| **total** | **281** | **253** | **253** |

Derivation of the difference: the Lead's per-project numbers count `.cs` files only. The remaining 28
files are the `.csproj`, `packages.lock.json`, `.csproj.DotSettings` and two `docker-compose.yml` files.
They were read as well, because the framework contract, the pinned images and the credential literals
live in them. No discrepancy remains.

All 281 rows of `READ_MANIFEST.tsv` were compared path-by-path and hash-by-hash against
`evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0/BASELINE_TRACKED_FILE_MANIFEST.tsv`
(5745 rows): **0 paths missing from the baseline, 0 hash mismatches**. Note the field order differs by
design — the rules file specifies `path<TAB>sha256` for the cohort manifest while the baseline manifest
is written `sha256<TAB>path`; the integrator has to transpose one of them before a byte-level diff.

## 2. Ledger shape

269 rows, ids `OBL-R0-PER-0001` … `OBL-R0-PER-0657`, all unique, all 19 mandatory fields present in
the prescribed order. `targetTests`, `evidenceRun` and `reviewer` are empty as required in R0; every
`disposition` is `PROPOSED_REPLACED_EXECUTING`. Numbering is blocked, not dense, so that a later
insertion inside a block does not renumber a neighbouring project:

| block | content | rows |
|---|---|---:|
| 0001–0092 | EF Core, inherited | 92 |
| 0100–0118 | EF Core, gap from product | 19 |
| 0200–0286 | Quartz, inherited | 87 |
| 0300–0307 | Quartz, gap from product | 8 |
| 0400–0430 | Azure.Table, inherited | 31 |
| 0450–0457 | Azure.Table, gap from product | 8 |
| 0500–0503 | DynamoDb, inherited | 4 |
| 0520–0525 | DynamoDb, gap from product | 6 |
| 0600–0605 | AmazonS3, gap only (no inherited tests) | 6 |
| 0650–0657 | Azure.Storage, gap only (no inherited tests) | 8 |

## 3. Anchor reconciliation — EF Core

Anchor `build/verification/expected/entity-framework-core.txt`, sha256
`9880e54935fbf468a5e12543c8ef709aa61403d4654bb0ed83ca86f1cc65fdd0` — matches the plan §9 byte for byte.
163 lines minus 3 documenting header lines = **160 identities**, matching the plan and matching
`minimumExecutedCases: 160` in `build/verification/VERIFICATION_MODEL.json`.

The 92 inherited ledger rows were expanded mechanically into identities by substituting each entry of
their `variants` list into the generic parameter of the symbol. Result:

- ledger identities: **160**
- anchor identities: **160**
- anchor identities without a ledger row: **0**
- ledger identities without an anchor identity: **0**

**No deviation in either direction.** The expansion is reproducible: 76 declared `[Test]` methods in the
project resolve to 144 identities through the `[TestFixture(typeof(...))]` parameterisation, and the
remaining 16 come from ten `Future_Specs` subclasses whose case bodies are inherited from
`src/ViciOne.ServiceBus.TestFramework` (4 for `EntityFrameworkOrderFutureSpecs`, 2 each for the Shake,
FryShake and Burger fixtures, 1 each for the remaining six). 144 + 16 = 160.

Parameterisation as measured, not assumed:

| fixture group | variants declared |
|---|---|
| AuditStore, ContainerTests, SagaWithDependency, SimpleSaga, SlowConcurrentSaga, all four TransactionConfiguration fixtures | SqlServer, SqlServerResiliency, Postgres (3) |
| JobConsumer, ReadOnlyTests, all four Turnout fixtures | SqlServer, Postgres (2) |
| BusOutboxDeliveryContext, all ReliableMessaging fixtures, TransactionalBusOutbox, Configurator_TransactionConfiguration, all Future fixtures | none (1) |

Two asymmetries are reported, not adopted: `ReadOnlyTests` and the Turnout fixtures omit the
`SqlServerResiliency` variant that their neighbours declare, and the whole `Future_Specs` group is
SQL-Server-only because `FutureSagaDbContextFactory` and the futures configurator hard-code
`UseSqlServer` and `SqlServerLockStatementProvider`. The EF futures obligation therefore has no
PostgreSQL coverage at all.

## 4. Anchor reconciliation — Quartz

Anchor `build/verification/expected/quartz.txt`, sha256
`839e125ffda20d96c8a8572ba3e051d7173ddb2b008019aedc3283aa953d87f5` — matches the plan §9 byte for byte.
92 lines minus 3 header lines = **89 identities**, matching the plan and `minimumExecutedCases: 89`.

- ledger identities: **89**
- anchor identities: **89**
- anchor identities without a ledger row: **0**
- ledger identities without an anchor identity: **0**

**No deviation in either direction.** 87 declared `[Test]` methods resolve to 89 identities: only
`Container_Specs.Using_quartz_with_serializer<T>` is parameterised, with `<Json>` and `<RawJson>`,
which adds 2.

Three anchor identities carry a file-name segment the others do not — `Request_Specs.…`,
`Reschedule_Specs.…`, `ScheduleTimeout_Specs.…` — because those three fixtures are declared inside a
nested namespace of the same name. This is a naming artefact of the source, reported here so the
integrator does not read it as a missing type.

## 5. Anchor reconciliation — Azure.Table and DynamoDb

The task asked which anchor covers these two projects. Measured answer: **none of the eleven**.

`grep` over all eleven files in `build/verification/expected/` returns 0 matches for `Azure.Table`, 0
for `DynamoDb`, 0 for `AmazonS3` and 0 for `Azure.Storage`. The reason is recorded in
`build/verification/VERIFICATION_MODEL.json`:

| capability | class | testProjects | runs |
|---|---|---|---|
| `persistence-azure-table` | `REAL_EPHEMERAL_CLOUD` | `tests/Persistence/ViciOne.ServiceBus.Azure.Table.Tests` | `[]` |
| `persistence-dynamodb` | `REAL_EPHEMERAL_CLOUD` | `tests/Persistence/ViciOne.ServiceBus.DynamoDbIntegration.Tests` | `[]` |
| `message-data-amazon-s3` | `REAL_EPHEMERAL_CLOUD` | `[]` | `[]` |
| `message-data-azure-storage` | `REAL_EPHEMERAL_CLOUD` | `[]` | `[]` |

So Azure.Table and DynamoDb have inherited *source* but no inherited *evidence*: 31 and 4 obligations
respectively exist as code that compiles and has never been recorded as passing or failing. Their
ledger rows carry that fact in `oldExecutionState` verbatim; per §9 of the plan the absence of an
execution record is never on its own a deletion ground.

This also means the total inherited identity count of this cohort against the anchors is 160 + 89 =
**249**, while the cohort's inherited obligation count is 249 + 35 = **284 identities across 214 rows**.
The 35 unanchored identities are new to the anchor set and must not be silently folded into it.

## 6. The AmazonS3 / Azure.Storage claim, verified

The Lead states these two projects have no inherited tests at all. Verified from the repository, and the
verification found one nuance worth reporting:

- No test project exists for either. The verification model lists `testProjects: []` for both.
- No anchor file contains one of their identities.
- A repository-wide grep for their type names and namespaces (`AmazonS3MessageDataRepository`,
  `AzureStorageMessageDataRepository`, `ViciOne.ServiceBus.AmazonS3`, `ViciOne.ServiceBus.AzureStorage`)
  returns **no hit in any `.cs` file outside the two projects themselves**.
- **Nuance:** two `ProjectReference` entries do exist —
  `tests/Transports/ViciOne.ServiceBus.AmazonSqsTransport.Tests` references AmazonS3 and
  `tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests` references Azure.Storage. They only
  link the assemblies; no code in those suites touches them. The verification model already records the
  same conclusion in its rationale: *"It was linked to the transport capability of the same provider,
  which proved nothing: that capability has no run either, and sharing a test project is not sharing a
  proof."*

The claim therefore holds. Their entire test scope is new work: 6 + 8 = 14 gap obligations derived from
the observable product surface (3 and 5 source files, read completely).

## 7. Environment classification

Every one of the 269 rows carries a profile.

| profile | rows | share |
|---|---:|---:|
| `UnitArchitecture` | 130 | 48 % |
| `LocalIntegration` | 132 | 49 % |
| `External` | 7 | 3 % |

Per project:

| project | UnitArchitecture | LocalIntegration | External |
|---|---:|---:|---:|
| EF Core (inherited + gap) | 28 | 83 | 0 |
| Quartz (inherited + gap) | 94 | 1 | 0 |
| Azure.Table (inherited + gap) | 3 | 33 | 3 |
| DynamoDb (inherited + gap) | 1 | 7 | 2 |
| AmazonS3 (gap only) | 2 | 3 | 1 |
| Azure.Storage (gap only) | 2 | 5 | 1 |

Rules applied:

- **EF Core** has no `External` row. PostgreSQL and SQL Server are real engines in short-lived
  containers; nothing in this product needs a cloud service. The 28 hermetic rows are the SQL-text
  formatters, the model shape, the configurator surface and `BusOutboxDeliveryContext_Specs`.
- **Quartz** is hermetic except for one row: `OBL-R0-PER-0306`, the persistent AdoJobStore, which needs
  a relational container. Everything else uses the in-memory scheduler.
- **Azure.Table** runs on Azurite (`UseDevelopmentStorage=true` → `127.0.0.1:10002`), which covers the
  whole Table surface the inherited cases use: create table, insert, replace and delete with `If-Match`
  ETag, point query, partition scan and batch transaction.
- **DynamoDb** runs on LocalStack (`http://localhost:4566`), which implements `ConditionExpression` on
  `PutItem`/`UpdateItem` — the exact mechanism this product uses for optimistic concurrency.

### 7.1 Every `External` row and why an emulator is not sufficient

This is the Azure/AWS capability matrix the Lead asked for. Each entry states the missing emulator
capability, not just "needs cloud".

| id | obligation | why the emulator cannot prove it |
|---|---|---|
| `OBL-R0-PER-0455` | `CosmosTableSagaRepositoryContext<TSaga>` — a shipped Cosmos-DB-for-Table load path with no test | Azurite emulates the **Azure Table Storage** service only. The Cosmos DB Table API is a different service with its own property-type support, its own consistency levels and 429/retry-after throttling responses; no supported Linux CI container reproduces it. §5 of the plan already lists Cosmos for Table separately in the matrix. |
| `OBL-R0-PER-0456` | Entra ID / managed-identity authentication against the Table endpoint | Azurite authenticates the Table service with **shared key and SAS only**; it exposes no OAuth token endpoint, so a `TokenCredential` path cannot be exercised at all. §7 of the plan mandates OIDC/Workload Identity, i.e. exactly this path. |
| `OBL-R0-PER-0457` | Azure Table service limits (1 MB entity, 255 properties, 64 KB per string property, 100 ops / 4 MB per batch) | Azurite does **not enforce** these limits. A local run is green precisely where production fails, so the limit is unobservable locally and the maximum supported saga size cannot be established. |
| `OBL-R0-PER-0523` | DynamoDB TTL actually deleting an expired saga item | TTL expiry is an asynchronous background process of the real service with a documented delay of up to 48 h and no client trigger. DynamoDB Local and LocalStack store the attribute but never run a sweeper. **Even against real AWS this is not provable inside a test run** — the honest disposition is a documented limitation plus `OBL-R0-PER-0521` (the written value is correct). |
| `OBL-R0-PER-0524` | Provisioned-throughput throttling (`ProvisionedThroughputExceededException`) | DynamoDB Local ignores provisioned capacity entirely and never throttles; LocalStack does not model the capacity system either. Only a real provisioned table produces the exception the retry policy must distinguish from a version conflict. |
| `OBL-R0-PER-0603` | S3 lifecycle expiration actually deleting objects | Lifecycle expiration is an asynchronous S3 background process with one-day granularity and up to 48 h delay; LocalStack accepts and stores the configuration but never runs an expiry sweeper. **Also not provable against real AWS inside a run** — same honest disposition as `OBL-R0-PER-0523`. |
| `OBL-R0-PER-0656` | Azure Blob `ClientSecretCredential` (Entra ID) constructor overload | Azurite authenticates the Blob service with the well-known shared key and with SAS; it rejects Bearer authentication and exposes no OAuth endpoint. §7 makes OIDC/Workload Identity the mandated credential path, so this overload is the one production will use. The sibling shared-key and SAS overloads **are** locally provable and are classified `LocalIntegration` (`OBL-R0-PER-0657`). |

Two of these seven — `0523` and `0603` — are not merely External but **not provable in any profile**.
They are flagged for the Lead as candidates for a documented limitation rather than a wave-C4b case.
The remaining five wait for wave C4b.

## 8. Open questions for the Lead

- **Q-PER-01** — `Container_Specs.cs:190` carries the inline comment *"TODO figure out why Postgres
  locking isn't working :("* and compensates by enabling `UseInMemoryOutbox` only for the Npgsql
  variant. The PostgreSQL pessimistic row-lock path is therefore not proven by the case that claims to
  prove it. Is this a known product defect, and should the rebuild prove the `FOR UPDATE` path or
  record the deficiency explicitly?
- **Q-PER-02** — The EF `SlowConcurrentSaga_Specs` expects `Counter == 2` while the Azure.Table twin of
  the same scenario expects `Counter == 3`. The two suites encode different semantics under the same
  name. Which is the intended contract?
- **Q-PER-03** — 32 inherited identities in this cohort (16 EF Core futures, 16 Azure.Table futures)
  have their case bodies in `src/ViciOne.ServiceBus.TestFramework`, which is outside this cohort's
  scope. Their `assertionIntent` is recorded as *not read here*. The TestFramework cohort must supply it
  before those 32 rows can become terminal; this is a cross-cohort dependency, not a gap.
- **Q-PER-04** — `SqlLockStatementProvider.TableNames` is a `protected static` cache keyed by entity
  type alone and shared process-wide, while the XML docs of `UseSqlServer(bool enableSchemaCaching)`
  say *"Set to false when using multiple DbContexts"*. Is the cross-context contamination a known and
  accepted limitation with an unsafe default (`enableSchemaCaching = true`)?
- **Q-PER-05** — `SetOptimisticConcurrency(false)` stores a transaction flag that
  `CreatePessimisticLockStrategy` never reads, and `PessimisticSagaRepositoryLockStrategy.IsTransactionEnabled`
  is a hard-coded `true`. Is silently ignoring the flag intended, or should the combination be rejected?
- **Q-PER-06** — `QuartzTimeAdjustment` is a test-only clock manipulator that mutates the process-global
  Quartz `SystemTime` and ships inside the product package
  `src/Scheduling/ViciOne.ServiceBus.QuartzIntegration`. Moving it into the test-support project is a
  public API change; R0 records the finding and does not decide it.
- **Q-PER-07** — `JobDetail_Specs.Should_return_the_properties_with_custom_factory` has a body
  byte-identical to its neighbour and installs no custom job factory. Should the rebuild honour the
  name (build the `ViciOneServiceBusJobFactory` obligation) or drop the duplicate identity? Dropping it
  would reduce the Quartz identity count by one against the anchor.
- **Q-PER-08** — `AzureTableSagaRepositoryContext.Insert` catches `Exception`, logs, and returns
  `default`, making a hard failure indistinguishable from a benign duplicate-key conflict. Is the
  swallow intended?
