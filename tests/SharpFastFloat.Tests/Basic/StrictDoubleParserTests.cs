using SharpFastFloat;
using System;
using System.Globalization;
using Xunit;

namespace SharpFastFloat.Tests.Basic
{
  public class StrictDoubleParserTests
  {
    [Theory]
    [InlineData("")]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData(".")]
    [InlineData("+.")]
    [InlineData("1e")]
    [InlineData("1e+")]
    [InlineData("1e-")]
    [InlineData(".e1")]
    [InlineData("1e1.0")]
    [InlineData("1.2.3")]
    [InlineData("1x")]
    [InlineData("123456789012345678901234567890x")]
    [InlineData(" 1")]
    [InlineData("1 ")]
    [InlineData("\t1")]
    [InlineData("\u00a01")]
    [InlineData("1\u2029")]
    [InlineData("1\0")]
    [InlineData("1,234")]
    [InlineData("1_000")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("0x10")]
    [InlineData("0b10")]
    [InlineData("0o10")]
    [InlineData("１２３")]
    [InlineData("١٢٣")]
    public void RejectsNonDecimalSyntax(string text)
    {
      Assert.False(FastDoubleParser.TryParseDoubleStrict(text.AsSpan(), out double value));
      Assert.Equal(0L, BitConverter.DoubleToInt64Bits(value));
    }

    [Theory]
    [InlineData("0", 0.0)]
    [InlineData("1", 1.0)]
    [InlineData("123", 123.0)]
    [InlineData("+123", 123.0)]
    [InlineData("00000123", 123.0)]
    [InlineData(".5", 0.5)]
    [InlineData("-.5", -0.5)]
    [InlineData("1.", 1.0)]
    [InlineData("1.e2", 100.0)]
    [InlineData("12.5E-1", 1.25)]
    [InlineData("9007199254740991", 9007199254740991.0)]
    [InlineData("9007199254740992", 9007199254740992.0)]
    [InlineData("9007199254740993", 9007199254740992.0)]
    [InlineData("9007199254740995", 9007199254740996.0)]
    [InlineData("18446744073709551616", 18446744073709551616.0)]
    [InlineData("1.7976931348623159e308", double.PositiveInfinity)]
    [InlineData("-1.7976931348623159e308", double.NegativeInfinity)]
    [InlineData("2.4703282292062328e-324", double.Epsilon)]
    [InlineData("1e999999999999999999999", double.PositiveInfinity)]
    [InlineData("-1e999999999999999999999", double.NegativeInfinity)]
    [InlineData("1e-999999999999999999999", 0.0)]
    [InlineData("0e999999999999999999999", 0.0)]
    public void ParsesDecimalSyntax(string text, double expected)
    {
      Assert.True(FastDoubleParser.TryParseDoubleStrict(text.AsSpan(), out double value));
      Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(value));
    }

    [Theory]
    [InlineData("-0")]
    [InlineData("-00000000000000000000000000000")]
    [InlineData("-0.0")]
    [InlineData("-0e9999999999999999999")]
    [InlineData("-1e-9999999999999999999")]
    public void PreservesNegativeZero(string text)
    {
      Assert.True(FastDoubleParser.TryParseDoubleStrict(text.AsSpan(), out double value));
      Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(value));
    }

#if NETFRAMEWORK
    [Theory(Skip = "Requires the correctly rounded modern .NET reference parser.")]
#else
    [Theory]
#endif
    [InlineData("2.2250738585072013e-308")]
    [InlineData("2.2250738585072014e-308")]
    [InlineData("4.9406564584124654e-324")]
    [InlineData("2.4703282292062327e-324")]
    [InlineData("1.7976931348623157e308")]
    [InlineData("1.00000000000000011102230246251565404236316680908203125")]
    [InlineData("1.00000000000000011102230246251565404236316680908203125001")]
    [InlineData("1.00000000000000033306690738754696212708950042724609375")]
    [InlineData("0.000000000000000000000000000000000012345678901234567890123456789")]
    public void RoundsLikeTheRuntime(string text)
    {
      AssertMatchesRuntime(text);
    }

    [Fact]
    public void HandlesLongDigitRunsAndExponentCancellation()
    {
      string zeros = new string('0', 70000);
      AssertParses("1" + zeros + "e-70000", 1.0);
      AssertParses("0." + zeros + "1e70001", 1.0);
      // Force exact fallback with an exponent beyond the old fallback cap.
      AssertParses("100000000000000011102230246251565404236316680908203125" + zeros + "e-70053", 1.0);
      AssertParses("0." + zeros + "100000000000000011102230246251565404236316680908203125e70001", 1.0);
    }

#if NETFRAMEWORK
    [Fact(Skip = "Requires the correctly rounded modern .NET reference parser.")]
#else
    [Fact]
#endif
    public void MatchesRuntimeForGeneratedDecimals()
    {
      Random random = new Random(1927);
      for (int sample = 0; sample < 5000; ++sample)
      {
        int length = random.Next(1, 150);
        char[] digits = new char[length];
        for (int i = 0; i < length; ++i) { digits[i] = (char)('0' + random.Next(10)); }
        string text = new string(digits);
        if ((sample & 1) == 0) { text = text.Insert(random.Next(length + 1), "."); }
        if ((sample & 2) == 0) { text = "-" + text; }
        text += "e" + random.Next(-250, 150).ToString(CultureInfo.InvariantCulture);
        AssertMatchesRuntime(text);
      }
    }

    [Fact]
    public void ReadsOnlyTheSuppliedSpan()
    {
      Assert.True(FastDoubleParser.TryParseDoubleStrict("x123456789y".AsSpan(1, 9), out double value));
      Assert.Equal(123456789.0, value);
      Assert.False(FastDoubleParser.TryParseDoubleStrict(default(ReadOnlySpan<char>), out _));
    }

    [Fact]
    public unsafe void ChunkParsersRejectNonAsciiInEveryLane()
    {
      for (int lane = 0; lane < 8; ++lane)
      {
        for (int code = 0; code <= char.MaxValue; ++code)
        {
          char[] text = "12345678".ToCharArray();
          text[lane] = (char)code;
          bool expected = code >= '0' && code <= '9';
          fixed (char* p = text)
          {
            Assert.Equal(expected, Utils.TryParseEightConsecutiveDigits(p, out uint value));
            if (lane < 4) { Assert.Equal(expected, Utils.TryParseFourConsecutiveDigits(p, out _)); }
            if (expected) { Assert.Equal(uint.Parse(new string(text), CultureInfo.InvariantCulture), value); }
          }
        }
      }
    }

    private static void AssertMatchesRuntime(string text)
    {
      double expected = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
      AssertParses(text, expected);
    }

    private static void AssertParses(string text, double expected)
    {
      Assert.True(FastDoubleParser.TryParseDoubleStrict(text.AsSpan(), out double actual), text);
      Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(actual));
    }
  }
}
