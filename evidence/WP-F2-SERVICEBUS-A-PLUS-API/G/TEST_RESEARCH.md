# Test research

## Changed behavior boundary

Work package G primarily adds documentation, examples, and executable architecture constraints. Its
global acceptance runs also exercised the preceding reliability and capability-package work against
real providers. They exposed related delivery, correlation, timestamp, and test-host defects; package
G corrects them because excluding a discovered product defect would violate the program's red-test
rule. Significant failure modes therefore include incorrect package consumers, incomplete guidance,
an inactive reliable-delivery source, stale tracked inbox state, shifted UTC timestamps, lost schedule
correlation, and accidental admission or rejection of headerless raw messages.

## Existing coverage reused

- `DeveloperJourneyArchitectureTests` already enforces package-only consumption, direct package
  ownership, lock-file entries, fresh packing, and absence of source references.
- `TestingPlatformConfigurationTests` already enforces one shared Unit floor across the public
  command owners and all provider-backed floors.
- `ApiSurfaceArchitectureTests` already prevents advanced SPI from becoming a preferred journey.
- `RequirementCoverageProjectionTests` binds every architecture assurance to compiled metadata.

## Added coverage

- The journey inventory is raised from 14 to 18 and the shell gate independently refuses any count
  other than 18 before packing.
- `ProductDocumentationArchitectureTests` scans every current product document after masking the
  legally required provenance paragraph, rejects internal process vocabulary, and requires both
  reliability diagrams and every state and acknowledgement name.
- The same test requires the changelog's three API-change sections, the principal async forms,
  unified capability registration, and every extracted package.
- `FetchProcedures_ProjectTransportTimestampsAsUtcDateTimeOffsetsAsync` executes both SQL Server fetch
  procedures and makes the provider result type, UTC offset, and exact sent/expiration values
  observable. Its ten direct assertions cover both normal and partitioned receive paths.
- Existing provider tests were strengthened by the final matrix: Entity Framework transactional
  dispatch and concurrency on PostgreSQL, Quartz scheduling on PostgreSQL, SQL Server request/job
  lifecycles, and the Azure Functions retry/fault path against the Service Bus emulator.

## Assertion-quality assessment

The three affected architecture classes use equality, collection, negative, string, structural,
and state assertions. The new documentation tests contain no assertion-free or trivial-only test.
Failures identify the missing file, forbidden term, missing state, missing call form, or missing
package rather than relying on a timeout.

Static assertion inventory: 15 test methods and 64 direct assertion calls across the three affected
classes. `DeveloperJourneyArchitectureTests` has 24 calls across 6 assertion APIs;
`ProductDocumentationArchitectureTests` has 9 calls across 3 APIs; and
`TestingPlatformConfigurationTests` has 31 calls across 7 APIs. Nested `Assert.All` predicates add
value-specific checks beyond the direct call count. Zero-assertion tests: 0. Trivial-only tests: 0.

## Pseudo-mutation plan

1. Reduce the expected journey range from 18 to 17 and require the journey architecture test to
   fail on the extra real file.
2. Change the shell gate's expected count from 18 to 17 and require it to fail before any package
   build.
3. Remove one required reliability terminal state from the document and require the documentation
   architecture test to fail by name.

Each mutation is applied separately, the narrowest owning check is executed, and the original text
is restored immediately. The clean checks are repeated afterward.
