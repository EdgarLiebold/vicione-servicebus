# Azure Service Bus emulator dead-letter capability test

## Finding and correction

During the exact-commit `c4b3faeda` provider profile, one of 28 Azure Service
Bus emulator tests failed at `CompleteMessageAsync` with `MessageLockLost`.
The test used only the Azure SDK and emulator, not ViciOne product code. It
started a pending dead-letter-subqueue receive before confirming the primary
receiver's dead-letter settlement. The broker log recorded an internal
duplicate sequence-number exception, faulted its queue cursor, and invalidated
the delivered lock. A complete new-fixture rerun of the unchanged suite passed
28/28, establishing an intermittent emulator path rather than a deterministic
ViciOne failure.

The test now awaits `DeadLetterMessageAsync` before opening the dead-letter
receiver. It still waits up to the fixture operation timeout for visibility,
checks the exact message ID, payload, dead-letter reason, and description, and
requires successful completion of the delivered dead-letter message. It does
not catch or retry lock failures. The test requirement projection is unchanged.

## Adversarial review and verification

The read-only red team traced the failure through the emulator broker log and
identified the overlapping cursor operations. It reviewed the final one-file
diff and returned PASS: the removed overlap was not a required product behavior,
the body assertion strengthens the oracle, and the completion check remains
strict. The review did not execute tests.

- Source/test commit: `90819758b`.
- Corrected full Azure Service Bus local suite before commit: 28/28 passed,
  zero failures and skips, with a zero-warning Release build.
- That corrected fixture log no longer contains the duplicate sequence-number
  or cursor-fault message from the failed run. It still contains three emulator
  `Entity size became negative` diagnostics on other queue operations, so an
  empty fixture findings list is not a claim that the emulator log is clean.
- Exact-commit provider coverage run: 28/28 passed, zero failures and skips;
  `artifacts/coverage-a-plus-20260923-90819758b/raw/local/ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests/coverage.cobertura.xml`,
  SHA-256 `09fed7d148084c6618640146f2e402ed7f43e6f2d0b5cf6e90f71a96ddf0be36`.
  The counted fixture findings list is empty.
- Exact-commit complete serial Unit/Architecture gate: 10,211/10,211 passed,
  zero failures and skips. An earlier attempt ran beside a build/coverage
  process and produced six Quartz `BadImageFormatException` failures from
  assembly file contention; its direct Unit coverage reports were excluded,
  and the stable serial gate and direct reports were rerun.

The Microsoft `code-testing-agent` skill was used before editing this test;
`run-tests`, `test-gap-analysis`, `assertion-quality`, and `coverage-analysis`
guided the command, race analysis, behavioral oracle, and subsequent profile.
The emulator's internal cursor fault remains an external-system limitation;
any recurrence after sequential settlement must be investigated separately.
