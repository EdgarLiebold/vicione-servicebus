# AGENTS.md

This repository is the complete Apache-2.0 source fork of **MassTransit 8.5.10** at the fixed
upstream commit, published under the product identity `ViciOne.ServiceBus`. Upstream provenance, the
Apache license and the modification notices stay; see [README.md](README.md), [LICENSE](LICENSE),
[NOTICE](NOTICE), [COPYRIGHT](COPYRIGHT), [MODIFICATIONS.md](MODIFICATIONS.md) and the generated
[CHANGELIST.md](CHANGELIST.md).

ViciOne changes are made only from a hash-bound development slice. What is in scope, what is
retained and what is removed is decided there and not in this file; a removal is valid only with the
capability evidence the slice requires.

The namespace and assembly identity, the outbox and the scheduler must not open a second cluster
communication path or a second persistence truth. Raise the Architect AI on any contradiction or
scope extension.

Code, identifiers and comments are English.
