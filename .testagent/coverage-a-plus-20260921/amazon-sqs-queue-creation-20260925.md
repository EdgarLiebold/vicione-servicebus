# Amazon SQS queue creation test phase, 25.09.2026

## Scope and product contract

Commit `29356de9e` adds `QueueCacheCreationTests` without changing product
code. The tests exercise the actual `QueueCache.GetAsync` path through an
AWS client proxy. They verify the exact create request (queue name, all
declared queue attributes and tags, no subscription-only attribute), the
returned queue URL and ARN, durable cache identity, FIFO flag inference,
rejection of an unsuccessful create response, and a second lookup after a
successful create followed by a failed attribute read. The latter must
discover the now-existing queue without issuing another create request.

## Test quality and adversarial checks

- The focused class passed 5/5 tests; the full Release SQS suite passed
  236/236, with zero failures and skips.
- A temporary mutation changing the inferred FIFO attribute from `true` to
  `false` failed the targeted test in its implicit-FIFO case. The mutation
  was reverted, and the focused class subsequently passed 5/5 again.
- Assertion review found no assertion-free or trivial-only test in this
  four-method class. Request fields, call order/count, provider status,
  cache identity, and retry state are checked with independent outcomes.
- The first adversarial read-only review found three P2 evidence gaps:
  an overbroad FIFO claim, no assertion on the `All` attribute request, and
  no post-create attribute-failure retry. All three were corrected. The
  final read-only re-review reported PASS with no remaining concrete P1/P2
  finding in this slice.

## Exact-commit coverage receipt

`artifacts/coverage-receipt-amazonsqs-29356de9e/receipt.json` verifies
236 tests, 11 unchanged binaries, and 1,423 tracked product sources on
commit `29356de9e`. The repository aggregator accepted it in partial mode
at `artifacts/coverage-sqs-29356de9e.json`.

The `ViciOne.ServiceBus.AmazonSqs` package in this **single SQS unit
receipt** has 69.02% line and 69.49% branch coverage. The queue-creation
async method is covered at 15/15 unique method lines and has methodical
CRAP 16, compared with 0/15 and CRAP 272 in the preceding SQS unit receipt
on `c476ddf1d`. SQS methods above CRAP 30 fell from 14 to 13. These
single-project figures do not establish product-wide A+.

The next measured SQS risks include uncovered receive-state paths and
publish-topology type discovery. The strict product gate still requires
all same-commit product-unit, provider, no-AVX2, and scalar receipts.
