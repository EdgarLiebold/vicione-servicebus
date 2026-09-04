# A+ final closure mutation validation

Date: 2026-09-04

Each mutation changed one production, configuration, sample, or governance mechanism. The named
owner failed for the intended reason. The mutation was then removed with `apply_patch`, and the
positive owner was rebuilt and passed. No mutant remains in the final tree.

| ID | One-cause mutation | Causal killing observation |
|---|---|---|
| M01 | RabbitMQ durable flag `true -> false` | The real broker returned the retained message, but `Properties.Persistent` was false; exactly the persistence assertion failed (2/3 other acceptance cases passed), run `vicione-0253e2f5365b`. |
| M02 | RabbitMQ mandatory flag `true -> false` | The real broker accepted an unroutable publish without `MessageReturnedException`; exactly the rejection test failed (2/3 passed), run `vicione-f09cfbc854ae`. |
| M03 | RabbitMQ publisher-confirm wait `true -> false` | The dispatch returned without observing the broker return; exactly the no-early-acceptance test failed (2/3 passed), run `vicione-2dfa765d91e4`. |
| M04 | Remove `StartTimeout` from startup validation | The invalid zero timeout no longer threw `OptionsValidationException`; exactly one theory row failed (4/5 passed). |
| M05 | Remove `EditorBrowsable(Never)` from the manual scheduler factory family | The five-layer reflection owner reported `Expected: Never`, `Actual: null`; exactly one API-layer test failed (3/4 passed). |
| M06 | Reintroduce an exact SDK `10.0.400` block with disabled roll-forward | `RepositorySelectsTheStableDotNetTenChannelWithoutAnSdkPatchPin` failed on the unexpected `sdk` property; the other 14 repository graph tests passed. |
| M07 | Change central RabbitMQ.Client `7.2.2 -> 7.2.1` without regenerating locks | Locked restore failed with `NU1004`, identifying the exact package and `[7.2.2,) -> [7.2.1,)` mismatch. |
| M08 | Replace the preferred consumer retry callback with unguided `AddConsumer<T>()` | `PreferredPackageJourneys_UseOnlyApplicationAndProviderEntryPoints` failed because `consumer.UseMessageRetry` disappeared; the other 3 API tests passed. |
| M09 | Reclassify removed MassTransit product identity as retained advanced SPI | The exact 12-entry heritage map reported expected `REMOVE_COMPATIBILITY_ONLY`, actual `RETAIN_ADVANCED_HIDDEN`; the other 3 API tests passed. |

Final restoration checks:

- real RabbitMQ Durable Sender acceptance: 3/3 passed, run `vicione-4db121109552`;
- host lifecycle startup validation: 5/5 passed;
- API/discoverability/heritage owner: 4/4 passed;
- repository graph owner: 15/15 passed;
- RabbitMQ project locked restore: passed;
- `git diff --check`: passed.

The mutation review exposed one initial test gap: valid-but-wrong heritage dispositions could pass
because the test checked only an allowed vocabulary. The owner now binds all 12 identifiers to their
exact expected terminal disposition; M09 proves the correction.
