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

<!-- OPENWIKI:START -->

## OpenWiki

This repository has a generated `openwiki/` evidence index. It is optional just-in-time context, not required startup reading.

- Do not enumerate, preload, or search wikis at task start. Use retrieval when the user asks for it, when unfamiliar architecture or dependency behavior materially affects the task, or when source inspection leaves an important uncertainty. Stop once the question is grounded.
- When those conditions apply and OpenWiki retrieval tools are available, use `openwiki_search` for just-in-time context and `openwiki_read` for the relevant complete sections. If search returns `workspace_required`, ask which listed workspace to use and retry with its ID.
- Use `openwiki_list_workspaces` or `openwiki_list_wikis` when workspace membership itself needs to be discovered.
- If the retrieval tools are unavailable, read `openwiki/quickstart.md` and follow its links to the relevant pages.
- Treat source code and tests as authoritative. A brief's unknowns and review items are verification gaps, not automatic requirements.
- Prefer the narrowest quiet validation that proves the changed behavior. Preserve complete failure output.

The scheduled OpenWiki GitHub Actions workflow refreshes the repository wiki. Do not hand-edit generated OpenWiki pages unless explicitly asked; prefer updating source code/docs and letting OpenWiki regenerate.

<!-- OPENWIKI:END -->
