# Iteration 100 — Futures API and Navigation Validation

Date: 2026-09-13

Baseline: `045284d49168fac7f9ab0068453d81473bb2b150`

Scope: the remaining Futures interface contract, the complete Futures project layout, its direct
consumer and tests, architecture rules, package contract, and full repository validation

## Ownership and layout decision

`src/ViciOne.ServiceBus` is the Core project directory, not an umbrella directory for every
assembly whose package name begins with `ViciOne.ServiceBus`. Futures remains the independently
packaged `src/ViciOne.ServiceBus.Futures` capability project beside Core. The cohesive external
integration families remain grouped under `Persistence`, `Scheduling`, and `Transports`; within
each project, source folders should mirror namespaces relative to an explicit root namespace.

The Futures project now declares `ViciOne.ServiceBus` as its root namespace. Its source layout
therefore has two intentional top-level branches:

- `Configuration/` for `ViciOne.ServiceBus.Configuration`;
- `Futures/` for `ViciOne.ServiceBus.Futures`, including `Contracts/` and
  `DependencyInjection/` namespace branches.

This separates package/assembly ownership from namespace navigation without nesting sibling
projects beneath Core or changing an assembly, package, namespace, or feature.

## Manual API and source review

The complete 55-file Futures owner had already been read manually in Iteration 88. This follow-up
reread the remaining unprefixed interface, its state-machine event consumer, correlation behavior,
comments, filename, project definition, moved source declarations, relevant tests, and package
contract. No source or documentation generator was used.

The public message interface `Get<TFuture>` is now `IGet<TFuture>`, and its filename is `IGet.cs`.
It still derives from `ICorrelatedBy<Guid>` and keeps its generic constraint. The public protected
`ResultRequested` event now consumes `IGet<TCommand>`. No compatibility alias remains because this
is a permanent Greenfield fork.

Thirty files were moved without namespace or behavior changes so their paths mirror their existing
namespaces. Thirty optional expression-style findings were then resolved manually with collection
expressions, target-typed construction, the current lock type, simplified null checks, and one
small private primary-constructor conversion. One informational primary-constructor suggestion is
retained deliberately for the public convenience future definition: its explicit constructor and
dedicated XML documentation make the framework boundary clearer, while the suggested rewrite is
cosmetic and would not improve its contract.

## Regression and mutation evidence

The permanent red-first `FuturesInterfaces_UseTheDotNetInterfacePrefix` rule initially failed with
exactly `Contracts/Get.cs:7:Get`; it passes after remediation. The permanent namespace/folder rule
initially reported the root, `Contracts`, and `DependencyInjection` mismatches and the evaluated
root namespace; it passes after the layout correction.

A deliberate removal of `IGet<TFuture> : ICorrelatedBy<Guid>` was killed by the Futures build with
six compilation errors, including missing correlation identity and topology configuration. The
inheritance was restored before final validation.

The first complete test run also exposed an unrelated real race in
`DependencyInjectionTestHarnessTests`: handler completion could win before the observer's
post-operation callbacks updated their counters. The test now awaits explicit asynchronous
consume, publish, and send observer milestones rather than manufacturing inactivity first. Its
focused regression and the complete suite pass. After source relocation, the next full run exposed
one stale hard-coded `DefaultFutureDefinition.cs` path in an architecture test; the path was
updated to the namespace-aligned location, its focused regression passed, and the final full run
was clean.

## Final validation

- Engineering Release build: 77 projects, 0 warnings, 0 errors.
- Complete serial Unit/Architecture solution: 6,241 passed, 0 failed, 0 skipped.
- Architecture host: 297 passed, 0 failed, 0 skipped.
- Core host with coverage: 3,278 passed, 0 failed, 0 skipped.
- Fresh full-host coverage: 75.3393% line, 68.0696% branch.
- Futures package coverage: 90.4990% line, 85.4839% branch, complexity 561.
- Coverage artifact: `/private/tmp/vsb-iteration100-futures-final.cobertura.xml`, SHA-256
  `f998dd9ddf18824fc5573b0a5b4d40656d60a8927b61675468e6b2f2250a6771`.
- Complete Engineering and Unit/Architecture format gates: passed with exit code 0.
- Futures IDE0130 namespace-placement audit: zero findings.
- Futures full info-level style audit: zero warnings/errors and one deliberately retained
  `IDE0290` informational suggestion at the public convenience definition.
- Developer journeys: 18 scenarios, 31 freshly packed packages, 3 isolated provider-testing
  consumers, and 30 runtime package APIs.
- Packed public API: 19,030 lines, SHA-256
  `a96d93cc091d97174baceb59fe5230228734c2b98a676431521e70a329cce9fa`.
- Requirements JSON, Git whitespace, old-identity, interface-name, source-path, preprocessor,
  dummy/placeholder, TODO/HACK, `NotImplementedException`, and empty-directory checks: passed.

The intentional packed API delta is limited to the interface identity and its event reference.
The strict gate first rejected those two expected contract lines; the explicit update run rebuilt
and executed the complete gate, and the final default comparison independently reproduced the
same 19,030-line hash.

The protected `review/` and `TestResults/` trees were not modified or staged. This is an author
validation, not an independent Red Team acceptance. The complete repository A+ goal remains active
for the remaining source owners and final audit.
