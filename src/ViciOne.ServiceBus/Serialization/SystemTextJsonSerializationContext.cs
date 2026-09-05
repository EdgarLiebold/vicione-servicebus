using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Serialization.JsonConverters;

namespace ViciOne.ServiceBus.Serialization;

[JsonSerializable(typeof(Fault))]
[JsonSerializable(typeof(FaultEvent))]
// [JsonSerializable(typeof(FaultEvent<>))]
[JsonSerializable(typeof(ReceiveFault))]
[JsonSerializable(typeof(ReceiveFaultEvent))]
[JsonSerializable(typeof(ExceptionInfo))]
[JsonSerializable(typeof(FaultExceptionInfo))]
[JsonSerializable(typeof(HostInfo))]
[JsonSerializable(typeof(BusHostInfo))]
// [JsonSerializable(typeof(MessageBatch<>))]
[JsonSerializable(typeof(ScheduleMessage))]
[JsonSerializable(typeof(ScheduleMessageCommand))]
// [JsonSerializable(typeof(ScheduleMessageCommand<>))]
[JsonSerializable(typeof(ScheduleRecurringMessage))]
[JsonSerializable(typeof(ScheduleRecurringMessageCommand))]
// [JsonSerializable(typeof(ScheduleRecurringMessageCommand<>))]
[JsonSerializable(typeof(CancelScheduledMessage))]
[JsonSerializable(typeof(CancelScheduledRecurringMessage))]
[JsonSerializable(typeof(CancelScheduledRecurringMessageCommand))]
[JsonSerializable(typeof(PauseScheduledRecurringMessage))]
[JsonSerializable(typeof(PauseScheduledRecurringMessageCommand))]
[JsonSerializable(typeof(ResumeScheduledRecurringMessage))]
[JsonSerializable(typeof(ResumeScheduledRecurringMessageCommand))]
[JsonSerializable(typeof(MessageEnvelope))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonMessageEnvelope))]
[JsonSerializable(typeof(SystemTextMessageDataReference))]
partial class SystemTextJsonSerializationContext :
    JsonSerializerContext
{
}
