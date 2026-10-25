using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace SharpFastFloat.Benchmarks
{
  public static class Program
  {
    public static void Main(string[] args)
    {
      var config = DefaultConfig.Instance.WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(100));
      BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
  }
}
