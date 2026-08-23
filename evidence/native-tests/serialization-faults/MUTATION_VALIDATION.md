# Serialization-fault mutation validation

Each mutation changed one test input or endpoint behavior, ran the complete three-case Release
cohort, and was reverted before the next mutation.

| Mutation | Observed causal result |
| --- | --- |
| Make the request consumer return the expected response instead of throwing `SerializationException` | 3 total; only `ConsumerSerializationException_ReachesTheRequestCallerAsAnExactFault` failed because no exception was thrown |
| Send the unreadable body with the registered System.Text.Json content type instead of the unsupported content type | 3 total; only `UnsupportedUnreadableBody_PublishesAnExactReceiveFaultWithoutDispatching` failed on the exact fault content type |
| Replace the nested Boolean value with a valid integer | 3 total; only `NestedContractTypeMismatch_PublishesAReceiveFaultWithoutDispatching` failed because no receive fault was published within the central operation timeout |

The final source restores all three original inputs. No mutation is retained in product or test code.
