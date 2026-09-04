# A+ final closure assertion and gap review

Date: 2026-09-04

## Scope

The final review covers the changed Durable Sender, host validation, provider-matrix, SDK/dependency,
API-layering, discoverability, and heritage owners. The mandatory repository-wide static source/test
pairing pass was performed once before implementation; it was used as a routing aid, not presented as
coverage.

The four directly changed behavioral/architecture test owners contain 78 assertion calls. They use
exact equality, exception type, positive/negative boolean, single-element cardinality, byte equality,
string containment/absence, metadata reflection, ordering, and source/package structure. No changed
test is assertion-free, tautological, random, delay-dependent, or based only on mock verification.

## Requirement-to-oracle review

| Changed behavior | Positive oracle | Negative/boundary oracle | Independent mutation |
|---|---|---|---|
| RabbitMQ transport acceptance | exact bytes, IDs, content type, persistence, result mode, post-stop broker retention | unroutable return and pre-canceled no-publish | M01-M03 |
| Durable provider honesty | supported providers require a named acceptance boundary and environment; RabbitMQ requires `real-*` | every unsupported provider requires `not-applicable` and no dispatcher | M02-M03 plus provider matrix architecture owner |
| Host lifecycle options | coherent positive timeouts pass `IStartupValidator` | zero/negative values and consumer-stop greater than stop fail with actionable property text | M04 |
| SDK selection | MTP stays selected and workflow follows `10.0.x` | any `global.json` SDK block or exact workflow patch fails | M06 |
| Exact dependency graph | all solution restores pass in locked mode | central version/lock divergence fails with NU1004 | M07 |
| Five API layers | app types visible; advanced types public but hidden; Testing packages engineering-only | leaked advanced type or Testing package in Shipping fails | M05 |
| Preferred journey | 14 package-only scenarios compile and use focused callbacks | advanced/historical tokens and loss of preferred retry/concurrency paths fail | M08 |
| Heritage disposition | all 12 entries have exact capability-based terminal decisions and real evidence paths | unknown, pending, wrong-but-valid, or restored MassTransit product identity fails | M09 |

## Gap verdict

One material gap was found and closed: the heritage test originally accepted any allowed disposition
for any identifier. The new exact map prevents semantic relabeling. No critical or high-risk gap
remains in the changed behavior after nine one-cause mutations. The real RabbitMQ cases exercise the
provider rather than a substitute; unproved external providers remain explicitly unsupported and are
therefore outside the advertised Durable Sender capability rather than silently counted as covered.
