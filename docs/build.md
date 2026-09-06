# Build, test, package, and deploy

ViciOne.ServiceBus requires a current stable .NET 10 SDK (`10.0.x`). `NuGet.config` defines the
package sources, and every project resolves through a tracked lock file.

## Build targets

| Target | Contents |
|---|---|
| `ViciOne.ServiceBus.slnx` | Product libraries and shipping packages |
| `ViciOne.ServiceBus.Engineering.slnx` | Product, tools, benchmarks, samples, and all test projects |
| `ViciOne.ServiceBus.Tests.Unit.slnx` | Hermetic unit and architecture tests |
| `ViciOne.ServiceBus.Tests.LocalIntegration.slnx` | PostgreSQL, Azurite, LocalStack, ActiveMQ, Artemis, and Event Hubs tests |
| `ViciOne.ServiceBus.Tests.SqlServerLocalIntegration.slnx` | Isolated SQL Server tests |
| `ViciOne.ServiceBus.Tests.AzureServiceBusLocalIntegration.slnx` | Official Azure Service Bus emulator tests |
| `ViciOne.ServiceBus.Tests.RabbitMqLocalIntegration.slnx` | Real RabbitMQ acceptance tests |

## Locked restore and strict build

Run these commands from the repository root:

```bash
dotnet restore ViciOne.ServiceBus.slnx --locked-mode
dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode

dotnet build ViciOne.ServiceBus.slnx -c Release --no-restore -warnaserror
dotnet build ViciOne.ServiceBus.Engineering.slnx -c Release --no-restore -warnaserror
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-restore -warnaserror
```

Do not combine `--no-incremental` with solution builds. Product projects are both solution members
and dependencies of other members; a normal build preserves the correct dependency graph.

## Formatting

```bash
dotnet format --verify-no-changes ViciOne.ServiceBus.Engineering.slnx
dotnet format --verify-no-changes ViciOne.ServiceBus.Tests.Unit.slnx
```

## Hermetic unit profile

The process exit code is the verdict. The checked-in MTP configuration makes warnings and skipped
tests fail.

```bash
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx \
  -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/unit --minimum-expected-tests 3818 \
  --max-parallel-test-modules 1
```

## Packages and compile-tested examples

```bash
dotnet pack ViciOne.ServiceBus.slnx -c Release --no-build --no-restore
tools/ci/verify_developer_journeys.sh
```

The second command creates a temporary feed, packs all thirty delivery packages, restores the
package-only samples, and compiles all eighteen journeys plus three isolated provider-testing
consumers with warnings as errors. A dedicated package-only API consumer restores all twenty-nine
runtime packages. The gate reflects their complete public surface, writes the deterministic
inventory under `artifacts/verification`, and compares it byte-for-byte with the committed
`docs/api/packed-public-api.txt` contract.

After an intentional API change, update the contract explicitly and inspect its diff:

```bash
tools/ci/verify_developer_journeys.sh --update-public-api-contract
git diff -- docs/api/packed-public-api.txt
```

### macOS `protoc` startup troubleshooting

If a macOS x64 build remains in `ProtoCompile` and the `Grpc.Tools` `protoc` process remains in
uninterruptible state, copy the resolved compiler from the NuGet cache to a temporary executable,
ad-hoc sign that copy, and select it explicitly:

```bash
mkdir -p /private/tmp/vicione-servicebus-protoc
cp ~/.nuget/packages/grpc.tools/2.83.0/tools/macosx_x64/protoc \
  /private/tmp/vicione-servicebus-protoc/protoc
chmod 755 /private/tmp/vicione-servicebus-protoc/protoc
codesign --force --sign - /private/tmp/vicione-servicebus-protoc/protoc
PROTOBUF_PROTOC=/private/tmp/vicione-servicebus-protoc/protoc \
  dotnet build ViciOne.ServiceBus.Engineering.slnx -c Release --no-restore -warnaserror
```

This workaround changes neither the tracked dependency nor the repository. Replace `2.83.0` when
the centrally managed `Grpc.Tools` version changes.

## Provider-backed profiles

Docker must be running, the pinned images in `build/test-infrastructure` must be available, and the
selected ports must be free. Each command starts isolated resources, runs the named MTP profile,
captures provider logs, and removes its resources. An unavailable provider is a failed or unexecuted
profile, never a successful fallback.

```bash
VICIONE_TESTS__Profile=LocalIntegration \
python3 tools/ci/run_broker_category.py \
  --broker postgres --broker azurite --broker localstack \
  --broker activemq --broker artemis --broker eventhubs \
  --allow-broker-outage activemq --command -- \
  dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx \
    -c Release --no-build --no-restore \
    --results-directory artifacts/test-results/local-integration \
    --minimum-expected-tests 326 --max-parallel-test-modules 1

VICIONE_TESTS__Profile=LocalIntegration \
python3 tools/ci/run_broker_category.py --broker postgres --broker mssql --command -- \
  dotnet test --solution ViciOne.ServiceBus.Tests.SqlServerLocalIntegration.slnx \
    -c Release --no-build --no-restore \
    --results-directory artifacts/test-results/sqlserver-local-integration \
    --minimum-expected-tests 60 --max-parallel-test-modules 1

VICIONE_TESTS__Profile=LocalIntegration \
python3 tools/ci/run_broker_category.py --broker servicebus --command -- \
  dotnet test --solution ViciOne.ServiceBus.Tests.AzureServiceBusLocalIntegration.slnx \
    -c Release --no-build --no-restore \
    --results-directory artifacts/test-results/azure-servicebus-local-integration \
    --minimum-expected-tests 24 --max-parallel-test-modules 1

VICIONE_TESTS__Profile=LocalIntegration \
python3 tools/ci/run_broker_category.py --broker rabbitmq --command -- \
  dotnet test --solution ViciOne.ServiceBus.Tests.RabbitMqLocalIntegration.slnx \
    -c Release --no-build --no-restore \
    --results-directory artifacts/test-results/rabbitmq-local-integration \
    --minimum-expected-tests 27 --max-parallel-test-modules 1
```

Restore and build the selected provider solution before running its command, using the same
`--locked-mode`, `-c Release`, `--no-restore`, and `-warnaserror` forms shown above.

## Updating dependencies

Only an intentional version change may rewrite lock files:

```bash
dotnet restore ViciOne.ServiceBus.slnx -p:RestoreLockedMode=false --force-evaluate
dotnet restore ViciOne.ServiceBus.Engineering.slnx -p:RestoreLockedMode=false --force-evaluate
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx -p:RestoreLockedMode=false --force-evaluate
git diff -- '**/packages.lock.json'
```

## Deployment checks

1. Restore and publish the application from its locked package graph.
2. Apply the schema documented in [migrations/README.md](migrations/README.md) before starting any
   reliable-messaging writer.
3. Configure exactly one transport and explicit `MessageLimits` for every bus.
4. Configure the reliable store, finite capacity, delivery policy, retention, message contracts,
   and an acceptance-capable transport when durable send is enabled.
5. Start the host and require startup validation and registered health checks to succeed.
6. Export the `ViciOne.ServiceBus` meter and activity source from the application host.

All SDK output is under `artifacts/sdk`; packages are under `artifacts/packages`; test results and
provider logs are under `artifacts/test-results` and `artifacts/run-output`.
