# ServiceBus review backup — 6 October 2026

This backup accompanies the reviewed ServiceBus source, tests and changelog on
`backup/servicebus-api-review-20261006`. The normal repository files carry the
implementation. The three ordered archive parts retain the complete durable
Suite review workspace and the repository evidence present before this backup
bundle was created, including locally ignored raw logs.

`MANIFEST.json` gives the compressed archive and chunk SHA-256 values.
`ARCHIVE_INPUTS.json` lists every archived regular-file path, size and SHA-256.
Temporary research directories and generated SDK/build caches are outside this
retained-workspace backup. No new build or test run is claimed by creating it.
The original review evidence retains its recorded validation boundaries.

To restore, first verify each chunk against `MANIFEST.json`, then concatenate the
parts in order and verify the archive SHA-256. Run these commands from this folder:

```sh
cat review-evidence.tar.gz.part-001 review-evidence.tar.gz.part-002 review-evidence.tar.gz.part-003 > /tmp/servicebus-review-evidence.tar.gz
shasum -a 256 /tmp/servicebus-review-evidence.tar.gz
```

Extract into a separate empty directory to inspect the retained files:

```sh
mkdir /tmp/servicebus-review-restored
tar -xzf /tmp/servicebus-review-evidence.tar.gz -C /tmp/servicebus-review-restored
```

The restored tree contains `SERVICEBUS_API_REVIEW_AND_REPAIR/` and
`repositories/vicione-servicebus/evidence/`. Existing evidence preserves its
original absolute provenance paths; the archived bytes can also be checked
using the relative paths and hashes in `ARCHIVE_INPUTS.json`.
