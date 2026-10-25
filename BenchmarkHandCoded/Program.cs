using csFastFloat;
using System;
using System.Diagnostics;
using System.Globalization;

namespace BenchmarkHandCoded
{
  internal class Program
  {
    public delegate double ParsingFunc(string[] values);
    public delegate double ParsingFuncUTF8(byte[][] values);

    static internal Tuple<double, double, double> time_it_ns<T>(string[] lines, ParsingFunc sut, long repeat)
    {
      double average = 0;
      double averageTpr = 0; // tick per run
      double minValue = double.MaxValue;

      //  warmup
      for (int i = 0; i != 250; i++)
      {

        sut(lines);
      }


      Stopwatch sw = new Stopwatch();

      for (int i = 0; i != repeat; i++)
      {
        // ...; 

        sw.Restart();
        sut(lines);

        sw.Stop();


        // NOTE : Elapsed.TotalMilliseconds (double) returns the total number of whole and fractional milliseconds elapsed since inception
        //        Elapsed.Milliseconds (int) returns the number of whole milliseconds in the current second => this is absolutely not what we need !

        var dif = sw.Elapsed.TotalMilliseconds * 1000000;

        average += dif;
        averageTpr += sw.ElapsedTicks;

        minValue = minValue < dif ? minValue : dif;
      }

      average /= repeat;
      averageTpr /= repeat;
      return new Tuple<double, double, double>(minValue, average, averageTpr);
    }
    static internal Tuple<double, double, double> time_it_ns_ut8<T>(byte[][] lines, ParsingFuncUTF8 sut, long repeat)
    {
      double averageTpr = 0;
      double average = 0;
      double minValue = double.MaxValue;

      //  warmup
      for (int i = 0; i != 250; i++)
      {

        sut(lines);
      }


      Stopwatch sw = new Stopwatch();

      for (int i = 0; i != repeat; i++)
      {
        // ...; 

        sw.Restart();
        sut(lines);

        sw.Stop();

        // NOTE : Elapsed.TotalMilliseconds (double) returns the total number of whole and fractional milliseconds elapsed since inception
        //        Elapsed.Milliseconds (int) returns the number of whole milliseconds in the current second => this is absolutely not what we need !


        var dif = sw.Elapsed.TotalMilliseconds * 1000000;
        averageTpr += sw.ElapsedTicks;
        average += dif;


        minValue = minValue < dif ? minValue : dif;
      }
      averageTpr /= repeat;
      average /= repeat;
      return new Tuple<double, double, double>(minValue, average, averageTpr);
    }

    internal static string[] GetLinesFromFile(string fileName) =>
        System.IO.File.ReadAllLines(fileName);

    private static double find_max_fast_float(string[] lines)
    {

      double max = double.MinValue;

      foreach (string l in lines)
      {
        double x = FastDoubleParser.ParseDouble(l);
        max = max > x ? max : x;
      }

      return max;
    }

    private static double find_max_fast_float_try(string[] lines)
    {

      double max = double.MinValue;

      foreach (string l in lines)
      {

        if (FastDoubleParser.TryParseDouble(l, out double x))
        {
          max = max > x ? max : x;
        }
        else
        {
          Console.WriteLine("bug");

        }
      }

      return max;
    }

    private static double find_max_fast_float_utf8(byte[][] lines)
    {

      double max = double.MinValue;
      foreach (var l in lines)
      {
        double x = FastDoubleParser.ParseDouble(l);
        max = max > x ? max : x;
      }

      return max;
    }

    private static double find_max_fast_float_try_utf8(byte[][] lines)
    {

      double max = double.MinValue;

      foreach (var l in lines)
      {
        if (FastDoubleParser.TryParseDouble(l, out double x))
        {
          max = max > x ? max : x;
        }
        else
        {
          Console.WriteLine("bug");

        }
      }

      return max;
    }



    private static double find_max_double_parse(string[] lines)
    {
      double max = double.MinValue;
      foreach (string l in lines)
      {
        double x = double.Parse(l, CultureInfo.InvariantCulture);
        max = max > x ? max : x;
      }

      return max;
    }

    static private void pretty_print(double volume, uint numberOfFloats, string name, Tuple<double, double, double> result)
    {
      double volumeMB = volume / (1024.0 * 1024.0);
      Console.Write("| {0,-40}|{1,8:f2} (+/- {2:f1} % ) |", name, volumeMB * 1000000000 / result.Item1, (result.Item2 - result.Item1) * 100.0 / result.Item2);
      Console.Write("{0,8:f2}  | ", numberOfFloats * 1000 / result.Item1);
      Console.Write(" {0,8:f2} | ", (double)result.Item1 / numberOfFloats);
      Console.Write(" {0,8:f2} |  \n", (double)result.Item3 / numberOfFloats);
    }

    private static void Main(string[] args)
    {
      string fileName = @"data/canada.txt";
      var lines = GetLinesFromFile(fileName);
      var linesUtf8 = Array.ConvertAll(lines, System.Text.Encoding.UTF8.GetBytes);
      int volume = 0;
      foreach (string l in lines)
      {
        volume += l.Length;
      }


      print_separator();
      double volumeMB = volume / (1024.0 * 1024.0);

      Console.WriteLine("canada.txt - Volume : {0:N2} MB", volumeMB);
      process_test(lines, linesUtf8, (double)volume);


      lines = GetLinesFromFile(@"data/mesh.txt");
      linesUtf8 = Array.ConvertAll(lines, System.Text.Encoding.UTF8.GetBytes);
      volume = 0;
      foreach (string l in lines)
      {
        volume += l.Length;
      }
      volumeMB = volume / (1024.0 * 1024.0);

      print_separator();
      Console.WriteLine("mesh.txt - Volume : {0:N2} MB", volumeMB);

      process_test(lines, linesUtf8, (double)volume);


      lines = GetLinesFromFile(@"data/synthetic.txt");
      linesUtf8 = Array.ConvertAll(lines, System.Text.Encoding.UTF8.GetBytes);

      volume = 0;
      foreach (string l in lines)
      {
        volume += l.Length;
      }
      volumeMB = volume / (1024.0 * 1024.0);

      print_separator();
      Console.WriteLine("synthetic.txt - Volume : {0:N2} MB", volumeMB);

      process_test(lines, linesUtf8, (double)volume);


    }


    private static void print_header()
    {
      print_separator();
      Console.WriteLine("| Test name".PadRight(42) + "|                 MB/s |" + " Mfloat/s | min ns/f | avg tick/f |");
      print_separator();
    }
    private static void print_separator()
    {
      Console.WriteLine("-----------------------------------------------------------------------------------------------------");
    }

    private static void process_test(string[] lines, byte[][] linesUtf8, double volume)
    {

      print_header();

      pretty_print(volume, (uint)lines.Length, "Double.Parse", time_it_ns<double>(lines, find_max_double_parse, 100));
      pretty_print(volume, (uint)lines.Length, "FastParser.ParseDouble", time_it_ns<double>(lines, find_max_fast_float, 100));
      pretty_print(volume, (uint)lines.Length, "FastParser.TryParseDouble", time_it_ns<double>(lines, find_max_fast_float_try, 100));
      pretty_print(volume, (uint)lines.Length, "FastParser.ParseDouble UT8", time_it_ns_ut8<double>(linesUtf8, find_max_fast_float_utf8, 100));
      pretty_print(volume, (uint)lines.Length, "FastParser.TryParseDouble UT8", time_it_ns_ut8<double>(linesUtf8, find_max_fast_float_try_utf8, 100));
      print_separator();

    }

  }
}
