using SharpFastFloat;
using System;
using System.Globalization;
using System.Text;
using Xunit;

namespace SharpFastFloat.Tests.Basic
{
  public class RegularScannerTests
  {
    [Theory]
    [InlineData("1", 1.0)]
    [InlineData("123", 123.0)]
    [InlineData("1234567", 1234567.0)]
    [InlineData("12345678", 12345678.0)]
    [InlineData("123456789", 123456789.0)]
    [InlineData("1234567890123456", 1234567890123456.0)]
    [InlineData("1234567890123456789", 1234567890123456789.0)]
    [InlineData("18446744073709551616", 18446744073709551616.0)]
    [InlineData("123456789012345678901234567890", 123456789012345678901234567890.0)]
    [InlineData("0000000000000000000000000012345678", 12345678.0)]
    [InlineData("12345678.125", 12345678.125)]
    [InlineData("12345678e-2", 123456.78)]
    [InlineData("12,345,678", 12345678.0)]
    [InlineData("12345678,12345678", 1234567812345678.0)]
    [InlineData("12345678,1,12345678", 12345678112345678.0)]
    [InlineData("1,234,567,890,123,456,789,012,345,678", 1234567890123456789012345678.0)]
    public void PreservesValuesAndConsumedPrefix(string number, double expected)
    {
      AssertParses(number + "tail", number.Length, expected, (float)expected,
        number.IndexOf(',') >= 0 ? NumberStyles.Float | NumberStyles.AllowThousands : NumberStyles.Float);
    }

    [Theory]
    [InlineData("12345678,", 12345678.0, 8)]
    [InlineData("12345678,,9", 12345678.0, 8)]
    [InlineData("12345678,9,", 123456789.0, 10)]
    [InlineData("1234567\uFF198", 1234567.0, 7)]
    [InlineData("1\u0132345678", 1.0, 1)]
    public void StopsAtInvalidGroupingAndNonAsciiDigits(string input, double expected, int consumed)
    {
      AssertParses(input, consumed, expected, (float)expected, NumberStyles.Float | NumberStyles.AllowThousands);
    }

    [Fact]
    public void KeepsGroupingOptInAndSupportsCustomSeparators()
    {
      AssertParses("12345678,12345678", 8, 12345678, 12345678, NumberStyles.Float);
      AssertParses("12345678.12345678,5tail", 19, 1234567812345678.5, (float)1234567812345678.5,
        NumberStyles.Float | NumberStyles.AllowThousands, ',', '.');
      AssertParses("12345678.5tail", 10, 12345678.5, 12345678.5f,
        NumberStyles.Float | NumberStyles.AllowThousands, '.', '.');
    }

    [Fact]
    public void PreservesNegativeZeroAcrossLongIntegerRuns()
    {
      string input = "-" + new string('0', 80);
      AssertParses(input, input.Length, BitConverter.Int64BitsToDouble(long.MinValue),
        Utils.Int32BitsToSingle(int.MinValue), NumberStyles.Float);
    }

    [Fact]
    public void DoesNotReadOutsideTheSpan()
    {
      ReadOnlySpan<char> chars = "x12345678y".AsSpan(1, 8);
      byte[] bytes = Encoding.ASCII.GetBytes("x12345678y");
      Assert.True(FastDoubleParser.TryParseDouble(chars, out int charCount, out double charValue));
      Assert.True(FastDoubleParser.TryParseDouble(bytes.AsSpan(1, 8), out int byteCount, out double byteValue));
      Assert.Equal(8, charCount);
      Assert.Equal(8, byteCount);
      Assert.Equal(12345678.0, charValue);
      Assert.Equal(charValue, byteValue);
    }

