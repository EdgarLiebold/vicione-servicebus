# NewId identity and boxing slice

## Behavior

The public `NewId` equality operator was unexecuted in the previous complete
product profile and scored CRAP 42. Existing value tests established typed
equality and order for selected values. The new four-case theory changes each
of the four stored 32-bit identity words independently. It checks `==`, `!=`,
typed and boxed `Equals`, nonzero comparison, and dictionary lookup. The equal
case checks the converse operators, equal hashes, and dictionary retrieval.
Two further tests verify `Equals(object)` and `CompareTo(object)` with equal and
distinct IDs, null, foreign types, and both order directions.

No product implementation was changed. These assertions reject omission of any
identity word and mismatches between typed, boxed, operator, and hash-based
comparison paths.

## Verification

- The focused `NewIdValueTests` class passed all 12 then-current cases before
  the final boxed boundary cases were added.
- The complete Abstractions suite with Microsoft CodeCoverage passed 774/774.
  Its final report is `/private/tmp/vsb-newid-final-v3.cobertura.xml`, SHA-256
  `189a77b3e7eecdd2c7db030b276e792dee14b016bbf7432f49401d4e16513259`.
- `op_Equality`, `op_Inequality`, `Equals(object)`, and `CompareTo(object)` each
  reach 100% lines and branches in that report. Their resulting CRAP scores
  are 6, 1, 4, and 6. The comparison and boxed-equality gains are local to
  this Abstractions run; a new complete product profile is still required.
- Read-only adversarial review returned PASS for identity, boxing, requirement
  mapping, and dictionary assertions.
- The full Release Unit solution build with warnings as errors passed with
  zero warnings and zero errors.
- The complete Release Unit/Architecture gate passed 10,242/10,242 with zero
  failures and zero skips. Its Architecture assembly completed in 7m 28s.

The tests prove per-word distinction. They do not prove the sign or unsigned
lexicographic ordering for each word, especially values with the highest bit
set. That ordering contract needs its own targeted test. The full A+ goal
remains open.
