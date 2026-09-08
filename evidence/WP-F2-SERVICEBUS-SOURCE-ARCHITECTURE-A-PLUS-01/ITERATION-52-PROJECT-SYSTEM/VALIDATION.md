# Iteration 52: Product project-system normalization

Date: 2026-09-08

Branch: `feature/servicebus-a-plus-api`

Baseline: `3d5f38f04b104d312da01058589471015ec7ee71` (`servicebus-a-plus-remediation-iteration-51-2026-09-08`)

## Scope and review method

This iteration begins the complete, file-by-file source architecture review authorized by
`WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01`. It reviews the repository-wide project system
before implementation files because those rules define the effective identity, build, packaging,
signing, dependency, and artifact behavior of every source project.

The following files were read completely and evaluated together:

- `.editorconfig`
- `Directory.Build.props`
- `Directory.Build.targets`
- `Directory.Packages.props`
- `signing.props`
- `src/Directory.Build.props`
- `tests/Directory.Build.props`
- `tests/Directory.Build.targets`
- all 32 product `.csproj` files below `src`

No source comment generator or bulk comment-rewrite script was used. Comments changed in this
cohort were rewritten manually after the owning configuration and its effective MSBuild behavior
had been read and understood.

The source census at the start of the review contains 4,119 C# files below `src`. Reading and
reviewing those implementation files remains an explicit subsequent part of this work package;
this document makes no claim that the implementation-file pass is already complete.

## Findings and corrections

### Strong-name signing

`ViciOne.ServiceBus.SqlTransport.SqlServer` was the only ordinary product assembly whose project did
not import `signing.props`. Its evaluated `SignAssembly` value was `false`, while comparable provider
assemblies evaluated to `true`.

Correction:

- imported the repository signing policy into the SQL Server transport project;
- added a repository architecture test that evaluates every product assembly project and requires
  `SignAssembly=true` plus the canonical `ViciOne.ServiceBus.snk` key path;
- verified the compiled SQL Server transport with `sn -T`; it now has public-key token
  `b8e0e9f2f1e657fa`, identical to the core assembly.

### Product and package identity

The analyzer implementation and the analyzer packaging project need different evaluated package
identities even though only the latter is published. NuGet uses `PackageId` as the identity of a
project reference in restore graphs. The implementation therefore retains the internal
`ViciOne.ServiceBus.Analyzers.Assembly` identity, while the separate build-only packaging project
publishes the consumer package `ViciOne.ServiceBus.Analyzers` without emitting a library assembly.

Correction:

- preserved and documented the distinct implementation and build-only packaging identities;
- added architecture rules requiring these two explicit mappings, project/assembly/package identity
  alignment for every other product project, and case-insensitive uniqueness across all evaluated
  product package identities;
- normalized the SQL Server package title to the canonical project spelling.

### Package descriptions

Several product packages either inherited the generic repository description or appended that
generic description to a short provider label. Those descriptions did not state the actual package
capability clearly.

Correction:

- supplied concise, capability-specific descriptions for the affected core, abstraction,
  orchestration, persistence, scheduling, transport, testing, analyzer, and integration packages;
- added an explicit reviewed `PackageId`-to-description contract for all 30 packable products; the
  architecture rule rejects missing, extra, generic, placeholder, or unreviewed descriptions.

### Project references and physical project identity

The SignalR project reached sibling projects through `../../src/...`, which resolves correctly but
is not the canonical shortest relative path. All product directories already match their project
names, and there are no loose product `.cs` files directly in the `src` root.

Correction:

- shortened both SignalR project references to their canonical sibling paths;
- added architecture rules for existing referenced projects, canonical shortest reference paths,
  project-directory/project-name equality, and equality between the raw declarations and the full
  ProjectReference set evaluated by MSBuild. Imported or conditionally inactive hidden edges can no
  longer evade the rule.

The higher-level mix observed below `src` is not yet classified as an error. `Persistence`,
`Scheduling`, and `Transports` currently group provider families; direct children currently represent
cross-cutting or first-class capability packages. A physical move based only on visual symmetry would
be premature. The final placement decision will be made after the implementations, namespaces,
dependencies, and public API of each capability have been read in the file-by-file pass.

### Build comments, formatting, and stale artifact policy

- replaced the inaccurate `Unsafe` comment above `DebugSymbols` with a comment describing the actual
  current property;
- rewrote signing, analyzer packaging, and RabbitMQ internal-access comments to describe current
  functionality rather than history;
- normalized inconsistent XML empty-element spacing in the touched project files;
- removed the obsolete SQL transport test artifact alias and its overlap narrative; no current
  project, target, or test depended on that alias.

## Lockfile disposition

A forced full graph evaluation followed by an ordinary locked restore produced a stable dependency
graph. Every lockfile difference was inspected.

- Analyzer lockfiles retain the internal `.Analyzers.Assembly` project identity so it cannot collide
  with the aggregate consumer package.
- MessagePack and dependent benchmark/test lockfiles remove stale Courier, JobService, and Sagas
  edges that are absent from the current MessagePack project.
- Local-integration lockfiles remove stale JobService edges from Quartz and other no-longer-present
  project dependencies.
- The Entity Framework Core test lockfile reclassifies centrally pinned SQLite dependencies from
  ordinary transitive to `CentralTransitive`; resolved versions and content hashes are unchanged.
