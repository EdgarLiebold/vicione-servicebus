# T80 SQL scheduler registration

Manual review found that `AddSqlMessageScheduler` and its typed overload
dereferenced a missing registration configurator before validating their public
input. Both now reject it with `ArgumentNullException("configurator")`.

The tests use a real Microsoft DI service collection and resolve the registered
services. They prove existing default and typed schedulers retain precedence,
non-SQL host configurations fail at resolution, an injected clock is retained,
the typed scheduler does not appear as an untyped scheduler, and both paths
create one scheduler per scope. The typed scope and precedence assertions close
two independent P2 gaps identified by the read-only adversarial Red Team.

The affected SQL xUnit v3/MTP project passed 233/233 tests with no failures or
skips after the final test edit. Product and test bytes were committed as
`cc543750f` without further change; the test result applies to those bytes.
The T74 33-profile report remains the latest complete Line/Branch/CRAP
measurement. Subsequent coherent packets will use affected-project tests; a
whole-repository measurement follows at the agreed multi-packet interval or
earlier for a cross-project contract change.
