# C38 Native Source Layout — Structure Analysis

The native test tree contained 280 C# source files, but its existing architecture suite enforced
only compiled assembly, dependency, package and solution boundaries. Folder-to-namespace mirroring
was therefore a convention without an executable guard.

`NativeTestSourceLayoutTests` now evaluates every `tests2/**` project through the pinned MSBuild
graph, reads its real `Compile` items and `RootNamespace`, and parses each included file with Roslyn.
Every file must be inside its project, contain exactly one namespace declaration in its entire syntax
tree, and match the project root plus its physical relative directory. Nested or multiple namespace
truths therefore cannot bypass the folder rule.

The first run found nine files in two specialized support assemblies. Their namespaces already
expressed the accepted common test-infrastructure ownership. The missing truth was the projects'
implicit root namespace, so both projects now declare their intentional roots explicitly. Assembly
names and public namespaces remain unchanged.
