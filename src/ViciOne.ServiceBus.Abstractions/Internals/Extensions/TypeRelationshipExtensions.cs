namespace ViciOne.ServiceBus.Internals
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;


    internal static class TypeRelationshipExtensions
    {
        static readonly GenericTypeMatchCache _genericTypeMatches = new GenericTypeMatchCache();

        public static bool ImplementsInterface<TInterface>(this Type type)
        {
            return ImplementsInterface(type, typeof(TInterface));
        }

        public static bool ImplementsInterface(this Type type, Type interfaceType)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(interfaceType);

            if (!interfaceType.IsInterface)
                throw new ArgumentException($"The type must be an interface: {TypeCache.GetShortName(interfaceType)}", nameof(interfaceType));

            if (interfaceType.IsGenericTypeDefinition)
                return _genericTypeMatches.GetMatches(type, interfaceType).Length > 0;

            if (interfaceType.ContainsGenericParameters)
            {
                throw new ArgumentException(
                    $"The interface must be closed or a generic type definition: {TypeCache.GetShortName(interfaceType)}", nameof(interfaceType));
            }

            return interfaceType.IsAssignableFrom(type);
        }

        public static bool TryGetTaskResultType(this Type type, [NotNullWhen(true)] out Type? resultType)
        {
            if (type.TryGetSingleClosedGenericArguments(typeof(Task<>), out Type[] arguments))
            {
                resultType = arguments[0];
                return true;
            }

            resultType = null;
            return false;
        }

        public static bool ClosesGenericType(this Type type, Type genericTypeDefinition)
        {
            ValidateGenericTypeDefinition(type, genericTypeDefinition);

            return _genericTypeMatches.GetClosedMatches(type, genericTypeDefinition).Length > 0;
        }

        public static IReadOnlyList<Type> GetClosedGenericTypes(this Type type, Type genericTypeDefinition)
        {
            ValidateGenericTypeDefinition(type, genericTypeDefinition);

            Type[] matches = _genericTypeMatches.GetClosedMatches(type, genericTypeDefinition);

            return Array.AsReadOnly((Type[])matches.Clone());
        }

        public static bool TryGetSingleClosedGenericType(this Type type, Type genericTypeDefinition,
            [NotNullWhen(true)] out Type? closedType)
        {
            ValidateGenericTypeDefinition(type, genericTypeDefinition);

            Type[] matches = _genericTypeMatches.GetClosedMatches(type, genericTypeDefinition);
            if (matches.Length == 0)
            {
                closedType = null;
                return false;
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException(
                    $"The type {TypeCache.GetShortName(type)} closes {TypeCache.GetShortName(genericTypeDefinition)} more than once: "
                    + string.Join(", ", matches.Select(TypeCache.GetShortName)));
            }

            closedType = matches[0];
            return true;
        }

        public static bool TryGetSingleClosedGenericArguments(this Type type, Type genericTypeDefinition, out Type[] arguments)
        {
            if (type.TryGetSingleClosedGenericType(genericTypeDefinition, out Type? closedType))
            {
                arguments = closedType.GetGenericArguments();
                return true;
            }

            arguments = [];
            return false;
        }

        public static Type[] GetSingleClosedGenericArguments(this Type type, Type genericTypeDefinition)
        {
            if (type.TryGetSingleClosedGenericArguments(genericTypeDefinition, out Type[] arguments))
                return arguments;

            throw new ArgumentException(
                $"The type {TypeCache.GetShortName(type)} does not close {TypeCache.GetShortName(genericTypeDefinition)}", nameof(type));
        }

        public static Type GetSingleClosedGenericArgument(this Type type, Type genericTypeDefinition)
        {
            Type[] arguments = type.GetSingleClosedGenericArguments(genericTypeDefinition);
            if (arguments.Length != 1)
            {
                throw new InvalidOperationException(
                    $"The generic type {TypeCache.GetShortName(genericTypeDefinition)} has {arguments.Length} generic arguments instead of one");
            }

            return arguments[0];
        }

        static void ValidateGenericTypeDefinition(Type type, Type genericTypeDefinition)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(genericTypeDefinition);

            if (!genericTypeDefinition.IsGenericTypeDefinition)
            {
                throw new ArgumentException(
                    $"The type must be a generic type definition: {TypeCache.GetShortName(genericTypeDefinition)}",
                    nameof(genericTypeDefinition));
            }
        }
    }
}
