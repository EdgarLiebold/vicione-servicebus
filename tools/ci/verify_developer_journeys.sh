#!/usr/bin/env bash
set -euo pipefail

# Retained engineering gate: compile the documented journeys only against freshly packed packages.

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_dir/../.." && pwd)"
dotnet_cli="${DOTNET_CLI:-dotnet}"
build_server_arguments=(--disable-build-servers -m:1)
temporary_root="$(mktemp -d "${TMPDIR:-/tmp}/vicione-developer-journeys.XXXXXX")"
package_feed="$temporary_root/packages"
global_packages="$temporary_root/global-packages"
nuget_config="$temporary_root/NuGet.config"
sample_project="$repository_root/samples/DeveloperJourneys/ViciOne.ServiceBus.Samples.DeveloperJourneys.csproj"
public_api_baseline="${PUBLIC_API_BASELINE_OUTPUT:-$repository_root/artifacts/verification/public-api-baseline.txt}"

journey_count="$(find "$repository_root/samples/DeveloperJourneys" -maxdepth 1 -type f -name 'Journey*.cs' | wc -l | tr -d '[:space:]')"
if [[ "$journey_count" != "18" ]]; then
  printf 'Expected 18 developer journeys, found %s.\n' "$journey_count" >&2
  exit 1
fi

cleanup() {
  rm -rf "$temporary_root"
}
trap cleanup EXIT

mkdir -p "$package_feed" "$global_packages"

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

projects=(
  "src/ViciOne.ServiceBus.Abstractions/ViciOne.ServiceBus.Abstractions.csproj"
  "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj"
  "src/ViciOne.ServiceBus.Sagas/ViciOne.ServiceBus.Sagas.csproj"
  "src/ViciOne.ServiceBus.Courier/ViciOne.ServiceBus.Courier.csproj"
  "src/ViciOne.ServiceBus.Futures/ViciOne.ServiceBus.Futures.csproj"
  "src/ViciOne.ServiceBus.JobService/ViciOne.ServiceBus.JobService.csproj"
  "src/ViciOne.ServiceBus.Mediator/ViciOne.ServiceBus.Mediator.csproj"
  "src/ViciOne.ServiceBus.Initializers/ViciOne.ServiceBus.Initializers.csproj"
  "src/Transports/ViciOne.ServiceBus.RabbitMq/ViciOne.ServiceBus.RabbitMq.csproj"
  "src/Transports/ViciOne.ServiceBus.AzureServiceBus/ViciOne.ServiceBus.AzureServiceBus.csproj"
  "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ViciOne.ServiceBus.EntityFrameworkCore.csproj"
  "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.Sagas/ViciOne.ServiceBus.EntityFrameworkCore.Sagas.csproj"
  "src/Scheduling/ViciOne.ServiceBus.Quartz/ViciOne.ServiceBus.Quartz.csproj"
  "src/ViciOne.ServiceBus.MessagePack/ViciOne.ServiceBus.MessagePack.csproj"
  "src/ViciOne.ServiceBus.Testing/ViciOne.ServiceBus.Testing.csproj"
)

for project in "${projects[@]}"; do
  "$dotnet_cli" pack "$repository_root/$project" \
    --configuration Release \
    --no-restore \
    "${build_server_arguments[@]}" \
    --output "$package_feed" \
    -p:ContinuousIntegrationBuild=true
done

expected_packages=(
  "ViciOne.ServiceBus.Abstractions.1.0.0.nupkg"
  "ViciOne.ServiceBus.1.0.0.nupkg"
  "ViciOne.ServiceBus.Sagas.1.0.0.nupkg"
  "ViciOne.ServiceBus.Courier.1.0.0.nupkg"
  "ViciOne.ServiceBus.Futures.1.0.0.nupkg"
  "ViciOne.ServiceBus.JobService.1.0.0.nupkg"
  "ViciOne.ServiceBus.Mediator.1.0.0.nupkg"
  "ViciOne.ServiceBus.Initializers.1.0.0.nupkg"
  "ViciOne.ServiceBus.RabbitMq.1.0.0.nupkg"
  "ViciOne.ServiceBus.AzureServiceBus.1.0.0.nupkg"
  "ViciOne.ServiceBus.EntityFrameworkCore.1.0.0.nupkg"
  "ViciOne.ServiceBus.EntityFrameworkCore.Sagas.1.0.0.nupkg"
  "ViciOne.ServiceBus.Quartz.1.0.0.nupkg"
  "ViciOne.ServiceBus.MessagePack.1.0.0.nupkg"
  "ViciOne.ServiceBus.Testing.1.0.0.nupkg"
)

for package in "${expected_packages[@]}"; do
  test -f "$package_feed/$package"
done

restore_arguments=(
  restore "$sample_project"
  --configfile "$nuget_config"
  --force-evaluate
)
if [[ "${1:-}" == "--update-lock" ]]; then
  restore_arguments+=("-p:RestoreLockedMode=false")
fi

"$dotnet_cli" "${restore_arguments[@]}"
"$dotnet_cli" build "$sample_project" \
  --configuration Release \
  --no-restore \
  --no-incremental \
  "${build_server_arguments[@]}" \
  -p:RestoreLockedMode=true \
  -p:TreatWarningsAsErrors=true

"$dotnet_cli" run \
  --file "$repository_root/tools/public-api-baseline/PublicApiBaseline.cs" \
  -- \
  "$global_packages" \
  "$package_feed" \
  "$public_api_baseline"

printf 'Developer journey package-consumer gate passed: 18 scenarios, 15 freshly packed ViciOne packages, packed public API baseline generated.\n'
