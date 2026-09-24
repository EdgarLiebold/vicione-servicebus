# Azure Service Bus host authentication and port contract

## Product findings

The previous complete product profile (`b4d370ac5`, 36 reports) placed
`ConnectionContextFactory.CreateConnection` at CRAP 42.08 with 33/42 lines
covered. Review of its public configuration path found several behavioral
errors before adding coverage cases:

- Assigning an endpoint-only connection string through the setter retained it
  as an active authentication mode, unlike the string constructor. It wrongly
  rejected separately supplied named-key, SAS, and token credentials.
- Incomplete or mixed shared-access fields, duplicate/missing endpoints,
  entity-bound strings, and null explicit credentials could leave ambiguous
  authentication or permit ambient credential fallback.
- Credentialless emulator/custom-port strings could lose SDK-only transport
  settings. Credential-bearing custom-port strings without effective emulator
  mode were accepted even though the credential constructors omit the port.
- Host and entity URI formatting dropped a non-default port. The factory's
  context also dropped that port, so derived input addresses advertised a
  different namespace address from the one used by the SDK client.
- Caller-supplied messaging clients could point at a different namespace from
  the configured host, causing topology addresses and message operations to
  disagree.

The configurator now validates and normalizes both connection-string entry
points before mutation, rejects conflicting credentials and namespace changes,
and follows the SDK's last-valid-value handling for repeated emulator flags.
Custom ports require either a credential-bearing emulator string or both
caller-supplied SDK clients. The factory keeps the configured namespace
authority and port when it creates a context, while removing any entity scope.
The public client constructor and factory both reject a messaging-client
namespace mismatch.

## Behavioral evidence

The new tests exercise all credential mode orders and replacements, failed
setter atomicity, malformed endpoint and key fields, emulator flag precedence,
schema-free local endpoints, custom-port host/entity round trips, and the two
allowed factory client routes. The partial-client matrix rejects each missing
client separately. The supplied administration client is observed through an
actual `CreateQueueAsync` call with the exact queue name, cancellation token,
and sentinel exception; the messaging client is observed through its processor
factory. A scoped host proves that the factory strips the scope while retaining
the port in a derived entity input address. Mismatched namespaces are rejected
at both configuration and factory boundaries.

The endpoint-only setter, null explicit credential, and schema-free emulator
cases failed before the corresponding product fixes. The tests were reviewed
against concrete mutants rather than selected for line-count growth.

The Microsoft `code-testing-agent`, `test-gap-analysis`, `assertion-quality`,
`run-tests`, and `coverage-analysis` skills guided test design, adversarial
checks, execution, and measurement. The read-only Red Team returned PASS after
the last namespace and scoped-address regression checks.

## Gates and next measurement

- Azure Service Bus Unit: 311/311 passed.
- Release Unit/Architecture build: zero warnings and zero errors.
- Complete Unit/Architecture test gate: 10,332/10,332 passed, zero failures or
  skips, in 4m 05s.
- A new exact-commit product-wide coverage and CRAP profile remains required
  before this iteration can claim a measured global improvement. The older
  `b4d370ac5` profile remains the comparison point, not evidence for this diff.

The wider A+ goal remains open; the prior complete profile measured
83,851/93,466 lines (89.7128%), 30,008/36,616 conservative branches
(81.9532%), and 45 methods above CRAP 30.
