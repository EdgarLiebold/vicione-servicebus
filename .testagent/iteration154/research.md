# Iteration 154 — EF MessageJournal research

Personal review covers six product files / 353 lines and both owning test files / 759 lines. The
existing 20-case unit owner proved mapping/configuration but left `AppendAsync` entirely dependent
on unavailable PostgreSQL. Running the public relational store on SQLite reproduced a product
failure: SQLite cannot translate the mapped `DateTimeOffset` retention comparison.

The mapping now persists `ObservedAt` as UTC ticks, retaining instant semantics while making
retention ordering/comparison provider-neutral. Six new SQLite cases prove append, retention,
capacity, invalid inputs, cancellation, duplicate rollback, minimum timestamp and cache-key
overloads. Four isolated compiled mutants are killed. Final owner coverage is 132/133 lines
(99.2481%) and 16/16 branches, maximum CRAP 4.
