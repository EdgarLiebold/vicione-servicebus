# Unused Abstractions helpers

The last complete 36-report profile at exact source/test commit `4e7b54c22`
ranked two internal Abstractions methods at CRAP 42 each: the generic
`QueryStringExtensions.GetValueFromQueryString<T>` and
`TypeExtensions.CanBeNull`. Both had zero covered lines. A repository-wide
source/test reference search found no call site for either method. The
containing types are internal, and the methods do not participate in a public
API contract. Both methods were therefore removed rather than adding tests
whose sole effect would be to execute unused code.

The existing `TryGetValueFromQueryString` and other type helpers remain. The
Release Abstractions test project passed 786/786 tests with no failures or
skips. The complete Release Unit/Architecture solution built with zero
warnings and errors. Independent read-only adversarial review returned PASS:
it found no current or historical source use, including friend assemblies,
and confirmed the internal helpers are outside the public API contract.
A complete product-wide profile is still required to update global coverage
and CRAP totals; the A+ goal remains open.
