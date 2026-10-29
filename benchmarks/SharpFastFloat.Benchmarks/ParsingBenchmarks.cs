using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System;
using System.Globalization;
using System.Text;

namespace SharpFastFloat.Benchmarks
{
  [MemoryDiagnoser]
  [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
  [CategoriesColumn]
  public class ParsingBenchmarks
  {
    private byte[] _utf8;

    [Params("1", "123", "12345678", "1234567890123456", "12345678.12345678",
      "1.2345678901234567e-100", "9007199254740993")]
    public string Input { get; set; }

    [GlobalSetup]
    public void Setup()
    {
      _utf8 = Encoding.ASCII.GetBytes(Input);
      double expectedDouble = double.Parse(Input, CultureInfo.InvariantCulture);
      float expectedFloat = float.Parse(Input, CultureInfo.InvariantCulture);
      if (StrictDoubleUtf16() != expectedDouble || RegularDoubleUtf16() != expectedDouble ||
          StrictDoubleUtf8() != expectedDouble || RegularDoubleUtf8() != expectedDouble ||
          StrictFloatUtf16() != expectedFloat || RegularFloatUtf16() != expectedFloat ||
          StrictFloatUtf8() != expectedFloat || RegularFloatUtf8() != expectedFloat)
      {
        throw new InvalidOperationException("Benchmark parsers disagree on the input value.");
      }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DoubleUtf16")]
    public double RuntimeDoubleUtf16()
    {
      double.TryParse(Input.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value);
      return value;
    }

    [Benchmark]
    [BenchmarkCategory("DoubleUtf16")]
    public double RegularDoubleUtf16()
    {
      FastDoubleParser.TryParseDouble(Input.AsSpan(), out double value);
      return value;
    }

    [Benchmark]
    [BenchmarkCategory("DoubleUtf16")]
    public double StrictDoubleUtf16()
    {
      FastDoubleParser.TryParseDoubleStrict(Input.AsSpan(), out double value);
      return value;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DoubleUtf8")]
    public double RuntimeDoubleUtf8()
    {
      double.TryParse(_utf8.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value);
      return value;
    }

    [Benchmark]
    [BenchmarkCategory("DoubleUtf8")]
    public double RegularDoubleUtf8()
    {
      FastDoubleParser.TryParseDouble(_utf8.AsSpan(), out double value);
      return value;
    }

    [Benchmark]
    [BenchmarkCategory("DoubleUtf8")]
    public double StrictDoubleUtf8()
    {
      FastDoubleParser.TryParseDoubleStrict(_utf8.AsSpan(), out double value);
      return value;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("FloatUtf16")]
    public float RuntimeFloatUtf16()
    {
      float.TryParse(Input.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value);
      return value;
    }

    [Benchmark]
    [BenchmarkCategory("FloatUtf16")]
    public float RegularFloatUtf16()
    {
      FastFloatParser.TryParseFloat(Input.AsSpan(), out float value);
      return value;
    }

    [Benchmark]
    [BenchmarkCategory("FloatUtf16")]
    public float StrictFloatUtf16()
    {
      FastFloatParser.TryParseFloatStrict(Input.AsSpan(), out float value);
      return value;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("FloatUtf8")]
    public float RuntimeFloatUtf8()
    {
      float.TryParse(_utf8.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value);
      return value;
    }

    [Benchmark]
    [BenchmarkCategory("FloatUtf8")]
    public float RegularFloatUtf8()
    {
      FastFloatParser.TryParseFloat(_utf8.AsSpan(), out float value);
      return value;
    }

    [Benchmark]
    [BenchmarkCategory("FloatUtf8")]
    public float StrictFloatUtf8()
    {
      FastFloatParser.TryParseFloatStrict(_utf8.AsSpan(), out float value);
      return value;
    }

  }
}
