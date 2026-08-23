# System.Text.Json Application-Format Mutation Validation

Date: 2026-08-23

All mutations ran in an isolated disposable copy of the final candidate. Each mutation changed one
production or test-contract fact, rebuilt `ViciOne.ServiceBus.Tests.csproj` in Release with locked
assets, and ran the complete 369-case core project through xUnit 4 on Microsoft Testing Platform 2.
The canonical working tree was not changed by these runs.

| Mutation | Expected owner | Result |
|---|---|---|
| Remove `JsonObjectCreationHandling.Populate` from the generated Protobuf partial type | Both generated-Protobuf compatibility facts | Exit 2; 367 passed and exactly the envelope/raw Protobuf facts failed because expected `AUD`, `USD` became an empty repeated field. |
| Change the envelope serializer media type from `application/vnd.vicione.servicebus+json` to `application/vnd.vicione.servicebus+xml` | Envelope application-format facts | Exit 2; 367 passed and exactly the envelope Protobuf/XML facts rejected the incorrect exact media type. |
| Change the raw serializer media type from `application/json` to `application/xml` | Raw application-format facts | Exit 2; 367 passed and exactly the raw Protobuf/XML facts rejected the incorrect exact media type. |
| Remove the raw-Protobuf row from `CoreRequirements.json` | Requirement projection | Exit 2; 368 passed and exactly `CoreRequirements_MatchCompiledRequirementMetadata` rejected the compiled-but-unprojected fact. |

Every mutant compiled successfully with zero warnings and zero errors before the deliberate test
failure. No mutant survived, no filter was used, and no minimum-count failure substituted for the
named behavioral or projection failure.

Verdict: **PASS**.
