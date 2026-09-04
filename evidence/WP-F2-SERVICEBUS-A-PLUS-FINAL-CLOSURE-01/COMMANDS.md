# Reproduction commands

Date: 2026-09-04

The final environment used these variables for deterministic local execution:

```bash
export DOTNET_CLI_HOME=/private/tmp/vicione-sb-dotnet-home-10400
export DOTNET_ROOT=/private/tmp/vicione-dotnet-10-current
export DOTNET_MULTILEVEL_LOOKUP=0
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1
export MSBUILDDISABLENODEREUSE=1
export NUGET_PACKAGES=/Users/edgar.liebold/.nuget/packages
DOTNET=/private/tmp/vicione-dotnet-10-current/dotnet
```

Core validation:

```bash
$DOTNET --info
$DOTNET restore ViciOne.ServiceBus.Engineering.slnx --locked-mode --disable-parallel
$DOTNET restore ViciOne.ServiceBus.slnx --locked-mode --disable-parallel
$DOTNET build ViciOne.ServiceBus.slnx -c Release --no-restore --disable-build-servers -p:ContinuousIntegrationBuild=true
$DOTNET build ViciOne.ServiceBus.Engineering.slnx -c Release --no-restore --disable-build-servers -p:ContinuousIntegrationBuild=true
$DOTNET test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-restore \
  --results-directory artifacts/test-results/unit --minimum-expected-tests 3490 \
  --max-parallel-test-modules 1
DOTNET_CLI=$DOTNET tools/ci/verify_developer_journeys.sh --update-lock
```

Real RabbitMQ acceptance:

```bash
python3 tools/ci/run_broker_category.py --broker rabbitmq --command -- \
  $DOTNET test --solution ViciOne.ServiceBus.Tests.RabbitMqLocalIntegration.slnx \
  -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/rabbitmq-local-integration \
  --minimum-expected-tests 27 --max-parallel-test-modules 1
```

Dependency audits:

```bash
$DOTNET package list --project ViciOne.ServiceBus.Engineering.slnx --outdated --format json --output-version 1
$DOTNET package list --project ViciOne.ServiceBus.Engineering.slnx --vulnerable --include-transitive --format json --output-version 1
$DOTNET package list --project ViciOne.ServiceBus.Engineering.slnx --deprecated --include-transitive --format json --output-version 1
```

The mutation commands used the focused classes documented in `MUTATION_VALIDATION.md`; each mutation
was applied and reverted separately. The real-provider mutations used a fresh RabbitMQ container and
the repository's canonical broker runner.
