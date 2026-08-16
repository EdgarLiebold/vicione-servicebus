namespace ViciOne.ServiceBus.Tests.SagaStateMachineTests.Automatonymous
{
    using System;
    using System.Globalization;
    using System.Text.Json;
    using System.Text.Json.Serialization;


    /// <summary>
    /// Writes a state as its name and reads it back through the machine that owns it.
    /// <para>
    /// A state is not data: two machines can both have a state called "True" and they are not the same
    /// object. So the name is the only thing worth writing, and reading it needs the machine to resolve
    /// the name against. That is why the converter is constructed per machine rather than registered
    /// once.
    /// </para>
    /// </summary>
    public class StateConverter<T> :
        JsonConverter<State>
        where T : StateMachine
    {
        readonly T _machine;

        public StateConverter(T machine)
        {
            _machine = machine;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(State).IsAssignableFrom(typeToConvert);
        }

        public override void Write(Utf8JsonWriter writer, State value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.Name ?? "");
        }

        public override State Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                var text = reader.GetString();

                return string.IsNullOrWhiteSpace(text) ? null : _machine.GetState(text);
            }

            throw new JsonException(string.Format(CultureInfo.InvariantCulture,
                "Error reading State. Expected a string but got {0}.", reader.TokenType));
        }
    }
}
