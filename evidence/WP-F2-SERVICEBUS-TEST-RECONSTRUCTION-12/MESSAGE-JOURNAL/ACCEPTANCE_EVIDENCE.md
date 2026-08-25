# MessageJournal acceptance evidence

## Frozen technical subject

- Commit: `c0753c84f32ac0e54e881aad0691b44dfa8ac57e`
- Tree: `6b9ca43eeaf4b0ae55e10758f5faf4db27fdfbdf`
- Branch: `test/servicebus-xunit4-mtp2-a-plus-v2`
- Remote: `origin/test/servicebus-xunit4-mtp2-a-plus-v2`

The technical commit replaces the inherited message-audit surface with the intentionally
incompatible, optional and default-off `MessageJournal`. The Suite audit owner remains outside this
capability. EF Core and Azure Table receive only policy-sanitized immutable entries and apply finite
size, count and append-driven age limits without a queue, retry carrier or background worker.

## Stationary acceptance runs

All commands ran from the repository root with the pinned .NET 10 SDK and locked restored graph.

| Subject | Command | Result |
|---|---|---|
| Complete engineering graph | `dotnet build ViciOne.ServiceBus.Engineering.slnx -c Release --no-restore --disable-build-servers --no-incremental -v:minimal` | exit 0; 0 warnings; 0 errors |
| Unit and architecture profile | `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore --results-directory artifacts/test-results/unit --minimum-expected-tests 1557 --max-parallel-test-modules 1` | exit 0; 1557 passed; 0 failed; 0 skipped |
| Local integration profile | `VICIONE_TESTS__Profile=LocalIntegration python3 tools/ci/run_broker_category.py --broker postgres --broker azurite --command -- dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx -c Release --no-build --no-restore --results-directory artifacts/test-results/local-integration --minimum-expected-tests 17 --max-parallel-test-modules 1` | exit 0; 17 passed; 0 failed; 0 skipped; ephemeral PostgreSQL and pinned Azurite cleaned |
| Apache-2.0 section 4(b) record | `python3 tools/identity/change_list.py` | exit 0; generated evidence matches; 7599 entries |

Real SQL Server, Azure SQL and Azure Table service validation remains explicitly bounded in
`TODO.md`; emulator evidence is not represented as cloud-service evidence.

## One-cause negative checks

Each mutation was applied alone, explicitly rebuilt, executed through xUnit 4/MTP 2, observed red
for the stated reason, and then removed with the source restored to the baseline hash below. The
mutant hash is the SHA-256 of the complete mutated source file.

| ID | Source and baseline SHA-256 | Single mutation and mutant SHA-256 | Focused result |
|---|---|---|---|
| MJ-M01 | `src/ViciOne.ServiceBus/Configuration/MessageJournalConfigurationExtensions.cs`, `c5b54cc537f5e2015ba4080aba6487b3d166fc4818171ac9954d968856ae0863` | stop rollback before handle index 0; `e49a35b357bc8145d678f95364c182e330e71091b8196337ee2eaada3e047e6c` | exit 2; expected disconnect order `[publish, send]`, actual `[publish]` |
| MJ-M02 | `src/ViciOne.ServiceBus/MessageJournal/MessageJournalProjection.cs`, `fd5c53b305cde44f40c5ae2a1d6a2dcbe2a5d022d0d070615211e3450e24ffb3` | return the owned body array instead of a detached copy; `c14821a1314f9146bdba8e258500d2d72338b877a397889b79cece3710fc8e7d` | exit 2; caller mutation changed the stored byte from 1 to 9 |
| MJ-M03 | `src/ViciOne.ServiceBus/MessageJournal/MessageJournalEntry.cs`, `ba67da65249aa635583096003edebbe63d768bcb508fd73bef82ad3cdd5588a5` | count raw UTF-8 rather than JSON-escaped UTF-8; `b9cdb17876ed5ff26e0952dfdb2c11c6333e58ba6efe287a6f86831168172671` | exit 2; oversized escaped content reached the store once instead of zero times |
| MJ-M04 | `src/Persistence/ViciOne.ServiceBus.Azure.Table/AzureTable/MessageJournal/AzureTableMessageJournalStore.cs`, `9954ad2b44af0a701886fe92be3d5450b50da7e1739c2a186f2d51d3b635fa70` | remove the journal row-key range from the partition query; `84155eec0fe9b6af5ec21c834afb5beb85d8db0a9c450b6e0529f77a7ca96aaa` | exit 2 against Azurite; the capacity control row entered the deletion batch and caused `InvalidDuplicateRow` |

The final tree contains none of the mutant hashes. `git diff --check`, all requirement-projection
tests, the no-skip floors, source/test path mirroring and the generated ChangeList gate pass on the
technical subject.
