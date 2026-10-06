# Automatic Azure Service Bus subscription identities

Automatic subscription names now identify the complete physical destination queue path and namespace authority. They use `auto-` followed by 40 lowercase hexadecimal SHA256 characters over a versioned, length-framed, case-insensitive identity. Queue base paths are included once, as they appear in the queue's SDK creation options and the subscription's `ForwardTo`. Credentials, query options and lifetime settings do not identify a destination.

This fixes collisions between destinations such as `branch-a/orders` and `branch-b/orders`. The name is deterministic across deployments; the hash provides a bounded strong identity, rather than a mathematical guarantee of uniqueness. Direct callers and custom publish topology implementations retain the existing method signature and must use the complete destination identity. `FormatSubscriptionName`, explicitly named `Subscribe` calls and explicitly named subscription endpoints retain their behavior.

Existing broker subscriptions require an explicit cutover. This naming change does not migrate, rename, drain or automatically delete legacy broker resources. Existing explicitly configured `RemoveSubscriptions` cleanup remains unchanged. Before upgrading:

1. Export the old and new namespace/topic/subscription/`ForwardTo` inventory, rules, filters, backlog and dead-letter counts. Confirm the intended owner of any previously colliding subscription.
2. Stop publishers, writers and receivers for the affected resources in a controlled maintenance window. Preserve a rollback and backlog-handling plan.
3. Provision and inspect the new subscriptions with the intended destination, rules and filters. Route retained messages only after their destination ownership has been confirmed.
4. Start the upgraded deployment and verify routing and correlated application acknowledgements. Remove old resources only through a separate, explicit operations action after the cutover is accepted.

Running old and new subscriptions together can create additional publication copies. Reusing a colliding old name is unsafe: broker reconciliation can change its `ForwardTo` to another destination. A rollback after writers restart requires the agreed message and resource recovery plan.
