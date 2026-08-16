```

BenchmarkDotNet v0.13.11, macOS 15.7.7 (24G720) [Darwin 24.6.0]
Intel Core i9-9900K CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.302
  [Host] : .NET 10.0.10 (10.0.1026.32716), X64 RyuJIT AVX2
  Dry    : .NET 10.0.10 (10.0.1026.32716), X64 RyuJIT AVX2

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                        | MessageBufferSize | Mean     | Error | Allocated |
|------------------------------ |------------------ |---------:|------:|----------:|
| **SystemTextJson_GetMessageBody** | **0**                 | **48.66 ms** |    **NA** |         **-** |
| **SystemTextJson_GetMessageBody** | **4096**              | **45.60 ms** |    **NA** |    **8280 B** |
