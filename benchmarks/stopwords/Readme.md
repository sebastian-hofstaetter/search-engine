# Stopword search experiments

This project benchmarks different methods to find a pre-defined set of stopwords in a text corpus.

We use a 5000 word subset of (preprocessed) Wikipedia text to simulate a real world distribution of stopword occurences (and probability to find one).

A crucial observation: Most english stopword lists contain only 4 characters or less.
This allows us to capture the value of a stopword as a single `long (Int64)` instead of a `String`, using unsafe code and pointer casting.

## Methods

- **StringSet**: `HashSet<string>` filled with stopwords
- **LongIteration**: `long[]` filled with stopword's long values; iterating over each entry to find a stopword 
- **LongBinarySearch**:  `long[]` filled with stopword's long values; using `Array.BinarySearch` to find a stopword 
- **LongHashSet**: `HashSet<long>` filled with stopword's long values
- **LongTreeSet**: `SortedSet<long>` filled with stopword's long values
- **StopWordSet**: Custom `HashSet<long>` that uses a hardcoded size and hardcoded `long` entries, which avoids saving and comparing the hashcode when evaluating a candidate entry


## Benchmark Environment 

```
Plugged-In, Windows Power Mode: Best Performance, Actual GHz ~4.2 (no throttling observed during the benchmarks)

BenchmarkDotNet=v0.10.12, OS=Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.309)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical cores and 4 physical cores
Frequency=2062499 Hz, Resolution=484.8487 ns, Timer=TSC
.NET Core SDK=2.1.103
  [Host]     : .NET Core 2.0.6 (Framework 4.6.26212.01), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.6 (Framework 4.6.26212.01), 64bit RyuJIT
```

## Benchmark Results


|            Method | StopWordCount |      Mean |     Error |    StdDev | Scaled |
| ----------------- |-------------- |----------:|----------:|----------:|-------:|
|         StringSet |            10 | 34.095 ns | 0.3140 ns | 0.2784 ns |   1.00 |
|     LongIteration |            10 |  8.071 ns | 0.1981 ns | 0.2034 ns |   0.24 |
|       LongHashSet |            10 | 11.650 ns | 0.1667 ns | 0.1478 ns |   0.34 |
|  LongBinarySearch |            10 | 18.193 ns | 0.2969 ns | 0.2777 ns |   0.53 |
|       LongTreeSet |            10 | 16.452 ns | 0.1097 ns | 0.1026 ns |   0.48 |
|       StopWordSet |            10 |  **3.690 ns** | 0.0524 ns | 0.0490 ns |   0.11 |
|                   |               |           |           |           |        |
|         StringSet |            20 | 34.880 ns | 0.1902 ns | 0.1686 ns |   1.00 |
|     LongIteration |            20 | 12.521 ns | 0.0709 ns | 0.0663 ns |   0.36 |
|       LongHashSet |            20 | 12.161 ns | 0.1093 ns | 0.1022 ns |   0.35 |
|  LongBinarySearch |            20 | 21.024 ns | 0.0918 ns | 0.0859 ns |   0.60 |
|       LongTreeSet |            20 | 18.572 ns | 0.1150 ns | 0.1075 ns |   0.53 |
|       StopWordSet |            20 |  **4.250 ns** | 0.0267 ns | 0.0250 ns |   0.12 |
|                   |               |           |           |           |        |
|         StringSet |            50 | 34.277 ns | 0.2420 ns | 0.2263 ns |   1.00 |
|     LongIteration |            50 | 20.633 ns | 0.0705 ns | 0.0589 ns |   0.60 |
|       LongHashSet |            50 | 13.380 ns | 0.0391 ns | 0.0327 ns |   0.39 |
|  LongBinarySearch |            50 | 22.290 ns | 0.0495 ns | 0.0439 ns |   0.65 |
|       LongTreeSet |            50 | 21.182 ns | 0.0599 ns | 0.0531 ns |   0.62 |
|       StopWordSet |            50 |  **3.081 ns** | 0.0377 ns | 0.0353 ns |   0.09 |
