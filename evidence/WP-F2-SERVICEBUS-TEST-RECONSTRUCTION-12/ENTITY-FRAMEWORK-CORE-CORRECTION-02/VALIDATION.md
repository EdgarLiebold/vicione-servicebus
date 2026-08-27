# Entity Framework Core and in-memory outbox correction validation

## Frozen technical subject

- Technical commit: `6bf471d954fb50138473cb1f2c0275362df5aebe`
- Technical tree: `098f6dcb9bae0c69f626d3cecbeb41cd91327738`
- Parent evidence commit: `a5ca0bd179fdd101f00760799f349b3a960e02e0`
- Previous technical correction: `6632dfa2017348f94a9f18e35bb2a08415dadc77`
- SDK: `/usr/local/share/dotnet/dotnet` 10.0.302

The final correction closes two independent retry-state defects found by the product Red Team and
one missing causal owner found by the test/evidence Red Team:

1. failed scheduled-message cancellation remains recoverable instead of being removed before the
   cancellation has succeeded;
2. rollback cleanup failure blocks provider re-entry while preserving and rethrowing the exact
   original business failure;
3. a Batch outbox checkpoint composes parent and every child outbox, and the real outer checkpoint
   path causally delegates scheduler rollback.

The test-only context factory remains xUnit-free and source-mirrored under `tests2/Testing`; behavior
and assertions stay in the owning Core and EF test projects. Product defects were fixed in product
code rather than accommodated in tests.

## Positive verification

Locked Engineering restore passed. The complete Engineering Release build passed with zero warnings
and zero errors. The unfiltered native UnitArchitecture profile passed **1,886/1,886**, the focused
EF UnitArchitecture project passed **55/55**, the real PostgreSQL/Azurite LocalIntegration profile
passed **70/70**, and the focused real-PostgreSQL EF profile passed **59/59**. Every test run reports
zero failure and zero skip.

The first manually composed LocalIntegration evidence command omitted
`VICIONE_TESTS__Profile=LocalIntegration`; the fail-closed configuration correctly rejected it as a
UnitArchitecture profile. The retained positive log is the subsequent canonical run with the
documented profile variable. No code or test was changed in response. Similarly, the CI-tool suite
was correctly re-run outside the filesystem sandbox because eight process-tree sabotage cases
deliberately invoke `ps`; the retained log reports 253/253. These execution-boundary facts are
recorded to prevent future trial-and-error diagnosis.

## Provider-variant closure

The inherited 90 EF obligations expand to 156 execution identities. The retained and mechanically
validated matrix remains unchanged: 77 executing native identities, 39 genuinely provider-neutral
consolidations, and 40 visible SQL Server/Azure SQL `EXTERNAL_PENDING` identities. Pending is never
counted green. `R0_VARIANT_DISPOSITIONS.json` and
`R0_VARIANT_DISPOSITION_VALIDATION.json` are copied byte-for-byte into this final evidence root.

## Negative verification

M07 through M11 are byte-exact, one-cause probes. Each mutant built cleanly, its single selected
native xUnit/MTP test failed with exit code 2 for the intended behavior, and the isolated worktree
was restored to the frozen technical commit before the next probe. Exact replacements, baseline and
mutant hashes, complete argument vectors, CTRF results and restore hashes are bound in
`MUTATION_EXECUTION.json`; their technical purpose is explained in `MUTATION_MANIFEST.md`.

## Verdict

The technical correction and local execution evidence are complete. Acceptance remains withheld
until the generated `CHANGELIST.md`, identity/change-list gates, final status attestation, and both
independent read-only Red-Team reviews pass against their final commits.
