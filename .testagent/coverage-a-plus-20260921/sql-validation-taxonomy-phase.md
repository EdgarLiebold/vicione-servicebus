# SQL receive validation and SQL Server taxonomy CRAP slice

## Scope and behavior

- `SqlReceiveEndpointConfiguration.Validate` retains every queue, maintenance, polling, locking,
  delivery, receive-mode, and base-configuration check in the same observable order. Queue and
  delivery validation now have separate methods so no single method exceeds the CRAP gate.
- `SqlServerConnectionContext.IsTransientErrorNumber` retains the exact closed set of 14 transient
  SQL Server error numbers in a sorted lookup table. This removes switch-generated branch
  complexity without broadening or narrowing the retry taxonomy.
- The existing invalid-endpoint test now asserts the complete ordered result sequence, including
  disposition, key, value, and exact message. This closes its prior omission of the invalid
  `LockDuration` result.
- A positive endpoint test proves every accepted lower boundary together with the complete legal
  queue-name character set.

## Hard behavior evidence

- SQL Unit/Contract project: 191/191 passed, 0 failed, 0 skipped.
- Exact SQL Server transient-number provider contract: 15/15 passed, covering all 14 transient
  numbers and a permanent counterexample.
- Complete Release Unit/Architecture profile: 9,841/9,841 passed, 0 failed, 0 skipped.

## Coverage and CRAP

Fresh focused report:
`artifacts/coverage-a-plus-20260921-sql-validation-taxonomy`.

| Baseline hotspot | Baseline CRAP | Reviewed result |
| --- | ---: | --- |
| `SqlReceiveEndpointConfiguration.Validate` | 34 | CRAP 6.1 plus helpers at 14 and 18; helper lines 24/24 |
| `SqlServerConnectionContext.IsTransientErrorNumber` | 38 | CRAP 2; exact behavior separately exercised by 15 provider cases |

The focused Unit report observes five assemblies. The SQL Server test project does not carry the
coverage extension, so its filtered provider run is retained as behavior evidence while the next
product-wide collection will measure it through the campaign's measurement overlay.

## Microsoft grade-tests assessment

The added positive-boundary test and the hardened invalid-boundary test both receive grade **A**
under the Microsoft `dotnet/skills` xUnit guidance. The reused provider taxonomy test also receives
grade **A**: it asserts each owned transient code and an unrelated permanent code. The
test-anti-pattern review found 0 Critical, 0 High, 0 Medium, and 0 Low findings.

## Pseudo-mutation discriminators

- Removing, reordering, weakening, or relabeling any validation result changes the exact projected
  tuple sequence.
- Rejecting an inclusive lower boundary or a legal queue-name character fails the positive test.
- Omitting, adding, duplicating, or misordering a transient number breaks either the binary search
  or one of the exact provider cases; classifying an unrelated code as transient fails the permanent
  counterexample.

## Adversarial review

The read-only reviewer verified that every validation remains present in the original order, base
results are still appended with the same parent key, and the lookup table is strictly ascending,
duplicate-free, and identical to the former switch. Final result: **PASS with no concrete
findings**.
