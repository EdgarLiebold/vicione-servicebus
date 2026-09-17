# Iteration 201 research

## Admitted source scope

- `SagaRegistrationConfiguratorExtensions.cs`: 100 baseline lines, six public generic/provider
  registration methods.
- `SagaDefinition.cs`: 86 baseline lines, one public generic definition type with public,
  protected and explicit-interface contracts.
- `SagaRegistrationExtensions.cs`: 247 baseline lines, thirteen public discovery/registration
  extensions.
- `SagaEndpointDefinition.cs`: 23 baseline lines, one public generic endpoint-definition type.

All four files were read completely by the lead before implementation delegation. They are new
unique source admissions, moving the cumulative exact count from 646 to 650 of 4,118 files
(15.784%).

## Material findings

- Runtime definition types can reach DI mutation before their family, closure and constructability
  are rejected with a stable boundary diagnostic.
- `SagaDefinition<TSaga>` forwards nullable required collaborators, accepts non-positive concurrency
  limits and caches a derived endpoint name even after its public endpoint definition changes.
- Discovery overloads lack receiver-first, array and element ownership; deferred queries can leave
  partial registrations when filters or later candidates fail.
- Empty `params Assembly[]` and `params Type[]` overload families make natural zero-argument calls
  ambiguous; the regular filter-only call is ambiguous across both families as well.
- Explicit type discovery does not consistently enforce closed concrete candidates or
  consumer-kind ownership, duplicate definitions are not rejected before mutation, and namespace
  matching is incorrectly case-insensitive.
- `SagaEndpointDefinition<TSaga>` delegates missing settings and formatter boundaries instead of
  owning them.

## Test strategy

Use direct `RequirementCoverage` mappings for exact API shape, receiver-first guards, stable
definition-type diagnostics, DI mutation isolation, endpoint-name dynamics, concurrency-limit
validation, settings/formatter ownership, closed/concrete/owned discovery, unique definition
pairing, filter evaluation and eager failure atomicity. Exercise real `ServiceCollection`
registration where collaborator identity and lifetime matter.
