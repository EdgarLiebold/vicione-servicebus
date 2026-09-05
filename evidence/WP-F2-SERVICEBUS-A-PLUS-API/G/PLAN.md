# Work package G plan

| Requirement | Implementation | Verification |
|---|---|---|
| Eighteen package-only developer journeys | Add Message Journal, Message Limits, Reliable Messaging Inbox, and Suite Composition journeys; make every existing bus-registration journey declare mandatory limits | Fresh-package journey gate plus architecture inventory |
| Current product documentation | Replace internal implementation-history narratives with installation, API, build, test, reliability, observability, database, and deployment guidance | Architecture vocabulary guard and command execution |
| Reliability diagram and states | Document Outbox, Carrier, Inbox, scheduling, retry, quarantine, acknowledgement levels, and operator actions | Architecture assertions over diagrams and all required state names |
| API change guide | Add old form, new form, and rationale tables for application calls, namespaces, provider names, and capability packages | Architecture assertions over the required sections and capabilities |
| Apache-2.0 modification notice | Add one A–G change paragraph to `MODIFICATIONS.md` and regenerate `CHANGELIST.md` | Identity gate and change-list generator check |
| Repository-wide acceptance | Strict product, engineering, and unit builds; format checks; three identical complete unit runs; vulnerability scan; static API measurements | Recorded commands and exit results in `VALIDATION.md` |
