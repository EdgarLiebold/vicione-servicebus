# Iteration 153 — EF relational-infrastructure research

The packet personally reads all 13 current C# files / 806 lines in the Entity Framework execution,
transaction-context, identifier-validation and lock-statement owner, plus three owning unit/local
test files / 1,141 lines and their project, requirement and deterministic lock support.

Review reproduced three product-boundary defects. The lock-statement cache encoded property
sequences with an in-band separator and therefore aliased a two-property sequence to a single legal
shadow-property name containing that separator. Explicit fallback schemas and model-supplied table,
schema and column names bypassed the existing portable identifier validator. Finally, an
unconfigured `DbContext` leaked EF Core's raw `InvalidOperationException` instead of the owner's
actionable `ConfigurationException`.

The cache key is now length-prefixed and culture-invariant. Every fallback or mapped relational
identifier is validated before formatting or caching. Runtime provider discovery retains the
original EF failure as the inner exception while exposing the stable ServiceBus configuration
contract. Eight focused test methods add twelve compiled cases for collision identity, provider
selection, schema semantics, identifier boundaries, required inputs, provider-less contexts and a
real SQLite outbox bus-ownership query.

All six hand-authored single-cause mutants compile and are killed by their designated test: restore
separator joining, bypass fallback validation, leak the EF provider exception, or bypass mapped
schema/table/column validation. The new tests contain no assertion-free, trivial-only or
self-referential case; they combine exact SQL/value, exception, parameter-name, message/type and
collection/cardinality checks.

Fresh owner instrumentation covers 249/261 lines (95.4023%) and 56/68 branches (82.3529%) across 61
methods. No method has CRAP above 30; the maximum is 14.4970. Isolated Roslyn pairing reports 10/13
direct pairs. `RelationalIdentifierValidator` and the PostgreSQL/SQL Server formatter files are
transitively exercised through their providers and runtime instrumentation; the pairing result is a
static name/reference heuristic, not a substitute for measured coverage.
