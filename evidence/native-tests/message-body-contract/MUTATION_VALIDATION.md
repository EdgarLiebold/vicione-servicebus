# MessageBody contract mutation validation

The valid mutations below ran in one disposable copy of the current working tree. Each copy used
the repository's locked dependency graph and a non-incremental Release build before its unfiltered
project test run. Every mutation was reverted in the disposable copy; no mutation touched or
remains in the canonical working tree.

| Mutation | Expected owner | Result |
|---|---|---|
| `MemoryMessageBody.Length` reduced by one | memory body contract | exit 2; four accessor variants failed |
| envelope-body length reduced by one | envelope body contract | exit 2; four accessor variants failed |
| object-body string-first cache encoded as ASCII | object body contract | exit 2; exactly the string-first variant failed |
| raw-body stream made writable | raw body contract | exit 2; four accessor variants failed |
| default JSON options changed to indented | serializer options contract | exit 2; one options fact failed |
| unsupported-body length returned zero | unsupported body contract | exit 2; one fact failed |
| valid additional Core `MessageBody` implementation added | Core type census | exit 2; one census fact failed |
| Core memory-body projection row removed | Core requirement projection | exit 2; one projection fact failed |
| valid additional MessagePack `MessageBody` implementation added | MessagePack type census | exit 2; one census fact failed |
| MessagePack census projection row removed | MessagePack requirement projection | exit 2; one projection fact failed |

An initial disposable-copy run without copied restore outputs failed before compilation with
`NETSDK1004`; it is deliberately excluded from the table and from all mutation claims. A first
type-census probe that did not compile is likewise excluded. Only compiling product mutants that
reached the native MTP verdict count as type-census evidence.
