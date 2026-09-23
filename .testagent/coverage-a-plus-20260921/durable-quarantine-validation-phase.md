# Durable-send quarantine validation phase

## Product behavior tested

- `DurableQuarantinePage_AcceptsAnAttemptAtTheAdmissionTimestamp` verifies that
  one attempt and equal admission/quarantine timestamps are valid, that the
  returned entry is preserved, and that the page cannot be mutated.
- `DurableQuarantinePage_RejectsCorruptIdentityAndDestinationBeforeExposure`
  rejects a default durable-send ID, a default contract identity, a missing
  destination, and a relative destination at the public page-creation path.
- `DurableQuarantinePage_RejectsImpossibleTerminalStateBeforeExposure`
  rejects a quarantine timestamp before admission, both missing and unknown
  failure categories, whitespace failure evidence, and zero delivery attempts.
  Each rejection checks the owning parameter name.
- The adversarial review identified an adjacent public boundary:
  `DurableQuarantinePage_RejectsANullEntryAtThePublicBoundary` verifies that a
  null fetched entry is rejected with `fetchedEntries`, before it can become
  visible through a page.
- The four cases are recorded under `REQ-VSB-DURABLE-SEND-QUARANTINE` in the
  Abstractions requirement projection. No product source or comments changed.

## Verification

- Focused Microsoft CodeCoverage: 16/16 selected contract tests passed;
  `DurableSendQuarantineEntry.Validate` has 14/14 measured lines and
  complexity/CRAP 20. The preceding complete profile at `09235f22d` measured
  8/14 lines and CRAP 51.49. This is a focused result, not a new product-wide
  profile.
- Isolated Abstractions before the fourth case: 767/767 passed, including the
  requirement projection. The final complete gate below includes that case.
- Isolated RabbitMQ: 387/387 passed. Isolated Quartz: 267/267 passed.
- Complete serial Unit/Architecture solution after the fourth case:
  10,202/10,202 passed, zero
  failures and skips. The command used `--max-parallel-test-modules 1` after a
  concurrent attempt had an Abstractions projection failure (then corrected),
  a RabbitMQ assertion failure, a Quartz timeout, and a long Architecture run.
  The successful final log is
  `artifacts/coverage-a-plus-20260923-quarantine/unit-architecture-final.log`.

The next full 36-report profile must be collected at the committed source/test
revision before these focused numbers can be included in global A+ metrics.
