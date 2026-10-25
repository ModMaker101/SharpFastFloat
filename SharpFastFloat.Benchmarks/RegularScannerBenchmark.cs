using BenchmarkDotNet.Attributes;
using System;
using System.Globalization;
using System.Text;

namespace SharpFastFloat.Benchmarks
{
  [MemoryDiagnoser]
  public class RegularScannerBenchmark
  {
    private byte[] _utf8;

    [Params("1", "123", "1234567", "12345678", "1234567890123456", "1234567890123456789",
      "123456789012345678901234567890", "123,456,789", "12345678,12345678",
      "12345678.12345678", "1.2345678901234567e-100", "1234567890123456tail")]
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
