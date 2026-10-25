using SharpFastFloat;
using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using Xunit;

namespace SharpFastFloat.Tests.Basic
{
  public class ExactMidpointTests
  {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedMidpointsRoundExactly(bool single)
    {
      int precision = single ? 23 : 52;
      int bias = single ? 127 : 1023;
      ulong hiddenBit = 1UL << precision;
      ulong infinity = single ? 0x7F800000UL : 0x7FF0000000000000UL;
      ulong[] boundaries = { 0, 1, 2, hiddenBit - 2, hiddenBit - 1, hiddenBit,
        hiddenBit + 1, (ulong)bias << precision, ((ulong)bias << precision) + 1, infinity - 2, infinity - 1 };
      foreach (ulong lower in boundaries)
      {
        CheckMidpoint(lower, precision, bias, 2);
        // Exercise the bounded coefficient and sticky suffix, not only exact ties.
        CheckMidpoint(lower, precision, bias, single ? 180 : 900);
      }
      Random random = new Random(14873);
      byte[] bytes = new byte[8];
      for (int i = 0; i < 160; ++i)
      {
        random.NextBytes(bytes);
        ulong lower = (ulong)BitConverter.ToInt64(bytes, 0) % infinity;
        CheckMidpoint(lower, precision, bias, 2);
      }
    }

    private static void CheckMidpoint(ulong lower, int precision, int bias, int extraDigits)
    {
      ulong fraction = lower & ((1UL << precision) - 1);
      int power = (int)(lower >> precision);
      ulong mantissa = power == 0 ? fraction : fraction | (1UL << precision);
      int exponent = Math.Max(power, 1) - bias - precision - 1;
      BigInteger midpoint = mantissa * 2 + 1;
      int scale = 0;
      if (exponent < 0)
      {
        midpoint *= BigInteger.Pow(5, -exponent);
        scale = -exponent;
      }
      else { midpoint <<= exponent; }
      midpoint *= BigInteger.Pow(10, extraDigits);
      scale += extraDigits;
      for (int offset = -1; offset <= 1; ++offset)
      {
        string text = (midpoint + offset).ToString(CultureInfo.InvariantCulture)
          + "e-" + scale.ToString(CultureInfo.InvariantCulture);
        ulong expected = offset < 0 ? lower : offset > 0 ? lower + 1 : lower + (lower & 1);
        AssertAll(text, expected, precision == 23);
        ulong sign = precision == 23 ? 0x80000000UL : 0x8000000000000000UL;
        AssertAll("-" + text, expected | sign, precision == 23);
      }
    }

    [Theory]
    [InlineData("1.00000000000000011102230246251565404236316680908203125", 0x3FF0000000000000L)]
    [InlineData("1.0000000000000001110223024625156540423631668090820312500001", 0x3FF0000000000001L)]
    [InlineData("1.0000000000000001110223024625156540423631668090820312499999", 0x3FF0000000000000L)]
    public void PrefixGroupingAndCustomSeparatorsUseExactRounding(string text, long expected)
    {
      string custom = text.Replace('.', ',');
      Assert.True(FastDoubleParser.TryParseDouble(custom.AsSpan(), out int consumed, out double value, decimalSeparator: ','));
      Assert.Equal(custom.Length, consumed);
      Assert.Equal(expected, BitConverter.DoubleToInt64Bits(value));
      string grouped = "0,00" + text + "suffix";
      Assert.True(FastDoubleParser.TryParseDouble(grouped.AsSpan(), out consumed, out value, NumberStyles.Float | NumberStyles.AllowThousands));
      Assert.Equal(grouped.Length - 6, consumed);
      Assert.Equal(expected, BitConverter.DoubleToInt64Bits(value));
      byte[] bytes = Encoding.ASCII.GetBytes(grouped);
      Assert.True(FastDoubleParser.TryParseDouble(bytes.AsSpan(), out consumed, out value, NumberStyles.Float | NumberStyles.AllowThousands));
      Assert.Equal(grouped.Length - 6, consumed);
      Assert.Equal(expected, BitConverter.DoubleToInt64Bits(value));
    }

    private static void AssertAll(string text, ulong expected, bool single)
    {
      byte[] bytes = Encoding.ASCII.GetBytes(text);
      if (single)
      {
        Assert.True(FastFloatParser.TryParseFloatStrict(text.AsSpan(), out float value));
        AssertFloatBits(expected, value);
        Assert.True(FastFloatParser.TryParseFloatStrict(bytes.AsSpan(), out value));
        AssertFloatBits(expected, value);
        Assert.True(FastFloatParser.TryParseFloat(text.AsSpan(), out value));
        AssertFloatBits(expected, value);
        Assert.True(FastFloatParser.TryParseFloat(bytes.AsSpan(), out value));
        AssertFloatBits(expected, value);
      }
      else
      {
        Assert.True(FastDoubleParser.TryParseDoubleStrict(text.AsSpan(), out double value));
        Assert.Equal(expected, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));
        Assert.True(FastDoubleParser.TryParseDoubleStrict(bytes.AsSpan(), out value));
        Assert.Equal(expected, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));
        Assert.True(FastDoubleParser.TryParseDouble(text.AsSpan(), out value));
        Assert.Equal(expected, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));
        Assert.True(FastDoubleParser.TryParseDouble(bytes.AsSpan(), out value));
        Assert.Equal(expected, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));
      }
    }

    private static unsafe void AssertFloatBits(ulong expected, float value)
    {
      Assert.Equal((uint)expected, *(uint*)&value);
    }
  }
}
