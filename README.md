# Smart-Net-Mapper

## TODO

- Type conversion
- Nested mapper
- Struct support

## Benchmark

``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1083 (21H1/May2021Update)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=5.0.302
  [Host]    : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT
  MediumRun : .NET 5.0.8 (5.0.821.31504), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                      Method |      Mean |     Error |    StdDev |    Median |       Min |       Max |       P90 |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|-------:|------:|------:|----------:|
|            SimpleAutoMapper | 70.860 ns | 0.3637 ns | 0.5443 ns | 70.719 ns | 69.951 ns | 72.131 ns | 71.446 ns | 0.0038 |     - |     - |      64 B |
|           SimpleAutoMapper2 | 68.764 ns | 2.1617 ns | 2.8859 ns | 66.789 ns | 65.546 ns | 71.958 ns | 71.792 ns | 0.0038 |     - |     - |      64 B |
|            SimpleTinyMapper | 28.342 ns | 0.0512 ns | 0.0734 ns | 28.342 ns | 28.157 ns | 28.462 ns | 28.435 ns | 0.0038 |     - |     - |      64 B |
|         SimpleInstantMapper | 90.897 ns | 0.3800 ns | 0.5201 ns | 90.783 ns | 90.196 ns | 91.729 ns | 91.622 ns | 0.0095 |     - |     - |     160 B |
|             SimpleRawMapper | 32.524 ns | 0.0884 ns | 0.1296 ns | 32.495 ns | 32.303 ns | 32.896 ns | 32.685 ns | 0.0038 |     - |     - |      64 B |
|           SimpleSmartMapper | 11.008 ns | 0.0188 ns | 0.0251 ns | 11.012 ns | 10.971 ns | 11.071 ns | 11.034 ns | 0.0038 |     - |     - |      64 B |
| SimpleInstantMapperWoLookup | 80.560 ns | 0.4066 ns | 0.6086 ns | 80.363 ns | 79.642 ns | 82.163 ns | 81.364 ns | 0.0095 |     - |     - |     160 B |
|     SimpleRawMapperWoLookup | 25.415 ns | 0.0497 ns | 0.0697 ns | 25.390 ns | 25.323 ns | 25.567 ns | 25.518 ns | 0.0038 |     - |     - |      64 B |
|   SimpleSmartMapperWoLookup |  8.413 ns | 0.0303 ns | 0.0425 ns |  8.398 ns |  8.353 ns |  8.558 ns |  8.454 ns | 0.0038 |     - |     - |      64 B |
|                  SimpleHand |  7.292 ns | 0.0089 ns | 0.0130 ns |  7.289 ns |  7.273 ns |  7.321 ns |  7.313 ns | 0.0038 |     - |     - |      64 B |
|             MixedAutoMapper | 64.288 ns | 0.1090 ns | 0.1527 ns | 64.281 ns | 64.009 ns | 64.604 ns | 64.501 ns | 0.0038 |     - |     - |      64 B |
|            MixedAutoMapper2 | 66.974 ns | 0.7344 ns | 1.0992 ns | 66.681 ns | 65.317 ns | 69.612 ns | 68.842 ns | 0.0038 |     - |     - |      64 B |
|             MixedTinyMapper | 43.747 ns | 0.1883 ns | 0.2760 ns | 43.707 ns | 43.216 ns | 44.598 ns | 44.010 ns | 0.0067 |     - |     - |     112 B |
|          MixedInstantMapper | 77.210 ns | 0.2583 ns | 0.3787 ns | 77.181 ns | 76.506 ns | 78.052 ns | 77.697 ns | 0.0123 |     - |     - |     208 B |
|              MixedRawMapper | 31.100 ns | 0.0835 ns | 0.1197 ns | 31.102 ns | 30.871 ns | 31.363 ns | 31.241 ns | 0.0038 |     - |     - |      64 B |
|            MixedSmartMapper |  7.960 ns | 0.0180 ns | 0.0247 ns |  7.959 ns |  7.908 ns |  8.017 ns |  7.992 ns | 0.0038 |     - |     - |      64 B |
|            SingleAutoMapper | 60.857 ns | 0.2332 ns | 0.3491 ns | 60.876 ns | 60.280 ns | 61.412 ns | 61.303 ns | 0.0013 |     - |     - |      24 B |
|           SingleAutoMapper2 | 63.393 ns | 1.5106 ns | 2.2610 ns | 62.440 ns | 61.083 ns | 66.565 ns | 66.398 ns | 0.0014 |     - |     - |      24 B |
|            SingleTinyMapper | 23.240 ns | 0.1309 ns | 0.1919 ns | 23.190 ns | 22.915 ns | 23.666 ns | 23.510 ns | 0.0014 |     - |     - |      24 B |
|         SingleInstantMapper | 18.679 ns | 0.1451 ns | 0.2127 ns | 18.779 ns | 18.291 ns | 18.941 ns | 18.878 ns | 0.0029 |     - |     - |      48 B |
|             SingleRawMapper | 14.558 ns | 0.0211 ns | 0.0282 ns | 14.560 ns | 14.512 ns | 14.617 ns | 14.594 ns | 0.0014 |     - |     - |      24 B |
|           SingleSmartMapper |  5.502 ns | 0.0055 ns | 0.0079 ns |  5.502 ns |  5.485 ns |  5.516 ns |  5.511 ns | 0.0014 |     - |     - |      24 B |
