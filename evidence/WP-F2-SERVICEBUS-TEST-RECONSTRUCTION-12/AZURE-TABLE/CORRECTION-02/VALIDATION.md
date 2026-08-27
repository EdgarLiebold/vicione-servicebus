# Azure Table M14 fixture-evidence correction

This append-only evidence correction closes the sole MINOR from the independent review of technical
commit `497a7cd5570969bee13f35abb46bb37a7372dbee` and evidence commit
`9309c34e2aa3d2dedff750a21f9cc0174fb3a61f`. It changes no product, test, build or CI byte.

M14 was repeated with the identical one-cause source mutation: remove only the MessageJournal ETag
lease `UpdateReplace` action. The mutated source SHA-256 was
`6eecbc00d83356aac654bd6de7b6db9145ec178ffc0be3688ed12356c0fe02b3`; after the run the source was
restored to `9954ad2b44af0a701886fe92be3d5450b50da7e1739c2a186f2d51d3b635fa70`, exactly the technical tree.

The Release LocalIntegration project build completed with exit 0, zero warnings and zero errors. A
fresh run-scoped Azurite fixture `vicione-ded60e29fe87` was then started on loopback, its endpoint was
projected to the child only, and the exact single test ran under xUnit 4 / Microsoft Testing Platform.
It failed with exit 2, one test, zero pass, one failure and zero skip because all eight synchronized
appends persisted instead of exactly one. This is the intended causal verdict when the lease action
is absent.

The correction binds the complete wrapper stdout/stderr, raw CTRF, endpoint projection, captured
Azurite broker log and `fixture-findings.json`. The latter contains an empty findings array and the
same broker-log SHA-256 recorded in `M14_EXECUTION.json`, proving the runner completed its guarded
teardown without a fixture finding. `SHA256SUMS` covers every non-manifest file in this correction.
