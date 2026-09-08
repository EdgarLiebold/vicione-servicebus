namespace ViciOne.ServiceBus.StateMachineVisualizer.Tests;

internal sealed class GenericGraphFixtureOuter<TOuter>
{
    internal sealed class Message<TMessage>;

    internal sealed class Failure<TDetail> : Exception;
}
