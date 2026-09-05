# Work package F status

Status: complete

Completed:

- Optional Sagas, Courier, Futures, Job Service, Mediator, and Initializers behavior is owned by six
  dedicated packages; saga/future/job EF persistence is owned by `EntityFrameworkCore.Sagas`.
- The two foundation assemblies do not declare capability-owned types and do not reference a
  capability assembly.
- `IConsumerKind` replaces hard-coded endpoint and dispatcher type switches with complete discovery,
  owner selection, definition, naming, endpoint configuration, service-instance, companion-endpoint,
  and test-harness contracts.
- Consumers remain the core fallback. Sagas, activities, futures, and jobs contribute explicit kinds;
  a third-party kind is proven through real primary and companion delivery and wins over a matching
  fallback registration.
- Base Entity Framework Core retains reliable messaging and journal persistence without a capability
  dependency. The separate saga package owns saga repositories, job sagas, and futures.
- Solution files, the native workflow, provider capability data, benchmark/sample/test references,
  and all affected lockfiles resolve the new graph explicitly.
- All seven capability packages pack. Fourteen package-only Developer Journeys compile against 15
  freshly packed packages, and SuiteComposition builds and runs with exactly core, RabbitMq, and base
  EntityFrameworkCore as direct product dependencies.
- The compiled API inventory is complete, the vulnerability inventory has zero findings, and three
  bounded mutations are killed after closing one initially surviving owner-precedence test gap.
- Strict builds and format gates are green. The complete profile passed three times at 3,703/3,703
  with identical totals and zero skipped tests.
- Temporary `.testagent` planning files were absorbed into this evidence and removed.
- `review/**`, protected legal files, and the package-G `CHANGELIST.md` remain outside the package-F
  change set.
