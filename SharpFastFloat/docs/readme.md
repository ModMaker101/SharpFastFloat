# SharpFastFloat

[![.NET](https://github.com/ModMaker101/SharpFastFloat/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ModMaker101/SharpFastFloat/actions/workflows/dotnet.yml)

A performance-focused fork of [csFastFloat](https://github.com/CarlVerret/csFastFloat),
derived from [fast_float](https://github.com/fastfloat/fast_float).

Targets .NET 8, .NET 9, and .NET Standard 2.0. Dedicated .NET 5 and .NET Core 3.1
builds are no longer included; the .NET Standard build retains portable fallbacks.

SharpFastFloat provides correctly rounded float and double parsing for UTF-16 and
UTF-8. It includes strict decimal entry points, configurable regular parsing,
eight-digit scanning with x64/ARM SIMD, and stack-backed exact midpoint rounding.

```csharp
using SharpFastFloat;

bool valid = FastDoubleParser.TryParseDoubleStrict("123.45e-2".AsSpan(), out double value);
bool floatValid = FastFloatParser.TryParseFloatStrict("123.45e-2".AsSpan(), out float single);
```

Both strict methods also accept `ReadOnlySpan<byte>` for UTF-8/ASCII. They require
an entire decimal token with an optional sign, decimal point, and exponent. They
reject whitespace, grouping, NaN/Infinity text, radix prefixes, and trailing content.
Numeric overflow produces signed infinity; underflow preserves signed zero.
Callers handle application-specific trimming and non-decimal syntax.

`TryParseDouble`, `TryParseFloat`, `ParseDouble`, and `ParseFloat` remain available
for configurable separators, `NumberStyles`, and prefix parsing with an optional
`charactersConsumed` output. Public separator parameter names are `decimalSeparator`
and `thousandsSeparator`.

When migrating from csFastFloat, reference `SharpFastFloat` and change the namespace
import to `using SharpFastFloat;`. The assembly identity has changed, so rebuild
consuming projects. Parser class and method names are retained.

See the [repository README](https://github.com/ModMaker101/SharpFastFloat) for full
API details, benchmarks, build instructions, and attribution. Licensed under MIT;
original csFastFloat and fast_float copyright notices are preserved.
