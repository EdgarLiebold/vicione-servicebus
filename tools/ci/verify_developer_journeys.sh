#!/usr/bin/env bash
set -euo pipefail

# Compiles the documented journeys and isolated package consumers against freshly packed packages.

update_lock=false
update_public_api_contract=false
for argument in "$@"; do
  case "$argument" in
    --update-lock)
      update_lock=true
      ;;
    --update-public-api-contract)
      update_public_api_contract=true
      ;;
    *)
      printf 'Unknown argument: %s\n' "$argument" >&2
      exit 2
      ;;
  esac
done

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_dir/../.." && pwd)"
dotnet_cli="${DOTNET_CLI:-dotnet}"
build_server_arguments=(--disable-build-servers -m:1)
temporary_root="$(mktemp -d "${TMPDIR:-/tmp}/vicione-developer-journeys.XXXXXX")"
package_feed="$temporary_root/packages"
global_packages="$temporary_root/global-packages"
temporary_lock_root="$temporary_root/locks"
nuget_config="$temporary_root/NuGet.config"
sample_project="$repository_root/samples/DeveloperJourneys/ViciOne.ServiceBus.Samples.DeveloperJourneys.csproj"
public_api_consumer_project="$repository_root/samples/PackageConsumers/PublicApiBaseline/ViciOne.ServiceBus.Samples.PublicApiBaselinePackageConsumer.csproj"
isolated_consumer_projects=(
  "$repository_root/samples/PackageConsumers/AzureServiceBusTesting/ViciOne.ServiceBus.Samples.AzureServiceBusTestingPackageConsumer.csproj"
  "$repository_root/samples/PackageConsumers/EventHubsTesting/ViciOne.ServiceBus.Samples.EventHubsTestingPackageConsumer.csproj"
  "$repository_root/samples/PackageConsumers/RabbitMqTesting/ViciOne.ServiceBus.Samples.RabbitMqTestingPackageConsumer.csproj"
)
public_api_contract="${PUBLIC_API_CONTRACT_OUTPUT:-$repository_root/artifacts/verification/public-api-contract.txt}"
committed_public_api_contract="$repository_root/docs/api/packed-public-api.txt"

journey_count="$(find "$repository_root/samples/DeveloperJourneys" -maxdepth 1 -type f -name 'Journey*.cs' | wc -l | tr -d '[:space:]')"
if [[ "$journey_count" != "18" ]]; then
  printf 'Expected 18 developer journeys, found %s.\n' "$journey_count" >&2
  exit 1
fi

cleanup() {
  rm -rf "$temporary_root"
}
trap cleanup EXIT

mkdir -p "$package_feed" "$global_packages" "$temporary_lock_root"

printf '%s\n' \
  '<?xml version="1.0" encoding="utf-8"?>' \
  '<configuration>' \
  '  <packageSources>' \
  '    <clear />' \
  "    <add key=\"fresh-vicione\" value=\"$package_feed\" />" \
  '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />' \
  '  </packageSources>' \
  '  <disabledPackageSources><clear /></disabledPackageSources>' \
  '</configuration>' > "$nuget_config"

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/private/tmp/dotnet-home}"
export DOTNET_MULTILEVEL_LOOKUP=0
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1
export MSBUILDDISABLENODEREUSE=1
export NUGET_PACKAGES="$global_packages"

restore_package_consumer() {
  local project="$1"
  local project_file="${project##*/}"
  local project_name="${project_file%.csproj}"
  local tracked_lock="${project%/*}/packages.lock.json"
  local transient_lock="$temporary_lock_root/$project_name.packages.lock.json"
  local restore_arguments=(
    restore "$project"
    --configfile "$nuget_config"
    --force-evaluate
  )

  if $update_lock; then
    restore_arguments+=("-p:RestoreLockedMode=false")
  else
    test -f "$tracked_lock"
    cp "$tracked_lock" "$transient_lock"
    restore_arguments+=(
      "-p:NuGetLockFilePath=$transient_lock"
      "-p:RestoreLockedMode=true"
    )
  fi

  "$dotnet_cli" "${restore_arguments[@]}"
}

testing_package_projects=(
  "$repository_root/src/ViciOne.ServiceBus.Testing/ViciOne.ServiceBus.Testing.csproj"
  "$repository_root/src/Transports/ViciOne.ServiceBus.AzureServiceBus.Testing/ViciOne.ServiceBus.AzureServiceBus.Testing.csproj"
  "$repository_root/src/Transports/ViciOne.ServiceBus.EventHubs.Testing/ViciOne.ServiceBus.EventHubs.Testing.csproj"
  "$repository_root/src/Transports/ViciOne.ServiceBus.RabbitMq.Testing/ViciOne.ServiceBus.RabbitMq.Testing.csproj"
)

"$dotnet_cli" pack "$repository_root/ViciOne.ServiceBus.slnx" \
  --configuration Release \
  --no-restore \
  "${build_server_arguments[@]}" \
  --output "$package_feed" \
  -p:ContinuousIntegrationBuild=true

for project in "${testing_package_projects[@]}"; do
  "$dotnet_cli" pack "$project" \
    --configuration Release \
    --no-restore \
    "${build_server_arguments[@]}" \
    --output "$package_feed" \
    -p:ContinuousIntegrationBuild=true
done

