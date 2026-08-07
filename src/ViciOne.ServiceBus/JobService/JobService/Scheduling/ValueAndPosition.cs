// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.JobService.Scheduling;

readonly struct ValueAndPosition
{
    public ValueAndPosition(int value, int position)
    {
        Value = value;
        Position = position;
    }

    public int Value { get; }
    public int Position { get; }

    public void Deconstruct(out int value, out int position)
    {
        value = Value;
        position = Position;
    }
}
