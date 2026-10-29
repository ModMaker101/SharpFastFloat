# SharpFastFloat benchmarks

These results describe specific SharpFastFloat microbenchmarks, not a universal
speedup over the .NET runtime. Inputs, hardware, runtime versions, and tradeoffs
are listed with each measurement.

The suites below have since been consolidated. Commands use the current class
names and may cover different input sets than the historical runs in the tables.
For new comparisons, run both revisions with the same harness.

## Formatting and strict-parser benchmarks

Variable names follow .NET conventions: camelCase locals and parameters,
`_camelCase` private instance fields, `s_camelCase` private static fields, and
PascalCase constants, exposed fields, classes, methods, and local functions. These rules are configured in
`.editorconfig`. Named argument callers should use `decimalSeparator`,
`thousandsSeparator`, and `charactersConsumed`; the previous parameter names
contained underscores. Positional calls and binary method signatures are unchanged.

Formatting uses the repository `.editorconfig` and the .NET SDK formatter:

```sh
dotnet format SharpFastFloat.sln
dotnet format SharpFastFloat.sln --verify-no-changes
```

CI checks formatting and runs strict and regular scanner tests on x64 and ARM64 with hardware
intrinsics both enabled and disabled. Run the end-to-end and digit-run benchmarks
(including scalar, four-digit, and eight-digit accumulation) with:

```sh
dotnet run -c Release --project benchmarks/SharpFastFloat.Benchmarks -- --filter '*ParsingBenchmarks*' '*DigitChunkBenchmark*' --job short
```

A short BenchmarkDotNet run on an AMD EPYC 9V74, Debian 13, .NET 9.0.21
(three warmups and three 100 ms measurement iterations) produced these means:

| Decimal input | Strict API | Configurable API | Runtime TryParse |
| --- | ---: | ---: | ---: |
| `1` | 4.59 ns | 15.40 ns | 27.46 ns |
| `123` | 5.99 ns | 20.18 ns | 32.97 ns |
| `12345678` | 4.96 ns | 17.21 ns | 39.65 ns |
| `1234567890123456` | 6.75 ns | 20.29 ns | 53.52 ns |
| `12345678.12345678` | 9.19 ns | 22.43 ns | 56.91 ns |
| `9007199254740993` | 15.15 ns | 30.32 ns | 67.18 ns |

All three APIs allocated zero bytes in these cases. The configurable and runtime
APIs accept broader syntax, so these comparisons measure decimal-token throughput
rather than equivalent validation contracts.

The isolated digit-run comparison measured:

| Digit run | Scalar | Four-digit chunks | Eight-digit chunks |
| --- | ---: | ---: | ---: |
| `1` | 0.27 ns | 1.03 ns | 1.00 ns |
| `123` | 0.89 ns | 1.88 ns | 1.87 ns |
| `1234` | 1.37 ns | 2.68 ns | 2.52 ns |
| `12345678` | 3.13 ns | 4.60 ns | 1.86 ns |
| `1234567890123456` | 7.64 ns | 8.68 ns | 3.03 ns |
| `1234567890123456789` | 8.59 ns | 11.40 ns | 4.34 ns |

These measurements favor eight-digit SIMD chunks for longer runs and scalar
handling for short inputs. Four-digit parsing remains available to the benchmarks
and the portable eight-digit fallback, without adding a four-digit fast path to
the strict scanner. Results are from a shared x64 environment; ARM performance
still needs measurement on ARM hardware.

## Regular scanner integer-run optimization

The regular UTF-16 and UTF-8 scanners process ungrouped integer runs in eight-digit
chunks, then finish with a scalar tail. UTF-16 uses the existing SIMD/portable
helper; UTF-8 uses the existing portable eight-digit machinery. The fast path
counts digits by pointer difference and avoids group-separator checks per digit.
Short inputs and inputs with grouping enabled retain the scalar path. Prefix
consumption, configurable separators, overflow correction, and rounding retain
their existing behavior. This shared scanner serves both float and double APIs.

The dedicated benchmark covers short integers, chunk boundaries, long integers,
grouping, fractions, exponents, and trailing text in all four API paths:

