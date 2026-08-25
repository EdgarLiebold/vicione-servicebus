# RabbitMQ address mutation manifest

## Binding

- Final technical commit: `fd0d1d1d111ba2f834cf228cf804d43e17c95063`
- Final technical tree: `0cb7bbd170328eb0d565513e6755e554c441c4bb`
`MUTATION_RECIPES.json` is the canonical, machine-readable mutation definition. It binds the final
technical commit and, for every M01-M19 entry, the exact target, baseline hash, unique replacement,
replacement occurrence and mutant hash. Its application contract fails before execution if any
baseline, occurrence count, mutant hash or restore hash differs.

All 19 mutations were applied and executed serially in one disposable Git worktree detached at the
final technical commit. `MUTATION_EXECUTION.json` binds the exact working directory, build and native
MTP argument vectors, exit codes, raw-result hashes and counts for every run. Every recipe was also
resolved and hashed against the final technical tree before execution.

Each mutant was built serially with:

```text
env DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 /usr/local/share/dotnet/dotnet build <project> --configuration Release --no-restore --disable-build-servers /m:1 /nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false -v:quiet
```

`<project>` was
`tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj`
for M01 through M14 and M16 through M19, and
`tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj` for M15.
Every build exited `0`. Each named native MTP executable then ran with the exact filter and minimum
shown below, `--report-xunit-ctrf`, the evidence directory as `--results-directory`, and the listed
JSON as `--report-xunit-ctrf-filename`. Every test command exited `2` because the intended assertion
rejected the one mutation; no run skipped a test.

## Mutations

