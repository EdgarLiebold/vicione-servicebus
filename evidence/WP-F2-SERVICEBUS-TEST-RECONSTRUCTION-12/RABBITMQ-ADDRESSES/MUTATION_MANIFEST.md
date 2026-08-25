# RabbitMQ address mutation manifest

## Binding

- Technical commit: `a3ce3c697634234242473b6428d5e292901985eb`
- Technical tree: `9d02b5144c9ad246e641f7c81e41393f00124ad1`
- Every command was executed from the repository root.
- `/usr/bin/script -q -e` captured standard output and standard error together and propagated the
  child exit code.
- Every mutation exited with code `2` because the named test rejected that single change.
- Every target was restored immediately afterward; its final hash equals its baseline hash.

The exact environment prefix after each capture path was:

```text
env DOTNET_ROOT=/usr/local/share/dotnet DOTNET_CLI_HOME=/private/tmp/vsb-dotnet-cli-home DOTNET_GENERATE_ASPNET_CERTIFICATE=false DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1 DOTNET_MULTILEVEL_LOOKUP=0 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 /usr/local/share/dotnet/dotnet test
```

## M01 — default AMQP port

- Target: `src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqHostAddress.cs`
- Baseline/post-restore SHA-256: `d1bf7d4410e7ea433d9a919500d6a6e0d927126b27859e504a0b29bc225e1107`
- Mutation: the `rabbitmq`/`amqp` default changes from `5672` to `5673`.
- Mutant SHA-256: `92ddb9516fcc118f3e186c93ff5a89c5be1ee9541d27c50952b1f9fabcf438f5`
- Command suffix: `/bl:/private/tmp/vsb-rabbit-m01.binlog --project tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqHostAddressTests.SupportedSchemes_UseTheirDefaultPortAndRoundTrip --minimum-expected-tests 4 --max-parallel-test-modules 1`
- Raw result: `mutations/M01-default-port.txt`; 2/4 cases failed on exact port equality.

## M02 — encoded virtual-host ownership

- Target: `src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqHostAddress.cs`
- Baseline/post-restore SHA-256: `d1bf7d4410e7ea433d9a919500d6a6e0d927126b27859e504a0b29bc225e1107`
- Mutation: render the decoded virtual host without `Uri.EscapeDataString`.
- Mutant SHA-256: `25c63a6e219c182bebde88cfe7b2bb33efc6f8683672a7c425e9bf1c4295a82e`
- Command suffix: `/bl:/private/tmp/vsb-rabbit-m02.binlog --project tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqHostAddressTests.EncodedVirtualHost_IsDecodedInMemoryAndEncodedInTheUri --minimum-expected-tests 1 --max-parallel-test-modules 1`
- Raw result: `mutations/M02-virtual-host-encoding.txt`; the encoded slash became a path separator.

## M03 — direct-constructor name validation

- Target: `src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqEndpointAddress.cs`
- Baseline/post-restore SHA-256: `eb63a0d960e3fef9a77cc08468f1bd213a37eaeda561bffd143266d74e6e2941`
- Mutation: remove `ValidateEntityName(exchangeName)` from the public direct constructor.
- Mutant SHA-256: `43044cfc6ee3b76e69aa7760d7c7828f4686a646175f6e36821399672288813d`
- Command suffix: `/bl:/private/tmp/vsb-rabbit-m03.binlog --project tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqEndpointAddressTests.DirectConstruction_ValidatesEntityNames --minimum-expected-tests 1 --max-parallel-test-modules 1`
- Raw result: `mutations/M03-direct-name-validation.txt`; the expected address exception disappeared.

## M04 — credential suffix preservation

- Target: `src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqTransport/RabbitMqAddressExtensions.cs`
- Baseline/post-restore SHA-256: `17107918bca9ce3ba335251be5ad95360a9375cc8842551bab5c997283bd0abc`
- Mutation: split user information at the last colon instead of the first.
- Mutant SHA-256: `767ddb60c443ec489f8ce080ea48fff7870bcc5285a816f5128e4ff457d326f3`
- Command suffix: `/bl:/private/tmp/vsb-rabbit-m04.binlog --project tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqTransport.RabbitMqAddressExtensionsTests.Credentials_AreDecodedWithoutTruncatingThePassword --minimum-expected-tests 4 --max-parallel-test-modules 1`
- Raw result: `mutations/M04-credential-suffix.txt`; the password prefix leaked into the username.

## M05 — TTL option mapping

- Target: `src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqTransport/RabbitMqAddressExtensions.cs`
- Baseline/post-restore SHA-256: `17107918bca9ce3ba335251be5ad95360a9375cc8842551bab5c997283bd0abc`
- Mutation: map TTL to `x-message-ttl-invalid` instead of RabbitMQ's `x-message-ttl`.
- Mutant SHA-256: `a53a8c3ce13ae7038d013ecaf47f8000ac97101d1b6c9d008d56fe0e0df62a41`
- Command suffix: `/bl:/private/tmp/vsb-rabbit-m05.binlog --project tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqTransport.RabbitMqAddressExtensionsTests.TimeToLive_BecomesTheExactQueueArgument --minimum-expected-tests 1 --max-parallel-test-modules 1`
- Raw result: `mutations/M05-ttl-option-mapping.txt`; the required queue argument was absent.

## M06 — caller-owned binding isolation

- Target: `src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqEndpointAddress.cs`
- Baseline/post-restore SHA-256: `eb63a0d960e3fef9a77cc08468f1bd213a37eaeda561bffd143266d74e6e2941`
- Mutation: return a caller-supplied `IReadOnlyList<string>` instead of copying it.
- Mutant SHA-256: `238284e0afe6d3bed924bef937cfba6510b7405bf77a49d828cc6a593195e858`
- Command suffix: `/bl:/private/tmp/vsb-rabbit-m06.binlog --project tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqEndpointAddressTests.DirectConstruction_OwnsItsBindingSnapshot --minimum-expected-tests 1 --max-parallel-test-modules 1`
- Raw result: `mutations/M06-binding-snapshot.txt`; later caller mutation changed the address value.

## M07 — requirement-projection omission sabotage

M07 is a test-contract sabotage, not a product mutation.

- Target: `tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/Requirements/RabbitMqAddressRequirements.json`
- Baseline/post-restore SHA-256: `db2323ee8c1e8d61c380aa103b531e4c82473fda286c1b958f86b0b2ecaa2752`
- Mutation: remove the projection row for `host-settings-port-and-root-virtual-host` while leaving the compiled attributed test intact.
- Mutant SHA-256: `a68fa60625aa76eabd95afffd1e7b3fab3f488841704442eeda93e165869872f`
- Command suffix: `/bl:/private/tmp/vsb-rabbit-m07.binlog --project tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.RabbitMqTransport.Tests.Requirements.RequirementCoverageProjectionTests.RabbitMqAddressRequirements_MatchCompiledRequirementMetadata --minimum-expected-tests 1 --max-parallel-test-modules 1`
- Raw result: `mutations/M07-requirement-projection-omission.txt`; the compiled test was reported as missing from the projection.
