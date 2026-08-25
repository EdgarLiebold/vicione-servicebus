# RabbitMQ address mutation manifest

## Binding

- Final technical commit: `5fd962f927c7f0fe8de0d3240f2a3129449ef41b`
- Final technical tree: `d8f50b9995b8a457067396d49c7c4969acf0e9aa`
`MUTATION_RECIPES.json` is the canonical, machine-readable mutation definition. It binds the final
technical commit and, for every M01-M18 entry, the exact target, baseline hash, unique replacement,
replacement occurrence and mutant hash. Its application contract fails before execution if any
baseline, occurrence count, mutant hash or restore hash differs.

All 18 mutations were applied and executed serially in one disposable Git worktree detached at the
final technical commit. `MUTATION_EXECUTION.json` binds the exact working directory, build and native
MTP argument vectors, exit codes, raw-result hashes and counts for every run. Every recipe was also
resolved and hashed against the final technical tree before execution.

Each mutant was built serially with:

```text
env DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 /usr/local/share/dotnet/dotnet build <project> --configuration Release --no-restore --disable-build-servers /m:1 /nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false -v:quiet
```

`<project>` was
`tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj`
for M01 through M14 and M16 through M18, and
`tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj` for M15.
Every build exited `0`. Each named native MTP executable then ran with the exact filter and minimum
shown below, `--report-xunit-ctrf`, the evidence directory as `--results-directory`, and the listed
JSON as `--report-xunit-ctrf-filename`. Every test command exited `2` because the intended assertion
rejected the one mutation; no run skipped a test.

## Mutations

