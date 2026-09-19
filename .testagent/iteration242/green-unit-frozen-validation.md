# Frozen Unit/Architecture validation · 19.09.2026

This is a hash-bound complete Unit/Architecture test result, not product-wide A+ acceptance or real-provider coverage.

- Immutable validation copy: `/private/tmp/vicione-servicebus-green-freeze.H2NAR1/repositories/vicione-servicebus`; local snapshot commit `a2373f95efaa801b7f2c6905d7d2f06dc8e696fb`.
- Sorted tracked/untracked `src`+`tests` content digest, excluding the protected `review/**` and `TestResults/**` paths: `a91b25cd499715630a606aabb24cde8603ba41231f427ccc553140bd12334295` both before and after copy. The current worktree yielded the same digest after the test run, even though unrelated Git HEAD advanced during parallel work. The local snapshot commit contains copied repository support files and is not a production-branch commit.
- Lead-only `dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode -p:NuGetAudit=false --disable-build-servers -v quiet`: exit 0.
- Lead-only `dotnet build ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-restore -m:1 --disable-build-servers -v quiet`: exit 0, **0 warnings, 0 errors**.
- Lead-only unfiltered `dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore --results-directory artifacts/iteration242-post-ef-frozen-unit-results -v minimal`: exit 0; **9,604 total, 9,604 passed, 0 failed, 0 skipped**, elapsed 4m 31s. All 23 included test projects reported success, including Architecture, Core, EF, SQL Transport, Amazon S3, Amazon SQS, RabbitMQ and Azure Service Bus Unit projects. No test filter was applied.

This closes the previously open frozen Unit/Architecture rerun after the EF JSON comparison/snapshot repair and SQL connection-open ownership fix. It does **not** prove that the full real-provider matrix, package/API journeys, current whole-fork line/branch coverage and CRAP, or source/API A+ dispositions have passed. The current-first-read checker and strict review-pair checker remain separate from test status; see [source-review-control.md](source-review-control.md). If `src` or `tests` bytes change, this result becomes a historical baseline for the new content hash.
