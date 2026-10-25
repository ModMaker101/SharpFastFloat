# SharpFastFloat

[![NuGet](https://img.shields.io/nuget/vpre/SharpFastFloat.svg)](https://www.nuget.org/packages/SharpFastFloat)
[![Build](https://github.com/ModMaker101/SharpFastFloat/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ModMaker101/SharpFastFloat/actions/workflows/dotnet.yml)

Fast, correctly rounded `float` and `double` parsing for .NET, with UTF-16 and
UTF-8 span APIs. SharpFastFloat offers complete-token decimal parsing, configurable
prefix parsing, and allocation-free conversion with exact midpoint rounding.

## Install

```sh
dotnet add package SharpFastFloat --version 0.1.0-preview.2
```

Import the `SharpFastFloat` namespace. The package is currently a preview release.

## Supported frameworks

| Package target | Implementation |
|---|---|
| .NET 8 | Optimized parsing with hardware intrinsics where available |
| .NET 9 | Optimized parsing with hardware intrinsics where available |
| .NET Standard 2.0 | Portable fallbacks for compatible runtimes |

The optimized builds use x64 SIMD or ARM NEON where supported and retain scalar
fallbacks. The .NET Standard asset also supports older runtimes implementing that
standard; it does not require .NET 8.

## Parse a complete decimal token

Use the strict methods when the entire input must be a decimal number:

```csharp
using System;
using System.Text;
using SharpFastFloat;

bool valid = FastDoubleParser.TryParseDoubleStrict(
    "123.45e-2".AsSpan(), out double value); // true; value = 1.2345

byte[] utf8 = Encoding.UTF8.GetBytes("-12.5e2");
bool singleValid = FastFloatParser.TryParseFloatStrict(
    utf8.AsSpan(), out float single); // true; single = -1250
```

| Method | Inputs | Result |
|---|---|---|
| `FastDoubleParser.TryParseDoubleStrict` | `ReadOnlySpan<char>` or `ReadOnlySpan<byte>` | `double` |
| `FastFloatParser.TryParseFloatStrict` | `ReadOnlySpan<char>` or `ReadOnlySpan<byte>` | `float` |

Strict parsing accepts ASCII digits, an optional leading `+` or `-`, a decimal
point (`.`), and an optional exponent. Inputs such as `123`, `-.5`, `1.`, and
`1.25e-10` are valid. The complete span must match.

Whitespace, grouping separators, `NaN`/`Infinity` text, radix prefixes, empty
inputs, and trailing content are rejected. Failure returns `false` and sets the
result to zero. Numeric overflow returns signed infinity; underflow and zero
preserve the sign. Float conversion rounds directly to binary32.

Trim whitespace and handle application-specific non-decimal syntax before calling
these methods. Byte spans contain UTF-8/ASCII decimal tokens.

## Parse a numeric prefix

The regular methods support strings, spans, and unsafe pointer ranges. They parse
a numeric prefix and optionally report how much input was consumed:

```csharp
using System;
using SharpFastFloat;

bool valid = FastDoubleParser.TryParseDouble(
    "123.45rest".AsSpan(), out int charactersConsumed, out double value);
// valid = true; value = 123.45; charactersConsumed = 6
```

Use `TryParseFloat` for a `float` result. `ParseDouble` and `ParseFloat` provide
throwing alternatives. Trailing content can remain after a successful regular
parse; use a strict method for complete decimal-token validation.

### Separators and grouping

Regular parsing accepts explicit separators and `NumberStyles` options. Enable
`AllowThousands` to accept grouping in the integer part:

```csharp
using System;
using System.Globalization;
using SharpFastFloat;

bool valid = FastDoubleParser.TryParseDouble(
    "1.234.567,89".AsSpan(),
    out double value,
    NumberStyles.Float | NumberStyles.AllowThousands,
    decimalSeparator: ',',
    thousandsSeparator: '.'); // value = 1234567.89
```

Grouping is accepted between integer digits without enforcing group sizes.
Without `AllowThousands`, parsing stops at a grouping separator: `"1,234"`
parses as `1` with one character consumed. Decimal syntax is configured through
the separator arguments rather than inferred from the current culture.

## Performance

SharpFastFloat uses eight-digit scanning, fast returns for bounded integers,
discarded-zero tracking, and stack-backed integer comparisons for difficult
rounding boundaries. Span parsing avoids per-number heap allocations.

Selected UTF-16 microbenchmarks for the midpoint comparison improvement:

| Case | Before | After |
|---|---:|---:|
| Double: `90071992547409930001e-4` | 468.50 ns | 69.92 ns |
| Double: exact midpoint above 1 | 422.64 ns | 116.76 ns |
| Float: midpoint plus a tiny tail | 359.55 ns | 110.65 ns |

These measurements used AMD EPYC 9V74 and .NET 9.0.21; all measured cases
allocated zero bytes. They describe specific inputs and changes, not an
application-wide speedup. See [benchmark results and reproduction commands](https://github.com/ModMaker101/SharpFastFloat/blob/master/docs/benchmarks.md)
for exact inputs, UTF-8 results, scanner measurements, and tradeoffs.

## Migrating from csFastFloat

Reference the `SharpFastFloat` package and replace `using csFastFloat;` with
`using SharpFastFloat;`. The assembly and namespace changed, so rebuild consumers.
Parser class and method names are retained. Named arguments use
`decimalSeparator`, `thousandsSeparator`, and `charactersConsumed`.

## Development

See [contributing and release instructions](https://github.com/ModMaker101/SharpFastFloat/blob/master/CONTRIBUTING.md)
for builds, tests, formatting, benchmarks, and NuGet trusted publishing.

## License and acknowledgments

SharpFastFloat is maintained by [ModMaker101](https://github.com/ModMaker101) and
is a fork of [csFastFloat](https://github.com/CarlVerret/csFastFloat), originally
created by Carl Verret, based on [fast_float](https://github.com/fastfloat/fast_float)
by Daniel Lemire and its contributors. Original copyright notices are preserved.

Licensed under [MIT](https://github.com/ModMaker101/SharpFastFloat/blob/master/LICENSE).
The parsing algorithm is described in Daniel Lemire's
[Number Parsing at a Gigabyte per Second](https://arxiv.org/abs/2101.11408).
