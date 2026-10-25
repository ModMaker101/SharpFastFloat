using BenchmarkDotNet.Attributes;
using System;
using System.Text;

namespace SharpFastFloat.Benchmarks
{
  [MemoryDiagnoser]
  public class MidpointRoundingBenchmark
  {
    private byte[] _utf8;

    [Params("123", "12345678.12345678", "90071992547409930000e-4",
      "90071992547409930001e-4",
      "1.00000000000000011102230246251565404236316680908203125",
      "1.000000059604644775390625000000000000000000000001")]
    public string Input { get; set; }

    [GlobalSetup]
    public void Setup() => _utf8 = Encoding.ASCII.GetBytes(Input);

    [Benchmark]
    public double DoubleUtf16()
    {
      FastDoubleParser.TryParseDoubleStrict(Input.AsSpan(), out double value);
      return value;
    }

    [Benchmark]
    public double DoubleUtf8()
    {
      FastDoubleParser.TryParseDoubleStrict(_utf8.AsSpan(), out double value);
      return value;
    }

    [Benchmark]
    public float FloatUtf16()
    {
      FastFloatParser.TryParseFloatStrict(Input.AsSpan(), out float value);
      return value;
    }

    [Benchmark]
    public float FloatUtf8()
    {
      FastFloatParser.TryParseFloatStrict(_utf8.AsSpan(), out float value);
      return value;
    }
  }
}
