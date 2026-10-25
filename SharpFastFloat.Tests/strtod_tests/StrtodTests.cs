using ApprovalTests.Namers;
using SharpFastFloat;
using SharpFastFloat.Tests;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace SharpFastFloat.Tests.suppl_tests
{


  public class StrtodTests : BaseTestClass
  {

#if NET8_0
    const string Version = "8.0";

#endif

#if NET9_0
    const string Version = "9.0";
#endif

#if NET8_0_OR_GREATER

    /// <summary>
    /// strtod test cases
    /// https://github.com/ahrvoje/numerics/blob/master/strtod/strtod_tests.toml
    /// </summary>
    [SkippableFact]
    void StrtodTestsFile()
    {
      Skip.If(base.NoDiffToolDetected(), "No diff tool detected");

      StringBuilder sb = new StringBuilder();

      foreach (var fileName in Directory.GetFiles("strtod_tests", "*.txt"))
      {
        Console.WriteLine(fileName);
        var fs = System.IO.File.OpenText(fileName);
        int counter = 0;
        while (!fs.EndOfStream)
        {
          string curntLine = fs.ReadLine();


          if (curntLine.StartsWith("[["))
          {
            sb.AppendLine("");
            sb.AppendLine("");
            sb.AppendLine(curntLine);



          }




          if (curntLine.StartsWith("UID") || curntLine.StartsWith("int"))
          {
            sb.AppendLine(curntLine);

          }

          if (curntLine.StartsWith("str ="))
          {

            sb.AppendLine(curntLine);
            var sut = curntLine.Split('=')[1].Replace("\"", "").Trim();


            if (!double.TryParse(sut, out double dbVal))
            {
              sb.AppendLine("d   = *** can't parse *** ");
            }
            else
            {
              sb.AppendLine($"d   = {dbVal}");
            }

            if (!FastDoubleParser.TryParseDouble(sut, out double ffVal))
            {
              sb.AppendLine("ff   = *** can't parse *** ");
            }
            else
            {
              sb.AppendLine($"ff  = {ffVal}");
            }


          }

          counter++;
        }
        fs.Close();

        using (ApprovalResults.ForScenario(Version))
        {
          ApprovalTests.Approvals.Verify(sb.ToString());

        }


      }


    }
#endif
  }


}

