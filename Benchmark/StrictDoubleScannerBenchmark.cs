using BenchmarkDotNet.Attributes;
using System;

namespace csFastFloat.Benchmark
{
  [MemoryDiagnoser]
  public class StrictDoubleScannerBenchmark
  {
    [Params("1", "123", "-0", "12345678", "1234567890123456",
      "90071992547409930000e-4", "90071992547409930001e-4",
      "1.0000000000000000000000000000000000000000",
      "1.2345678901234567000000000000000000",
      "1.2345678901234567000000000000000001",
      "1.00000000000000011102230246251565404236316680908203125")]
    public string Input { get; set; }

    [Benchmark]
    public double Strict()
    {
      FastDoubleParser.TryParseDoubleStrict(Input.AsSpan(), out double value);
      return value;
    }
  }
}
