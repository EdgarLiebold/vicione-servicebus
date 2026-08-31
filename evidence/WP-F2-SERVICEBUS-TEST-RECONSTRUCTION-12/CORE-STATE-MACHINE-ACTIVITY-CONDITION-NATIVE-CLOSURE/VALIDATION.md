# Core state-machine activity and condition native closure

## Frozen subject

- Technical commit: `9b96f8d35c97bf68d10f1b508baaa2c38d8d30b4`
- Technical tree: `fda755dd4e4bf4acfa2d49668ba1fabac64d7421`
- Direct Technical parent: `b1f70e2fb09b846a986eb83305bc5dae577bb03d`

`TECHNICAL.patch.gz` is byte-equal to the Git delta between the parent and Technical commit.
`TECHNICAL_SCOPE.json` enumerates its exact 20-path scope.

## Closure and retirement

- The terminal map contains exactly 40 unique inherited obligations. Every row is
  `REPLACED_EXECUTING` in `UnitArchitecture` and resolves to one of seven native xUnit owners.
- Those owners materialize 16 cases across declarative and dynamic construction styles. They bind
  full transition-hook order and payloads, Initial/Finally semantics, custom/data activities,
  synchronous and asynchronous condition branches, continuation and mutually exclusive filters.
- Ten fully replaced inherited files and 1,330 legacy lines are removed atomically. The Technical
  delta is +964/-1,335, a net reduction of 371 lines, while the executable floor rises from 2,597
  to 2,613.
- The containing legacy directories remain intentionally present because they still contain 30 and
  21 tracked files. No empty directory remains, and the frozen historical expected list is unchanged.

## Positive execution

```text
dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode --disable-build-servers /p:NuGetAudit=false /bl:<technical-engineering-restore.binlog>
dotnet build ViciOne.ServiceBus.Engineering.slnx --configuration Release --no-restore --disable-build-servers /m:1 /nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false /bl:<technical-engineering-build.binlog>
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-build --no-restore --results-directory <run-root> --minimum-expected-tests 2613 --max-parallel-test-modules 1 --fail-skips on --report-xunit-ctrf --parallel none /bl:<unit-test-dotnet-test-dotnet-test.binlog>
```

- Focused activity/condition execution: 16/16, zero failed/skipped.
- Complete UnitArchitecture: 2,618/2,618 across 21 CTRFs, zero
  failed/skipped/pending/other, above the fail-closed floor of 2,613.
- Requirement projection: 1/1; the separate 40-row map binds every inherited obligation.
- Locked Engineering restore, Engineering Release build, post-mutation focused build and scoped
  Roslyn format verification are successful. Build diagnostics report zero warnings/errors.
- The locked restore first stalled inside the macOS sandbox. The identical command completed in
  15 seconds outside that boundary. The scoped Roslyn operation required the same known sandbox
  workaround. This was an environment diagnosis, not a source-code workaround.

## Causal mutation closure

M01-M11 each replace one selected product-code occurrence against the frozen Technical commit. The
manifest records every literal occurrence count, selected index, baseline SHA-256, mutant SHA-256
and restored SHA-256. Independent reconstruction from the frozen blobs reproduces every mutant.

Every mutant builds and turns its intended owner red. The 28 executed mutation cases are 28/28
failed with no pass or skip. The axes bind transition ordering and AfterLeave publication,
Finalize/Finally/Initially targets, custom/data action execution, both condition truth paths,
condition continuation and data-event filter construction. After every mutation, the product blob
is restored byte-exactly; the final focused control is 16/16.

## Repository and environment gates

- CI tool self-tests: 257/257.
- Identity self-tests: 148/148 after the complete Evidence inventory and generated CHANGELIST are staged.
- Verification Model: PASS.
- Generated CHANGELIST: PASS after the complete Evidence inventory is staged.
- `SHA256SUMS` covers every Evidence file except itself.

No cloud fixture was required. No remote push was performed. Independent read-only acceptance,
architecture binding and any later remote publication remain separate actions.
