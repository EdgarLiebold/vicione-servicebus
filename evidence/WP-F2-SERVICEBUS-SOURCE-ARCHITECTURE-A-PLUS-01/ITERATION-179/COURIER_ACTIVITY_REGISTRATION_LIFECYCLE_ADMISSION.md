# Iteration 179 — Courier activity registration lifecycle admission

## Result

This packet personally reads the complete shared registration-context and container-selector
contracts plus the complete Courier activity definitions, definition contracts, host
configurators, registrations and registration configurators (18 files / 1,289 lines), and the
complete owning test file (426 lines). All eighteen source files are newly admitted.

Compensatable and execute-only activity registrations now resolve and publish their definitions
under a dedicated lock. Endpoint-definition overrides are applied to a local definition before it
becomes visible, so concurrent callers cannot observe a partially initialized definition or cause
duplicate container resolution. The registration contract validates a missing context before
consulting cached state, keeping the public diagnostic stable before and after initialization.
Matching configure actions, endpoint topology suppression, explicit endpoint registration,
exclusion, default-definition fallback and full execute/compensate host composition remain intact.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 18 / 1,289 | `c25991aac26d4ce7debeacf30ffde1fd5a9d35b3f00991a4bb881e1029cc7f8a` | `4dd7de8d210b98f0fde05d5a20551868b74a57528982cad07f093b3951bc45bc` |
| Tests | 1 / 426 | `7b3839498e2883eac5c0686ec55c48323b0edc004aefa636f9655a61f041de36` | `31e1e482178676348b2dcc54f8f1f8abd3ce86d166af940526b780622e5d9b3c` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 178 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `src/ViciOne.ServiceBus.Abstractions/Configuration/DependencyInjection/IRegistrationContext.cs` | `b563e6b489e812b6b4613a5bd76a1581d7c835dbdf1a206310fbf8ecba21d272` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/ActivityDefinition.cs` | `f2eeda3e421bf05c154401debf9e9c7af1d80591a0bea45337d6e26839206a61` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/CompensateActivityHostConfigurator.cs` | `38092527b8e7179fc1c364339698fe435f72a045d8c6c097286680214e787324` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/DefaultActivityDefinition.cs` | `8b0c5824d73b3c285e78f8011bceaa5a7a2dc359bc2f9e92faabeb2460cee89d` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/DefaultExecuteActivityDefinition.cs` | `99ccfb1031668191ead0b6212e97bbe0ef8821e38f872c131af7509df8f01b5d` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/ExecuteActivityDefinition.cs` | `6c31bb41e9a35e44b267eb2270cd7d5e3500bc501b203107652197e55040da3b` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/ExecuteActivityHostConfigurator.cs` | `dd515aec22ce7fe0b9f1ceeeafb383146de75e6653ffcc793b16ba84165bf3ee` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/IActivityDefinition.cs` | `651a362fddf408a407ff63100e8efefb6add68c6729a33b9ece8cb6fd58e5847` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/IActivityRegistration.cs` | `bed0acc495df0f099c7a7e839564ab760f8b4afed26cc0b4b6deb43e5e4af91d` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/IActivityRegistrationConfigurator.cs` | `ed6722d75b1771cec20b99c2e6ab38144451a5814a9f5ccfeedf8d503c4ec6bf` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/IExecuteActivityDefinition.cs` | `ffc013bac69f3644f68242d24f48328c6a0f111be7e088d52019dede83cdb186` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/IExecuteActivityRegistration.cs` | `fb49bdc381e7c6ec2cb2b1c06d005a77c69c6ba1cae3a1986d46666ce704885d` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/IExecuteActivityRegistrationConfigurator.cs` | `69181e0cd041931a1888565c4046cb443298b46d2134bfede8f79853ede4d8d6` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/Registration/ActivityRegistration.cs` | `c1ced58aa978c363c4b6e41120785e4a6dc3e2046084dbcd049ce2bd3392ea78` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/Registration/ActivityRegistrationConfigurator.cs` | `765efa266ee7d15ac288a86fd444b4cdca22b4718c98418d93e2072bc1661f81` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/Registration/ExecuteActivityRegistration.cs` | `fc1f71aad0ff0d12503aa01e7522afa34652efa6477b160ace523d039ebd7026` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/Registration/ExecuteActivityRegistrationConfigurator.cs` | `ad5cb69f62c7a532d234829e6a1653897a836df02e95508a05c15faddbbe89ab` |
| Source | `src/ViciOne.ServiceBus/Configuration/DependencyInjection/IContainerSelector.cs` | `84a562c2a3e59783a9471f27e745d0101c45392a091fe2cc800d383794859b85` |
| Test | `tests/ViciOne.ServiceBus.Tests/Courier/ActivityRegistrationLifecycleTests.cs` | `64527d633b691ee208742fa969f217c6e574900e210bf1a9542f0302cb49bb85` |

Cumulative personal source admission is 376/4,118 current C# files.

## Proof

The seven owning tests prove constructor dependency boundaries; stable definition-context
diagnostics before and after caching; exact-once atomic definition and endpoint-override
publication under sixteen simultaneous callers for both activity shapes; explicit endpoint
callback behavior and exclusion; all registration endpoint input guards; and exact matching of
definition callbacks, configure actions, topology flags and endpoint specifications.

Seven requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`60e92d19c070260f4500072bccb694741f57738d8195498d08cd2bfcef3a7ff1`.

Eight successfully compiled single-cause mutants were killed and restored: replace the activity
definition lock with a per-call lock; replace the execute-only definition lock with a per-call
lock; remove each registration contract's context guard; skip the compensatable definition's
execute endpoint override; skip its compensation endpoint override; skip the execute-only
definition's endpoint override; and enable consume topology on a generated activity endpoint. All
product sources were restored to the personally re-read final bytes before the final gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-179-final.cobertura.xml`,
SHA-256 `5826d1bcfe40da6187f8236d05580a1513d3008226401a0d1e7848b24bfc00ac`.
All four registration and registration-configurator classes report 100% line coverage. Their
branch coverage is respectively 90.91%, 100%, 92.86% and 100%; maximum target complexity and CRAP
are both 22. Unit sorted-display-name SHA-256 is
`ba627444e9fcd9790d2f63d42fc7e2170a3f12791c579c0514dfa76e435f2301`.

| Gate | Result |
| --- | --- |
| New owning test class | 7/7 passed |
| Combined registration boundary and container tests | 23/23 passed |
| Full Core Release | 4,848/4,848 passed with suite parallelism disabled |
| Full EF unit Release | 249/249 passed |
| Strict Release Core/Courier/EF/local builds | 0 warnings, 0 errors |
| Courier/Core format | Exit 0; 0 files required formatting |
| Unit/EF/local requirement projections | 1/1, 1/1 and 1/1 passed |
| Mutation probes | 8/8 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
The local database-dependent matrix remains externally configured; its requirement projection and
Release build are green, and no credential was inferred or written.

Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-179-courier-activity-registration-lifecycle-admission-2026-09-17`.

Whole-fork personal reading, global API/naming/nullability/coverage and configured external-provider
acceptance remain open. Remote publication remains an independent delivery step and cannot pause or
deactivate the active goal.
