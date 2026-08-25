# RabbitMQ address mutation manifest

## Binding

- Final review commit: `838c883500a2290e871e686a34a6c93f5e9dd215`
- Final review tree: `73a8970def7491f06604a94df104bdeeef40e162`
- Final executable commit: `8939c04827a4e35ef37561acbabd704a9ba910dc`
- Final executable tree: `e74d17e781120315a32a4f1432f30dba611244d0`
- RabbitMQ correction commit: `03b375879f887a2bdab4f378fbad12b24d3db668`
- RabbitMQ correction tree: `9326fc2f9ccb4761dec0ce1f0e18f9a58eb41e00`
- Isolated worktree: `/private/tmp/vsb-rmq-mutations.F4XHqa`

M01 through M14 ran against the RabbitMQ correction commit. The next executable delta is only
`SagaConnectorTests.cs`, which is outside the RabbitMQ test project's evaluated source and project
graph. M15 shares the core test project with that delta and was therefore rerun against the final
executable commit. The final review commit changes only `.testagent/status.md`. Every product target
has the same baseline hash in all three commits.

Each mutant was built serially with:

```text
env DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 /usr/local/share/dotnet/dotnet build <project> --configuration Release --no-restore --disable-build-servers /m:1 /nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false -v:quiet
```

`<project>` was
`tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj`
for M01 through M14 and `tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj` for M15.
Every build exited `0`. Each named native MTP executable then ran with the exact filter and minimum
shown below, `--report-xunit-ctrf`, the evidence directory as `--results-directory`, and the listed
JSON as `--report-xunit-ctrf-filename`. Every test command exited `2` because the intended assertion
rejected the one mutation; no run skipped a test.

## Mutations

