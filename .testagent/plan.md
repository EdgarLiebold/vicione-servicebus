# Plan — F1a correction and acceptance

This is the executable plan. A green subset never authorizes a repository-wide claim.

## 1. Correct the foundation

1. Condition test-only package versions on `ViciOneNativeTestTree`; restore every product lock file
   to its product-only resolution.
2. Rename the shared support project and namespace to
   `ViciOne.ServiceBus.Tests.Infrastructure` so it cannot be confused with shipped product API.
3. Materialize only `ViciOne.ServiceBus.Tests.Unit.slnx`; create LocalIntegration and External with
   their first executable cohorts.
4. Replace the free-form profile with `TestProfile`; validate timeout, local endpoints, external
   provider selection, real-resource mode, duplicate selectors, and all non-secret provider fields.
5. Deny every ArchUnitNET adapter while allowing exactly the framework-neutral core.
6. Count packages only from lock-file package nodes, excluding project references.

## 2. Bind architecture rules to real graphs

`RepositoryGraphTests` must prove from repository files and evaluated projects that:

- all product projects are independent of `tests2`;
- every native `PackageReference` obtains its version from CPM;
- only the two named Roslyn component projects pin `LangVersion`;
- every materialized profile has an executable native test project;
- every executable native test project belongs to exactly one profile;
- the Unit profile has exactly its F1a closure;
- Engineering contains every native test project;
- every solution project path exists.

Existing tests continue to prove parent imports, test-entry classification, runner configuration,
compiled assembly direction, package closure, and typed configuration behavior. The external
preflight test is structural only; concrete provider cohorts own behavioral access proof.

## 3. Required execution order

1. unlocked, forced restore only for intentionally changed native lock files;
2. locked restore of `ViciOne.ServiceBus.slnx`;
3. locked restore of `ViciOne.ServiceBus.Engineering.slnx`;
4. locked restore of `ViciOne.ServiceBus.Tests.Unit.slnx`;
5. Release build of the same three targets with `--no-restore --no-incremental`;
6. unfiltered native MTP run of Unit with `--no-build --no-restore`;
7. static test-quality and source-gap analyses;
8. isolated sabotage runs;
9. evidence and active documentation update;
10. one clean correction commit, then two independent read-only reviews.

## 4. Mandatory sabotage cases

Each mutation is applied to an isolated copy and must fail for its named reason:

- remove either nested parent import;
- add `TngTech.ArchUnitNET.xUnitV3`;
- alter or duplicate `xunit.runner.json`;
- add a skipped test;
- pin `LangVersion` in a new unauthorized location;
- add a product reference into `tests2`;
- add an empty native profile solution;
- select External with no provider or an emulator provider.

The canonical checkout is never mutated by a sabotage run.

## 5. Acceptance boundary

F1a is accepted only when all commands above are green, every changed file has been reviewed, no
claim exceeds its measured evidence, and both independent reviewers pass the same frozen commit.
F1b, behavior migration, inherited-test deletion, and push remain outside this correction.
