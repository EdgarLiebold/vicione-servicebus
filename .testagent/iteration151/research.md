# Iteration 151 — Azure Table research

The packet personally reads all 37 product C# files / 2,585 lines and all 16 owning unit/local C#
files / 4,263 lines, plus both projects and requirement projections.

Review covers public composition, table/key validation, DI lifetime, saga load/insert/update/delete,
exact ETag concurrency classification, cancellation identity, native and serialized property
conversion, Azure property limits, atomic bounded journal repair, foreign-row ownership, and the
provider-level Saga, Future, Courier and Job Service scenarios. No current product defect was
reproduced. Microsoft primary documentation confirms that PartitionKey and RowKey are bounded at
1,024 characters, so the Unicode boundary test and current validator are correct.

The unit project lacked the centrally pinned repository-standard
`Microsoft.Testing.Extensions.CodeCoverage` reference. Adding it and deterministic lock entries
enables native coverage without changing product or C# test behavior. Instrumentation reports
1,456/1,788 lines, 408/528 branches, complexity 292 and 156 methods, with no CRAP above 30.

Static pairing finds 33/37 direct name pairs. The four internal validators/converters missed by
name are exercised indirectly by key, table-name, composition and entity-conversion tests. The
local requirement projection passes; all 26 real provider cases stop before product behavior
because profile `UnitArchitecture` lacks Azure Table endpoint settings and credentials.
