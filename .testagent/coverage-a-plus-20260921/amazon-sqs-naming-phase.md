# Amazon SQS/SNS naming and scoped topology phase

## Product defects fixed

- Open generic parameters and partially open types could be named as publish contracts.
- Namespace, nested-type, and configured-separator boundaries could collapse distinct CLR
  contracts onto one durable SNS topic. Empty or duplicate custom separators are now rejected.
- A long canonical name could exceed the 256-character SNS limit, and its first hash fallback
  could collide exactly with a different, shorter contract. The hashed form now has a reserved
  marker and a stable digest; a scoped topic is compacted after its scope prefix is applied.
- Type-based destinations could disagree with the actual scoped publish topology. The resolver
  now uses the publish topology's destination.
- Reapplying scoped host settings stacked prefixes. Changing a scope after message or publish
  topology exists could leave a cached type on the old topic. The built-in message topology
  exposes whether it has captured a naming convention, and unsafe late changes fail before host
  settings mutate. Opaque custom message-topology configurators reject scoped settings because
  their cached naming state cannot be inspected.

## A+ test oracles and mutation discriminators

- Exact ordinary and custom-separator names preserve the supported naming contract. Namespace,
  nested-type, and custom-separator collision pairs must produce distinct valid SNS names.
- An exact SHA-256 digest collision fixture compares a long contract with a short contract whose
  CLR name is the long contract's digest. They must remain distinct and valid at 256 characters.
- Two long contracts with the same readable prefix must retain different hash suffixes both
  unscoped and in actual scoped publish destinations. An overlong scope must reject a long topic
  with the specific configuration error.
- Runtime type destinations must match actual publish topics, including scope. Open and partially
  open types must fail before naming. Repeated host settings must keep exactly one scope prefix.
- Factory, direct publish, and preexisting message-topology paths must reject late scope changes
  without changing host state. A delegating opaque topology must remain usable unscoped and reject
  scoped configuration atomically.

These oracles fail if generic-type validation, separator escaping, hash-mode separation, digest
suffixes, scoped length handling, publish-address lookup, idempotence, or cached-topology guards
are removed. Dynamic contract types are built in memory, without a broker, clock, network, or
process-global state.

## Verification

- Microsoft skills used: `code-testing-agent`, `run-tests`, `coverage-analysis`,
  `find-untested-sources`, `test-gap-analysis`, `grade-tests`, and the .NET test-analysis extension.
- The 18 new xUnit tests assert concrete names, addresses, rejection causes, or unchanged host
  state. Per-test grade under the Microsoft rubric: 18 A, 0 below A. The scope and collision tests
  use exact adversarial pairs, independent SNS validation, and both positive and negative cases.
- Read-only adversarial review found and drove fixes for open generic shapes, separator collisions,
  late Message-before-Host topology, digest-name collisions, post-prefix overlength, and opaque
  custom topology. Its final review of the product change returned **PASS**.
- Final Release build: zero warnings and zero errors. The SQS project passed 165/165 tests with
  Microsoft CodeCoverage. The complete Unit/Architecture gate passed 9,881/9,881 with zero failures
  and zero skips on the exact final product and test source.

## Coverage boundary

The fresh partial aggregate combines 19 coverage-enabled test projects, including Amazon SQS and
Event Hubs local integration. It observes 32/32 assemblies but omits other provider integration
projects and is not comparable with the older 36-report, 88.2482% baseline. Cobertura lacks
individual taken-branch identities, so merged branch coverage is reported as an interval. This
phase does not claim the requested global A+ gate.

- Aggregate partial lines: 76,249/92,823 = 82.1445%.
- Aggregate partial branches: 27,595–29,191/36,550 = 75.4993–79.8659%.
- Methods with CRAP > 30: 243. The changed formatter and scoped-host methods are below CRAP 30;
  the highest changed method is `AmazonSqsHostConfiguration.Settings` at CRAP 22 and 100% lines.
- Amazon SQS assembly in this partial aggregate: 1,830/3,078 lines and 794/1,438 branches.
- Raw current SQS report: `TestResults/artifacts/coverage-a-plus-20260921-sqs-final-v4`.
- Partial aggregate analysis: `artifacts/coverage-a-plus-20260921-sqs-v4-19-analysis/summary.json`.
