using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
//using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using SharpFastFloat.Structures;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SharpFastFloat.Benchmarks
{

  //[MemoryDiagnoser]
  [SimpleJob(RuntimeMoniker.Net90)]
  [Config(typeof(Config))]
  public class FFBenchmark
  {
    private string[] _lines;
    private byte[][] _linesUtf8;

    private class Config : ManualConfig
    {
      public Config()
      {
        AddColumn(StatisticColumn.Min);
        AddColumn(new MFloatPerSecColumn());
        AddColumn(new VolumePerSecColumn());
      }
    }

    [Benchmark(Description = "Utf8Parser")]
    public double Utf8Parser()
    {
      double max = double.MinValue;

      foreach (byte[] l in _linesUtf8)
      {
        if (!System.Buffers.Text.Utf8Parser.TryParse(l, out double d, out int consumed) || consumed != l.Length)
          throw new InvalidOperationException();

        max = d > max ? d : max;
      }
      return max;
    }

    [Benchmark(Description = "FastFloat.TryParseDouble() - UTF8")]
    public double FastParserUtf8()
    {
      double max = double.MinValue;

      foreach (byte[] l in _linesUtf8)
      {
        FastDoubleParser.TryParseDouble(l, out double d);
        max = d > max ? d : max;
      }
      return max;
    }

    [Benchmark(Description = "FastFloat.TryParseDouble()")]
    public double FastParser()
    {
      double max = double.MinValue;

      foreach (string l in _lines)
      {
        FastDoubleParser.TryParseDouble(l, out double d);
        max = d > max ? d : max;
      }
      return max;
    }





    [Benchmark(Description = "ParseNumberString() only")]
    public double FastParserPNS()
    {
      double max = double.MinValue;

      foreach (string l in _lines)
      {
        unsafe
        {

          fixed (char* p = l)
          {
            var pni = ParsedNumberString.ParseNumberString(p, p + l.Length);
            max = pni.Exponent > max ? pni.Exponent : max;
          }



        }
      }
      return max;
    }



    [Benchmark(Baseline = true, Description = "Double.Parse()")]
    public double DoubleStd()
    {
      double max = double.MinValue;
      foreach (string l in _lines)
      {
        double d = double.Parse(l, CultureInfo.InvariantCulture);

        max = d > max ? d : max;
      }
      return max;
    }


    [Params(@"data/canada.txt", @"data/mesh.txt", @"data/synthetic.txt")]
    public string FileName;

    [GlobalSetup]
    public void Setup()
    {
      Console.WriteLine("reading data");
      _lines = System.IO.File.ReadAllLines(FileName);
      _linesUtf8 = Array.ConvertAll(_lines, System.Text.Encoding.UTF8.GetBytes);
    }
  }








  public class Program
  {
    public static void Main(string[] args)
    {

      var config = DefaultConfig.Instance.WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(100));
      BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

    }
  }
}
