# Work package C plan

1. Capture the pre-layering reflection inventory and retain it with the package evidence.
2. Move public types into the Application, Configuration, Advanced, Provider, Operations, and Testing namespaces defined by the review.
3. Canonicalize assembly, package, project, namespace, solution, workflow, lock-file, sample, and capability-manifest identities.
4. Make dependency-injection entry points discoverable from `Microsoft.Extensions.DependencyInjection` and every public transport selector discoverable from `ViciOne.ServiceBus.Configuration`.
5. Encapsulate the service collection behind the registration configurator and hold the Application builder surface at no more than 20 members.
6. Remove public compatibility-only heritage, public internals, obsolete bridges, and `EditorBrowsable(Never)` hiding.
7. Consolidate the duplicate interface-message emitter and bind the analyzer to signature shape instead of a hand-maintained method catalog.
8. Generate and architecture-bind the exact root Application API baseline.
9. Complete XML documentation for every public type and member, then remove every CS1591 suppression.
10. Prove the migration tool idempotent, pack every canonical package, compile all Developer Journeys, and execute the complete UnitArchitecture profile three times.
11. Run a separate internal adversarial audit in both async-naming directions after the canonical rename.

No compatibility namespaces, aliases, forwarding types, or obsolete bridges are introduced.
