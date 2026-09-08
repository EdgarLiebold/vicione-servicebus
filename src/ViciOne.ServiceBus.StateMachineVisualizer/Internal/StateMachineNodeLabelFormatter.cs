using System;
using System.Collections.Generic;
using System.Text;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.StateMachineVisualizer.Internal;

internal static class StateMachineNodeLabelFormatter
{
    internal static string Format(StateMachineGraphNode node, string openTypeDelimiter, string closeTypeDelimiter)
    {
        if (node.Kind == StateMachineGraphNodeKind.Exception)
            return $"catch {FormatTypeName(node.ExceptionType!, includeNamespace: true)}";

        if (node.MessageType is null)
            return node.Name;

        Type messageType = UnwrapFault(node.MessageType);
        return string.Concat(node.Name, openTypeDelimiter, FormatTypeName(messageType), closeTypeDelimiter);
    }

    static string FormatTypeName(Type type, bool includeNamespace = false)
    {
        if (type.IsArray)
        {
            string commas = new(',', type.GetArrayRank() - 1);
            return $"{FormatTypeName(type.GetElementType()!, includeNamespace)}[{commas}]";
        }

        return FormatNamedType(type, includeNamespace);
    }

    static string FormatNamedType(Type type, bool includeNamespace)
    {
        Type definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        var hierarchy = new Stack<Type>();
        for (Type? current = definition; current is not null; current = current.DeclaringType)
            hierarchy.Push(current);

        Type[] arguments = type.IsGenericType ? type.GetGenericArguments() : [];
        var argumentIndex = 0;
        StringBuilder result = new();
        while (hierarchy.TryPop(out Type? component))
        {
            if (result.Length == 0)
            {
                if (includeNamespace && !string.IsNullOrEmpty(component.Namespace))
                    result.Append(component.Namespace).Append('.');
            }
            else
                result.Append('.');

            int arityMarker = component.Name.IndexOf('`', StringComparison.Ordinal);
            string componentName = arityMarker < 0 ? component.Name : component.Name[..arityMarker];
            result.Append(componentName);

            if (arityMarker < 0)
                continue;

            int arity = int.Parse(component.Name.AsSpan(arityMarker + 1), System.Globalization.CultureInfo.InvariantCulture);
            result.Append('<');
            for (var index = 0; index < arity; index++)
            {
                if (index > 0)
                    result.Append(", ");

                result.Append(FormatTypeName(arguments[argumentIndex++], includeNamespace));
            }

            result.Append('>');
        }

        return result.ToString();
    }

    static Type UnwrapFault(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Fault<>)
            ? type.GetGenericArguments()[0]
            : type;
}