| ID | Target; baseline SHA-256 | Exact one-cause mutation; mutant SHA-256 | Native filter; minimum | Raw CTRF SHA-256; result |
|---|---|---|---|---|
| M01 | `RabbitMqHostAddress.cs`; `d1bf7d4410e7ea433d9a919500d6a6e0d927126b27859e504a0b29bc225e1107` | default port `5672` -> `5673`; `92ddb9516fcc118f3e186c93ff5a89c5be1ee9541d27c50952b1f9fabcf438f5` | `RabbitMqHostAddressTests.SupportedSchemes_UseTheirDefaultPortAndRoundTrip`; 4 | `M01-default-port.json`; `e63f176d75fe470b35aa29bc30c4a6712adfb8fea0a60b8214d27e9ef7b222c0`; 2/4 failed |
| M02 | `RabbitMqHostAddress.cs`; `d1bf7d4410e7ea433d9a919500d6a6e0d927126b27859e504a0b29bc225e1107` | remove `Uri.EscapeDataString` from virtual-host rendering; `25c63a6e219c182bebde88cfe7b2bb33efc6f8683672a7c425e9bf1c4295a82e` | `RabbitMqHostAddressTests.EncodedVirtualHost_IsDecodedInMemoryAndEncodedInTheUri`; 1 | `M02-virtual-host-encoding.json`; `9d74d3f8e3b987b7ca7cf3b854dcd851dd9bc5a4ede0f1e15786e92383550a2c`; 1/1 failed |
| M03 | `RabbitMqAddressExtensions.cs`; `ce55062321464fe3722f72c8863821e97763ea74d340919f73f854aeea74ae31` | credential delimiter `IndexOf(':')` -> `LastIndexOf(':')`; `c9676b414f900f42151882808a1f89ae68bbe22d93f75c88d36a9830dadc4435` | `RabbitMqAddressExtensionsTests.Credentials_AreDecodedWithoutTruncatingThePassword`; 4 | `M03-credential-suffix.json`; `c315b5d96c3dcc375fab1244a1363ecdb8f86ed6c85d40123f8593d3034b599b`; 1/4 failed |
| M04 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | return caller-owned `IReadOnlyList<string>` before validation/copy; `6a3ef235d28eb5d57c9de25892e22676507e1fe1db23659e22321d1547472e75` | `RabbitMqEndpointAddressTests.DirectConstruction_OwnsItsBindingSnapshot`; 1 | `M04-binding-snapshot.json`; `bd5242fed6cc54530782fcb6830ebfd5af60d16050a2539f7cfa77dae2e98633`; 1/1 failed |
| M05 | `RabbitMqAddressRequirements.json`; `e0a0f23c740355db3035a5aa6bd45d539a3705ad5b6a382053f0a3e14d1cc9f7` | remove `host-settings-port-and-root-virtual-host`; `272aa73a2ccd6f9cb1a95785435da539b3233c50b78c0c4384c01e2575ea8b98` | `RequirementCoverageProjectionTests.RabbitMqAddressRequirements_MatchCompiledRequirementMetadata`; 1 | `M05-requirement-projection-omission.json`; `12250236c57392f954cf0327e3a378af0ed080c49da7ad800bd64b85a78aef31`; 1/1 failed |
| M06 | `QueryStringExtensions.cs`; `a5247757895020de752152377a9a777685d9fbfaad5678d84720e0c05041a01b` | value remainder -> `element[(separator + 1)..].Split('=')[0]`; `6721d2eb4043e4714c104a976981d1c5193f6818bb8ef0a23f4ec3a6ddcbc5db` | `RabbitMqEndpointAddressTests.QueryValues_PreserveRawAndEncodedSeparators`; 2 | `M06-query-truncation.json`; `2ab943c737250da0f7eda70a7fd43e1a2c3cddc10619fcdb9370a2ab9b1d5245`; 1/2 failed |
| M07 | `RabbitMqBusTopology.cs`; `8e2187c845a038e5e7757eaa98ac348ce8b727ff907f44ead0d97aeaaa6fcfa0` | named overload host -> `new Uri("rabbitmq://localhost/")`; `f8d16f2d9522ef19954f2027ea9e2adddb933e707238af3dd860ea85e21a6547` | `RabbitMqBusTopologyTests.DestinationAddresses_UseTheFinalConfiguredHostForBothPublicOverloads`; 1 | `M07-topology-named-overload.json`; `72a1cff5d40f004d81ba1b2e051ac082a7d98269f963c14e641ed9f5bf7df18a`; 1/1 failed |
| M08 | `RabbitMqBusTopology.cs`; `8e2187c845a038e5e7757eaa98ac348ce8b727ff907f44ead0d97aeaaa6fcfa0` | typed overload host -> `new Uri("rabbitmq://localhost/")`; `6c4e7e92b027de77cdb1f0032ad2e636784f5779a15b96cecf1746c0376e7a86` | same filter as M07; 1 | `M08-topology-typed-overload.json`; `8dad087346cc78a4ecd9fc69b3d6f57284dec6005d75af37eacde95b6f94f9c9`; 1/1 failed |
| M09 | `RabbitMqAddressExtensions.cs`; `ce55062321464fe3722f72c8863821e97763ea74d340919f73f854aeea74ae31` | numeric TTL `.Value` -> `.Value.ToString()`; `b9450b546dd1c3408423b846bdfcd5f55ab38fcfd2a2bb8974295b8d00725abe` | `RabbitMqAddressExtensionsTests.TimeToLive_BecomesTheExactQueueArgument`; 1 | `M09-ttl-string.json`; `3b2614aaa49ebc56c3dfd4e58dc1ff1d99a9f242e4de3eb468417d2a9eccacbc`; 1/1 failed |
| M10 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | short exchange name drops `Uri.UnescapeDataString`; `324d780c22bb2df4a8245d09a07a8ec16e442021f1ffc81627ed0ac4d5b0a3fe` | `RabbitMqEndpointAddressTests.ShortAddresses_ResolveAgainstTheHost`; 4 | `M10-short-name-decoding.json`; `072e2da5b47c04e741963d9dc618bfaf8e998a5816adca37a2716546c9faa1af`; 1/4 failed |
| M11 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | remove direct exchange-name validation; `88c1211e8b5272ab2b9821941d4902679ad64e7db26e3aab5b5087a18a408a57` | `RabbitMqEndpointAddressTests.DirectConstruction_ValidatesEveryEntityNameInput`; 4 | `M11-direct-exchange-validation.json`; `9292362f2b832f4b23e52f4f95cdd0447cab716d41b4ce577cef233f12efa15e`; 1/4 failed |
| M12 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | remove direct queue-name validation; `88824bf0e58c4afc584c9083fa2c52c11801c2135be47511448980d95e2779ed` | same filter as M11; 4 | `M12-direct-queue-validation.json`; `5937b1a2f10970731b229bd22326593606aa573297365a4da9341b6d8f1888b9`; 1/4 failed |
| M13 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | remove alternate-exchange validation; `17e51ecd180bfc90bc0c5bc7ba326126bc4a704ec9a05013eec5462bb19a5303` | same filter as M11; 4 | `M13-direct-alternate-validation.json`; `9d6c9e1ae0d4699361e2168b230411e5ee73bfe8b8a86f2c22c529a808d3e9d6`; 1/4 failed |
| M14 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | remove binding-name validation; `fda6bbed0b660c3036d944471a346a0559c32f8104641f29ea4a9e27490880de` | same filter as M11; 4 | `M14-direct-binding-validation.json`; `456e0bcab442eaba65a97bc054d60f3e39ce8d84e9e151fa92226ae413548967`; 1/4 failed |
| M15 | `TrackedActivity.cs`; `4342740720b5ee26c6408557e45a4de1deadb06ddf950e031824a1af4209eb9e` | idle deadline `_idleTimeout` -> `_idleTimeout / 2`; `ba8f525bb407b689667d30946bdcb9334f5fd082fc91eb5d616b7e22deba5fbc` | class `TelemetryMonitorTests`; 4 | `M15-telemetry-half-idle-deadline.json`; `eaede274477dbd420fd690cb81645cbae8fce782653a982c482cf2c8cae80bca`; 3/4 failed |
| M16 | `RabbitMqBusTopology.cs`; `8e2187c845a038e5e7757eaa98ac348ce8b727ff907f44ead0d97aeaaa6fcfa0` | named exchange data constructor -> interpolated short URI; `9db7a3f5b607a84343c308654af7e72362b63daa767a64fb7edf42f426e45e69` | `RabbitMqBusTopologyTests.NamedDestination_RejectsUriSyntaxInsteadOfInterpretingItAsConfiguration`; 1 | `M16-topology-name-injection.json`; `0afbde04d32212c22ea78e933f9bb43a31f12a37396b11f2ec676ece411b5b86`; 1/1 failed |
| M17 | `RabbitMqEndpointAddress.cs`; `42266ca567681d1150c572878574f05dfbab43383046c92f04990ca419b557c9` | short queue name drops `Uri.UnescapeDataString`; `4a3b2609afbcb04efd9a02704df0f22b9620b17eb0a504382b443b47fdb3b03f` | `RabbitMqEndpointAddressTests.ShortAddresses_ResolveAgainstTheHost`; 4 | `M17-short-queue-name-decoding.json`; `e74cb7f5f5e76ca8db2fe652bf2f88353f11f3a1e18404c79ea581417a12ed0d`; 1/4 failed |
| M18 | `RabbitMqBusTopology.cs`; `8e2187c845a038e5e7757eaa98ac348ce8b727ff907f44ead0d97aeaaa6fcfa0` | typed exchange data constructor -> interpolated short URI; `a9d674b6a061839e9aaeea0e1626e3bc852f5b8e1190cb177b0bb9e8eaf770e4` | same filter as M16; 1 | `M18-topology-formatted-name-injection.json`; `f4f8e7607153cb9ec83737476b768768d1475da626c186fbccd5c2bbe68e4c70`; 1/1 failed |

The RabbitMQ native executable was used for M01 through M14 and M16 through M18; the core native
executable was used for M15. After every run the mutation was reversed with the exact inverse edit.
The canonical recipes were independently applied in memory to the final technical tree: all 18
baseline hashes, occurrence counts and computed mutant hashes matched exactly.
