# R0 probe — exact MTP command form, and a write-scope blocker it exposes

Lead plan section 8: "Die vom `dotnet-test:run-tests`-Skill bestätigte genaue Kommandoform wird im
Execution Plan festgeschrieben." Requirement `REQ-TEST-106`: "Native MTP discovery, execution,
timeouts, minimum expected tests and machine-readable results are used for the exact profile
commands."

This probe establishes that command form by measurement, in a disposable project outside the
product repository. The bound candidate was not touched: `repositories/vicione-servicebus`
remained at `ae73c6da748e3bc3257dffa4971ee8680e086207` with a clean worktree throughout.

## Probe subject

A minimal disposable project under the session scratchpad, deliberately mirroring the target
contract: `net10.0`, `OutputType=Exe`, one single direct package reference
`xunit.v3.mtp-v2` **4.0.0**, one `[Fact]`. Restore succeeded from the configured feeds, so the
mandated entry package is available at the required version.

Runner identity reported by the built executable: `xUnit.net v3 In-Process Runner v4.0.0+8bf043c053
(64-bit .NET 10.0.10)`. SDK: 10.0.302.

## Measurements

Each row changes exactly one property and keeps every unrelated precondition satisfied.

| # | Condition | Command | Exit | Observed |
|---|---|---|---:|---|
| A | `global.json` **without** `"test": { "runner": … }` | `dotnet test --project P/P.csproj` | ≠0 | Falls into the **VSTest** path (`--target:VSTest`, `VSTestNoLogo`, `VSTestSessionCorrelationId`) and dies with `MSBUILD : error MSB1001: Unbekannter Schalter … --project` |
| C | Same, plus `TestingPlatformDotnetTestSupport=true` in `Directory.Build.props` | `dotnet test --project P/P.csproj` | 1 | Identical VSTest path and identical MSB1001. The property does **not** switch SDK 10 to MTP |
| C2 | Same, positional path form | `dotnet test P/P.csproj` | 1 | `Microsoft.Testing.Platform.MSBuild.targets(320,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.` |
| B | `global.json` **with** `"test": { "runner": "Microsoft.Testing.Platform" }` | `dotnet test --project P/P.csproj` | 0 | Native MTP run, `gesamt: 1 · erfolgreich: 1 · übersprungen: 0` |
| D | No opt-in, direct execution | `dotnet run --project P/P.csproj -- --minimum-expected-tests 1` | 3 | `error: unknown option: --minimum-expected-tests`. `--help` on this path prints the **xUnit native in-process runner CLI** (dash-style `-list`, `-explicit`, `-failSkips`, …) and exposes no MTP option at all |
| E1 | With opt-in, positive path | `dotnet test --project P/P.csproj --minimum-expected-tests 1` | 0 | Green |
| E2 | With opt-in, **only the demanded minimum changed** | `dotnet test --project P/P.csproj --minimum-expected-tests 2` | **9** | `Testlaufzusammenfassung: Mindestens erwartete Testrichtlinienverletzung, 1 Tests wurden ausgeführt, mindestens erwartet: 2` — and `fehlgeschlagen: 0`, so the process failed for the count policy and for nothing else |

E1 versus E2 is the discriminating negative probe: one isolated property changed, every unrelated
precondition still satisfied, and the failure message names exactly the assurance under test.

## Conclusion

1. The exact command form for every release-relevant profile run is
   `dotnet test --solution <profile>.slnx <MTP arguments>` / `dotnet test --project <path> <MTP arguments>`,
   with MTP arguments passed **directly** (no `--` separator) and **no** positional path.
2. That form requires the SDK 10 opt-in `"test": { "runner": "Microsoft.Testing.Platform" }` in
   `global.json`. Measured, not assumed: without it SDK 10 routes `dotnet test` to VSTest, and
   VSTest is both banned by the Lead plan (no `Microsoft.NET.Test.Sdk`) and refused outright by
   the MTP targets on .NET 10.
3. The MSBuild property `TestingPlatformDotnetTestSupport` is the SDK 8/9 signal and is **not** a
   substitute on SDK 10 (row C).
4. Direct execution via `dotnet run` is not a substitute either (row D): it reaches only the xUnit
   native runner CLI, which has no `--minimum-expected-tests`. The zero-test and minimum-count lock
   that `REQ-TEST-106` and Lead plan section 8 demand is reachable **only** through the
   `dotnet test` MTP path.

## Blocker — stop class `WRITE_SCOPE_CHANGE`

`repositories/vicione-servicebus/global.json` is tracked, currently contains only the SDK pin, and
is **not** covered by any of the 91 write scopes in the team manifest. The target architecture
cannot execute a single release-relevant profile run without editing it.

This is not a deviation Team 1 may decide: `WRITE_SCOPE_CHANGE` is an explicit `mustStop` class in
the Development Slice. It is carried into the R0 checkpoint as a structured question with options
rather than resolved locally. R0 research continues meanwhile, because no census obligation depends
on the answer.
