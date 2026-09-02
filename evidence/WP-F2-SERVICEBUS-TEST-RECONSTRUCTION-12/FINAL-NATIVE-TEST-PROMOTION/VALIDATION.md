# Final native test-tree promotion

Date: 2026-09-02

Technical commit: `ab1780450aa86ec758714ef5586e9335f1c65964`

Technical tree: `e8226794dda3aae268616f5df7516f45277494de`

## Accepted result

The transitional native tree is now the only active test tree: `tests2/` is absent and all native
projects live below `tests/`. The inherited NUnit benchmark project, the legacy TestFramework
project, the Python/unittest and VSTest verification stack, and the second workflow are removed only
after exact terminal capability dispositions and stronger native owners were present.

The final post-mutation UnitArchitecture execution is 2,984/2,984 with zero failures and zero skips.
That run contains 156/156 Architecture cases and 96/96 benchmark cases. Real local provider runs are
338/338 general LocalIntegration, 60/60 SQL Server, 24/24 Azure Service Bus emulator, and 17/17
RabbitMQ, all without skips. The general profile executed the Azure Table tests against Azurite; this
is an observed provider run, not a compile-only claim.

Locked restore and the complete Product and Engineering Release builds passed with zero warnings and
zero errors. Release packing after the clean Product build created all 19 expected NuGet packages.
The clean artifact gate accepted 41 artifacts: 22 DLLs, 19 packages, zero external PDBs, and 22
assemblies carrying embedded symbols. The vulnerability inventory reports zero findings, including
zero high/critical findings and zero unresolved transitive paths.

## Quality and mutation evidence

The final-promotion test delta contains 84 methods, 107 materialized cases, and 247 direct
assertions. Assertion-quality, anti-pattern, and complete 19-smell review found no Critical or
Warning issue. The delta contains no ignored test, real sleep, wall-clock oracle, fixed provider
port, unseeded random input, timeout-as-success, assertion-free test, or console-only verdict.

Eight independent one-cause counterexamples were killed. They bind the 57-row verification
disposition, workflow Unit command, exact Unit solution membership, run-root ownership, fixture
cleanup failure propagation, loopback-only provider publication, benchmark quantile rank, and the
Release external-PDB projection. Every mutation target was restored to its recorded SHA-256 before
the final build and test run. `MUTATION_MANIFEST.json` records the targets, causal observations, and
restored hashes.

## Diagnostic observations retained honestly

After the host restart, the first cold SQL Server profile reached 59/60 because first-use DDL timed
out. The exact failed method then passed alone and the unchanged warm full profile passed 60/60.
This was classified as a cold infrastructure observation, not converted into a green attempt.

The first `pack --no-build` exposed a real build projection defect: Release embedded symbols while
MSBuild still advertised nonexistent external PDB outputs. The central Release contract now sets
`_DebugSymbolsProduced=false`; a new Architecture carrier binds both `DebugType=embedded` and that
projection for every product project. The exact restore/build/pack sequence then created 19/19
packages and the one-cause inverse projection mutation was killed.

For reproducible local execution after a restart, use the system SDK at `/usr/local/share/dotnet`
with an isolated `DOTNET_CLI_HOME`. On macOS, Docker/Colima and .NET/MSBuild/MTP IPC require execution
outside the workspace sandbox. Restore and build use `--disable-build-servers`; the MTP test
application must not receive that option. SQL Server 2025 required a Colima VM with 8 GiB RAM and
four CPUs on this host; the default 4 GiB/two-CPU VM failed during SQLPAL initialization.

## Scope and publication

The user-owned untracked `review/` directory is excluded from every commit and gate. The Evidence
child binds the technical commit above; remote publication is a separate non-force push under the
standing explicit authorization.
