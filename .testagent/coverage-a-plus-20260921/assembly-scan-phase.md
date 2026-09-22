# Core assembly scanning: selected-file identity and failure evidence

## Product correction

The last complete 35-report profile at `e1a965290` measured 80,159/90,376
lines (88.6950%), a conservative branch interval of 80.2199–86.9956%, and
111 methods above CRAP 30. It ranked `AssemblyFinder.MoveNext` at CRAP 272
with 0/29 lines and `AssemblyScanner.FindTheCallingAssembly` at CRAP 156
with 0/13 lines.

`AssemblyFinder` scanned a selected file but first loaded an assembly by the
file's simple name. A valid file named `ViciOne.ServiceBus.dll` containing
the test assembly therefore returned the already loaded Core assembly. A
renamed valid assembly also failed when its simple name could not be resolved;
the fallback passed a path to `Assembly.Load(string)`, which loads by name.
The finder now calls `Assembly.LoadFrom(file)` for the selected candidate.
It still skips invalid images and reports other failures from that path to
the load-failure callback.

## Hard behavioral regressions

| Contract | Test |
| --- | --- |
| Application-base discovery with a selected file name | `FindAssemblies_ResolvesAFilteredAssemblyFromTheApplicationBaseDirectory` |
| A renamed valid DLL yields its manifest identity | `FindAssemblies_UsesTheManifestIdentityOfARenamedAssemblyFile` |
| A filename colliding with an already loaded Core assembly does not substitute Core | `FindAssemblies_DoesNotSubstituteAnAlreadyLoadedAssemblyNamedLikeTheFile` |
| Recursive file-name filtering sees accepted and rejected DLLs | `FindAssemblies_AppliesTheFileNameFilterRecursively` |
| An EXE is returned only when executable scanning is enabled | `FindAssemblies_IncludesExecutableFilesOnlyWhenRequested` |
| A disappearing selected file reports its exact path in both callback and exception | `FindAssemblies_ReportsTheActualPathFailureWhenASelectedFileDisappears` |
| An invalid image is skipped while another selected assembly is still returned | `FindAssemblies_SkipsAnInvalidImageAndStillReturnsTheOtherSelectedAssembly` |
| Caller discovery registers the user assembly and scans its requested type | `TheCallingAssembly_RegistersTheUserAssemblyForTypeScanning` |

The renamed-file test failed before the first loader fix. The adversarial
filename-collision test then failed against the first fix: it returned
`ViciOne.ServiceBus` instead of the selected file's test-assembly identity.
The final tests use actual temporary assembly files and public scanner APIs.
The Microsoft `grade-tests` rubric assigns all eight tests A (90–100 band):
their oracles include exact assembly identities, observed file names,
negative results, a concrete `FileNotFoundException.FileName`, and scanned
types. No test uses sleeps, mocks, or assertion-free execution.

## Gates and measured effect

- Final Release Unit/Architecture build: zero warnings and zero errors.
- Complete Unit/Architecture gate on final bytes: 9,920/9,920 passed; zero
  failed and zero skipped.
- Focused Core Unit with Microsoft CodeCoverage on final bytes: 6,262/6,262
  passed; raw report `artifacts/coverage-a-plus-20260922-assembly-scan/core-unit-final-bytes.cobertura.xml`.
- In that report, `AssemblyFinder.MoveNext` covers 24/25 lines and 13/16
  branches, CRAP 272 → 16.02. `FindTheCallingAssembly` covers 13/13 lines
  and 10/12 branches, CRAP 156 → 12. These are targeted results, not a new
  35-report product aggregate.
- The adversarial read-only review first found the filename collision and
  then tightened the disappeared-file and EXE test oracles. Its final
  re-review returned PASS with no remaining concrete finding.

The default .NET assembly load context may reuse an assembly with the same
manifest identity from its cache or host probing paths. This phase proves
manifest identity selection for files whose names collide with another
assembly; it does not promise distinct runtime assemblies for two files with
the same manifest identity. See [Microsoft's default probing description](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing).

The complete product-wide A+ requirement remains open. The `e1a965290`
aggregate is the last complete comparison profile; the changed Core bytes
require a fresh 35-report aggregate before any new global claim.
