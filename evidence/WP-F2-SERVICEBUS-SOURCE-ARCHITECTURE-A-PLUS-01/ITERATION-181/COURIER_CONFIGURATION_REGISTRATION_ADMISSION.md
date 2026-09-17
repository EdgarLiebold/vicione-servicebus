# Iteration 181 — Courier configuration and registration admission

## Result

This packet personally reads the complete remaining Courier configuration surface: public host
overloads, typed pipe adapters, typed/runtime/scan registration, dependency-injection registration
and host adapters, and routing-slip configuration (10 files / 1,170 lines). It also completely
reads or authors the five direct owning test files (1,290 lines). All ten source files are newly
admitted.

Host overloads now reject a missing receiver before other invalid inputs. Runtime, generic and DI
registration reject abstract, open or mismatched activity/definition types before changing the
service collection. Explicit activity scans construct both compensatable and execute-only plans,
run filters, normalize ambiguous definition failures and validate selected definitions before the
first registration, so a later failure cannot leave a partial container mutation. Duplicate input
types remain idempotent, filtered-out invalid candidates remain ignored, and every retained public
operation remains synchronous and therefore correctly has no `Async` suffix.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 10 / 1,170 | `7e1e34d7c208d5bab9b875d297d09f70094949a4730b22616783e151ea70e67a` | `2ea4a6d551c3916eb66e20a79ab41a5121b00da9fcadcab92a8b61c7989ebea6` |
| Tests | 5 / 1,290 | `5299e0a54979685b2f4b75d43218ca4723e8a5799689bb5400cfe9cd1ed201c7` | `5a64762dffd640786d6e2d8799a80af0ef0ec7317638f8f87e8604b1958383f3` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 180 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/ActivityPipeConfiguratorExtensions.cs` | `d9ba40da1f82be11e7512913e7a9338f8271d29fd129293f6f661902331ab044` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/CourierHostConfiguratorExtensions.cs` | `b072644a33161f02a67f2e3e0fc568bf2e1949ae9ff3403dc3411bf1e98e8c49` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/CourierRegistrationConfiguratorExtensions.cs` | `744c21ecc39ad9b45ef0c7b3d32b30078e9ec8815531bad4c03f082b29c1327b` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/CourierRegistrationConfiguratorRuntimeExtensions.cs` | `4d2f3685f7d65e8cd344d403899e073a4b9c406c93c040a93de786ab9dde4576` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/CourierRegistrationExtensions.cs` | `3f69644777a3d773d6cdd89003b9a6e24635c2d2c6ad9cea027b2f7191d32ac4` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/DependencyInjectionActivityRegistrationExtensions.cs` | `da1c3ed305d6b5b530df4cd8dabaa272d71675e291a8e62ba55b737cae2d68eb` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/DependencyInjectionCourierReceiveEndpointExtensions.cs` | `95abdd7dfbcaeb996fda538fda0be627b3ab3fbf35c2171f6f763beb8d0c59e9` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/DependencyInjectionExecuteActivityRegistrationExtensions.cs` | `e8f403722911eab835e04d734eca9fbb38fafd8b0cf956011aa58245fd1f10b2` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/IRoutingSlipConfigurator.cs` | `7efbad2989899d886862cbbcab2e04d9117a00849f9a57a46df26177d7f4a0a4` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/RoutingSlipConfigurator.cs` | `98d62f36cc4368468d61c3ecc54a62f782eddf8fddb3eaabb976ccec2a1a7fc4` |
| Test | `tests/ViciOne.ServiceBus.Tests/Courier/CourierConfigurationSurfaceTests.cs` | `6534e64e343b3bcc76d04fd145ac321915324bbba21f8097fbdeffa27ffec162` |
| Test | `tests/ViciOne.ServiceBus.Tests/Courier/CourierConsumerKindContractTests.cs` | `5d31315cc7be72a2cd9715a73c1ae98caa0775ab6124f88deaba61014a25bc50` |
| Test | `tests/ViciOne.ServiceBus.Tests/Courier/CourierHostResultContractTests.cs` | `6f15a885b294c5b014d264616385850703b23d6e02dd8f90f003fd0240e40d11` |
| Test | `tests/ViciOne.ServiceBus.Tests/Courier/CourierRegistrationBoundaryTests.cs` | `ef481895f529ec4a0083853d8cce0d5476676a0a32d6ef823e51aa0b928042b4` |
| Test | `tests/ViciOne.ServiceBus.Tests/Courier/RoutingSlipHostConfigurationTests.cs` | `46e7208a442303b5086e3cc361b21b55359e1d64b6721e26eb164602f1642068` |

Cumulative personal source admission is 398/4,118 current C# files.

## Proof

The first 19-case parent-baseline run passed 15 and failed 4. Three failures directly exposed the
product defects: receiver validation occurred after another input, a late scan-filter failure left
four service descriptors behind, and an abstract runtime activity was admitted. The fourth failure
exposed a missing scoped-consume-context interface in the new proxy fixture and was corrected before
any green claim. The final 19 cases cover all twelve host overloads, receiver/input priority,
callback and specification cardinality, all three DI host overloads, both typed pipe adapters,
routing-slip build/validation, generic/runtime/assembly/namespace/explicit scan shapes, concrete
type admission, definition matching, ambiguous/duplicate definitions, filter failure and atomic
service effects.

Six requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`508bc824ac218743e520fcdacce0ae5afe091161ef4fd6421b6dd6f4bbaf91c4`.

Eight successfully compiled single-cause mutants were killed and restored: remove receiver-first
validation; remove the runtime concrete-type guard; register compensatable activities before the
execute-only scan plan; allow the second definition for one activity; admit an abstract selected
definition; stop translating ambiguous definition contracts; forward an activity pipe
specification twice; and stop comparing definition generic arguments. All product sources were
restored to the personally re-read final bytes before the final gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-181-final.cobertura.xml`,
SHA-256 `2f6284fc152e684d618ae77179be840a4f344b91bca46dfe0e636280e3e3bb29`.
The direct pipe, host, registration-configurator, runtime-registration, DI-host and routing-slip
implementation classes report 100% line coverage. `CourierRegistrationExtensions` reports 98.98%
line and 96.77% branch coverage; its only source gap is the private namespace-null guard made
unreachable by the public guard, while the compiler also reports the default namespace predicate
body when the scanned namespace contains no activities. `PlanRegistrations` reports 100% line and
branch coverage; maximum target complexity and CRAP are both 22, below the threshold of 30. The two
DI registration adapters report 83.33% line coverage in the deliberately narrow focus and 100% in
the complete unit snapshot. The interface contains no executable lines. Unit sorted-display-name
SHA-256 is `f9622fbc081bb45ef60942569bcc00da3418baffa2dea9e08d63df3541d7a019`.

| Gate | Result |
| --- | --- |
| Focused Courier configuration/registration | 19/19 passed |
| Full Core Release | 4,858/4,858 passed with suite parallelism disabled |
| Full EF unit Release | 249/249 passed |
| Strict Release Abstractions/Core/Courier/EF/test/local builds | 0 warnings, 0 errors |
| Courier/Core format | Exit 0; 0/5,473 files required formatting |
| Unit/EF/local requirement projections | 1/1, 1/1 and 1/1 passed |
| Mutation probes | 8/8 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
The local database-dependent matrix remains externally configured; its requirement projection and
Release build are green, and no credential was inferred or written.

Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-181-courier-configuration-registration-admission-2026-09-17`.

Whole-fork personal reading, global API/naming/nullability/coverage and configured external-provider
acceptance remain open. Remote publication remains an independent delivery step and cannot pause or
deactivate the active goal.
