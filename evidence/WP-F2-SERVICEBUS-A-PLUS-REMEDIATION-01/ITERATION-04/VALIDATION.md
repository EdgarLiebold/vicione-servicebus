# A+ remediation iteration 4 validation

Date: 2026-09-06

## Scope

This iteration makes physical navigation from a declared C# type to its source deterministic across every evaluated product and native-test compile item:

- ordinary production files are named for their public top-level type;
- test files are named for their test class while remaining free to own local test contracts;
- public or independently reusable declarations formerly hidden in unrelated files have their own files;
- genuine cohesive declaration groups and partial implementation fragments are represented by exact path-to-type manifests;
- generated and infrastructure filenames are recognized only when their syntax proves the classification; and
- existing feature-oriented folders and namespaces remain intact where they express product ownership more clearly than mechanical namespace mirroring.

The changes are source organization and architecture enforcement. No product behavior or supported feature was removed.

## Architecture guard

`SourceFileNamingArchitectureTests` evaluates the real MSBuild `Compile` items of all product and native-test projects and parses them with Roslyn. It validates exact type identities including visibility, declaration kind, name, and generic arity. Reviewed manifests must exist, match exactly, and remain necessary; a stale exception fails the test.

The guard also rejects declarations concealed in `GlobalUsings` or assembly-information files, generated-looking filenames without an actual generated-code header, and secondary test classes. Test-method recognition covers both direct and qualified xUnit `Fact` and `Theory` attributes.

## Feature and API preservation

A before/after declaration inventory established:

- no removed top-level declarations;
- no removed public API symbols;
- no removed public parameters; and
- byte-identical contents for every pure move or rename pair.

Files that owned multiple independently navigable public declarations were split without changing signatures or implementations. One local generic comparer refactor required to separate its extension type was reviewed for behavior equivalence. An initially changed diagnostic in advanced registration was restored exactly. Documentation lost during mechanical splits was restored and clarified where ownership changed.

## Adversarial and mutation evidence

The internal Red Team identified overly broad early handling for reviewed paths, partial fragments, infrastructure suffixes, generated-looking filenames, and qualified xUnit attributes. The guard was hardened before final validation.

Four isolated mutation groups were then killed and restored:

1. A correctly named single-type file was given an unrelated filename; the guard reported its exact repository-relative path.
2. An extra declaration was introduced into a reviewed cohesive group; the exact type manifest rejected it.
3. A partial fragment received a different partial owner; the exact owner manifest rejected it.
4. Declarations were inserted into a `GlobalUsings` file and into a secondary qualified-`[Xunit.Fact]` test class; both classifications were rejected.

All mutation sources were removed before the final build and complete test run.

## Repository validation

| Gate | Result |
|---|---|
| `ViciOne.ServiceBus.Tests.Unit.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Complete Unit/Architecture profile | PASS — 3,768 passed, 0 failed, 0 skipped |
| `ViciOne.ServiceBus.Engineering.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Engineering whitespace verification | PASS |
| Engineering style verification at warning severity | PASS |
| Git whitespace validation | PASS |

This is internal engineering and adversarial-review evidence, not an independent external or Red Team acceptance.