    [Fact]
    public void StopsAtNonAsciiBytesInEveryChunkLane()
    {
      for (int lane = 0; lane < 8; ++lane)
      {
        for (int code = 128; code <= byte.MaxValue; ++code)
        {
          byte[] input = Encoding.ASCII.GetBytes("123456789");
          input[lane] = (byte)code;
          Assert.Equal(lane != 0, FastDoubleParser.TryParseDouble(input.AsSpan(), out int doubleCount, out double doubleValue));
          Assert.Equal(lane != 0, FastFloatParser.TryParseFloat(input.AsSpan(), out int floatCount, out float floatValue));
          Assert.Equal(lane, doubleCount);
          Assert.Equal(lane, floatCount);
          double expected = lane == 0 ? 0 : double.Parse("123456789".Substring(0, lane), CultureInfo.InvariantCulture);
          Assert.Equal(expected, doubleValue);
          Assert.Equal((float)expected, floatValue);
        }
      }
    }

#if NETFRAMEWORK
    [Fact(Skip = "Requires the correctly rounded modern .NET reference parsers.")]
#else
    [Fact]
#endif
    public void MatchesRuntimeForGeneratedGroupedAndUngroupedDecimals()
    {
      Random random = new Random(7312);
      for (int sample = 0; sample < 2000; ++sample)
      {
        StringBuilder input = new StringBuilder();
        StringBuilder normalized = new StringBuilder();
        if ((sample & 1) == 0) { input.Append('-'); normalized.Append('-'); }
        bool grouped = (sample & 2) == 0;
        char decimalSeparator = (sample & 4) == 0 ? '.' : ',';
        char thousandsSeparator = decimalSeparator == '.' ? ',' : '_';
        int digits = random.Next(1, grouped ? 16 : 101);
        for (int i = 0; i < digits; ++i)
        {
          char digit = (char)('0' + random.Next(10));
          input.Append(digit);
          normalized.Append(digit);
          if (grouped && i + 1 < digits && random.Next(5) == 0) { input.Append(thousandsSeparator); }
        }
        if ((sample & 8) == 0)
        {
          input.Append(decimalSeparator);
          normalized.Append('.');
          int fractionDigits = grouped ? random.Next(0, 16 - digits) : random.Next(1, 21);
          for (int i = 0; i < fractionDigits; ++i)
          {
            char digit = (char)('0' + random.Next(10));
            input.Append(digit);
            normalized.Append(digit);
          }
        }
        string exponent = "e" + (grouped ? 0 : random.Next(-150, 151)).ToString(CultureInfo.InvariantCulture);
        input.Append(exponent);
        normalized.Append(exponent);
        int consumed = input.Length;
        input.Append("tail");
        double expectedDouble = double.Parse(normalized.ToString(), CultureInfo.InvariantCulture);
        float expectedFloat = float.Parse(normalized.ToString(), CultureInfo.InvariantCulture);
        AssertParses(input.ToString(), consumed, expectedDouble, expectedFloat,
          grouped ? NumberStyles.Float | NumberStyles.AllowThousands : NumberStyles.Float, decimalSeparator, thousandsSeparator);
      }
    }

    private static void AssertParses(string input, int expectedConsumed, double expectedDouble, float expectedFloat,
      NumberStyles styles, char decimalSeparator = '.', char thousandsSeparator = ',')
    {
      byte[] utf8 = Encoding.UTF8.GetBytes(input);
      Assert.True(FastDoubleParser.TryParseDouble(input.AsSpan(), out int doubleChars, out double doubleUtf16,
        styles, decimalSeparator, thousandsSeparator), input);
      Assert.True(FastDoubleParser.TryParseDouble(utf8.AsSpan(), out int doubleBytes, out double doubleUtf8,
        styles, (byte)decimalSeparator, (byte)thousandsSeparator), input);
      Assert.True(FastFloatParser.TryParseFloat(input.AsSpan(), out int floatChars, out float floatUtf16,
        styles, decimalSeparator, thousandsSeparator), input);
      Assert.True(FastFloatParser.TryParseFloat(utf8.AsSpan(), out int floatBytes, out float floatUtf8,
        styles, (byte)decimalSeparator, (byte)thousandsSeparator), input);
      Assert.Equal(expectedConsumed, doubleChars);
      Assert.Equal(expectedConsumed, doubleBytes);
      Assert.Equal(expectedConsumed, floatChars);
      Assert.Equal(expectedConsumed, floatBytes);
      Assert.True(BitConverter.DoubleToInt64Bits(expectedDouble) == BitConverter.DoubleToInt64Bits(doubleUtf16), "UTF-16 double: " + input);
      Assert.True(BitConverter.DoubleToInt64Bits(expectedDouble) == BitConverter.DoubleToInt64Bits(doubleUtf8), "UTF-8 double: " + input);
      Assert.True(BitConverter.ToInt32(BitConverter.GetBytes(expectedFloat), 0) == BitConverter.ToInt32(BitConverter.GetBytes(floatUtf16), 0), "UTF-16 float: " + input);
      Assert.True(BitConverter.ToInt32(BitConverter.GetBytes(expectedFloat), 0) == BitConverter.ToInt32(BitConverter.GetBytes(floatUtf8), 0), "UTF-8 float: " + input);
    }
  }
}
