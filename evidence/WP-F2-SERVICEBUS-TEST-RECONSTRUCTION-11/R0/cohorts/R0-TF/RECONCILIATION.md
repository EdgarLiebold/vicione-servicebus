# R0-TF — Reconciliation

Cohort `R0-TF`, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`, wave R0.
Baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`, tree `e5897e7632be4f491e01d51221ee59081d4d2aa0`.
Scope: `git ls-files src/ViciOne.ServiceBus.TestFramework`.

## 1. Completeness (TLP-017)

| Measure | Derivation | Value |
|---|---|---:|
| Tracked files in scope | `git ls-files src/ViciOne.ServiceBus.TestFramework \| wc -l` at the baseline commit | 147 |
| of which `.cs` | same list, `grep -c '\.cs$'` | 144 |
| Non-`.cs` in scope | `.csproj`, `.log4net.xml`, `packages.lock.json` | 3 |
| Files read completely | every path in `READ_MANIFEST.tsv`, read with `cat` / `sed -n` in this session | 147 |
| Files unread | — | 0 |
| Lines read | `git ls-files … \| xargs cat \| wc -l` | 5474 |
| `READ_MANIFEST.tsv` rows | `path<TAB>sha256`, `LC_ALL=C` sorted | 147 |

The set of files read equals `git ls-files <scope>` in both directions; the check was run
programmatically against the manifest (0 missing, 0 extra, 0 duplicates).

Cross-check against the wave manifest `../../BASELINE_TRACKED_FILE_MANIFEST.tsv` (5745 rows,
147 of them in this scope): after normalising the column order, the 147 rows are **identical
in path and SHA-256**, 0 differences. Note for the integrator: the wave manifest is written
`sha256<TAB>path`, while section 7 of the reading rules prescribes `path<TAB>sha256` for a
cohort manifest. `READ_MANIFEST.tsv` follows the rules; the byte-for-byte reconciliation
therefore needs the column swap, it is not a content difference.

## 2. Ledger shape

`LEDGER_DRAFT.jsonl` carries **163** rows:

- **147 file rows** — one per tracked file in scope, `discoveredCase` prefixed `FILE:`,
  `notes` beginning `category=…`. `obligationId` `OBL-R0-TF-0001` … `OBL-R0-TF-0147`,
  assigned in `LC_ALL=C` path order, so the manifest and the ledger reconcile line for line.
- **16 case rows** — one per inherited test case, `obligationId` `OBL-R0-TF-0148` …
  `OBL-R0-TF-0163`.

### Disposition counts over the 144 `.cs` files (plan section 10 categories)

| Plan section 10 category | Count | Ledger `disposition` |
|---|---:|---|
| domain-neutral, needed by at least two owners → new capability in `ViciOne.ServiceBus.Testing` | 27 | `PROPOSED_REPLACED_EXECUTING` |
| behaviour- or domain-specific → rebuilt in the owning test project | 101 | `PROPOSED_REPLACED_EXECUTING` |
| is itself a test case → test obligation in the owning test project | 8 files carrying 16 cases | `PROPOSED_REPLACED_EXECUTING` |
| no needed capability → removal with a technical reason | 8 | `PROPOSED_REMOVED_NO_CAPABILITY` |
| **total** | **144** | |

The 3 non-`.cs` files are all `PROPOSED_REMOVED_NO_CAPABILITY` (they disappear with the
project), so the file rows split 27 / 101 / 8 / 11.

The eight `.cs` removals are `ActivityTestContextConfigurator.cs`,
`ITestFixtureContainerFactory.cs`, `ThreadSafeRandom.cs`, `Messages/DeleteMessage.cs` and the
four files under `Logging/`. Every one carries a substantive technical reason in its ledger
`notes`; none is justified by usage count alone.

## 3. Identity reconciliation against the eleven inherited anchors

All eleven anchor files under `build/verification/expected/` were re-hashed at the baseline
commit. **Every path, SHA-256 and identity count matches the Lead plan section 9 table
exactly** — 3114 identities in total, no deviation to explain.

The TestFramework project itself contributes **no** identity to any anchor: it references
`NUnit` but no test SDK and no runner, so nothing in it is collected. Its NUnit-attributed
members reach an anchor only through fixtures derived in the referencing test projects.