```sh
dotnet run -c Release --project benchmarks/SharpFastFloat.Benchmarks -- --filter '*ParsingBenchmarks*' '*RegularSyntaxBenchmarks*' --job short --warmupCount 3 --iterationCount 5 --iterationTime 100 --launchCount 1
```

Before/after means on AMD EPYC 9V74, Debian 13, .NET 9.0.21, using the same harness
and the scanner from commit `7482f3a` as the baseline:

| API | Input | Before | After |
| --- | --- | ---: | ---: |
| DoubleUtf16 | `1` | 15.93 ns | 15.94 ns |
| DoubleUtf16 | `123` | 17.01 ns | 16.83 ns |
| DoubleUtf16 | `1234567890123456` | 19.57 ns | 17.91 ns |
| FloatUtf16 | `1234567890123456` | 19.16 ns | 16.05 ns |
| FloatUtf8 | `1234567890123456` | 18.32 ns | 16.09 ns |
| DoubleUtf16 | `123456789012345678901234567890` | 50.94 ns | 41.77 ns |
| FloatUtf16 | `123456789012345678901234567890` | 46.07 ns | 35.45 ns |
| FloatUtf8 | `123456789012345678901234567890` | 44.18 ns | 37.52 ns |
| FloatUtf8 | `123` | 7.39 ns | 8.31 ns |
| FloatUtf16 | `1.2345678901234567e-100` | 16.19 ns | 17.66 ns |

These short shared-machine measurements show gains on longer integer runs and
small fixed overhead on some short/scientific inputs (around 0–1.5 ns here).
All measured paths allocate zero bytes. The optimization applies when grouping
is disabled; grouping retains its existing scalar implementation.

A repeat of the UTF-8 double benchmarks resolved a high-variance eight-digit
measurement: `12345678` measured 16.03 ns, compared with 17.09 ns before.

## Strict entry-point parity and discarded-zero optimization

The strict float and double APIs share UTF-16 and UTF-8 scanner implementations,
including chunk parsing, integer returns, and exact versus uncertain discarded
tails. Original spans remain available for exact rounding. The UTF-8 decimal
fallback also supports large explicit exponents that cancel very long mantissas,
matching the UTF-16 fallback.

Run the short-input/zero-tail before-and-after harness and the four-entry-point
comparison (which also includes the configurable parsers) with:

```sh
dotnet run -c Release --project benchmarks/SharpFastFloat.Benchmarks -- --filter '*MidpointRoundingBenchmark*' '*ParsingBenchmarks*' --job short --warmupCount 3 --iterationCount 5 --iterationTime 200 --launchCount 1
```

Measured means on AMD EPYC 9V74, Debian 13, .NET 9.0.21, with scanner commit
`6df711d` as the before baseline and identical harness settings:

| UTF-16 strict double input | Before | After |
| --- | ---: | ---: |
| `-0` | 4.69 ns | 3.59 ns |
| `1` | 4.73 ns | 2.55 ns |
| `1.0000000000000000000000000000000000000000` | 31.25 ns | 27.87 ns |
| `1.00000000000000011102230246251565404236316680908203125` | 421.17 ns | 430.80 ns |
| `1.2345678901234567000000000000000000` | 25.25 ns | 21.00 ns |
| `1.2345678901234567000000000000000001` | 24.98 ns | 25.30 ns |
| `123` | 6.27 ns | 4.40 ns |
| `12345678` | 5.13 ns | 6.44 ns |
| `1234567890123456` | 6.84 ns | 7.58 ns |
| `90071992547409930000e-4` | 454.02 ns | 20.05 ns |
| `90071992547409930001e-4` | 464.86 ns | 473.67 ns |

The zero-tail midpoint example avoids an unnecessary exact fallback; the
neighboring nonzero-tail example still takes that fallback to preserve rounding.
The added tracking costs roughly 0.7–1.3 ns on the sampled 8/16-digit integers,
while the specialized paths improve one- and three-digit inputs. These are short
shared-machine measurements.

The same run measured strict entry points as follows:

