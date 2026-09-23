# Consumer-definition concurrency policy contract slice

## Scope

- `ConsumerDefinition<TConsumer>` policy and legacy concurrency-limit setters in Abstractions.
- Four attributed methods execute five cases. The Abstractions requirement projection has four
  matching entries. The 36-report product-wide baseline remains `ac363722c`.

## Product defect corrected

`ConcurrentMessageLimit` previously stored an invalid input before creating its parallel policy.
The policy factory then threw, leaving the public legacy limit changed while the effective policy
still held the previous value. The setter now validates and creates the new policy before updating
either field. This preserves the last valid configuration after a rejected limit.

## Behavior checked

- Parallel, serial, and null policies project the correct legacy limit and reach the actual
  consumer configurator with the same policy identity when present.
- An untyped partitioned policy is rejected with the correct parameter name and cannot replace
  the prior effective parallel policy.
- The legacy limit creates a parallel policy at the inclusive maximum of 1,024 and clears both
  values when set to null.
- Limits zero and 1,025 fail and preserve the prior legacy getter, effective policy identity,
  and policy forwarded at configuration time. These two cases failed against the previous source.

## Verification

- Focused Microsoft CodeCoverage: 5/5 passed, zero failures and skips. Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/consumer-definition-focused.cobertura.xml`,
  SHA-256 `3d14ea25ba04ee4acb4d9a3eb9c2b0792475316b11cfdbd90d23a9416fde94f0`.
- Complete current-byte Abstractions CodeCoverage: 764/764 passed, zero failures and skips.
  Cobertura:
  `artifacts/coverage-a-plus-20260923-ac363722c/consumer-definition-abstractions.cobertura.xml`,
  SHA-256 `a84f316cd856feffb21858b3208e5b412c6d5717f1e6c32fd06e0fb2770c5f5c`.
- Complete final-byte Unit/Architecture solution: 10,189/10,189 passed, zero failures and skips.
- Read-only adversarial review: PASS. It found no surviving behavior mutant in the changed setter;
  the red regression observes both retained state and the actual configuration boundary.

## Focused method result

| Setter | Previous product-wide CRAP | Complete Abstractions lines | Reported branches | Current CRAP |
| --- | ---: | ---: | ---: | ---: |
| `ConcurrencyPolicy` | 72 | 9/9 | 100% | 8 |
| `ConcurrentMessageLimit` | — | 6/6 | 100% | 2 |

This is a targeted method result, not a refreshed product-wide aggregate. Global A+ remains open.
