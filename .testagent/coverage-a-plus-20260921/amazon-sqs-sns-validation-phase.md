# Amazon SQS SNS notification validation

The complete product-wide profile at `151bf0dc1` measured
`SqsMessageBody.ParseNotification` at CRAP 42 with all 27 lines covered.
The single large predicate was split into the same short-circuit sequence:
root kind, notification identity (Type, MessageId, TopicArn, Message), then
signature metadata (Timestamp, SignatureVersion, Signature, SigningCertURL).
The parser still throws the same structural `InvalidDataException`, preserves
the JSON parse inner exception, and reads message attributes only after
the mandatory envelope fields pass. No contract or diagnostic changed.

The existing `AmazonSqsEnvelopeTests` cover exact direct and wrapped payloads,
each missing mandatory SNS field, each invalid discriminator, malformed JSON,
and a non-object root. Their positive case verifies the distinct topic ARN
and application payload, detecting swapped outputs. No test was added solely
to increase coverage.

The focused Microsoft CodeCoverage report is
`artifacts/coverage-sqs-notification-classification-20260924/raw/final/coverage.cobertura.xml`,
SHA-256 `fa844fc2c6791588decee748e0190752459a20ddd7387ce1543424402e950063`.
`ParseNotification`, `TryGetNotificationIdentity`, and
`HasValidNotificationSignature` are respectively 12/12, 9/9, and 10/10
lines, with CRAP 6, 12, and 24. These are focused Amazon SQS unit results,
not a new product-wide profile.

The final Amazon SQS suite passed 211/211. The Release Unit/Architecture
gate passed 10,261/10,261 with no failures or skips, and the final Release
solution build had zero warnings and errors. A read-only adversarial review
compared field ordering, short-circuit behavior, exceptions, and the existing
test oracles against the old predicate and returned PASS with no concrete
regression finding.

The last complete product-wide profile remains `151bf0dc1`: 89.6626% line,
81.8534% conservative branch observation, and 52 methods with CRAP > 30.
Global A+ remains open until a fresh all-assembly profile of current bytes
is collected and the remaining gaps are resolved.
