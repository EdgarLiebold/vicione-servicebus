# Amazon SQS/SNS subscription identity phase

## Product defects fixed

- A subscription's structural comparer compared its topic and queue by object reference. An
  extension collection therefore rejected an independently created but equivalent broker relation
  as a conflict. It now uses the topic and queue structural comparers and their compatible hashes.
- The subscription name comparer distinguished CLR subclasses even when the SNS topic and SQS queue
  names were the same. Extension collections could contain two definitions for one broker pair.
  Name identity now follows the two broker names across subclasses, while structural identity can
  still reject a different relation type.

## Product behavior tested

- Independent topic and queue objects with equivalent declarations yield one subscription handle
  and one stored relation. The two structural hashes agree.
- Changed topic or queue broker settings, changed source or destination names, and an alternate
  subscription CLR type remain distinct structural definitions. The same broker pair with
  conflicting settings or a different CLR type fails without replacing the stored relation.
- Name identity distinguishes both source and destination names and detects the same broker pair
  across CLR subclasses.
- The regular public builder reuses a repeated topic-to-queue subscription handle and retains
  exactly one relation with the correct source and destination.

## Test quality and review

- The eight source-owned xUnit tests use exact handle, relation-count, broker-name, exception,
  unchanged-state, and hash-contract assertions. They require no broker, time, random input,
  network, or mutable test-global state. Per-test grade under the Microsoft rubric: 8 A, 0 below A.
- Three focused tests failed on the old product code and passed after the comparer correction.
  The regular builder regression is deliberately separate: it guards the standard API and is not
  claimed to prove the extension-only defect.
- Read-only adversarial review of the final exact diff returned **PASS**. It confirmed the hash
  contract and the separation between extension collection and regular builder paths.
- Microsoft `code-testing-agent`, `run-tests`, `coverage-analysis`, `grade-tests`, and the .NET
  test-analysis guidance were used for this bounded phase.

## Verification and coverage

- The eight focused tests passed 8/8. The Amazon SQS project passed 187/187 with Microsoft
  CodeCoverage. The Release Unit/Architecture solution build had zero warnings and zero errors;
  its complete test rerun passed 9,903/9,903 with zero failures and zero skips.
- The structural subscription comparer moved from CRAP 110 to 12.7; the name comparer moved from
  CRAP 110 to 11.38. Both selected hotspots are below CRAP 30.
- The fresh 19-report partial aggregate observes 32/32 source assemblies: 76,341/92,835 lines
  = 82.2330%; 27,701–29,505/36,564 branches = 75.7603–80.6941%; 235 methods exceed CRAP 30.
  Cobertura omits taken-branch IDs, so branch coverage is an interval. Other provider integration
  reports are absent. This profile cannot be compared directly with the older complete profile
  and does not claim global A+ coverage.
- Raw SQS report: `TestResults/artifacts/coverage-a-plus-20260922-sqs-subscription-final-8`.
  Partial aggregate: `artifacts/coverage-a-plus-20260922-sqs-subscription-final-19-analysis/summary.json`.
