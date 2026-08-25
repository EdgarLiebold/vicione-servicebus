# C33 one-cause mutation validation

Every mutation was applied alone, built in Release with locked existing assets, executed through the
native xUnit 4/MTP 2 artifact, observed red for the named cause, and reverted before the next case.

| Mutation | Expected owner test | Observed failure |
|---|---|---|
| Alias `Exception.Data` instead of snapshotting it | `Construction_DetachesDiagnosticDataFromTheSourceException` | snapshot data absent/aliased; test red |
| Replace ordinal-ignore-case dictionary with the default comparer | `ApplicationDiagnosticData_WinsOverWrappedExceptionDataCaseInsensitively` | uppercase lookup rejected; test red |
| Emit an empty JSON object instead of dictionary entries | `ApplicationDiagnosticData_IsCarriedByExactlyOnePublishedFault` | received fault lacks `USERNAME`; test red |
| Report the application wrapper instead of its wrapped exception | `ApplicationDiagnosticData_IsCarriedByExactlyOnePublishedFault` | exact exception identity differs; test red |
| Replace current process ID with zero | `CachedHost_DescribesTheCurrentProcessAndRuntime` | exact process identity differs; test red |
| Reintroduce the ignored public boolean constructor | `PublicConstruction_ContainsOnlyTheParameterlessWireConstructor` | two public constructors found; test red |
| Change one projected variant key | `CoreRequirements_MatchCompiledRequirementMetadata` | missing compiled row and orphan projected row both reported; test red |

The mutations attack product lifetime, comparison, serialization, exception identity, runtime
capture, public API and test-estate completeness. None is a test-only tautology.
