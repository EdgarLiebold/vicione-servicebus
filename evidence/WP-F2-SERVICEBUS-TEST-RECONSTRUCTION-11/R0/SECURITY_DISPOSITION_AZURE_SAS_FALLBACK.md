# R0 security disposition — committed Azure Service Bus access key

**Class: `PRODUCT_LEGAL_OR_RISK_DECISION`. This item stops for a decision; it is not closed by Team 1.**

Requirement `REQ-TEST-105`; Lead plan section 7 ("Der geerbte SAS-Fallback und `client.p12` werden
nicht migriert"); `PO-2026-08-20-05` item 6 for the handling rule.

Per Lead plan section 7, evidence contains **no secret value and no hash of one** — a hash would let
secret rotation be tracked. This document states structure, reachability and effect only.

## Finding

`tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests/Configuration.cs` resolves its
Azure Service Bus settings as `Environment.GetEnvironmentVariable(...) ?? <committed literal>`:

| Setting | Environment variable | Committed fallback |
|---|---|---|
| Key name | `VICIONE_SERVICEBUS_ASB_KEYNAME` | present, non-empty |
| **Key value** | `VICIONE_SERVICEBUS_ASB_KEYVALUE` | **present, non-empty, matches a base64 shared-access-key shape of ≥ 40 characters** |
| Namespace | `VICIONE_SERVICEBUS_ASB_NAMESPACE` | `vicione-servicebus-build` |

Measured: the file contains exactly one literal matching `"[A-Za-z0-9+/]{40,}={0,2}"`, on the key
value line.

## Why this is materially worse than `client.p12`

`client.p12` is an unreferenced upstream fixture whose subject cannot be read and which no code
loads. This one is the opposite on every axis:

1. **It names its target.** The namespace fallback `vicione-servicebus-build` says exactly which
   Azure resource the key authenticates against.
2. **It is reachable by default.** The `??` is a fallback, not a placeholder: running the inherited
   Azure tests with the three environment variables unset authenticates with the committed key.
3. **What then runs is destructive.**
   `AzureServiceBusTestSetUpFixture` is an assembly-level `[SetUpFixture]` whose `[OneTimeSetUp]`
   enumerates **every topic and every queue in the target namespace and deletes them**
   (`DeleteTopicAsync` / `DeleteQueueAsync` in two `foreach` loops), and its `catch (Exception)`
   swallows every failure, so the deletion is silent whether it succeeds or fails.

The three together form one chain: an unset environment variable is sufficient for a developer or CI
job to authenticate against a named Azure namespace with a committed key and empty it, without an
error being reported.

## Disposition

**`STOP_FOR_ROTATION`.** Section 7 forbids migrating the SAS fallback, so the code path disappears
with the rebuilt Azure owner regardless. That handles the future. It does not handle the present:

- The key is in Git history from before this work package and **deleting it from `HEAD` revokes
  nothing** — the same rule `PO-2026-08-20-05` item 6 states for `client.p12`.
- Unlike `client.p12`, the material here is directly usable and its target is named, so the second
  branch of the rule applies without ambiguity: this is an indication of genuinely usable key
  material, and the security item stops for revocation or rotation.

**Required, and outside Team 1's authority:** rotate or revoke the shared access key on the
`vicione-servicebus-build` namespace, and confirm whether that namespace exists and what it holds.
Team 1 cannot determine from the repository whether the key is still live — that requires the Azure
subscription, which the package does not have and which wave C4b is waiting for anyway.

**Team 1 proceeds meanwhile on** the rebuilt configuration owner: one typed configuration owner, one
local secret store, OIDC/workload identity, no fallback credential of any kind. The rebuilt Azure
fixtures will fail closed when configuration is absent instead of falling back — which is the
control that makes this class of finding impossible rather than merely absent.

## Related, same file family

`R0-CLOUD` additionally reports `admin`/`admin` hard-coded in **product** code (`LocalstackHost()`,
`AmazonSqsTestHarness`) and process-global `AWS_*` environment mutation in `Storage_Specs`. The
product-code occurrences are a `CONTRACT_OR_PUBLIC_API_CHANGE` decision under section 15 and are
reported with this item rather than changed.
