# Azure Service Bus publish validation and topic snapshots

## Product behavior corrected

`ServiceBusMessagePublishTopology<T>.Validate()` inherited an empty validation path, so invalid
topic names and Azure's idle-deletion minimum never reached publish-topology validation. Validation
now delegates to the topic configurator for included message types and checks the complete path,
including `BasePath`. Excluded message types neither declare topics nor block validation.

A subscription evaluates the publish topic's options early. Later changes to the publish
configurator could make subscription, broker, and sender settings diverge. A public getter also
returned the mutable internal SDK options, bypassing the setter guard. Evaluated topic settings
now reject changed setter values, and `CreateTopicOptions` returns a separate projection. Direct
mutation of that returned SDK object is no longer a configuration route. The interface comment,
API guide, and changelog describe this behavior; the high-level publish configurator does not
expose `Status` or `AuthorizationRules`, while custom broker declarations can supply SDK options
through `IBrokerTopologyBuilder.CreateTopic`.

## Hard tests and adversarial findings

Seven source-owned tests in `ServiceBusPublishValidationTests` cover two simultaneous validation
failures, the five-minute acceptance boundary, invalid and overlong composed paths, excluded
topics, early subscription evaluation, isolation of a mutated public SDK options result including
status and authorization, and exact projection of all configured topic fields to both publish
and sender broker topologies. Each test has a requirement projection.

Red phases observed zero validation failures where two were required, accepted invalid base paths,
rejected an excluded topic, permitted a post-subscription option change, and let a mutated getter
change the broker declaration. The green suite has direct state and failure oracles for each case.
The red team found and prompted fixes for the excluded-topic behavior, early snapshot drift,
mutable getter, and an initially incomplete sender-field oracle. Its final read-only review
returned PASS. The Microsoft `code-testing-agent`, `run-tests`, `crap-score`, and prior
`coverage-analysis` guidance shaped the focused tests, MTP invocation, and score check.

## Focused measurement and remaining scope

The final focused Azure Unit report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/topic-validation-final.cobertura.xml`.
The Azure Unit suite passed 220/220 after the field-oracle correction. Its report has SHA-256
`04f20141f84740d2910629b1f6ea30b33da8ed03bdf59005fc2a9957e261e835`. Under Microsoft
CodeCoverage, `ServiceBusTopicConfigurator.Validate` has CRAP 12, its SDK option projection has
CRAP 20, `ServiceBusMessagePublishTopology<T>.Validate` has CRAP 2, the public getter has CRAP 1,
and the setter freeze guard has CRAP 4. These are focused values; they do not replace the last
complete product-wide profile at `44d9e3254` or establish global A+.

The locked Release Unit/Architecture build had zero warnings and errors. A first highly parallel
run had one Quartz integration timeout among 9,989 tests. That test passed alone in two seconds,
and the complete rerun with at most two concurrent modules passed 9,989/9,989 without failures or
skips. The Azure emulator project was missing a direct `Microsoft.Testing.Extensions.CodeCoverage`
dependency: with `--coverage`, MTP discovered zero tests and exited 5, while its 28 tests passed
without instrumentation. Adding the missing package and its NuGet lock entry made a broker-free
coverage probe pass; the subsequent isolated official-emulator run passed 28/28 with Microsoft
CodeCoverage and empty fixture findings (`vicione-8723ca9210b2`). The emulator report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/topic-validation-emulator.cobertura.xml`, SHA-256
`ca260908eea01c3794dc823cdda7c20ef3d42456dfd4748bde373dad8c2a38fa`.

An isolated, clean checkout of exact source/test/documentation commit
`2f3a4b6ebe93d688fc9346587f9d0f1c6325330a` passed locked restores and
zero-warning Release builds for both Azure Unit and Azure emulator graphs.
Azure Unit passed 220/220 with Microsoft CodeCoverage; its report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/azure-unit-exact-2f3a4b6.cobertura.xml`,
SHA-256 `9e77250b611292761fa71000dc23d563a5715aabea22c99d12f099fa14442f7b`.
The exact-commit official emulator passed 28/28 with Microsoft CodeCoverage
and empty fixture findings (`vicione-4d220f5fb313`); its report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/azure-emulator-exact-2f3a4b6.cobertura.xml`,
SHA-256 `d56d9de80144c9329d2d62cf0830ff2b9ff2ddff3bcad72ef27f469356452012`.
The first isolated emulator attempt under `/private/tmp` could not start Docker
because that host path was not mountable; rerunning from a clean workspace
checkout resolved the fixture issue without changing tracked files.
