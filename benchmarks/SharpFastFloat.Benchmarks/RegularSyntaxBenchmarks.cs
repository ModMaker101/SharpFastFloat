using BenchmarkDotNet.Attributes;
using System;
using System.Globalization;
using System.Text;

namespace SharpFastFloat.Benchmarks
{
  [MemoryDiagnoser]
  public class RegularSyntaxBenchmarks
  {
    private byte[] _utf8;

    [Params("123,456,789", "12345678,12345678", "1234567890123456tail")]
    public string Input { get; set; }

    public NumberStyles Styles { get; private set; }

    [GlobalSetup]
    public void Setup()
    {
      _utf8 = Encoding.UTF8.GetBytes(Input);
      Styles = Input.IndexOf(',') >= 0 ? NumberStyles.Float | NumberStyles.AllowThousands : NumberStyles.Float;
    }

    [Benchmark]
    public double DoubleUtf16()
    {
      FastDoubleParser.TryParseDouble(Input.AsSpan(), out double value, Styles);
      return value;
    }

    [Benchmark]
    public double DoubleUtf8()
    {
      FastDoubleParser.TryParseDouble(_utf8.AsSpan(), out double value, Styles);
      return value;
    }

    [Benchmark]
    public float FloatUtf16()
    {
      FastFloatParser.TryParseFloat(Input.AsSpan(), out float value, Styles);
      return value;
    }

    [Benchmark]
    public float FloatUtf8()
    {
      FastFloatParser.TryParseFloat(_utf8.AsSpan(), out float value, Styles);
      return value;
    }
  }
}
