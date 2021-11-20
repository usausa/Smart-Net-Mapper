# Smart-Net-Mapper

## TODO

- Type conversion
- Nested mapper
- Struct support

## Benchmark

``` ini
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=6.0.100
  [Host]    : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
  MediumRun : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  
```
|                      Method |      Mean |     Error |    StdDev |    Median |       Min |       Max |       P90 |  Gen 0 | Allocated |
|---------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|-------:|----------:|
|            SimpleAutoMapper | 65.466 ns | 0.9948 ns | 1.4267 ns | 64.705 ns | 63.738 ns | 67.147 ns | 67.078 ns | 0.0038 |      64 B |
|           SimpleAutoMapper2 | 64.700 ns | 0.2078 ns | 0.2981 ns | 64.724 ns | 63.975 ns | 65.383 ns | 65.067 ns | 0.0038 |      64 B |
|            SimpleTinyMapper | 24.601 ns | 0.2740 ns | 0.4101 ns | 24.602 ns | 23.903 ns | 25.191 ns | 25.099 ns | 0.0038 |      64 B |
|         SimpleInstantMapper | 61.863 ns | 0.2972 ns | 0.4167 ns | 61.856 ns | 61.064 ns | 62.838 ns | 62.292 ns | 0.0095 |     160 B |
|             SimpleRawMapper | 33.114 ns | 0.3367 ns | 0.5040 ns | 32.896 ns | 32.317 ns | 34.165 ns | 33.866 ns | 0.0038 |      64 B |
|           SimpleSmartMapper | 11.746 ns | 0.0430 ns | 0.0644 ns | 11.759 ns | 11.607 ns | 11.880 ns | 11.810 ns | 0.0038 |      64 B |
| SimpleInstantMapperWoLookup | 54.184 ns | 0.1680 ns | 0.2515 ns | 54.159 ns | 53.744 ns | 54.609 ns | 54.549 ns | 0.0095 |     160 B |
|     SimpleRawMapperWoLookup | 24.994 ns | 0.0704 ns | 0.1054 ns | 24.990 ns | 24.810 ns | 25.174 ns | 25.137 ns | 0.0038 |      64 B |
|   SimpleSmartMapperWoLookup |  8.704 ns | 0.0290 ns | 0.0424 ns |  8.702 ns |  8.643 ns |  8.810 ns |  8.747 ns | 0.0038 |      64 B |
|                SimpleDirect |  8.028 ns | 0.0249 ns | 0.0365 ns |  8.029 ns |  7.957 ns |  8.102 ns |  8.078 ns | 0.0038 |      64 B |
|                SimpleInline |  7.407 ns | 0.0346 ns | 0.0518 ns |  7.395 ns |  7.341 ns |  7.553 ns |  7.476 ns | 0.0038 |      64 B |
|             MixedAutoMapper | 62.985 ns | 0.7563 ns | 1.1319 ns | 62.989 ns | 61.427 ns | 66.342 ns | 63.971 ns | 0.0038 |      64 B |
|            MixedAutoMapper2 | 61.519 ns | 0.2114 ns | 0.2964 ns | 61.561 ns | 60.778 ns | 62.014 ns | 61.834 ns | 0.0038 |      64 B |
|             MixedTinyMapper | 39.092 ns | 0.3748 ns | 0.5610 ns | 39.103 ns | 38.306 ns | 39.904 ns | 39.798 ns | 0.0067 |     112 B |
|          MixedInstantMapper | 78.591 ns | 0.2413 ns | 0.3612 ns | 78.470 ns | 77.880 ns | 79.463 ns | 79.054 ns | 0.0123 |     208 B |
|              MixedRawMapper | 32.251 ns | 0.3167 ns | 0.4642 ns | 32.313 ns | 31.508 ns | 33.211 ns | 32.889 ns | 0.0038 |      64 B |
|            MixedSmartMapper |  8.576 ns | 0.0256 ns | 0.0376 ns |  8.575 ns |  8.507 ns |  8.657 ns |  8.619 ns | 0.0038 |      64 B |
|            SingleAutoMapper | 58.018 ns | 0.3764 ns | 0.5398 ns | 58.054 ns | 57.280 ns | 58.918 ns | 58.611 ns | 0.0014 |      24 B |
|           SingleAutoMapper2 | 58.242 ns | 0.5740 ns | 0.8233 ns | 57.793 ns | 57.103 ns | 59.245 ns | 59.166 ns | 0.0014 |      24 B |
|            SingleTinyMapper | 20.350 ns | 0.0811 ns | 0.1213 ns | 20.323 ns | 20.158 ns | 20.616 ns | 20.544 ns | 0.0014 |      24 B |
|         SingleInstantMapper | 18.291 ns | 0.1142 ns | 0.1674 ns | 18.290 ns | 18.042 ns | 18.586 ns | 18.503 ns | 0.0029 |      48 B |
|             SingleRawMapper | 14.929 ns | 0.1577 ns | 0.2311 ns | 15.074 ns | 14.547 ns | 15.209 ns | 15.181 ns | 0.0014 |      24 B |
|           SingleSmartMapper |  6.144 ns | 0.0197 ns | 0.0289 ns |  6.144 ns |  6.095 ns |  6.201 ns |  6.176 ns | 0.0014 |      24 B |
