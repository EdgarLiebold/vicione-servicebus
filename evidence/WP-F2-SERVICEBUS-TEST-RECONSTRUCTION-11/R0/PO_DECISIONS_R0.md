# Product Owner decisions taken during R0

Recorded verbatim in substance, with the consequence each one has for the work. Taken by the Product
Owner in the external product chat that launched this package.

## PO-R0-01 · The Azure namespace no longer exists; cloud is rebuilt from scratch

**Decision.** "die umgebung existiert nicht mehr, wenn die cloud am schluss dran ist kannst du das
komplett neu aufbauen - nicht altes ist relevant"

**Consequence for the security item.** The committed shared access key in
`tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests/Configuration.cs` points at the
namespace `vicione-servicebus-build`, which the Product Owner states no longer exists. Shared access
keys are scoped to a namespace and are regenerated when a namespace is created, so a key naming a
deleted namespace cannot authenticate against a later namespace of the same name. **No rotation
action is required and none is requested.**

The disposition in `SECURITY_DISPOSITION_AZURE_SAS_FALLBACK.md` therefore closes as follows: the
finding was correct, its live risk is removed by the resource being gone, and the literal disappears
with the rebuilt configuration owner because Lead plan section 7 forbids migrating the fallback in
any case. What does **not** change: the rebuilt configuration owner fails closed when configuration
is absent. The defect was never only the key — it was that a missing environment variable silently
selected a credential instead of stopping.

**Consequence for wave C4.** Inherited cloud infrastructure carries no weight: fixtures, resource
naming, provisioning and teardown are designed fresh against the current emulator and service
surface. This is consistent with cohort `R0-CLOUD`'s finding that the 246 inherited Azure, Event
Hubs and Amazon SQS tests have **no recorded green run anywhere in this repository** — no anchor
file covers them and the verification model declares no category for them. There is no inherited
evidence to preserve for these owners.

**Boundary that this decision does not touch:** the inherited cloud *test obligations* remain
obligations. "Nothing old is relevant" is read as being about the cloud environment and its
infrastructure, which is what the question asked about. The behavioural guarantee that no discovered
capability disappears without an explicit product decision is unchanged, and the 273 cloud
obligations stay in the ledger.

## PO-R0-02 · Structurally unprovable service behaviour is proven at the boundary we own

**Decision.** Option A. For DynamoDB time-to-live deletion and S3 lifecycle deletion, the rebuilt
tests assert that the product issues the correct instruction to the service; the service-side sweep,
which no emulator runs and which the real service does not perform inside any test window, is
recorded as a documented limitation instead of being pursued as a test.

**Consequence.** The two obligations get a terminal disposition rather than staying permanently
`NOT_EXECUTED`, so they do not block the freeze for a reason no one can remove. The disposition
token for this case does not exist in the current vocabulary and is the one open item that still
needs the Lead, who owns the proof vocabulary.

## PO-R0-03 · Rebuilt tests assert intended behaviour, not inherited defects

**Decision.** Option A, with the reasoning stated by the Product Owner: "die Tests sind nur dafür da
richtiges Verhalten sicherzustellen".

**Consequence.** Where the product does not deliver the intended behaviour, the rebuilt test is red
and the defect becomes a separate product work package. The four product defects found in R0 — the
SQL Server delete reporting false, the orphan cleanup never executing there, dead-letter maintenance
never throttling, the inverted dead-letter metric condition — are therefore **not** encoded as
expected behaviour. The same rule governs the analyzer diagnostic that is declared but never
reported, where two inherited identities currently pin its absence by name.

This also settles the general case for the roughly one third of inherited tests that cannot fail:
they are not reproduced. Each one's obligation is rebuilt so that it can fail for the behaviour it
names.

## PO-R0-04 · Priority: everything locally provable is finished to A+ first

**Decision.** "wir bauen alles was lokal möglich ist A+ fertig - cloud folgt später"

**Reading, and why no question is raised.** This matches the Lead plan's own wave order, which
already places the hermetic core, local persistence and local brokers before the external owners,
and which already splits the external wave into writing the tests and later running them against
real resources. The instruction is therefore taken as a priority confirmation, not as a change: the
locally provable estate is brought to completion first, and the external owners follow.

If the Product Owner meant something stronger — that the external test **code** is not written until
the accesses arrive, rather than only its real runs being deferred — that would deviate from the
Lead's frozen wave contract and needs a Lead decision. It is flagged in the checkpoint as a reading,
not resolved silently.
