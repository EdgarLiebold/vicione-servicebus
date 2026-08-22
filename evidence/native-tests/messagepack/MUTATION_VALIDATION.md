# MessagePack mutation validation

The candidate was exercised in a disposable detached worktree. Every mutation was applied alone,
the MessagePack project was rebuilt in `Release`, and the unfiltered native MTP project was run with
`--minimum-expected-tests 49`. No mutation was retained.

| Mutation | Expected owner | Result |
|---|---|---|
| Remove the `REQ-VSB-MESSAGEPACK-BODY` projection row | requirement projection | killed: 48 passed, 1 failed, exit 2 |
| Remove `MessagePackSecurity.UntrustedData` from the single option owner | security contract | killed: 48 passed, 1 failed, exit 2 |
| Alias cloned payload bytes instead of copying them | envelope ownership | killed: 47 passed, 2 failed, exit 2 |
| Add a direct `MessagePackSerializer` call outside `InternalMessagePackResolver` | architecture boundary | killed: 48 passed, 1 failed, exit 2 |
| Select JSON in the delayed-redelivery fixture | transport selection | initially survived; assertions were strengthened in `9eb0c2450647d18b0923236071cd25550f3dc696`, then killed: 48 passed, 1 failed, exit 2 |
| Report `Length + 1` from `MessagePackMessageBody` | body accessor contract | killed: 45 passed, 4 failed, exit 2 |

The initially surviving transport substitution is the reason the final tests assert the received
content type on the normal interface path and on both redelivery attempts. A delivery count or a
round-tripped value alone does not prove which serializer carried the message.