| ID | Target; baseline SHA-256 | Exact one-cause mutation; mutant SHA-256 | Native filter; minimum | Raw CTRF SHA-256; result |
|---|---|---|---|---|
| M01 | `RabbitMqHostAddress.cs`; `d1bf7d4410e7ea433d9a919500d6a6e0d927126b27859e504a0b29bc225e1107` | default port `5672` -> `5673`; `92ddb9516fcc118f3e186c93ff5a89c5be1ee9541d27c50952b1f9fabcf438f5` | `RabbitMqHostAddressTests.SupportedSchemes_UseTheirDefaultPortAndRoundTrip`; 4 | `M01-default-port.json`; `8192737615b9000eeb092df0aafd333cfb4cd2a58a00c51b546a43822033b02e`; 2/4 failed |
| M02 | `RabbitMqHostAddress.cs`; `d1bf7d4410e7ea433d9a919500d6a6e0d927126b27859e504a0b29bc225e1107` | remove `Uri.EscapeDataString` from virtual-host rendering; `25c63a6e219c182bebde88cfe7b2bb33efc6f8683672a7c425e9bf1c4295a82e` | `RabbitMqHostAddressTests.EncodedVirtualHost_IsDecodedInMemoryAndEncodedInTheUri`; 1 | `M02-virtual-host-encoding.json`; `a0b0df6115d6112e98bce1c73af0a7372495a22f8133ad7a2397e76eb64617b9`; 1/1 failed |
| M03 | `RabbitMqAddressExtensions.cs`; `ce55062321464fe3722f72c8863821e97763ea74d340919f73f854aeea74ae31` | credential delimiter `IndexOf(':')` -> `LastIndexOf(':')`; `c9676b414f900f42151882808a1f89ae68bbe22d93f75c88d36a9830dadc4435` | `RabbitMqAddressExtensionsTests.Credentials_AreDecodedWithoutTruncatingThePassword`; 4 | `M03-credential-suffix.json`; `b77cc16db3e7c7c6ac89623233b80a5187d9312727ef8f66dbb3f864421f8163`; 1/4 failed |
| M04 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | return caller-owned `IReadOnlyList<string>` before validation/copy; `d7bb8121eb9d9c7f7a05b8d8222ce8e74fd48eaceef3298a3fb6958d92fe480d` | `RabbitMqEndpointAddressTests.DirectConstruction_OwnsItsBindingSnapshot`; 1 | `M04-binding-snapshot.json`; `9adbe0d90f823f32ab0272c4fad424946e9e1b8ff37daad33a0e6894777512fd`; 1/1 failed |
| M05 | `RabbitMqAddressRequirements.json`; `3a728ce3b3973142f1fbe17bd235e3b7615805522c03e4959ff722282d9f4bad` | remove `host-settings-port-and-root-virtual-host`; `3d74dbf1f3da71570daff08b007ec50e14aa7ba528af81a8531e023c34a6323e` | `RequirementCoverageProjectionTests.RabbitMqAddressRequirements_MatchCompiledRequirementMetadata`; 1 | `M05-requirement-projection-omission.json`; `90997b6900a4468eace779e952229ea5aa5d378f8bbda416f1ae9e5bd6c2c1bf`; 1/1 failed |
| M06 | `QueryStringExtensions.cs`; `a5247757895020de752152377a9a777685d9fbfaad5678d84720e0c05041a01b` | value remainder -> `element[(separator + 1)..].Split('=')[0]`; `6721d2eb4043e4714c104a976981d1c5193f6818bb8ef0a23f4ec3a6ddcbc5db` | `RabbitMqEndpointAddressTests.QueryValues_PreserveRawAndEncodedSeparators`; 2 | `M06-query-truncation.json`; `d0ab61eee7c8426e75e5002c617dc9be07026a38fc18b7b9c63d3e1efdc81eab`; 1/2 failed |
| M07 | `RabbitMqBusTopology.cs`; `7e2343173ab6bc6e609fe531f69ccc82c3547c6fffa53aab2016a86cfb49f56b` | named overload host -> `new Uri("rabbitmq://localhost/")`; `e54767c53ae2cc6098f3c1fe3fed57079007bf2468b287df933f90017c8a2e8e` | `RabbitMqBusTopologyTests.DestinationAddresses_UseTheFinalConfiguredHostForBothPublicOverloads`; 1 | `M07-topology-named-overload.json`; `5b8dab0659b313c9b4c86fc5825df2b66ab9a108aa75954f8f8ca2701e5ffc79`; 1/1 failed |
| M08 | `RabbitMqBusTopology.cs`; `7e2343173ab6bc6e609fe531f69ccc82c3547c6fffa53aab2016a86cfb49f56b` | typed overload host -> `new Uri("rabbitmq://localhost/")`; `02dfb1f91cc6df5c9b50a0192fe3493380994fb1e70a049f13e6d53f6e376688` | same filter as M07; 1 | `M08-topology-typed-overload.json`; `fe37b09cc78b18d8a30c7fa5e62ae11255112f9f94fca5928c54c09e6c460261`; 1/1 failed |
| M09 | `RabbitMqAddressExtensions.cs`; `ce55062321464fe3722f72c8863821e97763ea74d340919f73f854aeea74ae31` | numeric TTL `.Value` -> `.Value.ToString()`; `b9450b546dd1c3408423b846bdfcd5f55ab38fcfd2a2bb8974295b8d00725abe` | `RabbitMqAddressExtensionsTests.TimeToLive_BecomesTheExactQueueArgument`; 1 | `M09-ttl-string.json`; `01235afc560640e9895aac7675dfa55e9f1b94b46b5963a15d13d7378ab409fd`; 1/1 failed |
| M10 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | short exchange name drops `Uri.UnescapeDataString`; `324d780c22bb2df4a8245d09a07a8ec16e442021f1ffc81627ed0ac4d5b0a3fe` | `RabbitMqEndpointAddressTests.ShortAddresses_ResolveAgainstTheHost`; 3 | `M10-short-name-decoding.json`; `ce0445fbfc3949d473120d013924684731705b5bc3191a5f0d2705c55c818e73`; 1/3 failed |
| M11 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | remove direct exchange-name validation; `88c1211e8b5272ab2b9821941d4902679ad64e7db26e3aab5b5087a18a408a57` | `RabbitMqEndpointAddressTests.DirectConstruction_ValidatesEveryEntityNameInput`; 4 | `M11-direct-exchange-validation.json`; `91680ddc787ef034f5693efb7c9c930148efe278c854765556a8f4ce3fda2962`; 1/4 failed |
| M12 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | remove direct queue-name validation; `88824bf0e58c4afc584c9083fa2c52c11801c2135be47511448980d95e2779ed` | same filter as M11; 4 | `M12-direct-queue-validation.json`; `6826abb9474c7b303bbfdb9179da772bb34c5f333ade790c8f7ea88fa3a93f77`; 1/4 failed |
| M13 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | remove alternate-exchange validation; `f111d0c844ab73b589268824c7a3ffeba818d196f371bec470ac0401303fd44b` | same filter as M11; 4 | `M13-direct-alternate-validation.json`; `801c2696241a0e18dcbffda44ea1824ffa3c65c68be28fa455474878aa56ae42`; 1/4 failed |
| M14 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | remove binding-name validation; `5d04a307cf7251534f473c27f9ae271ba6df6b179e98faa0d07de7170c4d65e8` | same filter as M11; 4 | `M14-direct-binding-validation.json`; `2caaf5163e00e1fcd26e5d08563384f3ac88ae8227f2b6aec9511e21a59ef1a1`; 1/4 failed |
| M15 | `TrackedActivity.cs`; `4342740720b5ee26c6408557e45a4de1deadb06ddf950e031824a1af4209eb9e` | idle deadline `_idleTimeout` -> `_idleTimeout / 2`; `ba8f525bb407b689667d30946bdcb9334f5fd082fc91eb5d616b7e22deba5fbc` | class `TelemetryMonitorTests`; 4 | `M15-telemetry-half-idle-deadline.json`; `1631cb755ad007a06215a2dd18d16a9208b85b8de0c94b4131620b6f98e046f9`; 3/4 failed |

The RabbitMQ native executable was used for M01 through M14; the core native executable was used
for M15. After every run the mutation was reversed with the exact inverse edit. The isolated
worktree finally reported an empty `git status --short`, `git diff --check` passed, and its commit and
tree were exactly the final executable values for M15.
