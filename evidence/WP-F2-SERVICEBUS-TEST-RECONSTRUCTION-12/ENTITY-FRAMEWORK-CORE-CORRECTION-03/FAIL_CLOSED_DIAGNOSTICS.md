# Fail-closed invocation diagnostics

Two operator invocations were intentionally not counted as product or test failures:

1. A direct `LocalIntegration` solution run omitted the canonical fixture runner. The test platform
   rejected all resource-backed cases because PostgreSQL and Azure Table credentials were absent.
2. The first fixture-runner invocation omitted `VICIONE_TESTS__Profile=LocalIntegration`. The runner
   supplied fresh resources and credentials, but the test platform rejected their use under the
   default `UnitArchitecture` profile.

No source, project, configuration, requirement, test or floor changed in response. The successful
proof uses the documented three-part boundary: explicit `LocalIntegration` profile, canonical
`run_broker_category.py` ownership of fresh PostgreSQL/Azurite resources, and the unfiltered MTP
command with its predeclared floor. This is the reusable diagnosis for future local runs.
