# SharpFastFloat : a fast and accurate float parser
[![.NET](https://github.com/ModMaker101/SharpFastFloat/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ModMaker101/SharpFastFloat/actions/workflows/dotnet.yml)

SharpFastFloat is a performance-focused fork of [csFastFloat](https://github.com/CarlVerret/csFastFloat),
derived from Daniel Lemire and the [fast_float authors' C++ library](https://github.com/fastfloat/fast_float).
It provides correctly rounded float and double parsing for UTF-16 and UTF-8,
strict decimal APIs, SIMD scanning on x64/ARM, and stack-backed exact midpoint rounding.

The original csFastFloat library is used by [Sep](https://github.com/nietras/Sep).
Historical upstream benchmark results are retained below; later sections document
measurements and improvements made in this fork.

To migrate from csFastFloat, reference the `SharpFastFloat` package/project and
replace `using csFastFloat;` with `using SharpFastFloat;`. The assembly and namespace
have changed, so consumers must rebuild. Parser class and method names are retained.
The solution is now `SharpFastFloat.sln`.

# Benchmarks

We use the realistic files  in /data. The mesh.txt data file contains numbers that are easier to parse whereas the canada.txt data file is representative of a more challenging scenario.  Synthetic.txt contains 150 000 random floats. We compare  the `Double.Parse()` function from the runtime library with our `FastFloat.ParseDouble()` function. The `ParseNumberString() only` function parses the string itself without any float computation: it might represent an upper bound on the possible performance.
Most recent benchmark done with .NET 9.0

``` ini

BenchmarkDotNet v0.14.0, Rocky Linux 9.4 (Blue Onyx)
Intel Xeon Gold 6338 CPU 2.00GHz, 2 CPU, 128 logical and 64 physical cores
.NET SDK 9.0.100
  [Host]   : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  .NET 9.0 : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=.NET 9.0  Runtime=.NET 9.0  

| Method                              | FileName           | Mean        | Min         | MFloat/s | MB/s     |
|------------------------------------ |------------------- |------------:|------------:|---------:|---------:|
| FastFloat.TryParseDouble()          | data/canada.txt    |  2,618.8 us |  2,614.8 us |    42.50 |   798.54 |
| Double.Parse()                      | data/canada.txt    | 11,216.7 us | 11,191.3 us |     9.93 |   186.57 |
|                                     |                    |             |             |          |          |
| FastFloat.TryParseDouble()          | data/mesh.txt      |    957.3 us |    949.3 us |    76.92 |   653.13 |
| Double.Parse()                      | data/mesh.txt      |  4,016.3 us |  4,005.7 us |    18.23 |   154.78 |
|                                     |                    |             |             |          |          |
| FastFloat.TryParseDouble()          | data/synthetic.txt |  3,141.1 us |  3,136.1 us |    47.83 |   899.86 |
| Double.Parse()                      | data/synthetic.txt | 14,575.3 us | 14,541.5 us |    10.32 |   194.07 |

```

In this repo [FastFloatTestBench](https://github.com/CarlVerret/FastFloatTestBench) we demonstrate a concrete performance gain obtained with FastFloat.ParseDouble() with the [CSVHelper](https://github.com/JoshClose/CsvHelper) library.  This is one of the fastest CSV parser available.



# Requirements

.NET Standard 2.0 or newer. Under .NET 5 framework or newer, the library takes advantage of the new Math.BigMul() and Sse4.1 (SIMD) functions.

# Compile and testing

As this library targets multiple framework, you can specify the target framework version with -f parameter :

``` command line
dotnet build -c Release -f net9.0
dotnet test -f net9.0

```
If you omit the target framework and you don't have both .net 8.0 and dotnetcore 3.1 SDKs installed you may experience an error when building or running tests.

The set of unit tests in /SharpFastFloat.Tests project combines unit tests from many libraries.  It includes tests used by the Go Team.
Additional info on Nigel Tao's work can be found [here](https://nigeltao.github.io/blog/2020/eisel-lemire.html#testing).  It also include strtod test file that can be found [here](https://github.com/ahrvoje/numerics/blob/master/strtod/strtod_tests.toml).




Some unit tests are based on [Approvals.net library](https://github.com/approvals/ApprovalTests.Net).  They require a diff tool installed on your computer.  Tests will be automatically skiped if no diff tool is found.


# Usage

`FastDoubleParser.TryParseDoubleStrict` and `FastFloatParser.TryParseFloatStrict`
each accept `ReadOnlySpan<char>` (UTF-16) or `ReadOnlySpan<byte>` (UTF-8/ASCII).
They parse a complete decimal token using fixed `.` syntax and ASCII digits.
They accept an optional sign, decimal point, and exponent (for example, `-12.5e+3`, `.5`, or
`1.`). They reject whitespace, grouping, trailing characters, NaN, Infinity, and
radix prefixes. Failure returns `false` and sets the result to zero. Overflow
returns signed infinity; underflow and zero preserve the sign.

```csharp
bool valid = FastDoubleParser.TryParseDoubleStrict("123.45e-2".AsSpan(), out double value);
bool singleValid = FastFloatParser.TryParseFloatStrict("123.45e-2".AsSpan(), out float single);
ReadOnlySpan<byte> utf8 = System.Text.Encoding.UTF8.GetBytes("123.45e-2");
bool utf8Valid = FastDoubleParser.TryParseDoubleStrict(utf8, out double utf8Value);
bool utf8SingleValid = FastFloatParser.TryParseFloatStrict(utf8, out float utf8Single);
```

For JavaScript conversion, the caller (such as SharpES) remains responsible for
trimming JavaScript whitespace, empty-string conversion, Infinity, and radix
prefixes before calling this decimal entry point. No separate decimal validation
pass is needed. The strict scanner validates while accumulating digits, uses
SIMD for long runs on x64 and ARM NEON, and retains the original span for exact
rounding when needed. Existing configurable parsing APIs keep their behavior.

All four strict entry points use the same scanner design. Float conversion rounds
directly to binary32. The scanner tracks whether discarded mantissa digits contain
any nonzero values: an all-zero tail is an exact scaled coefficient and skips the
uncertainty check against the next mantissa. Nonzero tails retain the exact
rounding fallback. Single ASCII digits return immediately, and short integer runs
avoid significant-digit bookkeeping. Exactly representable integers return
directly through 2^53 for double and 2^24 for float, preserving negative zero.

Two functions are available: `FastDoubleParser.ParseDouble` and `FastFloatParser.ParseFloat`. Since v3.0, TryParse pattern is supported for each function.

`String`, `char *`  and `ReadOnlySpan<char>` are supported inputs.

```C#
using SharpFastFloat;


double x;
float y;
double z;
double answer = 0;
foreach (string l in lines)
{
        x = FastDoubleParser.ParseDouble(l);
        FastDoubleParser.TryParseDouble(l, out z);
        
        y = FastFloatParser.ParseFloat(l);
}
```

Input strings are expected to be valid UTF-16.

Trailing content in the string is ignored.  You may pass an optional `out int charactersConsumed` parameter
`FastDoubleParser.ParseDouble(l, out int charactersConsumed)` if you wich to check how many characters were processed. Some users may want to fail when the number of characters consumed does not match the string length.


For UTF-8 or ASCII inputs, you may pass a `ReadOnlySpan<byte>` argument. You can also pass
an optional `out int charactersConsumed` parameter to track the number of characters consumed
by the number pattern.

## Thousands separators

To match the behaviour of `Double.Parse` / `Single.Parse` on inputs such as `"1,234,567.89"`,
pass `NumberStyles.AllowThousands`. The separator defaults to `,` and can be overridden via
the optional `thousandsSeparator` parameter (e.g. for the European convention where the
decimal separator is `,` and the thousands separator is `.`):

```C#
using System.Globalization;
using SharpFastFloat;

// Invariant culture: "," as thousands, "." as decimal.
double a = FastDoubleParser.ParseDouble(
    "1,234,567.89",
    NumberStyles.Float | NumberStyles.AllowThousands);                // 1234567.89

// European convention: "." as thousands, "," as decimal.
double b = FastDoubleParser.ParseDouble(
    "1.234.567,89",
    NumberStyles.Float | NumberStyles.AllowThousands,
    decimalSeparator: ',',
    thousandsSeparator: '.');                                        // 1234567.89
```

Without `AllowThousands`, the parser stops at the first separator (so `"1,234"` parses as
`1` with `charactersConsumed == 1`), preserving the prior default behaviour.




# Credit
This library was the main project in my master's degree under the direction of Professor [Daniel Lemire](https://github.com/lemire) at TELUQ University.
A special thanks to [Egor Bogatov](https://github.com/EgorBo) and all contributors for their really meaningful contribution.

# Reference

- Daniel Lemire, [Number Parsing at a Gigabyte per Second](https://arxiv.org/abs/2101.11408), [Software: Practice and Experience](https://onlinelibrary.wiley.com/doi/10.1002/spe.2984) 51 (8), 2021.

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
dotnet run -c Release --project SharpFastFloat.Benchmarks -- --filter '*StrictParserBenchmark*' '*DigitChunkBenchmark*' --job short
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
dotnet run -c Release --project SharpFastFloat.Benchmarks -- --filter '*RegularScannerBenchmark*' --job short --warmupCount 3 --iterationCount 5 --iterationTime 100 --launchCount 1
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
dotnet run -c Release --project SharpFastFloat.Benchmarks -- --filter '*StrictDoubleScannerBenchmark*' '*StrictEntryPointsBenchmark*' --job short --warmupCount 3 --iterationCount 5 --iterationTime 200 --launchCount 1
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
dotnet run -c Release --project SharpFastFloat.Benchmarks -- --filter '*MidpointRoundingBenchmark*' --job short --warmupCount 3 --iterationCount 5 --iterationTime 100 --launchCount 1
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

## Publishing to NuGet.org

`.github/workflows/publish.yml` uses [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
with GitHub Actions OIDC and `NuGet/login@v1`. No long-lived NuGet API key or GitHub
secret is required. The policy must match owner `ModMaker101`, repository
`SharpFastFloat`, workflow filename `publish.yml`, and NuGet user `ModMaker101`.
The workflow does not use a GitHub Actions environment, matching a policy without
an environment restriction.

After merging the release changes, tag the intended commit and push the tag:

```sh
git tag v4.1.6
git push origin v4.1.6
```

Use a new version for each release; prerelease tags such as `v4.1.6-preview.1` are
also supported. The tag supplies the package version, overriding the development
version in the project file. The workflow restores, checks formatting, builds all
targets, runs the .NET 8 and .NET 9 test suites, and packs the library before
requesting a short-lived NuGet key. It publishes the package and associated symbols
to NuGet.org and retains the package files as GitHub Actions artifacts. Ordinary
branch pushes and pull requests do not publish packages.
