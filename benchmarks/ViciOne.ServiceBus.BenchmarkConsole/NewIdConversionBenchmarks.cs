using System;
using BenchmarkDotNet.Attributes;

namespace ViciOne.ServiceBus.BenchmarkConsole;

[MemoryDiagnoser(false)]
public class NewIdConversionBenchmarks
{
    public Guid Guid = Guid.NewGuid();
    public NewId Max = NewId.Next();
    public NewId Min = NewId.Empty;

    [Benchmark]
    public Guid ToGuid()
    {
        return Max.ToGuid();
    }

    [Benchmark]
    public Guid ToSequentialGuid()
    {
        return Max.ToSequentialGuid();
    }

    [Benchmark]
    public byte[] ToByteArray()
    {
        return Max.ToByteArray();
    }

    [Benchmark]
    public NewId FromGuid()
    {
        return NewId.FromGuid(Guid);
    }

    [Benchmark]
    public NewId FromSequentialGuid()
    {
        return NewId.FromSequentialGuid(Guid);
    }

    [Benchmark]
    public string NewIdToString()
    {
        return Max.ToString();
    }
}
