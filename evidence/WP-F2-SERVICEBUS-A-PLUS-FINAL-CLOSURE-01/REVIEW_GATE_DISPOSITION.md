# API usability review gate disposition

Date: 2026-09-04

The requirement text below is quoted verbatim from the immutable API usability review. Evidence is
current implementation and executed validation, not the presence of a test file.

| Gate | Requirement | Prior verdict | Final verdict | Executed evidence |
|---:|---|---|---|---|
| 3 | `.NET 10.0.11 bzw. dann aktueller unterstützter Patch: restore, build, test und pack PASS.` | FAIL | PASS | Stable SDK 10.0.400/runtime 10.0.11; Shipping and Engineering locked restore and zero-warning Release build; Unit/Architecture 3,490/3,490; real RabbitMQ 27/27; eight packages and all 14 package-only journeys pass. |
| 13 | `Jede ungültige statische Konfiguration scheitert beim Startup mit actionable Error.` | PARTIAL | PASS | Eleven-family executable inventory; host lifecycle now uses `ValidateOnStart`; four causal invalid policies plus one positive policy pass 5/5; M04 is killed. |
| 17 | `Jeder Durable-Sender-Provider hat seine reale Acceptance Boundary mit echter Infrastruktur bewiesen.` | FAIL | PASS | The advertised provider set is InMemory consumer completion plus RabbitMQ real-broker publisher-confirmed persistent mandatory acceptance; all other external transports remain explicitly unsupported. Real RabbitMQ 27/27 and focused 3/3; M01-M03 killed. |
| 22 | `Public App API, Advanced SPI, Provider API, Operations API und Testing API sind klar geschichtet.` | PARTIAL | PASS | Five-layer documentation plus reflection/package architecture gate 4/4; Testing excluded from Shipping; advanced contracts hidden from default IntelliSense; M05 killed. |
| 24 | `IntelliSense führt einen neuen Entwickler zum **bevorzugten** und nicht nur zu irgendeinem funktionierenden Weg.` | PARTIAL | PASS | Preferred APIs remain visible, advanced patterns are hidden, README/XML docs point to DI-first application paths, and all 14 package-only journeys compile; M08 killed. |
| 25 | `Kein verbleibendes MassTransit-Erbe existiert nur deshalb, weil MassTransit es historisch so modelliert hat.` | PARTIAL | PASS | Twelve-item source/package/API inventory with an exact capability-based disposition per item; no production/sample MassTransit identity; M09 killed after closing the wrong-but-valid-disposition gap. |

The gate verdicts are scoped to the six items requested by the PO. They do not declare unrelated
future architecture-program work complete.
