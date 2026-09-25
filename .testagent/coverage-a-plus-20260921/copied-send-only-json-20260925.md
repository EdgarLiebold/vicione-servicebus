# Copied send-only JSON admission

Test commit: `3e4c2729fe1becdf51b749ed6a5d6d79c4e23933`.
Product source is unchanged. The source and test trees matched that commit
before the full Core run. No deserializer is registered for the tested
`application/json` or vendor `+json` media types. The tests verify that
copied send-only envelopes charge
the entire byte sequence as application body, reject one byte over the body
limit, retain the exact admitted bytes and text after source mutation, and
withhold JSON text for invalid syntax or invalid UTF-8. The full Core suite
passed 6,432/6,432 with Microsoft CodeCoverage; the report SHA-256 is
`709ace2d8e1c80c9a43c3be03a3f8449a9bc2525a0b6d1c5459cdfd051e6202e`
at `artifacts/coverage-core-3e4c2729f/coverage.cobertura.xml`.

A deliberate product mutation retained JSON text after failed validation.
The negative test failed, and the product source was restored byte-for-byte.
Read-only adversarial review identified and then verified closure of the
one-byte body boundary, source-mutation text, and vendor `+json` negative
cases; final verdict PASS.

In the Core report, `AdmittedCopyMessageBody.CreateOwned` has 27/36 covered
lines and 80% reported branches, versus 27/36 and 66.7% reported branches
in the preceding Core report. Its CRAP score remains 44.06. This test slice
therefore strengthens a real admission contract but does not close the
method's CRAP hotspot or the global A+ goal.

The Core binary was built after the final test edits but before the commit.
An isolated locked restore at this commit did not advance past project
discovery and was stopped after more than eight minutes; a second build from
the reused artifact SDK also ran almost eight minutes without completion and
was stopped. The reported test and coverage run used the earlier built binary
with the same source/test bytes. This is not a fresh isolated exact-commit
build receipt. A subsequent clean receipt and complete provider profile
remain required before a global A+ claim.
