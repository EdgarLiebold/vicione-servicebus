# Amazon SQS queue ownership and batch admission, 25.09.2026

## Product changes

- `aabb1ce99` serializes queue resolution, durable ownership transitions, and
  removal per queue name. QueueInfo rejects work after disposal and waits for
  admitted policy and batch operations before releasing their resources.
- `c476ddf1d` retries SQS client send/delete once when the resolved QueueInfo
  was already disposed or a closing batch channel rejected the entry before
  admission. A provider failure after admission is not retried.
- Both commits were pushed to `origin/feature/servicebus-a-plus-api`.

## Tests and adversarial review

- Release SQS unit suite on `c476ddf1d`: 231/231 passed, zero failures/skips.
- Queue ownership tests cover both resolution races, caller cancellation,
  independent queue names, provider failure, removal, evicted references,
  policy write during disposal, and admitted send/delete work during disposal.
- Client tests cover stale QueueInfo for both send and delete, provider failure
  without replay, a two-attempt bound, and a full old batch channel whose
  waiting target entry succeeds exactly once on a fresh queue.
- Adversarial read-only review found no remaining concrete deadlock, lazy worker
  leak, or duplicate provider send in this scope. Repeated eviction remains a
  bounded error after two queue lookups.

## Fresh coverage receipt

`artifacts/coverage-receipt-amazonsqs-c476ddf1d/receipt.json` verified 231
tests, 11 unchanged binaries, and 1,423 tracked product source files. Its
head is `c476ddf1deffbfdaee1fc27f022da65d47582a6e`. The Cobertura report
is `artifacts/coverage-receipt-amazonsqs-c476ddf1d/coverage.cobertura.xml`.
The receipt was generated from a clean tracked tree with the repository's
`tools/ci/coverage_receipt.py` runner.

Within **this SQS unit receipt**, the `ViciOne.ServiceBus.AmazonSqs` package
reports 68.42% line coverage and 68.48% branch coverage. A direct method
calculation using the repository aggregation formula found 14 SQS methods with
CRAP above 30. The largest observed risks in this receipt include an
uncovered `QueueCache` async state machine (CRAP 272), two uncovered
`AmazonSqsMessageReceiver` paths (156 and 110), and uncovered topology and
policy branches. These values are specific to this one test project and do
not merge coverage from other test projects.

The verified partial aggregator output is
`artifacts/coverage-sqs-c476ddf1d.json` with `status: partial`. It includes
the Core and Abstractions assemblies because the SQS tests load them; its
14.76% line and 13.14% branch rates are **not product-wide values**. The
strict product gate still needs the other same-commit unit and local-provider
receipts, both Abstractions portability profiles, and all expected assemblies.

The first sandboxed receipt attempt for `aabb1ce99` stalled during locked
restore and was interrupted without a receipt. The fresh escalated run for
`c476ddf1d` completed successfully.
