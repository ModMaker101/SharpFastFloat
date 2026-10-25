using BenchmarkDotNet.Attributes;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace SharpFastFloat.Benchmarks
{
  // Each invocation parses the complete selected dataset, not one token.
  [MemoryDiagnoser]
  public class DatasetBenchmarks
  {
    private string[] _utf16;
    private byte[][] _utf8;

    [Params("canada.txt", "mesh.txt", "synthetic.txt")]
    public string FileName { get; set; }

    [GlobalSetup]
    public void Setup()
    {
      _utf16 = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Data", FileName));
      _utf8 = Array.ConvertAll(_utf16, Encoding.ASCII.GetBytes);
      foreach (string input in _utf16)
      {
        double expected = double.Parse(input, CultureInfo.InvariantCulture);
        if (!FastDoubleParser.TryParseDoubleStrict(input.AsSpan(), out double actual) || actual != expected)
        {
          throw new InvalidOperationException("Dataset contains an invalid or incorrectly parsed decimal token.");
        }
      }
    }

    [Benchmark(Baseline = true)]
    public double RuntimeUtf16()
    {
      double maximum = double.MinValue;
      foreach (string input in _utf16)
      {
        double.TryParse(input.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value);
        if (value > maximum) { maximum = value; }
      }
      return maximum;
    }

    [Benchmark]
    public double RegularUtf16()
    {
      double maximum = double.MinValue;
      foreach (string input in _utf16)
      {
        FastDoubleParser.TryParseDouble(input.AsSpan(), out double value);
        if (value > maximum) { maximum = value; }
      }
      return maximum;
    }

    [Benchmark]
    public double StrictUtf16()
    {
      double maximum = double.MinValue;
      foreach (string input in _utf16)
      {
        FastDoubleParser.TryParseDoubleStrict(input.AsSpan(), out double value);
        if (value > maximum) { maximum = value; }
      }
      return maximum;
    }

    [Benchmark]
    public double RuntimeUtf8()
    {
      double maximum = double.MinValue;
      foreach (byte[] input in _utf8)
      {
        double.TryParse(input.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value);
        if (value > maximum) { maximum = value; }
      }
      return maximum;
    }

    [Benchmark]
    public double RegularUtf8()
    {
      double maximum = double.MinValue;
      foreach (byte[] input in _utf8)
      {
        FastDoubleParser.TryParseDouble(input.AsSpan(), out double value);
        if (value > maximum) { maximum = value; }
      }
      return maximum;
    }

    [Benchmark]
    public double StrictUtf8()
    {
      double maximum = double.MinValue;
      foreach (byte[] input in _utf8)
      {
        FastDoubleParser.TryParseDoubleStrict(input.AsSpan(), out double value);
        if (value > maximum) { maximum = value; }
      }
      return maximum;
    }

  }
}
