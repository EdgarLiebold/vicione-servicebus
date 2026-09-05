# Capability package test research

## Baseline

- Core source: 1,839 tracked C# files and 177,997 lines at commit `120714c4`.
- Nominal capability folders currently contain 313 tracked C# files: Sagas 64, SagaStateMachine 92, Courier 7, Futures 5, JobService 54, Mediator 7, and Initializers 75, plus capability integration outside those folders.
- Initial lexical coupling candidates outside the nominal folders: Sagas 96 files, Courier 88, Futures 13, JobService 33, Mediator 15, Initializers 43. These counts are deliberately treated as candidates; `COUPLING.md` records the compile-verified ownership decisions.
- Endpoint materialization currently hard-codes consumers, sagas, activities, execute activities, futures, and job-consumer detection in `BusRegistrationContext`.
- The repository uses .NET 10 Microsoft Testing Platform with xUnit v3 and repository-owned minimum test floors.
- The repository-wide Roslyn static-pairing heuristic scanned 4,155 source files and 854 test files;
  1,023 source files paired to at least one test. It identified the new `IConsumerKind` contract as the
  principal unpaired boundary. That result is a symbol-pairing heuristic rather than line or branch
  coverage, so the contract was closed with executing registration, endpoint, harness, delivery,
  architecture, and mutation assertions.

## Acceptance checklist

- `ConfigureEndpoints` discovers registered consumer kinds through the Advanced registration extension point.
- Ordinary consumers remain supported by the core without loading an optional capability.
- Saga, state-machine, activity, future, and job endpoint behavior remains observable after extraction.
- The core assembly contains no types owned by Sagas, SagaStateMachine, Courier, Futures, JobService, Mediator, or Initializers.
- Every new capability project builds and packs with its declared one-way dependencies.
- Entity Framework Core reliable messaging remains usable without loading saga support; saga persistence is isolated if compile-verified coupling requires the split.
- `samples/SuiteComposition` references only the core, RabbitMq, and EntityFrameworkCore product projects, runs an in-memory bus with a SQLite reliable store, and exercises limits, a consumer, a request, and a scheduled message.
- The architecture suite verifies both the core type boundary and SuiteComposition runtime assembly closure.
- All solution files, relevant test projects, lock files, workflows, provider capabilities, and journeys resolve the extracted packages explicitly.
- The developer-journey gate passes against freshly packed packages.
- The three canonical Release builds pass with warnings treated as errors.
- The UnitArchitecture profile passes three consecutive times with identical totals and zero skipped tests.
- A custom kind that overlaps the ordinary-consumer fallback is selected as the explicit owner; an
  unsupported custom harness-observation shape fails with an actionable configuration message.

## Test conventions

- Architecture constraints live in `tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Product` and have matching requirement-manifest entries.
- Runtime behavior remains in the nearest existing unit or local-integration project; test expectations and timeouts are not weakened during project extraction.
- New package-boundary tests assert loaded assemblies and reflected declaring assemblies rather than source paths.
- Environment-dependent validation is reported as not executed unless the real dependency was actually used.
- Targeted one-cause mutation checks cover custom harness observation, endpoint materialization, and
  explicit-owner precedence. An initially surviving precedence mutation required and received a
  stronger overlap assertion before acceptance.