| Input | Double UTF-16 | Double UTF-8 | Float UTF-16 | Float UTF-8 |
| --- | ---: | ---: | ---: | ---: |
| `1` | 2.59 ns | 2.65 ns | 2.57 ns | 2.87 ns |
| `123` | 4.41 ns | 4.80 ns | 4.41 ns | 4.11 ns |
| `1234567890123456` | 7.80 ns | 9.01 ns | 12.99 ns | 14.11 ns |
| `90071992547409930000e-4` | 19.99 ns | 20.73 ns | 20.34 ns | 20.44 ns |
| `167772170000000000000000e-16` | 25.73 ns | 26.05 ns | 26.50 ns | 26.74 ns |

All measured strict methods allocated zero bytes. Float halfway and subnormal
rounding, negative zero, malformed byte tails, zero-only discarded tails, and
large exponent cancellation are covered by the scanner test matrix on x64 and
ARM64, with hardware intrinsics enabled and disabled.

## Exact midpoint rounding

Ambiguous long mantissas now use a stack-backed integer comparison adapted from
upstream fast_float's `digit_comparison.h` and `bigint.h`. When the retained
19-digit coefficient and its successor round to adjacent values, the parser
compares the original decimal digits with their exact binary midpoint and selects
the correctly rounded endpoint, including ties to even. This avoids the older
repeated decimal-shifting fallback for these cases.

The implementation shares this path across float/double and UTF-16/UTF-8, supports
regular-parser grouping and custom decimal separators, and preserves signed zero,
subnormal and overflow rounding. It bounds significant digits at 114 for float and
769 for double, retains a sticky nonzero suffix, and uses two 4096-bit stack buffers
without heap allocations. Existing decimal-shifting conversion remains available
when the conversion engine cannot establish adjacent endpoints. The common scanner
and its discarded-zero optimization are retained.

`ExactMidpointTests` constructs exact midpoints with independent `BigInteger`
arithmetic, then checks ties and values immediately above and below them in all
four strict and all four regular entry points. It includes negative inputs,
subnormal/normal and finite/infinity transitions, and tails beyond the digit bounds.
The stack integer operations are also checked independently against `BigInteger`.
CI includes these tests on x64/ARM64 with intrinsics enabled and disabled.

Run the midpoint benchmarks with:

```sh
dotnet run -c Release --project benchmarks/SharpFastFloat.Benchmarks -- --filter '*MidpointRoundingBenchmark*' --job short --warmupCount 3 --iterationCount 5 --iterationTime 100 --launchCount 1
```

Before/after BenchmarkDotNet means on AMD EPYC 9V74, Debian 13, .NET 9.0.21,
using the same harness at commit `996080e` and with the midpoint comparison port
(three warmups, five 100 ms measurement iterations, one launch):

| API | Input | Before | After | Speedup |
|---|---|---:|---:|---:|
| DoubleUtf16 | `90071992547409930001e-4` | 468.50 ns | 69.92 ns | 6.70x |
| DoubleUtf8 | `90071992547409930001e-4` | 469.84 ns | 69.53 ns | 6.76x |
| DoubleUtf16 | `1.00000000000000011102230246251565404236316680908203125` | 422.64 ns | 116.76 ns | 3.62x |
| DoubleUtf8 | `1.00000000000000011102230246251565404236316680908203125` | 428.20 ns | 112.76 ns | 3.80x |
| FloatUtf16 | `1.000000059604644775390625000000000000000000000001` | 359.55 ns | 110.65 ns | 3.25x |
| FloatUtf8 | `1.000000059604644775390625000000000000000000000001` | 367.69 ns | 119.64 ns | 3.07x |

All 24 measured combinations allocated zero bytes. Common-input controls retained
similar costs: UTF-16 double `12345678.12345678` measured 18.86 ns before and
18.84 ns after, and the existing zero-tail shortcut `90071992547409930000e-4`
measured 20.24 ns before and 20.48 ns after. Short `123` measurements moved by
less than 0.6 ns in either direction across the four entry points. These are
short shared-machine runs, not an application-wide throughput claim. ARM64
correctness is configured in CI; these timings were collected on x64.
