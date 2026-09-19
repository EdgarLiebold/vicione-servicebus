# Isolated UnitArchitecture gate diagnostic · 2026-09-19

Snapshot: detached worktree at `e4e8777c2`, with clean tracked `src` and `tests`. The Release build of `ViciOne.ServiceBus.Tests.Unit.slnx` used the repository's coverage-universe MSBuild import and completed in 38.26 seconds with zero warnings and zero errors. This was a build and gate diagnosis, not a product-wide coverage result.

The unfiltered solution test run used `--no-build --no-restore` against that artifact tree. It was cancelled after the Architecture host completed with 11 failures and while the Core host was still running; therefore no complete pass/fail/skip count exists for this run. Observed failures included:

- The temporary 32-assembly coverage loader could not find `ViciOne.ServiceBus.EventHubs` and `ViciOne.ServiceBus.EventHubs.Testing`, because the UnitArchitecture solution does not build those two assemblies. This is a measurement setup failure.
- Architecture tests expected the default `artifacts/sdk` output while this diagnostic build used an isolated artifacts path. Those output-path failures do not show a product defect.
- The Amazon SQS canonical-correlation-header test failed on transport-text configuration. The Core MessageBody contract inventory did not include a concrete admitted body type. A request-deadline test timed out. Each needs an isolated reproduction before assigning a product defect or a test defect.
- Architecture checks reported outdated source-shape expectations for payload admission and inbound limits, eight generated documentation placeholders, seven EF/in-memory configuration-exception message locations, one native test file with multiple namespace declarations, and asynchronous naming violations. Several affected files were already being edited concurrently in the shared worktree; their uncommitted state was not part of this detached snapshot.

After the cancelled full run, building `ViciOne.ServiceBus.EventHubs.Testing.csproj` into the same Release artifact tree also built the two missing EventHubs assemblies with zero warnings. The focused `CoverageUniverseTests.LoadEverySourceAssemblyForCoverageDenominator` then passed 1/1. This fixes the isolated loader setup; the red full suite was not rerun.

Do not infer global A+ or an exact current product-wide line/branch/CRAP union from this diagnostic. The clean `93f6fe127` EF report remains the bounded measurement. A future full measurement needs a green exact-commit gate, `tools/ci/coverage.settings.xml` on every collection, and instrumented provider runs.
