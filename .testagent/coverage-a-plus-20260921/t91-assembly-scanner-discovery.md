# T91 — assembly scanner discovery and filters

Exact test commit: `d1523e474`.

## Product contracts checked

`ExecutableExtensionScan_LoadsManagedAssemblyAfterInvalidDllImage` copies a
managed test assembly to a file with an `.exe` extension and places a corrupt
`.dll` beside it. It proves the DLL-only scan adds nothing, while the scan
that includes `.exe` files continues after the corrupt DLL and discovers the
real test type. This checks the extension-selection and bad-image contracts;
the copied file is a library with a renamed extension, not an executable with
an entry point.

`PathScan_AssemblyPredicateRejectsLoadedDecoys` checks both path overloads
with a managed `.dll` or `.exe` candidate plus a decoy assembly. The callback
sees both loaded assembly identities, accepts only the candidate, and the
scanner exposes exactly the selected assembly and expected type.

## Verification

- Focused `AssemblyScannerTests`: 11/11 passed, no failures or skips.
- Complete Core project on exact commit `d1523e474`: 6,960/6,960 passed,
  no failures or skips.
- Isolated counterprobe changed only `AssemblyFinder`'s
  `BadImageFormatException` continuation to an early exit. The new test
  failed with expected scanner count 1 versus actual 0. Product source was
  restored with no diff, and the focused class passed again.
- Read-only Red Team found two P2 oracle issues: unspecified order among two
  `.exe` files and wording that overstated the renamed library as an
  executable. Both were corrected. Final re-review: PASS, no P1/P2.
- Both new `REQ-VSB-ASSEMBLY-SCAN-FILTER` variants are projected in
  `CoreRequirements.json`.

## Measurement boundary

No product source changed. T85 remains the latest complete 33-profile
Line/Branch/CRAP checkpoint. Global A+ remains open; the next complete profile
follows the agreed larger packet cadence.