| Anchor | TestFramework-derived identities | Which |
|---|---:|---|
| `core.txt` (1873) | 16 | `ViciOne.ServiceBus.Tests.ContainerTests.InMemory*FutureSpecs.*` and `InMemoryPriceCalculationFuture_*.*` — exactly the 16 inherited cases |
| `core.txt` (same file) | 3 additional | `ContainerTests.Scenarios.WhenAllCompletedOrFaulted.{Delayed_success, Error_partially_uploaded, Should_succeed}` — test methods that live in the test project but only run because `Futures/Tests/BatchFuture_Specs.cs` supplies the registration base class |
| `entity-framework-core.txt` (160) | 16 | `EntityFramework*FutureSpecs.*` and `EntityFrameworkPriceCalculationFuture_*.*` — the same 16 cases against the EF Core saga repository |
| the other nine anchors | 0 | none |

**Anchor identities not covered by a ledger row:** none in the TestFramework's own scope. The
remaining 3082 anchor identities belong to other cohorts.

**Ledger rows without an anchor identity — reported, not adopted:**

1. The `Azure.Table.Tests` derivations of the same 16 cases (`AzureTable*FutureSpecs.*`,
   `AzureTablePriceCalculationFuture_*.*`, 10 fixtures) and the `Azure.ServiceBus.Core.Tests`
   derivation (`AzureServiceBusFryFutureSpecs.Should_complete`, 1 fixture) exist in source at
   the baseline commit but appear in **no** anchor file — there is no `azure-table.txt` and no
   `azure-service-bus.txt` among the eleven. `grep -c` for the fixture-name pattern across all
   eleven anchors returns 19 (core) + 16 (EF Core) + 0 for every other file. So the same 16
   obligations are demonstrably executing on two repositories and demonstrably *not* recorded
   on two further ones. This is reported for Lead disposition; it is not adopted as truth
   either way (Q-1).
2. All 128 non-test `.cs` file rows are infrastructure or scenario material, not identities,
   so they have no anchor counterpart by construction.
3. `Futures/Tests/BatchFuture_Specs.cs` is a `[TestFixture]` with zero test methods. It
   carries no inherited case, yet three anchored `core.txt` identities depend on it.

## 4. The sixteen inherited test cases (plan section 10)

Found by reading all 144 `.cs` files; the count is exactly 16, in 8 files, in 10 fixture types.

| # | obligationId | File | Type | Method |
|---:|---|---|---|---|
| 1 | OBL-R0-TF-0148 | `ForkJoint/Tests/BurgerFuture_Specs.cs` | `BurgerFuture_Specs` | `Should_complete` |
| 2 | OBL-R0-TF-0149 | `ForkJoint/Tests/BurgerFuture_Specs.cs` | `BurgerFuture_Specs` | `Should_fault` |
| 3 | OBL-R0-TF-0150 | `ForkJoint/Tests/CalculateFuture_Specs.cs` | `CalculateFuture_Specs` | `Should_complete` |
| 4 | OBL-R0-TF-0151 | `ForkJoint/Tests/ComboFuture_Specs.cs` | `ComboFuture_Specs` | `Should_complete` |
| 5 | OBL-R0-TF-0152 | `ForkJoint/Tests/FryFuture_Specs.cs` | `FryFuture_Specs` | `Should_complete` |
| 6 | OBL-R0-TF-0153 | `ForkJoint/Tests/FryShakeFuture_Specs.cs` | `FryShakeFuture_Specs` | `Should_complete` |
| 7 | OBL-R0-TF-0154 | `ForkJoint/Tests/FryShakeFuture_Specs.cs` | `FryShakeFuture_Specs` | `Should_fault` |
| 8 | OBL-R0-TF-0155 | `ForkJoint/Tests/OrderFuture_Specs.cs` | `OrderFuture_Specs` | `Should_complete` |
| 9 | OBL-R0-TF-0156 | `ForkJoint/Tests/OrderFuture_Specs.cs` | `OrderFuture_Specs` | `Should_complete_durably` |
| 10 | OBL-R0-TF-0157 | `ForkJoint/Tests/OrderFuture_Specs.cs` | `OrderFuture_Specs` | `Should_fault_with_lettuce` |
| 11 | OBL-R0-TF-0158 | `ForkJoint/Tests/OrderFuture_Specs.cs` | `OrderFuture_Specs` | `Should_fault_with_lettuce_durably` |
| 12 | OBL-R0-TF-0159 | `ForkJoint/Tests/ShakeFuture_Specs.cs` | `ShakeFuture_Specs` | `Should_complete` |
| 13 | OBL-R0-TF-0160 | `ForkJoint/Tests/ShakeFuture_Specs.cs` | `ShakeFuture_Specs` | `Should_fault` |
| 14 | OBL-R0-TF-0161 | `Futures/Tests/PriceCalculationFuture_Specs.cs` | `PriceCalculationFuture_Specs` | `Should_complete` |
| 15 | OBL-R0-TF-0162 | `Futures/Tests/PriceCalculationFuture_Specs.cs` | `PriceCalculationFuture_RegistrationSpecs` | `Should_complete` |
| 16 | OBL-R0-TF-0163 | `Futures/Tests/PriceCalculationFuture_Specs.cs` | `PriceCalculationFuture_Faulted` | `Should_faulted` |

