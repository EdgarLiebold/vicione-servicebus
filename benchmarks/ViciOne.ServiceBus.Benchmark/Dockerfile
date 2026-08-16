# ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["signing.props", "."]
COPY ["ViciOne.ServiceBus.snk", "."]
COPY ["src/", "src/"]
COPY ["benchmarks/ViciOne.ServiceBus.Benchmark/", "benchmarks/ViciOne.ServiceBus.Benchmark/"]

WORKDIR "/src/benchmarks/ViciOne.ServiceBus.Benchmark"
RUN dotnet build "ViciOne.ServiceBus.Benchmark.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ViciOne.ServiceBus.Benchmark.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "vicione-servicebus-benchmark.dll"]
