# AGENTS.md

This repository was created from the complete Apache-2.0 source fork of **MassTransit 8.5.10** at
the fixed upstream commit and carries the retained, modernised capability scope under the product
identity `ViciOne.ServiceBus`. Upstream provenance, the Apache license and the change record
stay; see [README.md](README.md), [LICENSE.txt](LICENSE.txt),
[COPYRIGHT](COPYRIGHT), the [changelog](CHANGELOG.md) and the
[repository diff script](license/repository_diff.py).

For Suite product-code work, follow the current authorized scope and the
[Suite working agreement](../../vicione-architecture/governance/AI_WORKING_AGREEMENT.md).
The hash-bound development slice determines the ordinary product implementation scope; an explicit
Product Owner instruction may authorize a direct repository-maintenance task. This file does not
expand either scope. Record source changes in the changelog and keep the repository diff script
usable.

The namespace and assembly identity, the outbox and the scheduler must not open a second cluster
communication path or a second persistence truth. Raise the Architect AI on any contradiction or
scope extension.

Code, identifiers and comments are English.
