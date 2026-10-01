### Removed

- The inherited message-audit contracts, observers, configuration and provider implementations.
  Their useful diagnostic capture capability is superseded by the intentionally incompatible,
  policy-controlled `MessageJournal`; no audit compatibility alias remains.
- The inert `TypeAttributes.Serializable` flag on the dynamically emitted message proxy and its
  `SYSLIB0050` suppression. The modern serializers are unaffected.
