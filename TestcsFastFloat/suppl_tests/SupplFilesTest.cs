using csFastFloat;
using System;
using System.IO;
using Xunit;

namespace TestcsFastFloat.Tests.ff_suppl_tests
{
  public class SupplFilesTest : BaseTestClass
  {
    /// <summary>
    /// Verify FastParser for every .txt file of a given path
    /// </summary>
    /// <param name="args"></param>
    [Trait("Category", "Files Test")]
    [Fact()]
    private void AllFiles()
    {


      string pathValidation = "data_files";

      if (!Directory.Exists(pathValidation))
      {
        // Important: do not assume that path separator is \.
        pathValidation = "TestcsFastFloat" + Path.PathSeparator + pathValidation;
        if (!Directory.Exists(pathValidation))
        {
          Console.WriteLine("I looked for the data_files directory, and could not find it.");
          throw new ArgumentException("Invalid search path");
        }
      }

      foreach (var fileName in Directory.GetFiles(pathValidation, "*.txt"))
      {
        Console.WriteLine(fileName);
        try
        {
          VerifyFile(fileName);
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.ToString());
        }
      }
    }

    /// <summary>
    /// Check every line of the file which are required to be the following format
    /// 0000 00000000 0000000000000000 0e3
    /// ssss ffffffff dddddddddddddddd $$$$.....
    /// short float double string
    /// </summary>
    /// <param name="fileName">file to test</param>
    private static void VerifyFile(string fileName)
    {
      var fs = System.IO.File.OpenText(fileName);
      int counter = 0;
      int testCount = 0;
      while (!fs.EndOfStream)
      {
        string curntLine = fs.ReadLine();
        var sut = curntLine.Split();
        if (sut.Length != 4) throw new Exception($"Invalid file in file {curntLine}");

        // Make sense of s,f,d
        short expectedShort = ShortFromHexString(sut[0]);
        float expectedFloat = FloatFromHexString(sut[1]);
        double expectedDouble = DoubleFromHexString(sut[2]);

        // parse and assert equality
        float f = FastFloatParser.ParseFloat(sut[3]);
        Assert.True(expectedFloat == f);
        double d = FastDoubleParser.ParseDouble(sut[3]);
        Assert.True(expectedDouble == d);

        // parse and assert equality
        float fSpan = FastFloatParser.ParseFloat(sut[3].AsSpan());
        Assert.True(expectedFloat == fSpan);
        double dSpan = FastDoubleParser.ParseDouble(sut[3].AsSpan());
        Assert.True(expectedDouble == dSpan);

        // parse and assert equality
        float fUtf8 = FastFloatParser.ParseFloat(System.Text.Encoding.UTF8.GetBytes(sut[3]));
        Assert.True(expectedFloat == fUtf8);
        double dUtf8 = FastDoubleParser.ParseDouble(System.Text.Encoding.UTF8.GetBytes(sut[3]));
        Assert.True(expectedDouble == dUtf8);

        counter++;
        testCount += 6;
      }
      fs.Close();
      Console.WriteLine($"processed {counter} numbers, {testCount} tests in total.");
    }
  }
}
