using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ViciOne.ServiceBus.Analyzers.Internals;

/// <summary>Evaluates conversions supported by message initializer values.</summary>
sealed class MessageTypeConversion
{
    readonly SemanticModel _semanticModel;
    readonly ConversionGraph<ITypeSymbol> _typeSymbols;
    public MessageTypeConversion(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
        _typeSymbols = new ConversionGraph<ITypeSymbol>(100, SymbolEqualityComparer.Default);

        _typeSymbols.Add(semanticModel, SpecialType.System_String, SpecialType.System_Boolean);
        _typeSymbols.Add(semanticModel, SpecialType.System_Boolean, SpecialType.System_String, SpecialType.System_Object, SpecialType.System_SByte,
            SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_UInt16, SpecialType.System_Int32, SpecialType.System_UInt32,
            SpecialType.System_Int64, SpecialType.System_UInt64);

        _typeSymbols.Add(semanticModel, SpecialType.System_String, SpecialType.System_Byte);
        _typeSymbols.Add(semanticModel, SpecialType.System_Byte, SpecialType.System_String, SpecialType.System_Object, SpecialType.System_SByte,
            SpecialType.System_Int16, SpecialType.System_UInt16, SpecialType.System_Int32, SpecialType.System_UInt32,
            SpecialType.System_Int64, SpecialType.System_UInt64);

        _typeSymbols.Add(semanticModel, SpecialType.System_String, SpecialType.System_Decimal);
        _typeSymbols.Add(semanticModel, SpecialType.System_Decimal, SpecialType.System_String, SpecialType.System_Object, SpecialType.System_SByte,
            SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_UInt16, SpecialType.System_Int32, SpecialType.System_UInt32,
            SpecialType.System_Int64, SpecialType.System_UInt64);

        _typeSymbols.Add(semanticModel, SpecialType.System_String, SpecialType.System_Double);
        _typeSymbols.Add(semanticModel, SpecialType.System_Double, SpecialType.System_String, SpecialType.System_Object, SpecialType.System_SByte,
            SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_UInt16, SpecialType.System_Int32, SpecialType.System_UInt32,
            SpecialType.System_Int64, SpecialType.System_UInt64);

        _typeSymbols.Add(semanticModel, SpecialType.System_String, SpecialType.System_Int32);
        _typeSymbols.Add(semanticModel, SpecialType.System_Int32, SpecialType.System_String, SpecialType.System_Object, SpecialType.System_SByte,
            SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_UInt16, SpecialType.System_UInt32,
            SpecialType.System_Int64, SpecialType.System_UInt64);

        _typeSymbols.Add(semanticModel, SpecialType.System_String, SpecialType.System_Int64);
        _typeSymbols.Add(semanticModel, SpecialType.System_Int64, SpecialType.System_String, SpecialType.System_Object, SpecialType.System_SByte,
            SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_UInt16, SpecialType.System_Int32, SpecialType.System_UInt32,
            SpecialType.System_UInt64);

        _typeSymbols.Add(semanticModel, SpecialType.System_String, SpecialType.System_Int16);
        _typeSymbols.Add(semanticModel, SpecialType.System_Int16, SpecialType.System_String, SpecialType.System_Object, SpecialType.System_SByte,
            SpecialType.System_Byte, SpecialType.System_UInt16, SpecialType.System_Int32, SpecialType.System_UInt32,
            SpecialType.System_Int64, SpecialType.System_UInt64);

        _typeSymbols.Add(semanticModel, SpecialType.System_String, SpecialType.System_DateTime);
        _typeSymbols.Add(semanticModel, SpecialType.System_Int32, SpecialType.System_DateTime);
        _typeSymbols.Add(semanticModel, SpecialType.System_Int64, SpecialType.System_DateTime);
        _typeSymbols.Add(semanticModel, SpecialType.System_DateTime, SpecialType.System_String, SpecialType.System_Object, SpecialType.System_Int32,
            SpecialType.System_Int64);

        var dateTimeOffsetSymbol = GetRequiredType(typeof(DateTimeOffset));

        _typeSymbols.Add(semanticModel, SpecialType.System_DateTime, dateTimeOffsetSymbol);

        _typeSymbols.Add(semanticModel, SpecialType.System_String, dateTimeOffsetSymbol);
        _typeSymbols.Add(semanticModel, SpecialType.System_Int32, dateTimeOffsetSymbol);
        _typeSymbols.Add(semanticModel, SpecialType.System_Int64, dateTimeOffsetSymbol);
        _typeSymbols.Add(semanticModel, dateTimeOffsetSymbol, SpecialType.System_String, SpecialType.System_Object, SpecialType.System_Int32,
            SpecialType.System_Int64);

        var timeSpanSymbol = GetRequiredType(typeof(TimeSpan));

        _typeSymbols.Add(semanticModel, SpecialType.System_String, timeSpanSymbol);
        _typeSymbols.Add(semanticModel, SpecialType.System_Int32, timeSpanSymbol);
        _typeSymbols.Add(semanticModel, SpecialType.System_Int64, timeSpanSymbol);
        _typeSymbols.Add(semanticModel, timeSpanSymbol, SpecialType.System_String, SpecialType.System_Object, SpecialType.System_Int32,
            SpecialType.System_Int64);

        _typeSymbols.Add(semanticModel, SpecialType.System_String, SpecialType.System_Enum);
        _typeSymbols.Add(semanticModel, SpecialType.System_Enum, SpecialType.System_String, SpecialType.System_Object, SpecialType.System_SByte,
            SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_UInt16, SpecialType.System_Int32, SpecialType.System_UInt32,
            SpecialType.System_Int64, SpecialType.System_UInt64);

        _typeSymbols.Add(semanticModel, SpecialType.System_String, SpecialType.System_Object);

        var exceptionSymbol = GetRequiredType(typeof(Exception));

        var exceptionInfoSymbol = semanticModel.Compilation.GetTypeByMetadataName("ViciOne.ServiceBus.ExceptionInfo");
        if (exceptionInfoSymbol != null)
        {
            _typeSymbols.Add(exceptionInfoSymbol, exceptionSymbol);
            _typeSymbols.Add(semanticModel, SpecialType.System_String, exceptionInfoSymbol);
            _typeSymbols.Add(semanticModel, exceptionInfoSymbol, SpecialType.System_Object);
        }

        var uriSymbol = GetRequiredType(typeof(Uri));

        _typeSymbols.Add(semanticModel, SpecialType.System_String, uriSymbol);
        _typeSymbols.Add(semanticModel, uriSymbol, SpecialType.System_Object, SpecialType.System_String);

        var versionSymbol = GetRequiredType(typeof(Version));

        _typeSymbols.Add(semanticModel, SpecialType.System_String, versionSymbol);
        _typeSymbols.Add(semanticModel, versionSymbol, SpecialType.System_Object, SpecialType.System_String);

        var guidSymbol = GetRequiredType(typeof(Guid));

        _typeSymbols.Add(semanticModel, SpecialType.System_String, guidSymbol);
        _typeSymbols.Add(semanticModel, guidSymbol, SpecialType.System_Object, SpecialType.System_String);

        var newIdSymbol = semanticModel.Compilation.GetTypeByMetadataName("ViciOne.ServiceBus.NewId");
        if (newIdSymbol != null)
            _typeSymbols.Add(guidSymbol, newIdSymbol);
    }

