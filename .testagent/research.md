# Research — Abstractions test owner

## Sources read

- the relevant implementation under `src/ViciOne.ServiceBus.Abstractions`;
- all 19 C# files and the 549-value formatter corpus under the inherited
  `tests/ViciOne.ServiceBus.Abstractions.Tests` project;
- all 78 final semantic-ledger entries owned by that inherited project;
- the complete existing native test tree, its package graph, build rules, and requirement verifier;
- the mandatory Microsoft .NET test and MSBuild work rules.

## Disposition

The product behavior is split by resource boundary:

- deterministic exception-filter, type-extension, NewId value, formatting, parsing, generation,
  ordering, interoperability, and concurrency behavior belongs to `UnitArchitecture`;
- the two providers that read the actual host name or physical network interfaces belong to
  `LocalIntegration`;
- adapter enumeration/console-output cases, a BCL `ArgumentNullException.Message` observation, and
  compile-disabled ordering experiments are not product verdicts and are not recreated as tests.

The 549-value formatter corpus is embedded in the new assembly byte-for-byte. Random Guid and clock
inputs were replaced with fixed external values. Generation tests use injected tick, worker, and
process providers; the concurrency contract uses a private deterministic generator instead of
mutating the process-wide static generator. All executable methods carry passive requirement
metadata and are checked against one projection per assembly.

No production source or inherited test has been edited or deleted.