expected_packages=(
  "ViciOne.ServiceBus.Abstractions.1.0.0.nupkg"
  "ViciOne.ServiceBus.1.0.0.nupkg"
  "ViciOne.ServiceBus.ActiveMq.1.0.0.nupkg"
  "ViciOne.ServiceBus.AmazonS3.1.0.0.nupkg"
  "ViciOne.ServiceBus.AmazonSqs.1.0.0.nupkg"
  "ViciOne.ServiceBus.Analyzers.1.0.0.nupkg"
  "ViciOne.ServiceBus.Azure.Storage.1.0.0.nupkg"
  "ViciOne.ServiceBus.Azure.Table.1.0.0.nupkg"
  "ViciOne.ServiceBus.AzureServiceBus.1.0.0.nupkg"
  "ViciOne.ServiceBus.AzureServiceBus.Testing.1.0.0.nupkg"
  "ViciOne.ServiceBus.Sagas.1.0.0.nupkg"
  "ViciOne.ServiceBus.Courier.1.0.0.nupkg"
  "ViciOne.ServiceBus.DynamoDb.1.0.0.nupkg"
  "ViciOne.ServiceBus.EntityFrameworkCore.1.0.0.nupkg"
  "ViciOne.ServiceBus.EntityFrameworkCore.Sagas.1.0.0.nupkg"
  "ViciOne.ServiceBus.EventHubs.1.0.0.nupkg"
  "ViciOne.ServiceBus.EventHubs.Testing.1.0.0.nupkg"
  "ViciOne.ServiceBus.Futures.1.0.0.nupkg"
  "ViciOne.ServiceBus.Initializers.1.0.0.nupkg"
  "ViciOne.ServiceBus.JobService.1.0.0.nupkg"
  "ViciOne.ServiceBus.Mediator.1.0.0.nupkg"
  "ViciOne.ServiceBus.MessagePack.1.0.0.nupkg"
  "ViciOne.ServiceBus.Quartz.1.0.0.nupkg"
  "ViciOne.ServiceBus.RabbitMq.1.0.0.nupkg"
  "ViciOne.ServiceBus.RabbitMq.Testing.1.0.0.nupkg"
  "ViciOne.ServiceBus.SignalR.1.0.0.nupkg"
  "ViciOne.ServiceBus.SqlTransport.1.0.0.nupkg"
  "ViciOne.ServiceBus.SqlTransport.PostgreSql.1.0.0.nupkg"
  "ViciOne.ServiceBus.SqlTransport.SqlServer.1.0.0.nupkg"
  "ViciOne.ServiceBus.StateMachineVisualizer.1.0.0.nupkg"
  "ViciOne.ServiceBus.Testing.1.0.0.nupkg"
)

for package in "${expected_packages[@]}"; do
  test -f "$package_feed/$package"
done
actual_package_count="$(find "$package_feed" -maxdepth 1 -type f -name 'ViciOne.ServiceBus*.nupkg' | wc -l | tr -d '[:space:]')"
if [[ "$actual_package_count" != "${#expected_packages[@]}" ]]; then
  printf 'Expected exactly %s ViciOne packages, found %s.\n' "${#expected_packages[@]}" "$actual_package_count" >&2
  exit 1
fi

restore_package_consumer "$sample_project"
"$dotnet_cli" build "$sample_project" \
  --configuration Release \
  --no-restore \
  --no-incremental \
  "${build_server_arguments[@]}" \
  -p:RestoreLockedMode=true \
  -p:TreatWarningsAsErrors=true

"$dotnet_cli" run \
  --project "$sample_project" \
  --configuration Release \
  --no-build \
  --no-restore

for consumer_project in "${isolated_consumer_projects[@]}"; do
  restore_package_consumer "$consumer_project"
  "$dotnet_cli" build "$consumer_project" \
    --configuration Release \
    --no-restore \
    --no-incremental \
    "${build_server_arguments[@]}" \
    -p:RestoreLockedMode=true \
    -p:TreatWarningsAsErrors=true
  "$dotnet_cli" run \
    --project "$consumer_project" \
    --configuration Release \
    --no-build \
    --no-restore
done

restore_package_consumer "$public_api_consumer_project"
"$dotnet_cli" build "$public_api_consumer_project" \
  --configuration Release \
  --no-restore \
  --no-incremental \
  "${build_server_arguments[@]}" \
  -p:RestoreLockedMode=true \
  -p:TreatWarningsAsErrors=true

"$dotnet_cli" build "$repository_root/tools/public-api-baseline/ViciOne.ServiceBus.Build.PublicApiBaseline.csproj" \
  --configuration Release \
  --no-restore \
  "${build_server_arguments[@]}" \
  -p:RestoreLockedMode=true \
  -p:TreatWarningsAsErrors=true

"$dotnet_cli" run \
  --project "$repository_root/tools/public-api-baseline/ViciOne.ServiceBus.Build.PublicApiBaseline.csproj" \
  --configuration Release \
  --no-build \
  --no-restore \
  -- \
  "$global_packages" \
  "$package_feed" \
  "$public_api_contract"

if $update_public_api_contract; then
  mkdir -p "$(dirname "$committed_public_api_contract")"
  cp "$public_api_contract" "$committed_public_api_contract"
  printf 'Updated committed packed public API contract: %s\n' "$committed_public_api_contract"
elif [[ ! -f "$committed_public_api_contract" ]]; then
  printf 'Committed packed public API contract is missing: %s\n' "$committed_public_api_contract" >&2
  exit 1
elif ! cmp -s "$committed_public_api_contract" "$public_api_contract"; then
  printf 'Packed public API differs from the committed contract. Review the diff and update explicitly when intended.\n' >&2
  diff -u "$committed_public_api_contract" "$public_api_contract" || true
  exit 1
fi

printf 'Developer journey package-consumer gate passed: 18 scenarios, 31 freshly packed ViciOne packages, 3 isolated provider testing consumers executed, and 30 runtime package APIs match the committed baseline.\n'