Full obligation rows — contract, resolved variant list, boundaries, negative paths, old
execution state, assertion intent, executing identities — are in `LEDGER_DRAFT.jsonl`.

Ten of the sixteen are `Should_complete`. Because every fixture is derived per saga
repository, the sixteen cases resolve to 3 or 4 variants each: InMemory, EF Core, Azure Table
and — for `FryFuture_Specs` only — Azure Service Bus with `MessageSessionRepository` and
`RequiresSession` endpoints.

## 5. Per-file disposition index

`category` is the plan section 10 decision; `target` is the proposed target project.

| obligationId | File (relative to `src/ViciOne.ServiceBus.TestFramework/`) | category | target |
|---|---|---|---|
| OBL-R0-TF-0001 | `ActivityTestContext.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0002 | `ActivityTestContextConfigurator.cs` | REMOVE | - |
| OBL-R0-TF-0003 | `AsyncTestFixture.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing + ViciOne.ServiceBus.Testing.Xunit |
| OBL-R0-TF-0004 | `BusTestFixture.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing + ViciOne.ServiceBus.Testing.Xunit |
| OBL-R0-TF-0005 | `Courier/AddressActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0006 | `Courier/AddressArguments.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0007 | `Courier/AddressLog.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0008 | `Courier/FaultyActivity.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0009 | `Courier/FaultyArguments.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0010 | `Courier/FaultyCompensateActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0011 | `Courier/FaultyLog.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0012 | `Courier/FirstFaultyActivity.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0013 | `Courier/FirstFaultyCompensateActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0014 | `Courier/NastyFaultyActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0015 | `Courier/ObjectGraphActivityArguments.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0016 | `Courier/ObjectGraphTestActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0017 | `Courier/OuterObject.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0018 | `Courier/OuterObjectImpl.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0019 | `Courier/ReviseItineraryActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0020 | `Courier/ReviseToEmptyItineraryActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0021 | `Courier/ReviseWithNoChangeItineraryActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0022 | `Courier/SecondTestActivity.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0023 | `Courier/SetLargeVariableActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0024 | `Courier/SetLargeVariableArguments.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0025 | `Courier/SetVariableActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0026 | `Courier/SetVariableArguments.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0027 | `Courier/SetVariablesFaultyActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0028 | `Courier/SetVariablesFaultyArguments.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0029 | `Courier/TestActivity.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0030 | `Courier/TestArguments.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0031 | `Courier/TestLog.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0032 | `ForkJoint/Activities/DressBurgerActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0033 | `ForkJoint/Activities/DressBurgerArguments.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0034 | `ForkJoint/Activities/GrillBurgerActivity.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0035 | `ForkJoint/Activities/GrillBurgerArguments.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0036 | `ForkJoint/Activities/GrillBurgerLog.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0037 | `ForkJoint/Consumers/CookFryConsumer.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0038 | `ForkJoint/Consumers/CookFryConsumerDefinition.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0039 | `ForkJoint/Consumers/CookOnionRingsConsumer.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0040 | `ForkJoint/Consumers/PourShakeConsumer.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0041 | `ForkJoint/Contracts/Burger.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0042 | `ForkJoint/Contracts/BurgerCompleted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0043 | `ForkJoint/Contracts/BurgerPatty.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0044 | `ForkJoint/Contracts/CalculateCompleted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0045 | `ForkJoint/Contracts/ComboCompleted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0046 | `ForkJoint/Contracts/CookFry.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0047 | `ForkJoint/Contracts/CookOnionRings.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0048 | `ForkJoint/Contracts/Fry.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0049 | `ForkJoint/Contracts/FryCompleted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0050 | `ForkJoint/Contracts/FryReady.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0051 | `ForkJoint/Contracts/FryShake.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0052 | `ForkJoint/Contracts/FryShakeCompleted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0053 | `ForkJoint/Contracts/FryShakeFaulted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0054 | `ForkJoint/Contracts/FryShakeReady.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0055 | `ForkJoint/Contracts/FutureCompleted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0056 | `ForkJoint/Contracts/FutureFaulted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0057 | `ForkJoint/Contracts/OnionRingsCompleted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0058 | `ForkJoint/Contracts/OnionRingsReady.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0059 | `ForkJoint/Contracts/OrderBurger.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0060 | `ForkJoint/Contracts/OrderCalculate.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0061 | `ForkJoint/Contracts/OrderCombo.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0062 | `ForkJoint/Contracts/OrderCompleted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0063 | `ForkJoint/Contracts/OrderFaulted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0064 | `ForkJoint/Contracts/OrderFry.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0065 | `ForkJoint/Contracts/OrderFryShake.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0066 | `ForkJoint/Contracts/OrderLine.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0067 | `ForkJoint/Contracts/OrderLineCompleted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0068 | `ForkJoint/Contracts/OrderLineFaulted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0069 | `ForkJoint/Contracts/OrderOnionRings.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0070 | `ForkJoint/Contracts/OrderShake.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0071 | `ForkJoint/Contracts/PourShake.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0072 | `ForkJoint/Contracts/Shake.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0073 | `ForkJoint/Contracts/ShakeCompleted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0074 | `ForkJoint/Contracts/ShakeReady.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0075 | `ForkJoint/Contracts/Size.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0076 | `ForkJoint/Contracts/SubmitOrder.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0077 | `ForkJoint/Futures/BurgerFuture.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0078 | `ForkJoint/Futures/CalculateFuture.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0079 | `ForkJoint/Futures/ComboFuture.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0080 | `ForkJoint/Futures/FryFuture.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0081 | `ForkJoint/Futures/FryShakeFuture.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0082 | `ForkJoint/Futures/OnionRingsFuture.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0083 | `ForkJoint/Futures/OrderFuture.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0084 | `ForkJoint/Futures/ShakeFuture.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0085 | `ForkJoint/ItineraryPlanners/BurgerItineraryPlanner.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0086 | `ForkJoint/Services/Fryer.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0087 | `ForkJoint/Services/Grill.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0088 | `ForkJoint/Services/IFryer.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0089 | `ForkJoint/Services/IGrill.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0090 | `ForkJoint/Services/IShakeMachine.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0091 | `ForkJoint/Services/ShakeMachine.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0092 | `ForkJoint/Tests/BurgerFuture_Specs.cs` | TEST_OBLIGATION | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests |
| OBL-R0-TF-0093 | `ForkJoint/Tests/CalculateFuture_Specs.cs` | TEST_OBLIGATION | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests |
| OBL-R0-TF-0094 | `ForkJoint/Tests/ComboFuture_Specs.cs` | TEST_OBLIGATION | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests |
| OBL-R0-TF-0095 | `ForkJoint/Tests/FryFuture_Specs.cs` | TEST_OBLIGATION | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0096 | `ForkJoint/Tests/FryShakeFuture_Specs.cs` | TEST_OBLIGATION | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests |
| OBL-R0-TF-0097 | `ForkJoint/Tests/OrderFuture_Specs.cs` | TEST_OBLIGATION | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests |
| OBL-R0-TF-0098 | `ForkJoint/Tests/ShakeFuture_Specs.cs` | TEST_OBLIGATION | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests |
| OBL-R0-TF-0099 | `FutureTestFixture.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing + ViciOne.ServiceBus.Testing.Xunit |
| OBL-R0-TF-0100 | `Futures/BatchCompleted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0101 | `Futures/BatchFaulted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0102 | `Futures/BatchFuture.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0103 | `Futures/BatchRequest.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0104 | `Futures/CalculatePrice.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0105 | `Futures/CalculatePriceConsumer.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0106 | `Futures/PriceCalculation.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0107 | `Futures/PriceCalculationFuture.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests + ViciOne.ServiceBus.Azure.ServiceBus.Core.ExternalTests |
| OBL-R0-TF-0108 | `Futures/ProcessBatchItem.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0109 | `Futures/ProcessBatchItemCompleted.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0110 | `Futures/ProcessBatchItemConsumer.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0111 | `Futures/Tests/BatchFuture_Specs.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0112 | `Futures/Tests/PriceCalculationFuture_Specs.cs` | TEST_OBLIGATION | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.EntityFrameworkCoreIntegration.IntegrationTests + ViciOne.ServiceBus.Azure.Table.ExternalTests |
| OBL-R0-TF-0113 | `HealthCheckServiceExtensions.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0114 | `IFutureTestFixtureConfigurator.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0115 | `ITestFixtureContainerFactory.cs` | REMOVE | - |
| OBL-R0-TF-0116 | `InMemoryActivityTestFixture.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0117 | `InMemoryContainerTestFixture.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0118 | `InMemoryTestFixture.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing + ViciOne.ServiceBus.Testing.Xunit |
| OBL-R0-TF-0119 | `IntentionalTestException.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0120 | `IntrospectionExtensions.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0121 | `Logging/DiagnosticListenerObserver.cs` | REMOVE | - |
| OBL-R0-TF-0122 | `Logging/TestOutputListenerObserver.cs` | REMOVE | - |
| OBL-R0-TF-0123 | `Logging/TestOutputLogger.cs` | REMOVE | - |
| OBL-R0-TF-0124 | `Logging/TestOutputLoggerFactory.cs` | REMOVE | - |
| OBL-R0-TF-0125 | `MediatorTestFixture.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0126 | `Messages/DeleteMessage.cs` | REMOVE | - |
| OBL-R0-TF-0127 | `Messages/IMessageA.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0128 | `Messages/MessageA.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0129 | `Messages/MessageB.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0130 | `Messages/PingMessage.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0131 | `Messages/PingNotSupported.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0132 | `Messages/PongMessage.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0133 | `Sagas/ChoirTest.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests + ViciOne.ServiceBus.DynamoDbIntegration.IntegrationTests |
| OBL-R0-TF-0134 | `Sagas/PublishTestStartedActivity.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0135 | `Sagas/StartStateMachineTest.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0136 | `Sagas/TestInstance.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0137 | `Sagas/TestStarted.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0138 | `Sagas/TestStateMachineSaga.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0139 | `Sagas/TestUpdated.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0140 | `Sagas/UpdateTest.cs` | SHARED_CAPABILITY | ViciOne.ServiceBus.Testing |
| OBL-R0-TF-0141 | `TestConsumeContext.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0142 | `TestStateMachineExtensions.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0143 | `TestSymmetricKeyProvider.cs` | OWNER_PROJECT | ViciOne.ServiceBus.Tests |
| OBL-R0-TF-0144 | `ThreadSafeRandom.cs` | REMOVE | - |
| OBL-R0-TF-0145 | `ViciOne.ServiceBus.TestFramework.csproj` | REMOVE | - |
| OBL-R0-TF-0146 | `ViciOne.ServiceBus.TestFramework.log4net.xml` | REMOVE | - |
| OBL-R0-TF-0147 | `packages.lock.json` | REMOVE | - |