    ITypeSymbol GetRequiredType(Type type)
    {
        var metadataName = type.FullName ?? throw new ArgumentException("The type must have a metadata name.", nameof(type));

        return _semanticModel.Compilation.GetTypeByMetadataName(metadataName)
            ?? throw new InvalidOperationException($"The compilation does not reference '{metadataName}'.");
    }

    public bool CanConvert(Type type, ITypeSymbol sourceSymbol)
    {
        var symbol = GetRequiredType(type);

        return CanConvert(symbol, sourceSymbol);
    }

    public bool CanConvert(ITypeSymbol symbol, ITypeSymbol sourceSymbol)
    {
        while (true)
        {
            if (SymbolEqualityComparer.Default.Equals(symbol, sourceSymbol))
                return true;

            if (symbol.IsNullable(out var underlyingSymbol))
            {
                symbol = underlyingSymbol;
                continue;
            }

            if (sourceSymbol.IsNullable(out var underlyingSourceSymbol))
            {
                sourceSymbol = underlyingSourceSymbol;
                continue;
            }

            if (sourceSymbol.IsInVar(out var variableType))
            {
                sourceSymbol = variableType;
                continue;
            }

            if (TryGetTaskResultType(sourceSymbol, out var taskType))
            {
                sourceSymbol = taskType;
                continue;
            }

            if (TryGetMessageDataValueType(symbol, out var messageDataType))
                return CanConvertMessageData(messageDataType, sourceSymbol);

            if (sourceSymbol.ImplementsType(symbol))
                return true;

            return _typeSymbols.Contains(symbol, sourceSymbol);
        }
    }

