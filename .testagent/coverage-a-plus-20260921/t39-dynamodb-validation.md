# T39: DynamoDB registration validation

Base: `68481feb94577433b8ac928f113c92b4a4a9f198`.
Microsoft code-testing-agent and run-tests applied. MTP/xUnit v3, SDK 10.0.302.
No product source changed. Test-only DI implementation dependency uses the
existing central version 10.0.12 and its established lock hash.

## Requirements and evidence

All methods are in `DynamoDbRegistrationValidationTests`.

| Contract | Test | Cases |
| --- | --- | --- |
| Invalid configuration returns the exact diagnostic before repository registration or context creation | InvalidConfiguration_IsRejectedBeforeRepositoryRegistration | 11 |
| All five failures are reported together, including exception text; corrected configuration is revalidated and exception snapshot retained | MultipleFailures_AreReportedTogetherAndCorrectedConfigurationIsRevalidated | 1 |
| Valid TTL null/30 seconds, table lengths 3/255, V1/V2 and both factory overloads register immutable options and create context lazily using the actual DI provider | ValidBoundaries_RegisterFrozenOptionsAndCreateContextLazily | 4 |

The positive cases resolve actual DI services, mutate the retained configurator,
check every options value, count factory invocations and compare provider identity.
Negative cases use a strict registration probe and exact failure count/severity.
No claim of cloud persistence, context disposal or DynamoDB service availability.

## Verification

- MAIN `artifacts/t39-unit-b.log`: 44/44 pass, no skips, exit 0.
- MAIN `artifacts/t39-format.log`: verify-only formatting, exit 0.
- Initial focused build failed due to missing namespace/DI implementation;
  next attempt rejected the stale package lock. Both corrected; failed logs retained.
- Read-only adversarial plan and implementation review found an aggregate
  exception-text assertion gap; all expected keys/messages are now checked.
- GATE `/private/tmp/servicebus-reply-investigation`, isolated mutations:
  - `artifacts/t39-mutant-diagnostic.log`: omitted TimeProvider diagnostic,
    2 failures and 14 passing controls, exit 2.
  - `artifacts/t39-mutant-ttl.log`: changed TTL `<` to `<=`, 3 failures and
    13 passing controls, exit 2.
  - `artifacts/t39-mutant-gate.log`: removed public validation, 12 failures and
    4 passing controls, exit 2.
- All mutations manually restored. Both product files and test file compare
  byte-identically with MAIN. `artifacts/t39-restored.log`: 44/44, exit 0.
- Final independent read-only review confirms the corrected assertions,
  dependency lock, mutation results and byte equality; no remaining blocker.

Exact-commit measurement at 49dbda6d9 passes all 33 profiles, four fixture groups
and 12,894 executions. Validate reaches 14/14 lines, 14/14 branches and CRAP 14.
See [full measurement](product-wide-profile-49dbda6d9.md) for remaining gaps and
non-target observations. Global A+ is not established; remote push follows docs.