## 6. Open questions for the Lead

Each is a genuine decision, not a missing measurement. None is used as a reason to drop an
obligation; every affected row stays `PROPOSED_*` until the Lead disposes it.

**Q-1 — Two of the four future owners have no anchor at all.**
Cause: the eleven anchors cover core, EF Core, ActiveMQ, RabbitMQ, SQL transport, Quartz,
Abstractions, Analyzer, Benchmarks, Diagnostics and SignalR. The `Azure.Table.Tests` and
`Azure.ServiceBus.Core.Tests` derivations of the same 16 cases are therefore unrecorded.
Effect: if the rebuild reconciles only against the anchors, 11 executing fixtures silently
disappear and the reconciliation still reads green.
Alternatives: (a) treat the two projects as in-scope owners and carry all 16 obligations to
them without an anchor to compare against — full semantic coverage, but the identity count
cannot be checked against a frozen baseline; (b) treat them as out of scope for R0 and record
the 11 fixtures as a separate deferred obligation set — checkable, but the Azure Table and
Azure Service Bus future behaviour is unproven until the deferred set is worked.
Recommendation (a), because plan section 9 states explicitly that no missing old test method
frees an observable product behaviour from a disposition, and session-based futures on Azure
Service Bus are behaviour the plan's own owner table requires.

