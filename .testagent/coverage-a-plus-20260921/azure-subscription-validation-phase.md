# Azure Service Bus subscription validation

The complete product-wide profile at `151bf0dc1` measured the iterator
behind `ServiceBusSubscriptionConfigurator.Validate` at CRAP 46.44
(complexity 18, 5/9 lines covered). No product code changed in this phase.

`Validate_ReportsEveryIndependentSubscriptionErrorInOrder` asserts all four
simultaneous failures, exact ordering, member keys, failure dispositions,
complete diagnostic messages, and null rejected values.
`Validate_EnforcesIdleBoundsWithEitherRuleForm` asserts that four minutes
and a negative tick fail, while five minutes, zero, and an omitted value pass.
It also exercises filter-only and rule-only configuration. The existing
zero-idle test covers neither rule nor filter, and the first test covers both.
`Validate_UsesDistinctTopicAndSubscriptionNameRules` uses a slash in both
names: it is valid for a topic path and invalid for a subscription name.
This detects use or interchange of the two provider validators.

The focused Microsoft CodeCoverage report is
`artifacts/coverage-asb-subscription-validation-20260924/raw/final2/coverage.cobertura.xml`,
SHA-256 `3ffc549beb72c393e64cb8d519929d0a0b9eb7e1e87448a567ea97270c715380`.
The generated `Validate.MoveNext` is 9/9 lines covered and has CRAP 18.
This is a focused Azure unit result, not a new product-wide profile.

The Azure Service Bus suite passed 242/242 tests. The Release Unit/Architecture
gate passed 10,261/10,261 without failures or skips, and the final Release
solution build had zero warnings and errors. A read-only adversarial
review identified three missing oracles, which were added. The final review
returned PASS without a remaining concrete product regression. All three
new `RequirementCoverage` attributes have unique matching rows in
`AzureServiceBusRequirements.json`.

The last complete product-wide profile remains `151bf0dc1`: 89.6626% line,
81.8534% conservative branch observation, and 52 methods with CRAP > 30.
Global A+ remains open until a fresh all-assembly profile of current bytes
is collected and the remaining gaps are resolved.
