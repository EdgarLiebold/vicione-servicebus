# Work package G status

Status: complete

Implemented:

- Eighteen package-only developer journeys, including real public entry points for the message
  journal, explicit limits, reliable inbox operation, and Suite composition.
- Product-focused documentation for installation, API layers, building, testing, packaging,
  deployment, reliability, observability, and database schema deployment.
- Reliability flow and state diagrams with acknowledgement boundaries and typed operator actions.
- Application call-form, namespace, provider-name, and capability-package change tables.
- Apache-2.0 section 4(b) modification paragraphs for A through G.
- Architecture constraints for journey count, documentation vocabulary, reliability states, and API
  change coverage.
- Three killed documentation/example mutations and a zero-finding dependency-advisory inventory.
- Provider-acceptance corrections for the common Entity Framework transactional delivery source,
  SQL Server UTC timestamp projection, Quartz scheduler-token correlation, and explicit raw-message
  admission in the Azure Functions receiver test.
- The public Unit floor raised to the measured 3709 tests after three identical clean runs.

All build, format, Unit, package-consumer, static API, vulnerability, available provider-backed,
change-list, and source-identity acceptance commands are green. The candidate tree is ready for its
local work-package commit and separate independent review.