**Q-2 — Placement of the fast-food scenario material.**
Cause: plan section 10 forbids the fast-food domain from becoming shared infrastructure, while
plan section 5 admits a shared helper as soon as two owners need the same domain-neutral
capability. The 67 ForkJoint files serve four owners but are a sample domain.
Effect: taken literally, each of the three or four owners rebuilds its own copy of the burger
scenario — roughly 67 files times three.
Alternatives: (a) rebuild a small neutral future scenario per owner (no shared domain, some
duplication of a few small contracts); (b) build one neutral scenario in a test-only shared
place and let all owners derive it (one copy, but a scenario in shared infrastructure);
(c) move the domain to `samples/**` under the separate sample work package and have the tests
reference the sample.
Recommendation (a): the obligations are about the *Futures product feature* under different
saga repositories, not about the domain. A minimal neutral scenario per owner keeps the
domain out of `ViciOne.ServiceBus.Testing` as section 10 requires, and the duplicated surface
is a handful of small contracts, not the current 67 files. R0 has provisionally written the
four owning projects into `targetProject` for those rows.

**Q-3 — The choir concurrency saga has exactly two owners.**
Cause: `Sagas/ChoirTest.cs` is a sample domain (voices, harmony) but carries a real product
obligation — concurrent event delivery against one instance, `CompositeEvent`, `ISagaVersion`
optimistic concurrency — and is used by `ViciOne.ServiceBus.Tests` and
`DynamoDbIntegration.Tests`.
Effect: it sits exactly on the boundary between "two owners → shared" (section 5) and
"domain-specific → not shared" (section 10).
Alternatives: (a) rebuild it per owner under neutral naming; (b) make it a shared neutral
"four concurrent events plus composite event plus version" scenario in
`ViciOne.ServiceBus.Testing`.
Recommendation (a), consistent with Q-2. R0 has written both owning projects into the row.