- Fresh local `.nupkg` files contain NuGet-generated package-part identifiers, so rebuilding the same
  source can change their content hashes without changing package contents or dependency structure.
  Normal package-consumer validation now copies each tracked lock file to the isolated temporary
  workspace and evaluates the fresh package there in locked mode. Only explicit `--update-lock`
  operation may write tracked consumer locks. Two consecutive pre-correction gates exposed the former
  side effect; their transient sample-lock diffs were removed rather than committed. The post-correction
  gate passed and left all five tracked consumer locks byte-for-byte unchanged.

The verification restore

```text
dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode --disable-build-servers
```

completed successfully without any further lockfile change. A separate locked restore of
`ViciOne.ServiceBus.slnx` also passed and explicitly covered the analyzer packaging project omitted
from the engineering solution.

## Requirement and evidence map

| Requirement | Evidence | Result |
| --- | --- | --- |
| Canonical project/assembly/package identity | `EveryProductProject_HasCanonicalArtifactIdentity` | PASS |
| Evaluated package identities do not collide | `EveryProductProject_HasAUniqueEvaluatedPackageIdentity` | PASS |
| All emitted product assemblies use the repository signing policy | `EveryProductAssemblyProject_IsStrongNameSignedByTheRepositoryKey`; compiled-token inspection | PASS |
| Every published package describes its actual capability | `EveryPackableProduct_HasASpecificSentenceDescription` | PASS |
| Product project references are real and canonical | `EveryProductProjectReference_UsesTheCanonicalShortestRelativePath` | PASS |
| Product directory and project identities agree | `EveryProductProjectDirectory_MatchesItsProjectName` | PASS |
| Project-graph changes remain reproducible | locked restore after force evaluation | PASS |
| Existing behavior remains intact | complete UnitArchitecture profile | PASS |
| Real packages remain consumable | package/developer-journey gate | PASS |
| Packed public API remains stable | committed 29-assembly public API comparison | PASS |

## Test effectiveness

The six focused product metadata tests pass together: 6 passed, 0 failed, 0 skipped.

Four deliberate mutations were applied one at a time, executed against the focused test, and fully
restored:

1. removing the SQL Server signing import was killed by the signing-policy test;
2. reintroducing the SignalR `../../src` detour was killed by the canonical-reference test;
3. replacing the Courier description with the generic repository description was killed by the
   package-description test;
4. assigning Courier a `.Legacy` package identity was killed by the artifact-identity test.

The package-gate isolation test was also mutation-checked: deleting the transient
`NuGetLockFilePath` argument produced the expected focused-test failure, after which the line was
restored and the focused suite passed.

The internal read-only Red Team found that an interim simplification of the analyzer implementation's
`PackageId` would collide with the aggregate shipping package and leave its locked project graph
stale. The internal `.Analyzers.Assembly` identity was restored before the final build, a uniqueness
rule was added, and both product and engineering lock graphs passed. The Red Team also found Boolean
casing, empty-filter, imported-ProjectReference, and weak description-test gaps. All were corrected:
MSBuild Booleans are parsed semantically, filtered sets must be non-empty, raw references equal the
evaluated graph, and all 30 package descriptions have exact reviewed expectations. A final delta
review reported no remaining commit blocker. This was internal adversarial review, not independent
external acceptance.

The focused suite passed again after restoration. `git diff --check` also passed, confirming that no
mutation or whitespace residue remained.

A static source-to-test name-pairing analyzer reported 4,269 candidate source files across the whole
repository, 1,336 name-paired and 2,933 unpaired. This is a prioritization signal, not coverage:
extension methods, composition roots, reflection-owned code, and behavior tested through another
type commonly have no filename-paired test. No coverage or correctness claim is derived from this
heuristic.

## Validation results

### Build

`ViciOne.ServiceBus.Engineering.slnx`, Release:

- 0 warnings
- 0 errors

`ViciOne.ServiceBus.slnx`, Release:

- 0 warnings
- 0 errors
- analyzer implementation, code-fix, and aggregate packaging projects all compiled

### Unit and architecture tests

`ViciOne.ServiceBus.Tests.Unit.slnx`, Release, Microsoft Testing Platform v2, one test module at a
time:

- 4,544 passed
- 0 failed
- 0 skipped

The first complete run correctly rejected the new package-lock isolation test because its compiled
requirement metadata had not yet been added to the embedded requirement projection: 4,543 passed and
1 projection failure. The missing `REQ-VSB-DEVELOPER-JOURNEYS` variant was added, its focused meta-test
passed, and the complete 4,544-test profile then passed. This was a deterministic completeness gate,
not a flaky runtime failure.

### Formatting

`dotnet format ViciOne.ServiceBus.Tests.Unit.slnx --verify-no-changes --no-restore` passed with no
format changes. The sandboxed attempt could not create the Roslyn/MSBuild named-pipe socket; the
identical command outside the sandbox passed. This was an execution-environment restriction, not a
source or formatting defect.

### Packaged developer journeys

`tools/ci/verify_developer_journeys.sh` passed:

- exactly 30 freshly packed ViciOne packages;
- 18 executable developer-journey scenarios;
- 3 isolated provider-testing package consumers built and executed;
- 29 runtime package APIs matched the committed 21,598-line public API contract;
- generated contract SHA-256:
  `8e455b6cc856f18a4498304f34efcb804838299812eb4e4dac684357d61f2c72`.

## Iteration conclusion

The product project system is materially more explicit and now has executable regression rules for
the defects found in this cohort. This is an iteration boundary, not completion of the A+ source
architecture goal. The next iterations continue with complete personal reading of coherent source
projects, including code correctness, API shape, types, comments, filenames, namespaces, dependencies,
and the final physical folder taxonomy.