    bool CanConvertMessageData(ITypeSymbol targetValueType, ITypeSymbol sourceSymbol)
    {
        if (TryGetMessageDataValueType(sourceSymbol, out var sourceValueType))
            sourceSymbol = sourceValueType;

        if (IsByteArray(targetValueType))
            return IsByteArray(sourceSymbol) || sourceSymbol.SpecialType == SpecialType.System_String;

        if (targetValueType.SpecialType == SpecialType.System_String)
            return sourceSymbol.SpecialType == SpecialType.System_String;

        var streamType = GetRequiredType(typeof(Stream));
        if (SymbolEqualityComparer.Default.Equals(targetValueType, streamType))
            return sourceSymbol.ImplementsType(streamType);

        return targetValueType.IsReferenceType && CanConvert(targetValueType, sourceSymbol);
    }

    static bool IsByteArray(ITypeSymbol symbol)
        => symbol.IsArray(out var elementType) && elementType.SpecialType == SpecialType.System_Byte;

    static bool TryGetTaskResultType(ITypeSymbol symbol, out ITypeSymbol result)
    {
        if (symbol.TypeKind == TypeKind.Class
            && symbol.Name == "Task"
            && symbol.ContainingNamespace.Name == "Tasks"
            && symbol.ContainingNamespace.ContainingNamespace.Name == "Threading"
            && symbol.ContainingNamespace.ContainingNamespace.ContainingNamespace.Name == "System"
            && symbol is INamedTypeSymbol taskTypeSymbol
            && taskTypeSymbol.IsGenericType
            && taskTypeSymbol.TypeArguments.Length == 1)
        {
            result = taskTypeSymbol.TypeArguments[0];
            return true;
        }

        result = null!;
        return false;
    }

    static bool TryGetMessageDataValueType(ITypeSymbol symbol, out ITypeSymbol result)
    {
        if (symbol.TypeKind == TypeKind.Interface
            && symbol.Name == "MessageData"
            && symbol.ContainingNamespace.ToString() == "ViciOne.ServiceBus.Advanced.Serialization"
            && symbol is INamedTypeSymbol messageDataTypeSymbol
            && messageDataTypeSymbol.IsGenericType
            && messageDataTypeSymbol.TypeArguments.Length == 1)
        {
            result = messageDataTypeSymbol.TypeArguments[0];
            return true;
        }

        result = null!;
        return false;
    }
}


static class ConversionGraphExtensions
{
    public static void Add(this ConversionGraph<ITypeSymbol> graph, SemanticModel semanticModel, ITypeSymbol symbol, params SpecialType[] types)
    {
        ITypeSymbol[] typeSymbols = [.. types.Select(type => semanticModel.Compilation.GetSpecialType(type)).Cast<ITypeSymbol>()];

        graph.Add(symbol, typeSymbols);
    }

    public static void Add(this ConversionGraph<ITypeSymbol> graph, SemanticModel semanticModel, SpecialType specialType, params SpecialType[] types)
    {
        var specialTypeSymbol = semanticModel.Compilation.GetSpecialType(specialType);
        ITypeSymbol[] typeSymbols = [.. types.Select(type => semanticModel.Compilation.GetSpecialType(type)).Cast<ITypeSymbol>()];

        graph.Add(specialTypeSymbol, typeSymbols);
    }

    public static void Add(this ConversionGraph<ITypeSymbol> graph, SemanticModel semanticModel, SpecialType specialType, params ITypeSymbol[] typeSymbols)
    {
        var specialTypeSymbol = semanticModel.Compilation.GetSpecialType(specialType);

        graph.Add(specialTypeSymbol, typeSymbols);
    }
}
