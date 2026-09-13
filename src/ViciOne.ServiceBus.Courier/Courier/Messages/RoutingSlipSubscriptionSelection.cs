namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Validates routing-slip subscription flag selections at creation and deserialization boundaries.</summary>
internal static class RoutingSlipSubscriptionSelection
{
    /// <summary>Ensures that an event selection contains known flags and at least one lifecycle event.</summary>
    /// <param name="events">The event selection to validate.</param>
    /// <param name="parameterName">The parameter name reported when validation fails.</param>
    /// <returns>The validated selection.</returns>
    public static RoutingSlipEvents Validate(RoutingSlipEvents events, string parameterName)
    {
        const RoutingSlipEvents allowed = RoutingSlipEvents.All | RoutingSlipEvents.Supplemental;

        if ((events & ~allowed) != 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                events,
                "A routing-slip subscription cannot contain undefined event flags.");
        }

        if ((events & RoutingSlipEvents.All) == RoutingSlipEvents.None)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                events,
                "A routing-slip subscription must select at least one lifecycle event.");
        }

        return events;
    }

    /// <summary>Ensures that a content selection contains only known flags.</summary>
    /// <param name="contents">The content selection to validate.</param>
    /// <param name="parameterName">The parameter name reported when validation fails.</param>
    /// <returns>The validated selection.</returns>
    public static RoutingSlipEventContents Validate(RoutingSlipEventContents contents, string parameterName)
    {
        if ((contents & ~RoutingSlipEventContents.All) != 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                contents,
                "A routing-slip subscription cannot contain undefined content flags.");
        }

        return contents;
    }
}