**Q-4 — R0 has no terminal disposition token for an infrastructure file.**
Cause: the four terminal dispositions in the reading rules and plan section 9 all describe
*test obligations*. 128 of the 147 rows describe infrastructure or scenario material that
carries no obligation of its own.
Effect: `PROPOSED_REPLACED_EXECUTING` for a helper file is only meaningful transitively —
"the capability is re-provided and the tests that use it are green" — and
`PROPOSED_REMOVED_NO_CAPABILITY` is a token R0 invented because none of the four fits.
Alternatives: (a) accept the two proposal tokens as written and let the integrator bind each
helper row to the named tests that prove it; (b) add an explicit non-obligation row kind to
the ledger contract.
Recommendation (a) plus an explicit note in the wave binding that a helper row terminates only
when at least one named test that exercises it is green — otherwise a removed helper could be
declared terminal with nothing proving the capability was really unnecessary.

**Q-5 — `Futures/Tests/BatchFuture_Specs.cs` is a test fixture with no test.**
Cause: it exists only to give `WhenAllCompletedOrFaulted` (in `ViciOne.ServiceBus.Tests`) a
registration base class; the three test methods live downstream and are anchored in
`core.txt`.
Effect: it counts as neither a helper nor a case, and the `[TestFixture]` attribute on a
fixture with no test method is misleading.
Alternatives: (a) fold the registration into the owning test class and delete the base class;
(b) keep a base class for a future second owner.
Recommendation (a) — there is exactly one owner, and section 5 admits a shared helper only at
two.
