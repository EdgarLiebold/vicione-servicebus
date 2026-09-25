# Header-value wire conversion

Code and test commit: `2491c6df8`.

The source review found that `default(HeaderValue<string>)` was accepted as a
text header despite having no key or value. The first new test reproduced
this failure. Read-only adversarial review found a second failure: default
numeric and Boolean typed headers threw `ArgumentNullException` while being
converted. The expanded test reproduced that exception before the fix.

`HeaderValue<TValue>.IsStringValue` now uses the shared checked conversion.
Both shared conversion helpers reject a missing key with `false` and a default
output. Four behavioral tests verify default typed and untyped headers, exact
preservation of a valid typed string, URI text and key preservation, and a
value type whose `IFormattable` implementation returns no text. The
Abstractions requirement projection contains matching variants. The final
read-only red-team review found no P1/P2 issue.

A deliberate mutation removing the key guard from the scalar helper made
`DefaultHeaders_CannotProduceWireValues` fail with the expected missing-key
exception. The source was restored to SHA-256
`df553a39d58ab949c172bbb683f36fbc26e1c966b754b7fd4e82ad9d31369a69`,
rebuilt, and all nine `HeaderValueTests` passed.

On the exact code commit, Microsoft Testing Platform passed the full
Abstractions suite (917/917) with coverage and the Core regression suite
(6,439/6,439) using the new Abstractions assembly. Coverage report:
`artifacts/coverage-header-value-2491c6df8/coverage.cobertura.xml`, SHA-256
`98f8334e268d95bf2ac51a9e52e05f56436f47cb5faff3499d1f8600a6ddd08e`.
In this Abstractions-only report, the assembly has 70.2405% line and
72.5347% branch coverage. `HeaderValue.IsValueStringValue` has 19/21 lines,
93.75% branches, CRAP 16.22 (previously 10/18, 71.43%, CRAP 31.21);
`IsValueSimpleValue` has 15/17 lines, 92.86% branches, CRAP 14.32. The
overall assembly rates cannot be treated as a product-wide A+ result because
other test assemblies also exercise it and provider profiles remain separate.

The global A+ goal remains open. These tests used cached SDK dependencies;
the complete provider coverage profile still requires a working broker host.
