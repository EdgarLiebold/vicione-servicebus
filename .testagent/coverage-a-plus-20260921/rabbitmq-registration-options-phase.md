# RabbitMQ registration options and explicit credentials

## Product findings and corrections

RabbitMQ startup validation explicitly allowed non-null empty credential values
for brokers that accept anonymous authentication. The registration factory
discarded each empty value independently, and the client-factory projection
also treated an empty value as absent. An explicitly anonymous configuration
therefore fell back to RabbitMQ.Client's `guest` defaults. Mixed empty/nonempty
credential pairs could likewise be changed silently.

The corrected registration preserves both option values exactly. The client
projection now distinguishes an explicitly empty string from an unspecified
`null`: empty values reach RabbitMQ.Client unchanged, while `null` retains the
client default. No trimming or pairing rule is introduced.

## Hard-test and adversarial process

The first draft red run executed three cases and failed all three. One failure
revealed an invalid test assumption: the client-provided connection name is
passed to `CreateConnectionAsync` rather than stored on `ConnectionFactory`.
After correcting that oracle, the unchanged product passed the complete
nonempty options case and failed the two actual empty-credential contracts.

The final three attributed methods execute eight deterministic cases. They
cover:

- the full dependency-injection projection of host, port, virtual host,
  credentials, connection name, TLS name, certificate path and passphrase,
  protocol, client-certificate identity, and certificate policy;
- strict certificate validation with `Trust=false` and the explicit relaxed
  policy with `Trust=true`;
- standard TLS disabled at both host-settings and RabbitMQ.Client boundaries;
- normal, both-empty, both mixed empty/nonempty, and whitespace-preserving
  credential pairs; and
- the distinction between URI-explicit empty credentials and unspecified
  host-configurator credentials that retain the client defaults.

Three adversarial FAIL rounds found surviving mutants for disabled
client-certificate identity, paired or trimmed credentials, forced TLS, and
unconditionally relaxed certificate validation. Each received an exact
counterexample. Final read-only review returned PASS with no concrete
remaining product mutant or test-quality defect. The tests stop at the public
DI, host-settings, and RabbitMQ.Client configuration boundaries; they do not
claim live-broker authentication coverage.

## Verification and measurement

- Exact source/test commit: `22d0f5800`.
- Focused options and credential cohort: 8/8 passed, zero failures and skips.
- Complete RabbitMQ Unit project with Microsoft CodeCoverage: 386/386 passed,
  zero failures and skips.
- Complete current-byte Unit/Architecture gate: 10,156/10,156 passed, zero
  failures and skips.
- Exact detached checkout passed locked Engineering restore and a Release
  RabbitMQ-test build with zero warnings and zero errors.
- Exact detached complete Unit/Architecture gate: 10,156/10,156 passed, zero
  failures and skips.
- Exact coverage report:
  `artifacts/coverage-rabbitmq-registration-exact-22d0f5800/rabbitmq.cobertura.xml`,
  SHA-256 `5c05cfdce64d3719c0b5f7a9dcce953007a1ce48aa819cb204201bc07bec8570`.

| Exact-commit target method | Lines | Reported branches | Complexity / CRAP |
| --- | ---: | ---: | ---: |
| `RabbitMqRegistrationBusFactory.CreateBus` | 8/8 | 0/0 | 1 |
| host-options projection closure | 11/11 | 4/4 | 4 |
| TLS-options projection closure | 13/13 | 8/8 | 8 |
| `RabbitMqAddressExtensions.GetConnectionFactory` | 43/46 | 22/28 | 28.22 |

The former CRAP-72 registration closure is now fully covered and split by the
compiler into methods with maximum CRAP 8. `GetConnectionFactory` remains
partially covered, but its measured CRAP is below the threshold at 28.22; the
remaining lines and branches belong to other routing and metadata variants.

The mandatory Microsoft `code-testing-agent`, `run-tests`,
`coverage-analysis`, `crap-score`, `test-gap-analysis`, `assertion-quality`,
and `grade-tests` workflows governed test design, execution, measurement,
mutation-style review, assertion review, and the quality gate. This focused
result does not replace the product-wide profile; global A+ remains open.
