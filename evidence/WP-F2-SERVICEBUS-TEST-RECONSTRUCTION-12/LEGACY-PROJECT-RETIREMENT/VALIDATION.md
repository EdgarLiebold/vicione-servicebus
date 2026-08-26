# Native-replaced legacy test project retirement

## Frozen subject

- Technical commit: `ce1d3589096308b0e12ca3e500ab645a79d02aae`
- Technical tree: `74aa6a67a32afd770354021a320d6610b488aaff`
- Parent: `2610b521b41705bcd23fc607a2fcbeb2010da562`
- Branch: `test/servicebus-xunit4-mtp2-a-plus-v2`
- Product source changes: none
- Native `tests2` source, project and build changes: none

The technical commit removes only inherited test-estate files and their obsolete solution, workflow
and verification-model edges. `RETIRED_PATHS.txt` is the exact 36-path deletion set. Both retired
project directories are absent, including their formerly empty subdirectories; Git history is the
archive.

## Semantic closure

| Cohort | Terminal disposition | SHA-256 |
|---|---:|---|
| Abstractions | 70 executable replacements plus 8 non-product or non-executing obligations | `bdece6b549c3c4a53def79e7f52be4d26e2c55e30e5c99459656689d4c72cac9` |
| Analyzer and CodeFix | 115/115 obligations; 143 native replacement bindings; 0 remaining | `9aceeb9d59167099a7ef81f2be6f99eb59f8a3e919123f16eb27b465fdfaa9b7` |
| SignalR | 26 executable replacements plus 2 non-executing inherited methods | `03e50d66f86d0af6d0321fce3da08a341a39649a742b6c6d5d33b63096ac51a1` |

The retained product projects remain classified exactly once. The inherited verification model owns
no native runner or native case count: it uses `NATIVE_TEST_ESTATE` only to retain product-project
ownership and points to the terminal dispositions. The retained non-packable OrderWorkflow sample has
one explicit compile-proof owner. The model, required workflow and root solution hashes at the
technical commit are respectively `793eb65b5b7f4479080cd6c3b9d37a9699edbf6f3252d8629c73870d895be0a2`,
`b2f5f446c993d4519003000a0dfa6fe3b2a6c95113d51c59b8b00627a996ad2d` and
`d7131c6433ae1983a29118208da502fdfed0fe5095b11dba52cf4fa75be0d844`.

## Stationary positive gates

All .NET gates ran from the repository root against the frozen technical commit with .NET SDK
`10.0.302`, build servers disabled and no parallel .NET command. The two restores use NuGet's static
graph evaluation only to avoid the host sandbox's slow classic solution-graph enumeration; both are
still locked-mode restores of the complete solution graphs. `zsh -o pipefail` makes every recorded
raw-log command return the `dotnet` exit code rather than the `tee` exit code.

| Gate | Exact result | Raw evidence |
|---|---:|---|
| Root solution locked restore | exit 0; 35 projects resolved | `final-root-restore.txt` |
| Root solution Release build | exit 0; 0 warnings; 0 errors | `final-root-build.txt` |
| Engineering solution locked restore | exit 0; 40 projects resolved | `final-engineering-restore.txt` |
| Engineering solution Release build | exit 0; 0 warnings; 0 errors | `final-engineering-build.txt` |
| Native Analyzer executable, unfiltered | 114/114 passed; 0 failed; 0 skipped | `final-analyzers-test.json` |
| Native Analyzer CodeFix executable, unfiltered | 29/29 passed; 0 failed; 0 skipped | `final-analyzer-codefixes-test.json` |
| Native SignalR executable, unfiltered | 32/32 passed; 0 failed; 0 skipped | `final-signalr-test.json` |
| UnitArchitecture solution, unfiltered and serial by module | 1699/1699 passed; 0 failed; 0 skipped | `final-unit-test.txt` |
| Transition CI-tool self-tests | 206/206 passed | command verdict |
| Identity and change-list self-tests | 99/99 passed | command verdict |
| Verification-model bidirectional validation | exit 0; every retained project classified; no phantom run | command verdict |
| Generated Apache 2.0 section 4(b) change list | exact generated document accepted | command verdict |
| Git whitespace and staged-scope checks | exit 0 | command verdict |

The focused test reports are the original xUnit CTRF outputs. The full solution report is the exact
terminal stream of the fail-closed 1699-floor command; no report filename was shared across modules.

## Commands

```bash
dotnet restore ViciOne.ServiceBus.slnx --locked-mode --disable-build-servers \
  --disable-parallel --verbosity minimal /p:RestoreUseStaticGraphEvaluation=true
dotnet build ViciOne.ServiceBus.slnx -c Release --no-restore --disable-build-servers
dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode --disable-build-servers \
  --disable-parallel --verbosity minimal /p:RestoreUseStaticGraphEvaluation=true
dotnet build ViciOne.ServiceBus.Engineering.slnx -c Release --no-restore --disable-build-servers

dotnet test --project tests2/ViciOne.ServiceBus.Analyzers.Tests/ViciOne.ServiceBus.Analyzers.Tests.csproj \
  -c Release --no-build --no-restore --max-parallel-test-modules 1
dotnet test --project tests2/ViciOne.ServiceBus.Analyzers.CodeFixes.Tests/ViciOne.ServiceBus.Analyzers.CodeFixes.Tests.csproj \
  -c Release --no-build --no-restore --max-parallel-test-modules 1
dotnet test --project tests2/ViciOne.ServiceBus.SignalR.Tests/ViciOne.ServiceBus.SignalR.Tests.csproj \
  -c Release --no-build --no-restore --max-parallel-test-modules 1
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore \
  --minimum-expected-tests 1699 --max-parallel-test-modules 1

python3 -m unittest discover -s tools/ci -p 'test_*.py'
python3 -m unittest discover -s tools/identity -p 'test_*.py'
python3 tools/ci/verification/model.py
python3 tools/identity/change_list.py
```

The executed .NET commands also carried a unique `/bl:` path under ignored `artifacts/diagnostics`;
the review package keeps the compact raw text and CTRF outputs rather than duplicating large binary
logs in Git.

## Fail-closed boundaries

- The root solution and required workflow contain no retired Analyzer or SignalR project/job edge.
- The inherited model contains no retired Analyzer, SignalR or Abstractions test project, selection,
  run or expected-identity path.
- Reintroducing an unclassified project, a required workflow job without a model owner, a model run
  without its exact workflow job or a missing project remains rejected by the existing 206-test
  verification-tool suite and the model's bidirectional validation.
- The native workflow and its 1699-case floor are unchanged. No test, assertion, timeout, skip rule,
  package, product behavior or native project was changed to make this retirement green.
