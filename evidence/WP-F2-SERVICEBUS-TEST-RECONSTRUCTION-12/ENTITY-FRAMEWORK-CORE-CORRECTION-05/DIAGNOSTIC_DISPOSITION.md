# Diagnostic disposition

`diagnostics/fail-closed-missing-local-profile.log` is not positive evidence. It records the first
manual reconstruction of the required LocalIntegration command, which omitted the workflow-level
environment value `VICIONE_TESTS__Profile=LocalIntegration`.

The test infrastructure rejected all local configuration while the effective profile remained
`UnitArchitecture`. This is the required fail-closed behavior. The immediately following command
set the missing workflow-owned value, created fresh run-scoped PostgreSQL and Azurite resources,
and passed `70/70`; that raw result is `positive/local-integration-70.log`.

No credential, endpoint, product source, test source, or committed test setting was changed between
the diagnostic and positive runs.
