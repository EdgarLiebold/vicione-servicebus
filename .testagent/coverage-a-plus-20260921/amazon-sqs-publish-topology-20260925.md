# Amazon SNS publish topology input validation, 25.09.2026

## Product defect and correction

Commit `0487b4f0b` corrects `AmazonSqsPublishTopologyConfigurationExtensions`.
Previously, a null configurator or explicit type list produced a
`NullReferenceException`. Explicit registration could publish valid types
before a later null or invalid contract failed. Namespace discovery could
also publish earlier matches before a lazy user filter threw on a later type.

The extension now validates named required arguments, materializes both
selections before publishing, and validates all explicit message contracts
with `MessageTypeCache`. Invalid inputs and filter failures therefore do
not leave partial registrations. The Changelog records this product fix.

## Behavioral tests and adversarial review

- Five `AmazonSqsPublishTopologyDiscoveryTests` passed. They check valid
  closed contract selection, exact published type and per-call callback
  identity, explicit order, required argument names, and zero registration
  after a late invalid type or filter failure.
- The initial focused run failed 2/4 tests on the original product code:
  missing configurator/list produced `NullReferenceException` instead of
  named argument errors. After the first fix, the Red Team found two
  additional P2 paths. Both new counterexamples failed 2/5 before the
  second fix: a lazy filter left one type registered, and a later invalid
  `string` contract was accepted by the test proxy after an earlier valid
  registration. The corrected focused suite passed 5/5; the full Release
  SQS suite passed 241/241 with zero failures/skips.
- Assertion review found no assertion-free or trivial-only test in this
  five-method class. The tests compare published sets/order, exact callback
  pairing, exception identity and parameter names, and the absence of
  partial state. The final adversarial read-only review reported PASS and
  no remaining concrete P1/P2 finding in this slice.

## Exact-commit receipt

`artifacts/coverage-receipt-amazonsqs-0487b4f0b/receipt.json` verifies
241 tests, 11 unchanged binaries, and 1,423 tracked product source files
on `0487b4f0b`. The repository aggregator accepted the single receipt in
partial mode at `artifacts/coverage-sqs-0487b4f0b.json`.

Within this **SQS unit receipt**, the `ViciOne.ServiceBus.AmazonSqs`
assembly reports 69.59% line and 70.55% branch coverage. The scanned
publish-topology overload now has 10/11 measured method lines and CRAP
12.11. SQS methods above CRAP 30 fell from 13 to 12 compared with the
preceding SQS receipt on `29356de9e`. These values are not product-wide
coverage or CRAP results. The strict product gate still requires all
same-commit product-unit, provider, no-AVX2, and scalar receipts.
