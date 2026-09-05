# Mutation evidence

Three deliberately narrow mutations were executed independently. Each was restored immediately,
then its owning clean check passed again.

| Mutation | Owning check | Result |
|---|---|---|
| Journey inventory `1..18` changed to `1..17` | `DeveloperJourneyArchitectureTests` | Killed: collection equality named unexpected `Journey18`; 1 of 2 tests failed |
| Shell gate expected count changed from 18 to 17 | `verify_developer_journeys.sh` | Killed before packing: `Expected 17 developer journeys, found 18.` |
| Every `Abandoned` state label removed from `docs/reliability.md` | `ProductDocumentationArchitectureTests` | Killed: required `Abandoned` state absent; 1 of 2 tests failed |

Observed score: 3 killed / 3 injected, 0 survived, 0 without coverage. The clean journey and
documentation architecture classes subsequently passed 2/2 each. The complete package-consumer
gate had already passed all 18 scenarios against 15 freshly packed packages.
