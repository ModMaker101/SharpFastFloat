using BenchmarkDotNet.Attributes;
using System;
using System.Globalization;

namespace SharpFastFloat.Benchmarks
{
  [MemoryDiagnoser]
  public class StrictParserBenchmark
  {
    [Params("1", "123", "1234", "12345678", "1234567890123456", "9007199254740993",
      "123456789012345678901234567890", "12345678.12345678", "1.2345678901234567e-100")]
    public string Input { get; set; }

    [Benchmark(Baseline = true)]
    public double Runtime()
    {
      double.TryParse(Input.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value);
      return value;
    }

    [Benchmark]
    public double General()
    {
      FastDoubleParser.TryParseDouble(Input.AsSpan(), out double value);
      return value;
    }

    [Benchmark]
    public double Strict()
    {
      FastDoubleParser.TryParseDoubleStrict(Input.AsSpan(), out double value);
      return value;
    }
  }

  // Isolate validation and accumulation to compare four/eight digit chunks
  // against scalar runs, including the branch cost for short inputs.
  public unsafe class DigitChunkBenchmark
  {
    [Params("1", "123", "1234", "12345678", "1234567890123456", "1234567890123456789")]
    public string Input { get; set; }

    [Benchmark(Baseline = true)]
    public ulong Scalar()
    {
      ulong value = 0;
      foreach (char c in Input)
      {
        if (!Utils.IsInteger(c, out uint digit)) { break; }
        value = value * 10 + digit;
      }
      return value;
    }

    [Benchmark]
    public ulong FourDigits()
    {
      fixed (char* first = Input)
      {
        char* p = first;
        char* last = first + Input.Length;
        ulong value = 0;
        while (last - p >= 4 && Utils.TryParseFourConsecutiveDigits(p, out uint chunk))
        {
          value = value * 10000 + chunk;
          p += 4;
        }
        while (p != last && Utils.IsInteger(*p, out uint digit))
        {
          value = value * 10 + digit;
          ++p;
        }
        return value;
      }
    }

    [Benchmark]
    public ulong EightDigits()
    {
      fixed (char* first = Input)
      {
        char* p = first;
        char* last = first + Input.Length;
        ulong value = 0;
        while (last - p >= 8 && Utils.TryParseEightConsecutiveDigits(p, out uint chunk))
        {
          value = value * 100000000 + chunk;
          p += 8;
        }
        while (p != last && Utils.IsInteger(*p, out uint digit))
        {
          value = value * 10 + digit;
          ++p;
        }
        return value;
      }
    }
  }
}