| ID | Target; baseline SHA-256 | Exact one-cause mutation; mutant SHA-256 | Native filter; minimum | Raw CTRF SHA-256; result |
|---|---|---|---|---|
| M01 | `RabbitMqHostAddress.cs`; `d1bf7d4410e7ea433d9a919500d6a6e0d927126b27859e504a0b29bc225e1107` | default port `5672` -> `5673`; `92ddb9516fcc118f3e186c93ff5a89c5be1ee9541d27c50952b1f9fabcf438f5` | `RabbitMqHostAddressTests.SupportedSchemes_UseTheirDefaultPortAndRoundTrip`; 4 | `M01-default-port.json`; `53d2114b1a0b0e7bdb4f31c3e61367e01d0414ce9e2f226f1c52585783665e88`; 2/4 failed |
| M02 | `RabbitMqHostAddress.cs`; `d1bf7d4410e7ea433d9a919500d6a6e0d927126b27859e504a0b29bc225e1107` | remove `Uri.EscapeDataString` from virtual-host rendering; `25c63a6e219c182bebde88cfe7b2bb33efc6f8683672a7c425e9bf1c4295a82e` | `RabbitMqHostAddressTests.EncodedVirtualHost_IsDecodedInMemoryAndEncodedInTheUri`; 1 | `M02-virtual-host-encoding.json`; `1c5999bc78fbc6bff2c208dc18d371e119910dc45afd4aeaf1e4a24d35397e1f`; 1/1 failed |
| M03 | `RabbitMqAddressExtensions.cs`; `ce55062321464fe3722f72c8863821e97763ea74d340919f73f854aeea74ae31` | credential delimiter `IndexOf(':')` -> `LastIndexOf(':')`; `c9676b414f900f42151882808a1f89ae68bbe22d93f75c88d36a9830dadc4435` | `RabbitMqAddressExtensionsTests.Credentials_AreDecodedWithoutTruncatingThePassword`; 4 | `M03-credential-suffix.json`; `7108d384745fba248aa2f0e2a9c6e6526ab0981cede6d20246a59a669d3a7ab5`; 1/4 failed |
| M04 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | return caller-owned `IReadOnlyList<string>` before validation/copy; `6a3ef235d28eb5d57c9de25892e22676507e1fe1db23659e22321d1547472e75` | `RabbitMqEndpointAddressTests.DirectConstruction_OwnsItsBindingSnapshot`; 1 | `M04-binding-snapshot.json`; `3c54981f657ce712ca32af67e1bf207fbdd5ce1caf11a7ac26feb247195e7bf5`; 1/1 failed |
| M05 | `RabbitMqAddressRequirements.json`; `acb857aaee87cebedbc4d3fd099ffec422919b619074cdd96d311bef6456914a` | remove `host-settings-port-and-root-virtual-host`; `a1f09dcb4e8f66f3d817e27f2f94a8b7616f1c7f4a9465fa95fbf120fb86b058` | `RequirementCoverageProjectionTests.RabbitMqAddressRequirements_MatchCompiledRequirementMetadata`; 1 | `M05-requirement-projection-omission.json`; `62278526940449b250cfd1e922c9b819828044df54bd8ac993500d39e4505303`; 1/1 failed |
| M06 | `QueryStringExtensions.cs`; `a5247757895020de752152377a9a777685d9fbfaad5678d84720e0c05041a01b` | value remainder -> `element[(separator + 1)..].Split('=')[0]`; `6721d2eb4043e4714c104a976981d1c5193f6818bb8ef0a23f4ec3a6ddcbc5db` | `RabbitMqEndpointAddressTests.QueryValues_PreserveRawAndEncodedSeparators`; 2 | `M06-query-truncation.json`; `37e963f234c6b89321fbf998e24e7f786afa057f5f601bbd2187de6f9342e753`; 1/2 failed |
| M07 | `RabbitMqBusTopology.cs`; `8e2187c845a038e5e7757eaa98ac348ce8b727ff907f44ead0d97aeaaa6fcfa0` | named overload host -> `new Uri("rabbitmq://localhost/")`; `f8d16f2d9522ef19954f2027ea9e2adddb933e707238af3dd860ea85e21a6547` | `RabbitMqBusTopologyTests.DestinationAddresses_UseTheFinalConfiguredHostForBothPublicOverloads`; 1 | `M07-topology-named-overload.json`; `e1f4d8837033589e2c304606fdaa6e631f635a7e5224e6f43b412db6c4b86aee`; 1/1 failed |
| M08 | `RabbitMqBusTopology.cs`; `8e2187c845a038e5e7757eaa98ac348ce8b727ff907f44ead0d97aeaaa6fcfa0` | typed overload host -> `new Uri("rabbitmq://localhost/")`; `6c4e7e92b027de77cdb1f0032ad2e636784f5779a15b96cecf1746c0376e7a86` | same filter as M07; 1 | `M08-topology-typed-overload.json`; `9027294ea5627cacf7c7942a8b0bc9081fc6f7c666104db547c2b3d03a9f91e2`; 1/1 failed |
| M09 | `RabbitMqAddressExtensions.cs`; `ce55062321464fe3722f72c8863821e97763ea74d340919f73f854aeea74ae31` | numeric TTL `.Value` -> `.Value.ToString()`; `b9450b546dd1c3408423b846bdfcd5f55ab38fcfd2a2bb8974295b8d00725abe` | `RabbitMqAddressExtensionsTests.TimeToLive_BecomesTheExactQueueArgument`; 1 | `M09-ttl-string.json`; `1165090f6d0ab2e6cc2094bd599523b931205709c9d6804ac9b4c037b8a080d8`; 1/1 failed |
| M10 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | short exchange name drops `Uri.UnescapeDataString`; `324d780c22bb2df4a8245d09a07a8ec16e442021f1ffc81627ed0ac4d5b0a3fe` | `RabbitMqEndpointAddressTests.ShortAddresses_ResolveAgainstTheHost`; 4 | `M10-short-name-decoding.json`; `f9010ee81d618bdb83d6d82551d2666b1ed11765679c5073d4f60af86bf80eb8`; 1/4 failed |
| M11 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | remove direct exchange-name validation; `88c1211e8b5272ab2b9821941d4902679ad64e7db26e3aab5b5087a18a408a57` | `RabbitMqEndpointAddressTests.DirectConstruction_ValidatesEveryEntityNameInput`; 4 | `M11-direct-exchange-validation.json`; `8db61a423d2cd473be5783f5068d0b110942f762541572e5a8012d76d198284a`; 1/4 failed |
| M12 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | remove direct queue-name validation; `88824bf0e58c4afc584c9083fa2c52c11801c2135be47511448980d95e2779ed` | same filter as M11; 4 | `M12-direct-queue-validation.json`; `9b1b4a7bff671b7d387c6707f49fa666df0cfea98bdb838ad48683672b60fe93`; 1/4 failed |
| M13 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | remove alternate-exchange validation; `17e51ecd180bfc90bc0c5bc7ba326126bc4a704ec9a05013eec5462bb19a5303` | same filter as M11; 4 | `M13-direct-alternate-validation.json`; `150a4d3ccf4b1c9358242fb072a36f59326bdd82a0f36e6414808441ec6cac0a`; 1/4 failed |
| M14 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | remove binding-name validation; `fda6bbed0b660c3036d944471a346a0559c32f8104641f29ea4a9e27490880de` | same filter as M11; 4 | `M14-direct-binding-validation.json`; `daf389e25d3432c448cecb0578eb04762030f780b0b43d6b249da3ebe73a171b`; 1/4 failed |
| M15 | `TrackedActivity.cs`; `4342740720b5ee26c6408557e45a4de1deadb06ddf950e031824a1af4209eb9e` | idle deadline `_idleTimeout` -> `_idleTimeout / 2`; `ba8f525bb407b689667d30946bdcb9334f5fd082fc91eb5d616b7e22deba5fbc` | class `TelemetryMonitorTests`; 4 | `M15-telemetry-half-idle-deadline.json`; `c867cb962d57b4ef55338ab42541c684657c1b4131342da8ff2f8415da316b3a`; 3/4 failed |
| M16 | `RabbitMqBusTopology.cs`; `8e2187c845a038e5e7757eaa98ac348ce8b727ff907f44ead0d97aeaaa6fcfa0` | named exchange data constructor -> interpolated short URI; `9db7a3f5b607a84343c308654af7e72362b63daa767a64fb7edf42f426e45e69` | `RabbitMqBusTopologyTests.NamedDestination_RejectsUriSyntaxInsteadOfInterpretingItAsConfiguration`; 1 | `M16-topology-name-injection.json`; `e69a1d6cdafdac55186c64cdcd81a67c6e1dd4cc9784ce780dbcd2d0c549145f`; 1/1 failed |
| M17 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | short queue name drops `Uri.UnescapeDataString`; `4a3b2609afbcb04efd9a02704df0f22b9620b17eb0a504382b443b47fdb3b03f` | `RabbitMqEndpointAddressTests.ShortAddresses_ResolveAgainstTheHost`; 4 | `M17-short-queue-name-decoding.json`; `bb394450abc2ef7f4304a942731120d578ba1409a22840bbe4c7ed168cb721f6`; 1/4 failed |
| M18 | `RabbitMqBusTopology.cs`; `8e2187c845a038e5e7757eaa98ac348ce8b727ff907f44ead0d97aeaaa6fcfa0` | typed exchange data constructor -> interpolated short URI; `a9d674b6a061839e9aaeea0e1626e3bc852f5b8e1190cb177b0bb9e8eaf770e4` | same filter as M16; 1 | `M18-topology-formatted-name-injection.json`; `e42c6e92ae1b7b480b10a4be630ae26d82b11d5c0b6fa74ecd86a6df5166d00c`; 1/1 failed |
| M19 | `RabbitMqBusTopology.cs`; `8e2187c845a038e5e7757eaa98ac348ce8b727ff907f44ead0d97aeaaa6fcfa0` | classify every message type as temporary; `607acd73c13c98427081f43c8c58e7072b59d263af8c2ea66d1715d2fc2a776d` | `RabbitMqBusTopologyTests.TypedDestination_PreservesTemporaryAndDurableMessageLifetimes`; 1 | `M19-topology-message-lifetime.json`; `dd129f67b38c3bfb77d1243447bb563d8cffb125308c77560c4e146744256841`; 1/1 failed |

The RabbitMQ native executable was used for M01 through M14 and M16 through M19; the core native
executable was used for M15. After every run the mutation was reversed with the exact inverse edit.
The canonical recipes were independently applied in memory to the final technical tree: all 19
baseline hashes, occurrence counts and computed mutant hashes matched exactly.
