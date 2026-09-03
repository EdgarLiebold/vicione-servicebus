namespace ViciOne.ServiceBus.Internals
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;


    internal class DynamicImplementationBuilder :
        IImplementationBuilder
    {
        const MethodAttributes PropertyAccessMethodAttributes = MethodAttributes.Public
            | MethodAttributes.SpecialName
            | MethodAttributes.HideBySig
            | MethodAttributes.Final
            | MethodAttributes.Virtual
            | MethodAttributes.VtableLayoutMask;

        readonly ConcurrentDictionary<string, ModuleBuilder> _moduleBuilders;
        readonly string _proxyNamespaceSuffix = "ViciOne.ServiceBus.DynamicInternal" + Guid.NewGuid().ToString("N");
        readonly ConcurrentDictionary<Type, Lazy<Type>> _proxyTypes;

        public DynamicImplementationBuilder()
        {
            _moduleBuilders = new ConcurrentDictionary<string, ModuleBuilder>();

            _proxyTypes = new ConcurrentDictionary<Type, Lazy<Type>>();
        }

        public Type GetImplementationType(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));

            return _proxyTypes.GetOrAdd(interfaceType, x => new Lazy<Type>(() => CreateImplementation(x))).Value;
        }

        Type CreateImplementation(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Proxies can only be created for interfaces: " + interfaceType.Name, nameof(interfaceType));
            if (interfaceType.ContainsGenericParameters)
                throw new ArgumentException("Proxies can only be created for closed interfaces: " + interfaceType.Name, nameof(interfaceType));

            PropertyInfo[] properties = GetContractProperties(interfaceType);

            return GetModuleBuilderForType(interfaceType, moduleBuilder => CreateTypeFromInterface(moduleBuilder, interfaceType, properties));
        }

        static Type CreateTypeFromInterface(ModuleBuilder builder, Type interfaceType, IEnumerable<PropertyInfo> properties)
        {
            var typeName = "ViciOne.ServiceBus.DynamicInternal." +
                (interfaceType.IsNested && interfaceType.DeclaringType != null
                    ? $"{interfaceType.DeclaringType.Name}+{TypeCache.GetShortName(interfaceType)}"
                    : TypeCache.GetShortName(interfaceType));
            try
            {
                // The retained System.Text.Json and MessagePack serializers do not consume the CLI
                // serializable bit. Omitting it keeps the emitted data contract free of obsolete
                // formatter metadata and avoids a suppression for behavior the product does not use.
                var typeBuilder = builder.DefineType(typeName,
                    TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
                    typeof(object), new[] { interfaceType });

                typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

                foreach (var property in properties)
                {
                    var fieldBuilder = typeBuilder.DefineField("field_" + property.Name, property.PropertyType,
                        FieldAttributes.Private);

                    var propertyBuilder = typeBuilder.DefineProperty(property.Name,
                        property.Attributes | PropertyAttributes.HasDefault, property.PropertyType, null);

                    foreach (var attributeData in property.GetCustomAttributesData())
                        propertyBuilder.SetCustomAttribute(GetCustomAttributeBuilder(attributeData));

                    var getMethod = GetGetMethodBuilder(property, typeBuilder, fieldBuilder);
                    var setMethod = GetSetMethodBuilder(property, typeBuilder, fieldBuilder);

                    propertyBuilder.SetGetMethod(getMethod);
                    propertyBuilder.SetSetMethod(setMethod);
                }

                return typeBuilder.CreateTypeInfo().AsType();
            }
            catch (Exception ex)
            {
                var message = $"Exception creating proxy ({typeName}) for {TypeCache.GetShortName(interfaceType)}";

                throw new InvalidOperationException(message, ex);
            }
        }

        static PropertyInfo[] GetContractProperties(Type interfaceType)
        {
            // Dynamic message contracts are data shapes. Reject behavior and ambiguous wire names
            // here so every initializer and retained serializer observes the same contract.
            Type[] contractTypes = interfaceType.GetInterfaces()
                .OrderBy(x => x.AssemblyQualifiedName, StringComparer.Ordinal)
                .Prepend(interfaceType)
                .ToArray();
            PropertyInfo[] candidates = contractTypes
                .SelectMany(x => x.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .ToArray();
            var propertyAccessors = candidates
                .SelectMany(x => x.GetAccessors())
                .ToHashSet();
            MethodInfo unsupportedMethod = contractTypes
                .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .FirstOrDefault(x => !propertyAccessors.Contains(x));

            if (unsupportedMethod != null)
            {
                throw new ArgumentException(
                    $"Proxy interfaces can contain properties only; methods and events are not supported: {unsupportedMethod.Name}",
                    nameof(interfaceType));
            }

            foreach (var property in candidates)
            {
                if (property.GetMethod == null)
                {
                    throw new ArgumentException(
                        $"Proxy properties must be readable: {property.Name}",
                        nameof(interfaceType));
                }

                if (property.GetAccessors().Any(x => !x.IsAbstract))
                {
                    throw new ArgumentException(
                        $"Proxy properties cannot contain default implementations: {property.Name}",
                        nameof(interfaceType));
                }

                if (property.GetMethod.IsStatic || property.SetMethod?.IsStatic == true)
                {
                    throw new ArgumentException(
                        $"Proxy properties must be instance properties: {property.Name}",
                        nameof(interfaceType));
                }

                if (property.GetIndexParameters().Length != 0)
                {
                    throw new ArgumentException(
                        $"Proxy interfaces cannot contain indexers: {property.Name}",
                        nameof(interfaceType));
                }
            }

            return candidates
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => SelectContractProperty(interfaceType, group))
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToArray();
        }

        static PropertyInfo SelectContractProperty(Type interfaceType, IGrouping<string, PropertyInfo> group)
        {
            string[] declaredNames = group.Select(x => x.Name).Distinct(StringComparer.Ordinal).ToArray();
            if (declaredNames.Length != 1)
            {
                throw new ArgumentException(
                    $"Proxy property names must be unique in a case-insensitive contract: {string.Join(", ", declaredNames)}",
                    nameof(interfaceType));
            }

            Type[] propertyTypes = group.Select(x => x.PropertyType).Distinct().ToArray();
            if (propertyTypes.Length != 1)
            {
                throw new ArgumentException(
                    $"Proxy property declarations have conflicting types: {declaredNames[0]}",
                    nameof(interfaceType));
            }

            return group.FirstOrDefault(x => x.DeclaringType == interfaceType)
                ?? group.OrderBy(x => x.DeclaringType?.AssemblyQualifiedName, StringComparer.Ordinal).First();
        }

        static MethodBuilder GetGetMethodBuilder(PropertyInfo propertyInfo, TypeBuilder typeBuilder, FieldBuilder fieldBuilder)
        {
            var getMethodBuilder = typeBuilder.DefineMethod("get_" + propertyInfo.Name,
                PropertyAccessMethodAttributes,
                propertyInfo.PropertyType,
                Type.EmptyTypes);

            var il = getMethodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fieldBuilder);
            il.Emit(OpCodes.Ret);

            return getMethodBuilder;
        }

        static MethodBuilder GetSetMethodBuilder(PropertyInfo propertyInfo, TypeBuilder typeBuilder, FieldBuilder fieldBuilder)
        {
            var setMethodBuilder = typeBuilder.DefineMethod("set_" + propertyInfo.Name,
                PropertyAccessMethodAttributes,
                CallingConventions.HasThis,
                typeof(void),
                ReturnTypeCustomModifiersForProperty(propertyInfo),
                null,
                new[] { propertyInfo.PropertyType },
                null,
                null);

            var il = setMethodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, fieldBuilder);
            il.Emit(OpCodes.Ret);

            return setMethodBuilder;
        }

        static CustomAttributeBuilder GetCustomAttributeBuilder(CustomAttributeData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var propertyArguments = new List<PropertyInfo>();
            var propertyArgumentValues = new List<object>();
            var fieldArguments = new List<FieldInfo>();
            var fieldArgumentValues = new List<object>();
            foreach (var namedArg in data.NamedArguments)
            {
                var argName = namedArg.MemberName;
                var fi = data.AttributeType.GetField(argName);
                var pi = data.AttributeType.GetProperty(argName);

                if (fi != null)
                {
                    fieldArguments.Add(fi);
                    fieldArgumentValues.Add(GetCustomAttributeValue(namedArg.TypedValue));
                }
                else if (pi != null)
                {
                    propertyArguments.Add(pi);
                    propertyArgumentValues.Add(GetCustomAttributeValue(namedArg.TypedValue));
                }
            }

            var constructorArgs = data.ConstructorArguments.Select(GetCustomAttributeValue).ToArray();

            return new CustomAttributeBuilder(
                data.Constructor,
                constructorArgs,
                propertyArguments.ToArray(),
                propertyArgumentValues.ToArray(),
                fieldArguments.ToArray(),
                fieldArgumentValues.ToArray());
        }

        static object GetCustomAttributeValue(CustomAttributeTypedArgument argument)
        {
            if (argument.Value is not ReadOnlyCollection<CustomAttributeTypedArgument> elements)
                return argument.Value;

            Type elementType = argument.ArgumentType.GetElementType()
                ?? throw new InvalidOperationException($"Attribute value is not an array: {argument.ArgumentType}");
            Array values = Array.CreateInstance(elementType, elements.Count);
            for (var index = 0; index < elements.Count; index++)
                values.SetValue(GetCustomAttributeValue(elements[index]), index);

            return values;
        }

        TResult GetModuleBuilderForType<TResult>(Type interfaceType, Func<ModuleBuilder, TResult> callback)
        {
            var assemblyName = interfaceType.Namespace + _proxyNamespaceSuffix;

            var builder = _moduleBuilders.GetOrAdd(assemblyName, name =>
            {
                const AssemblyBuilderAccess access = AssemblyBuilderAccess.RunAndCollect;

                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), access);

                var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName);

                return moduleBuilder;
            });

            return callback(builder);
        }

        static Type[] ReturnTypeCustomModifiersForProperty(PropertyInfo propertyInfo)
        {
            var hasInitSetter = propertyInfo.SetMethod?.ReturnParameter?.GetRequiredCustomModifiers()?.Contains(typeof(IsExternalInit)) ?? false;

            return hasInitSetter ? new[] { typeof(IsExternalInit) } : null;
        }
    }
}
