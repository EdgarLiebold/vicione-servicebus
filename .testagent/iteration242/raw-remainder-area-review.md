# Historical 100-path source remainder: area review

The historical ../source-read-remainder.txt is the Git-derived exception set at
bac2c88f91f08fbef7cbc4ca6d391ced51c31793, relative to the PO baseline
e01a5e5eb3411412229221bd58b170b583ce6caa. It contains 100 paths, not
an assertion that 100 paths remained unread at the start of this iteration.
The separate first-read reconciliation records the current-byte reading
evidence. These paired dispositions record a narrower source-area decision;
they are not product-wide A+ approvals.

The first 50 paths have the existing source-admission.tsv /
source-dispositions.tsv pair. The other 50 have
remainder-source-admission.tsv / remainder-source-dispositions.tsv.
The latter admission file records exact current SHA-256 values and line counts;
no product file or comment was changed to make the ledger. A set comparison
against the historical 100 paths found 100/100 with a disposition entry.

## Area findings

- The runtime and configuration group was reviewed with its owner paths and
  tests by a separate read-only adversarial reviewer. The one concrete defect
  was RabbitMQ address caching: a host address read before UseSsl() retained
  the old scheme; the receive endpoint repeated the problem. A further
  adversarial pass found that changing host settings after Build could diverge
  from the runtime address snapshot. All three cases were demonstrated by
  focused red tests, corrected in 17f5dc4bd, and passed in the 239/239
  RabbitMQ Unit suite and 31/31 real local RabbitMQ suite. The final read-only
  review found no further concrete issue in that diff.
- The other runtime declarations cover cache-entry/key state, observer
  finalization, in-memory outbox identity, timeout and circuit-breaker
  configuration, journal terminal observers, registration matching,
  initializer name conversion, entity-name shortening and correlation IDs.
  The area review found no further reproducible defect. The surrogate-pair
  boundary in EntityNameShortener was corrected earlier in this iteration.
- A separate read-only adversarial pass checked the 14 compile-only global
  using files, nine JetBrains .DotSettings, four package locks, the internal
  initializer adapter and three graph declarations. It found no concrete
  source defect. Direct lock dependencies and central Roslyn versions matched
  their project files. A locked-mode restore and the final physical
  namespace-layout decision were outside that static check.

The current complete-source A+ gate remains open for the other src paths,
public API/parameter mapping, test reverse mapping, product-wide coverage and
CRAP disposition, and formal external review. The new 50 paired rows should
be read as current-byte review evidence, not a PASS for those broader gates.
