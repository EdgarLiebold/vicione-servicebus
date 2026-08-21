# Research — native xUnit 4 / Microsoft Testing Platform 2 foundation

Scope: F1a of `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-12`. The implementation follows the mandatory
Microsoft testing and MSBuild skills using the sequence research → plan → implementation → test →
independent review.

## Baseline

- SDK `10.0.302`, pinned with roll-forward disabled.
- `global.json` selects Microsoft Testing Platform.
- Root MSBuild owns locked restore and the common `artifacts/sdk` output.
- Product target is `net10.0`; only the two Roslyn components and their source-free package surface
  retain their documented `netstandard2.0` exception.
- The inherited `tests/**` and Python verification stack are behavior evidence only. They are not a
  design template and are not extended.

## Platform decisions

- Executable native test projects reference exactly `xunit.v3.mtp-v2` `4.0.0` as their entry
  package. MTP owns execution and the process verdict; xUnit owns discovery, cases, individual
  results, and assertions.
- Architecture rules use `TngTech.ArchUnitNET` core `0.13.4` through ordinary xUnit assertions.
  Every ArchUnitNET framework adapter is forbidden.
- Shared support code is non-executable, non-packable, and framework-neutral under the distinct
  product-neutral identity `ViciOne.ServiceBus.Tests.Infrastructure`.
- Test-only central package versions are conditional on `ViciOneNativeTestTree`. The root build
  derives that marker from the repository-relative project path and rejects later overrides, so a
  product project cannot opt into the test-only package boundary.
- A profile solution exists only after its first executable cohort. F1a materializes Unit only;
  invalid empty LocalIntegration/External solutions are forbidden.
- The checked-in configuration contains no credentials. User Secrets and `VICIONE_TESTS__` may
  provide non-secret resource coordinates; Azure.Identity and AWS SDK chains remain credential
  owners.

## Lead rejection of the first Team-1 candidate

The first candidate provided a useful native xUnit/MTP core but was not acceptable as a completed
foundation. Independent reproduction found:

1. full product locked restore failed because test-only CPM versions changed product lock files;
2. empty profile solutions failed as invalid solutions, not as MTP zero-test runs;
3. two claimed rules survived direct mutations: a root `LangVersion` pin and a no-op external
   preflight implementation;
4. the real package `TngTech.ArchUnitNET.xUnitV3` was not denied;
5. the free-string profile and external defaults allowed fail-open configuration;
6. support namespace/project identity collided with the shipped `ViciOne.ServiceBus.Testing`
   namespace;
7. the resolved package count mixed package nodes with project nodes;
8. active documents named nonexistent tests and described the superseded Python stack as active;
9. repository-wide project/profile graph rules were absent.

The Product Owner therefore assigned the bounded F1a correction directly to the Lead Architect.
No product behavior is changed.

## Required proof surfaces

- full locked restore and Release build of product, Engineering, and Unit targets;
- unfiltered Unit profile through native MTP;
- evaluated MSBuild graph plus parsed project/solution graph;
- full resolved package closures from tracked lock files;
- isolated mutations for every fail-closed rule;
- assertion-quality, anti-pattern, gap, and untested-source reviews;
- two independent read-only reviews after the corrected integrated commit.
