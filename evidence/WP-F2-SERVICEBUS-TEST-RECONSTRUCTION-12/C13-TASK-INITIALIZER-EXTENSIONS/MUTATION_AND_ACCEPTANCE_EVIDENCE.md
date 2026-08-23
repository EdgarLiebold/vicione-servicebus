# C13 task initializer extension mutation and acceptance evidence

Date: 2026-08-23

Technical commit: `462aaedd2f2184bd532816166733654d72ccce1b`
Technical tree: `6af55ac899101395c5064f7b7f7ddc04c10a51b1`

## Scope

This source-owner cohort replaces `TaskExtension_Specs.cs` with ordinary xUnit 4 tests executed by
Microsoft Testing Platform 2. It closes all ten inherited ledger obligations owned by that fixture.
The replacement uses no inherited TestFramework, fixed-time behavior assertion, polling, random
input, skipped case, environment resource, auxiliary runner, receipt, interceptor, or execution
sentinel.

The accepted technical inputs have these SHA-256 hashes:

| Path | SHA-256 |
| --- | --- |
| `src/ViciOne.ServiceBus/Initializers/TaskInitializerExtensions.cs` | `463b4a840068b6dbd935a97ce71b8405c229e1fb81f122c4776f356242cf8380` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/TaskInitializerExtensionsTests.cs` | `f768b206506ceb08c700051d237c23bfd198f3e4d4bb8a7993412bdeb5c6012b` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `9ba32423782e60d958de57c9e02103c66716648ce8c70c298d8279008bea5fa4` |
| `evidence/native-tests/task-initializer-extensions/INHERITED_BEHAVIOR_DISPOSITION.json` | `83b1c189cf74729dab2cc8e77c4d0970861dc9fe69cdb8eeb8061c9447da5b70` |

## Product and behavior result

- Eleven overlapping inherited `Select` overloads are replaced by seven explicit asynchronous
  overloads under `SelectAsync` and `SelectOrFallbackAsync`.
- The string-only duplicates are removed. The generic reference-type and nullable-value-type APIs
  retain projection, constant fallback, lazy synchronous fallback, and awaited asynchronous
  fallback capabilities.
- Every public overload validates its declared inputs in parameter order. Reference fallbacks
  cannot violate their non-null result contract by returning a null value, null task, or task with
  a null result.
- A null source value bypasses the selector. A present projected value bypasses all fallback
  factories. Source, selector, and fallback faults and cancellation propagate without wrapping or
  token substitution.
- No retained product call site uses the removed inherited names. The public API change is a
  deliberate greenfield normalization, not a compatibility shim; no capability is removed.
- Product and test paths and namespaces both use the `Initializers` source owner.

## Test-quality review

The new file contains 15 ordinary facts, 15 unique requirement rows, and direct assertions over
every selected-versus-fallback branch, lazy invocation count, result, exception identity,
cancellation token, and validation boundary. The two incomplete-task tests use explicit completion
barriers and release them in `finally`; the centrally configured operation timeout is only a safety
boundary. Static review found no delay, sleep, polling, random input, wall-clock dependency,
synchronous `Task.Wait`/`Result`, skip, assertion-free case, shared mutable fixture, hidden external
resource, or second configuration source.

## False-green attacks

Each mutation changed one behavior, built successfully, failed the responsible native test, and
was removed before the next mutation.

| Mutation | Observed verdict |
| --- | --- |
| Invoke the selector for a null source value | 1 failed, 13 passed; the selector-invocation count rejected it |
| Always return the reference constant fallback | 1 failed, 13 passed; the present selected value rejected it |
| Always return the nullable-value constant fallback | 1 failed, 13 passed; the present selected value rejected it |
| Invoke the reference synchronous fallback eagerly | 1 failed, 13 passed; the lazy invocation count rejected it |
| Invoke the nullable-value synchronous fallback eagerly | 1 failed, 13 passed; the lazy invocation count rejected it |
| Invoke the reference asynchronous fallback eagerly | 1 failed, 13 passed; the lazy invocation count rejected it |
| Invoke the nullable-value asynchronous fallback eagerly | 1 failed, 13 passed; the lazy invocation count rejected it |
| Wrap source faults and cancellation | 1 failed, 13 passed; exact exception identity rejected it |
| Remove one compiled requirement attribute | 1 failed, 229 passed; the requirement projection rejected it |
| Wrap asynchronous fallback faults and cancellation | 1 failed, 14 passed; exact delegate exception identity rejected it |

No surviving mutation is reported as killed.

## Final acceptance

| Gate | Result |
| --- | --- |
| Bounded `dotnet format` and `--verify-no-changes` for changed C# files | exit 0 |
| Focused `TaskInitializerExtensionsTests` run | 15 total; 15 passed; 0 failed; 0 skipped |
| Complete core-owner run | 231 total; 231 passed; 0 failed; 0 skipped |
| UnitArchitecture Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture run | 739 total; 739 passed; 0 failed; 0 skipped |
| Engineering Release build | exit 0; 0 warnings; 0 errors |
| JSON parsing, diff whitespace, forbidden timing/random/skip scan | pass |
| Requirement projection | 15 source attributes and 15 unique projection rows |
| Inherited closure | 10 of 10 obligations terminally mapped |

The inherited file is removed only in this accepted state. Git history remains the archive of its
original bytes. The empty, ignored root `TestResults` residue was removed; generated output remains
under `artifacts` or the explicit temporary run directories.
