# Event Hubs receive-header contract

The last complete product-wide profile at `44d9e3254` reported the `GetAll` iterator at
CRAP 72.75 with none of its nine lines covered. The header provider exposes Event Hubs
identifiers and application properties to the receive pipeline. Review found two observable
boundary errors: construction accepted a missing SDK event, and lookup treated whitespace-only
message and correlation IDs as present even though enumeration omitted them.

Four source-owned tests cover the public provider directly:

- `Constructor_RejectsMissingEventAtTheBoundary` checks the exact rejected argument.
- `GetAll_ProjectsPresentIdentifiersAndApplicationValuesWithoutNulls` checks both identifiers,
  a numeric property, an empty-string property, and omission of a null property.
- `BlankIdentifiers_AreAbsentFromEnumerationAndLookup` checks that both read APIs omit blank
  IDs and leave unrelated application data available.
- `TryGetHeader_UsesCaseInsensitiveIdentifiersAndExactApplicationKeys` checks positive mapped
  lookups and exact-case application lookup, including null and missing values.

Before the product correction, the focused red run failed 2/4: the constructor did not throw,
and a blank message ID was reported present. The correction rejects null `EventData` at
construction and gives `TryGetHeader` the same nonblank identity rule as `GetAll`, returning
null for an absent value. The Release test-project build passed with zero warnings and errors.

The focused .NET 10 xUnit/MTP run passed 4/4 with Microsoft CodeCoverage and xUnit TRX. Its
coverage report is `artifacts/coverage-a-plus-20260922-8abfe1e8a/eventhub-header-focused.cobertura.xml`,
SHA-256 `0ea04036d48c888464bf52ac9193105f35de64e43354deae7fcb53565ad5a74b`; the TRX is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/eventhub-header-focused/eventhub-header-focused.trx`,
SHA-256 `0e3eed17dac500bfa2b8700faa4a112203041e0102fa14fa62a9b3e3834f53d3`.

The complete real Event Hubs/Azurite emulator suite passed 61/61, zero failures and skips,
with Microsoft CodeCoverage and machine-readable TRX. Run `vicione-b58fde6bb7cb` has empty
fixture findings. Its report is
`artifacts/coverage-a-plus-20260922-8abfe1e8a/eventhub-header-full.cobertura.xml`,
SHA-256 `e3938362c21a7a55cca8adb12a3c8b4f04939bb79cc52613e632ecdb7a1e5597`;
its TRX is `artifacts/coverage-a-plus-20260922-8abfe1e8a/eventhub-header-full/eventhub-header-full.trx`,
SHA-256 `731e0a6f44d857f05316e4c866e1b4772b55790f9e2d4c30e53b584f1ca60f77`.
The TRX records 61 passed test results. The same-run coverage reports 3/3 constructor lines,
17/17 lookup lines, and 9/9 iterator lines, each with full reported branch coverage. The
targeted iterator CRAP is 8; `TryGetHeader` is 12.

The complete Engineering Release build passed with zero warnings and errors. The complete
Unit/Architecture gate passed 9,997/9,997 with zero failures and skips. The hashes of both
broker logs match the empty fixture-findings record.

Microsoft `code-testing-agent` and `run-tests` guided the regression design and .NET 10 MTP
execution; `coverage-analysis` guided the targeted risk review. The final independent read-only
adversarial review returned PASS after checking the test oracles, requirement projection, and
actual local Azure SDK acceptance of whitespace IDs and null application properties. The last
complete product-wide profile remains `44d9e3254`; global A+ remains open.
