using BenchmarkDotNet.Attributes;
using System;
using System.Text;

namespace csFastFloat.Benchmark
{
  [MemoryDiagnoser]
  public class StrictEntryPointsBenchmark
  {
    private byte[] _utf8;

    [Params("1", "123", "1234567890123456", "90071992547409930000e-4",
      "167772170000000000000000e-16",
      "1.000000059604644775390625000000000000000000000001")]
    public string Input { get; set; }

    [GlobalSetup]
    public void Setup() => _utf8 = Encoding.UTF8.GetBytes(Input);

    [Benchmark]
    public double StrictDoubleUtf16()
    {
      FastDoubleParser.TryParseDoubleStrict(Input.AsSpan(), out double value);
      return value;
    }

    [Benchmark]
    public double StrictDoubleUtf8()
    {
      FastDoubleParser.TryParseDoubleStrict(_utf8.AsSpan(), out double value);
      return value;
    }

    [Benchmark]
    public float StrictFloatUtf16()
    {
      FastFloatParser.TryParseFloatStrict(Input.AsSpan(), out float value);
      return value;
    }

    [Benchmark]
    public float StrictFloatUtf8()
    {
      FastFloatParser.TryParseFloatStrict(_utf8.AsSpan(), out float value);
      return value;
    }

    [Benchmark]
    public double RegularDoubleUtf16()
    {
      FastDoubleParser.TryParseDouble(Input.AsSpan(), out double value);
      return value;
    }

    [Benchmark]
    public double RegularDoubleUtf8()
    {
      FastDoubleParser.TryParseDouble(_utf8.AsSpan(), out double value);
      return value;
    }

    [Benchmark]
    public float RegularFloatUtf16()
    {
      FastFloatParser.TryParseFloat(Input.AsSpan(), out float value);
      return value;
    }

    [Benchmark]
    public float RegularFloatUtf8()
    {
      FastFloatParser.TryParseFloat(_utf8.AsSpan(), out float value);
      return value;
    }
  }
}
