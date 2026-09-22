# Runtime-typed recurring scheduler dispatch

## Product behavior under test

Three new tests exercise the public `IRecurringMessageScheduler` object
overloads through an `EndpointRecurringMessageScheduler` and a recording
advanced send endpoint. The runtime-typed path must send a command with the
actual payload contract, its independent message URN, original payload,
schedule, destination, and caller cancellation token. The explicit-interface
path must instead send exactly the selected interface URN, without the
concrete runtime marker, while preserving the same payload, schedule,
destination, untyped send pipe, and token. Both converter overloads must
reject a payload that does not implement the declared contract before any
provider send. The tests assert the emitted command and returned handle,
not merely successful execution.

The initial combined test used an endpoint lacking the advanced send contract;
it was corrected to model the required product collaborator. Adversarial review
found that command generic types alone did not prove wire contract identity;
independent URN assertions were added. The combined test was split into three
focused cases before the final run. Neither superseded run contributes
validation evidence.

## Current-source validation

- Release build of `ViciOne.ServiceBus.Tests` passed with zero warnings and
  errors. The final focused class run passed 14/14. The complete Core Unit
  run passed 6,271/6,271 with Microsoft CodeCoverage and
  `tools/ci/coverage.settings.xml`, zero failures and skips. Its report
  SHA-256 is
  `8079b975fa80d741927da858ef47d0606640f5d8007cc27c8e30bca69441f482`;
  test-log SHA-256 is
  `d19547b5d0e8b3c251b5086b5bbf8aafd5bc65058dfe697f8bf38a37def9e4ce`.
- The formerly uncovered no-pipe recurring converter state machine is now
  7/10 lines, 62.5% of instrumented branches, complexity 8, and CRAP about
  9.728. The pipe variant, previously 0/12 lines and CRAP 110 in the last
  full product profile, is now 8/12 lines, 60% branches, complexity 10,
  and CRAP about 13.704. The former baseline and current method bodies are
  the same, but the full product profile and focused Core report differ in
  test scope. These methods are below CRAP 30, not at A+ line or branch
  coverage. Publish and typed `IPipe<SendContext<T>>` variants remain outside
  this focused test evidence.
- The report and log are in
  `artifacts/coverage-a-plus-20260922-4488b29fe/recurring-phase/full/`.
  This is not a new product-wide measurement.

## Adversarial review

The independent read-only review returned PASS for the final three-test
diff, Requirements projection, and stored report hash and counts. It
verified that the type markers, pipe identity, token, command, handle, and
both mismatch paths have independent assertions. It explicitly did not
claim A+ line or branch coverage for all recurring scheduling variants.

## Gate and exact source/test commit

The complete Unit/Architecture gate passed 10,022/10,022 with zero failures
and skips. Its log SHA-256 is
`f1d6c8fa44eee8306ec4dd44e772f526338cb369e9b60ee5d72083468f97e417`.
The gate log is in the `recurring-phase/` artifact directory. Exact-commit
verification is pending.
