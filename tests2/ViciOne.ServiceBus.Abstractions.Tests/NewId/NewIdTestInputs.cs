namespace ViciOne.ServiceBus.Abstractions.Tests.NewId;

internal static class NewIdTestInputs
{
    internal static readonly DateTime Moment = new(2026, 8, 17, 21, 4, 5, DateTimeKind.Utc);

    internal static NewIdGenerator CreateGenerator(
        long? ticks = null,
        byte[]? workerId = null,
        byte[]? processId = null)
    {
        return new NewIdGenerator(
            new FixedTickProvider(ticks ?? Moment.Ticks),
            new FixedWorkerIdProvider(workerId ?? [1, 2, 3, 4, 5, 6]),
            processId is null ? null : new FixedProcessIdProvider(processId));
    }

    internal sealed class FixedTickProvider(long ticks) : ITickProvider
    {
        public long Ticks { get; } = ticks;
    }

    internal sealed class AdvancingTickProvider(long firstTick, long initialStep, long stepIncrease) : ITickProvider
    {
        private long _current = firstTick;
        private long _step = initialStep;

        public long Ticks
        {
            get
            {
                _current = checked(_current + _step);
                _step = checked(_step + stepIncrease);
                return _current;
            }
        }
    }

    internal sealed class FixedWorkerIdProvider(byte[] workerId) : IWorkerIdProvider
    {
        private readonly byte[] _workerId = [.. workerId];

        public byte[] GetWorkerId(int index) => [.. _workerId];
    }

    internal sealed class FixedProcessIdProvider(byte[] processId) : IProcessIdProvider
    {
        private readonly byte[] _processId = [.. processId];

        public byte[] GetProcessId() => [.. _processId];
    }
}
