namespace ViciOne.ServiceBus.Tests.SagaStateMachineTests.Automatonymous
{
    using System.Text.Json;
    using System.Text.Json.Serialization;


    /// <summary>
    /// Round trips a saga state machine instance through JSON, so a specification can prove that the
    /// current state survives being written and read back.
    /// <para>
    /// Only the two methods the specifications use are here. The stream overloads this helper carried
    /// alongside them had no caller.
    /// </para>
    /// </summary>
    public class JsonStateSerializer<TStateMachine, TInstance>
        where TStateMachine : StateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        readonly JsonSerializerOptions _options;

        public JsonStateSerializer(TStateMachine machine)
        {
            _options = new JsonSerializerOptions
            {
                // Both of the settings this replaced ignored nulls and defaults on the way out, which
                // is the single WhenWritingDefault condition here.
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                WriteIndented = true,
                Converters = { new StateConverter<TStateMachine>(machine) }
            };
        }

        public string Serialize<T>(T instance)
            where T : TInstance
        {
            return JsonSerializer.Serialize(instance, _options);
        }

        public T Deserialize<T>(string body)
            where T : TInstance
        {
            return JsonSerializer.Deserialize<T>(body, _options);
        }
    }
}
