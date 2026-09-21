# Amazon SQS/SNS topology declarations phase

## Product defects fixed

- Two declarations with the same queue or topic name reused one handle even when their AWS
  attributes, SNS subscription attributes, or tags differed. The later declaration's broker
  settings were silently lost. Structural equality now compares every group without depending
  on dictionary insertion order, while subscription-attribute keys retain their case-insensitive
  provider semantics.
- A topic's implicit `RawMessageDelivery=true` and an explicit `false` were previously treated as
  equal, so a false setting could disappear when the same topic was declared twice.
- Queue and topic name indexes distinguished CLR subclasses even though AWS names do not. An
  extension builder could register two entities for one broker name. The name index now compares
  AWS names across subclasses; structural equality can still reject different entity types.

## Product behavior tested

- Queue and topic diagnostics show exact lifecycle flags, attributes, subscription attributes,
  tags, and the default raw-delivery setting.
- Equivalent declarations reuse one handle regardless of dictionary insertion order or the
  spelling case of subscription keys. Changed values and changed keys in each metadata group
  compare unequal, even when dictionary counts and the values themselves remain the same.
- A conflicting second declaration throws and retains the first queue or topic definition.
  Topic raw delivery is exercised through the builder, including the implicit true default.
- Name comparers identify the same AWS name across CLR subclasses, while the structural
  comparers reject different CLR types. Builder tests prove that a subclass cannot add a second
  queue or topic with the same name.

## Test quality and review

- The 14 source-owned xUnit tests use exact output, handle identity, exception, retained-state,
  and field-level negative assertions. No broker, wall clock, random value, or mutable test-global
  state is involved. Each assertion distinguishes a plausible product regression. Per-test grade
  under the Microsoft rubric: 14 A, 0 below A.
- Read-only adversarial review found the silent metadata loss, the subtype name-index defect,
  and a missing same-count metadata-key negative control. All three findings were addressed;
  the final exact-diff review returned **PASS**.
- Microsoft `code-testing-agent`, `run-tests`, `coverage-analysis`, `test-gap-analysis`,
  `grade-tests`, and the .NET test-analysis guidance are used for this phase.

## Verification and coverage

- The 14 focused tests passed 14/14. The Amazon SQS project passed 179/179 with Microsoft
  CodeCoverage. The Release Unit/Architecture solution build had zero warnings and zero errors;
  its complete test rerun passed 9,895/9,895 with zero failures and zero skips.
- In the fresh 19-report partial aggregate, queue/topic diagnostics moved from CRAP 342 each to
  18 each; structural comparers from 156 each to 18 each; and name comparers from 72 each to
  6 each. All six selected methods are now below CRAP 30. The new metadata helper is CRAP 8 with
  all nine lines covered.
- The partial aggregate observes 32/32 source assemblies: 76,327/92,836 lines = 82.2170%;
  27,689–29,493/36,566 branches = 75.7233–80.6569%; 237 methods exceed CRAP 30. The branch
  interval reflects missing taken-branch IDs in Cobertura. Other provider integration reports
  are absent, so these figures are not directly comparable with the older complete profile and
  do not claim global A+ coverage.
- Raw SQS report: `TestResults/artifacts/coverage-a-plus-20260922-sqs-topology-final-14`.
  Partial aggregate: `artifacts/coverage-a-plus-20260922-sqs-topology-19-analysis/summary.json`.
