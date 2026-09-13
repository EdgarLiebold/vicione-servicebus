# Iteration 98: Saga interface API validation

## Scope and decision

Iteration 98 starts from remotely verified commit
`2234d03407d2ad2570c12e823e4fe0a01087be9d` and annotated tag
`servicebus-a-plus-remediation-iteration-97-2026-09-13`. It completes the bounded Saga
interface-naming finding that remained after Iteration 97.

All 47 interfaces owned by `ViciOne.ServiceBus.Sagas`, including three nested internal contracts,
now follow the .NET interface-prefix rule. Thirty top-level source files were renamed with their
primary types. Generic arity, variance, inheritance, members, attributes, implementations, and
features were preserved. References were updated through the complete source, provider, test,
sample, analyzer, reflection-identity, and isolated package-consumer closure. A transformed public
API multiset comparison found no unrelated contract change.

The physical source layout remains intentional. `src/ViciOne.ServiceBus` owns the Core assembly; it
is not an umbrella directory. Optional first-party capability assemblies such as Sagas, Courier,
Mediator, Futures, JobService, and Testing remain direct siblings. External integration families
are grouped under `Persistence`, `Scheduling`, and `Transports`. Nesting independent projects below
the Core project would communicate false ownership and would overlap the SDK project's recursive
compile scope. The project-reference direction, package identities, architecture tests, and
consumer journeys confirm the current ownership boundary.

## Documentation and regression protection

Every affected declaration and its surrounding XML documentation was reread after the rename.
Stale generic-parameter terminology and history-oriented wording were replaced manually with
current contract semantics. No behavior or documentation generator was used. A symbol-aware tool
was used only for reference-preserving mechanical renames after manual classification.

The red-first architecture requirement
`REQ-VSB-GREENFIELD-SAGA-API/interfaces-use-dotnet-prefix` initially reported exactly 47
violations. It is now a permanent test and passes. Removing the prefix from `ICorrelatedBy` killed
the isolated mutation with exactly one failing architecture case; the original declaration was
restored before the final validation.

## Validation

| Gate | Result |
| --- | --- |
| Engineering Release build | passed for all 77 projects; 0 warnings, 0 errors |
| Complete Unit/Architecture solution | 6,234 passed; 0 failed; 0 skipped |
| Architecture host | 293 passed; 0 failed; 0 skipped |
| Core test host | 3,275 passed; 0 failed; 0 skipped |
| Analyzer tests | 10 passed |
| Analyzer code-fix tests | 5 passed |
| Developer journeys | 18 scenarios passed |
| Fresh packages | 31 packages built and validated |
| Isolated provider consumers | 3 passed |
| Runtime package API contracts | all 30 matched |
| Engineering and Unit formatting | both `--verify-no-changes` gates passed |
| Requirements JSON | all files parse successfully |
| Diff whitespace | `git diff --check` passed |
| Saga interface inventory | 0 unprefixed declarations remain |
| Saga directives and hygiene | no preprocessor directives, dummy markers, empty directories, or temporary implementations |

The packed public API contains 19,029 lines with SHA-256
`1a4fdef247c3b4ece1e5b8fed35dbe533f8409541a4aedf3be2dce9be8891be6`.

Fresh Core-host instrumentation records 75.3365% line and 68.0573% branch coverage across all
assemblies loaded by that host. The Saga package records 62.4669% line and 54.4440% branch coverage,
with 2,523 methods, 15 methods above CRAP 30, and a maximum CRAP score of 254.280504. Coverage is
reported as risk evidence, not represented as proof of semantic completeness; the high-risk Saga
methods remain inputs to later test-strengthening work.

An in-sandbox coverage attempt failed before discovery because Microsoft Testing Platform could
not bind its local named-pipe socket (`SocketException`, permission denied). The identical command
was rerun outside that sandbox constraint and passed 3,275/3,275; this is an environment diagnosis,
not a product exception or suppressed test.

An info-level Saga style audit now reports 613 suggestions and no warning or error. The former 47
interface naming findings are absent. Most remaining suggestions are intentional public-facade
namespace placement (`IDE0130`) or separately reviewable modernization opportunities; none was
silently applied as a semantic rewrite.

The protected `review/` and `TestResults/` trees were neither changed nor staged. This iteration is
an author validation and does not claim an independent external Red-Team acceptance. The overall
source-wide A+ goal remains active for the remaining source owners and the final completion audit.
