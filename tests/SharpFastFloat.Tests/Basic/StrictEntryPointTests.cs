using SharpFastFloat;
using SharpFastFloat.Structures;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace SharpFastFloat.Tests.Basic
{
  public class StrictEntryPointTests
  {
    [Theory]
    [InlineData("16777216", 0x4B800000)]
    [InlineData("16777217", 0x4B800000)]
    [InlineData("16777219", 0x4B800002)]
    [InlineData("167772170000000000000000e-16", 0x4B800000)]
    [InlineData("167772170000000000000001e-16", 0x4B800001)]
    [InlineData("1.000000059604644775390625", 0x3F800000)]
    [InlineData("1.000000059604644775390625000000000000000000000001", 0x3F800001)]
    [InlineData("-1.000000059604644775390625000000000000000000000001", unchecked((int)0xBF800001))]
    [InlineData("1.000000178813934326171875", 0x3F800002)]
    [InlineData("1.1754943508222875e-38", 0x00800000)]
    [InlineData("1.1754942106924411e-38", 0x007FFFFF)]
    [InlineData("1.401298464324817e-45", 1)]
    [InlineData("7.00649232162408535461864791644958065640130970938257885878534141944895541342930300743319094181060791015625e-46", 0)]
    [InlineData("7.006492321624085354618647916449580656401309709382578858785341419448955413429303007433190941810607910156251e-46", 1)]
    [InlineData("3.4028234663852886e38", 0x7F7FFFFF)]
    [InlineData("3.5e38", 0x7F800000)]
    [InlineData("-3.5e38", unchecked((int)0xFF800000))]
    [InlineData("1e9999999999999999999999999", 0x7F800000)]
    [InlineData("0e9999999999999999999999999", 0)]
    [InlineData("1e-9999999999999999999999999", 0)]
    [InlineData("-0", int.MinValue)]
    [InlineData("-00000000000000000000000000000", int.MinValue)]
    [InlineData("-0.00000000000000000000000000000", int.MinValue)]
    [InlineData("-0e9999999999999999999999999", int.MinValue)]
    [InlineData("-1e-9999999999999999999999999", int.MinValue)]
    public void FloatRoundsDirectlyToBinary32(string text, int expectedBits)
    {
      AssertFloats(text, Utils.Int32BitsToSingle(expectedBits));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("+", false)]
    [InlineData("-.", false)]
    [InlineData("Infinity", false)]
    [InlineData("NaN", false)]
    [InlineData("0x10", false)]
    [InlineData("0b10", false)]
    [InlineData("0o10", false)]
    [InlineData("1,234", false)]
    [InlineData("1_234", false)]
    [InlineData(" 1", false)]
    [InlineData("1 ", false)]
    [InlineData("1\0", false)]
    [InlineData("\u00A01", false)]
    [InlineData("１", false)]
    [InlineData("١", false)]
    [InlineData(".5", true)]
    [InlineData("1.", true)]
    [InlineData("1.e2", true)]
    [InlineData("+123", true)]
    public void AllEntryPointsUseTheSameGrammar(string text, bool expected)
    {
      AssertGrammar(text, expected);
    }

    [Fact]
    public void ShortTokensMatchTheDecimalGrammar()
    {
      const string Alphabet = "019+-.eE ";
      Regex grammar = new Regex(@"\A[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?\z", RegexOptions.CultureInvariant);
      for (int length = 0, combinations = 1; length <= 4; ++length, combinations *= Alphabet.Length)
      {
        for (int sample = 0; sample < combinations; ++sample)
        {
          char[] chars = new char[length];
          int code = sample;
          for (int i = 0; i < length; ++i) { chars[i] = Alphabet[code % Alphabet.Length]; code /= Alphabet.Length; }
          string text = new string(chars);
          AssertGrammar(text, grammar.IsMatch(text));
        }
      }
    }

    [Theory]
    [InlineData("123456789012345678900000000", 1234567890123456789UL, 8L, false)]
    [InlineData("123456789012345678900000001", 1234567890123456789UL, 8L, true)]
    [InlineData("123456789012345678900000000100000000", 1234567890123456789UL, 17L, true)]
    [InlineData("123456789012345678910000000000000000", 1234567890123456789UL, 17L, true)]
    [InlineData("1.234567890123456789000000000", 1234567890123456789UL, -18L, false)]
    [InlineData("1.234567890123456789000000001", 1234567890123456789UL, -18L, true)]
    [InlineData("1234567890123456789.000000000", 1234567890123456789UL, 0L, false)]
    [InlineData("1234567890123456789.000000001", 1234567890123456789UL, 0L, true)]
    [InlineData("000000000000000000000000001234", 1234UL, 0L, false)]
    public unsafe void ScannerDistinguishesExactAndUncertainCoefficients(string text, ulong mantissa, long exponent, bool uncertain)
    {
      byte[] bytes = Encoding.UTF8.GetBytes(text);
      fixed (char* chars = text)
      fixed (byte* utf8 = bytes)
      {
        ParsedNumberString charNumber = StrictNumberScanner.Parse(chars, chars + text.Length);
        ParsedNumberString byteNumber = StrictNumberScanner.Parse(utf8, utf8 + bytes.Length);
        Assert.True(charNumber.Valid);
        Assert.True(byteNumber.Valid);
        Assert.Equal(mantissa, charNumber.Mantissa);
        Assert.Equal(mantissa, byteNumber.Mantissa);
        Assert.Equal(exponent, charNumber.Exponent);
        Assert.Equal(exponent, byteNumber.Exponent);
        Assert.Equal(uncertain, charNumber.TooManyDigits);
        Assert.Equal(uncertain, byteNumber.TooManyDigits);
      }
    }

    [Fact]
    public void NonAsciiBytesAreRejectedBeforeAndAfterTruncation()
    {
      foreach (int length in new[] { 1, 8, 20, 28 })
      {
        for (int lane = 0; lane < length; ++lane)
        {
          for (int code = 128; code <= byte.MaxValue; ++code)
          {
            byte[] bytes = Encoding.ASCII.GetBytes(new string('0', length));
            bytes[0] = (byte)'1';
            bytes[lane] = (byte)code;
            Assert.False(FastDoubleParser.TryParseDoubleStrict(bytes.AsSpan(), out double d));
            Assert.False(FastFloatParser.TryParseFloatStrict(bytes.AsSpan(), out float f));
            Assert.Equal(0L, BitConverter.DoubleToInt64Bits(d));
            Assert.Equal(0, FloatBits(f));
          }
        }
      }
    }

    [Fact]
    public void EmptyAndSlicedSpansAreHandledWithoutReadingOutsideThem()
    {
      Assert.False(FastDoubleParser.TryParseDoubleStrict(default(ReadOnlySpan<byte>), out _));
      Assert.False(FastFloatParser.TryParseFloatStrict(default(ReadOnlySpan<char>), out _));
      Assert.False(FastFloatParser.TryParseFloatStrict(default(ReadOnlySpan<byte>), out _));
      string text = "x12345678y";
      byte[] bytes = Encoding.ASCII.GetBytes(text);
      Assert.True(FastDoubleParser.TryParseDoubleStrict(bytes.AsSpan(1, 8), out double d));
      Assert.True(FastFloatParser.TryParseFloatStrict(text.AsSpan(1, 8), out float f16));
      Assert.True(FastFloatParser.TryParseFloatStrict(bytes.AsSpan(1, 8), out float f8));
      Assert.Equal(12345678.0, d);
      Assert.Equal(12345678.0f, f16);
      Assert.Equal(f16, f8);
    }

    [Fact]
    public void ExactFallbackHandlesLargeExponentCancellationInBothEncodings()
    {
      string zeros = new string('0', 700000);
      const string FloatMidpoint = "1000000059604644775390625";
      const string DoubleMidpoint = "100000000000000011102230246251565404236316680908203125";
      AssertValues("0." + zeros + FloatMidpoint + "e700001", 1.000000059604644775390625, 1.0f);
      AssertValues(FloatMidpoint + zeros + "e-700024", 1.000000059604644775390625, 1.0f);
      AssertValues("0." + zeros + DoubleMidpoint + "e700001", 1.0, 1.0f);
      AssertValues("0." + zeros + DoubleMidpoint + "1e700001", BitConverter.Int64BitsToDouble(0x3FF0000000000001), 1.0f);
    }

#if NETFRAMEWORK
    [Fact(Skip = "Requires the correctly rounded modern .NET reference parsers.")]
#else
    [Fact]
#endif
    public void GeneratedDecimalsMatchRuntimeInAllFourEntryPoints()
    {
      Random random = new Random(20261);
      for (int sample = 0; sample < 5000; ++sample)
      {
        int length = random.Next(1, 180);
        StringBuilder input = new StringBuilder();
        if ((sample & 1) == 0) { input.Append('-'); }
        int point = random.Next(length + 1);
        for (int i = 0; i < length; ++i)
        {
          if (i == point) { input.Append('.'); }
          input.Append((char)('0' + random.Next(10)));
        }
        if (point == length) { input.Append('.'); }
        // Exercise exact zero tails as well as sticky nonzero tails.
        if ((sample & 2) == 0) { input.Append('0', random.Next(0, 80)); }
        input.Append('e').Append(random.Next(-400, 151).ToString(CultureInfo.InvariantCulture));
        string text = input.ToString();
        AssertValues(text, double.Parse(text, CultureInfo.InvariantCulture), float.Parse(text, CultureInfo.InvariantCulture));
      }
    }

    private static int FloatBits(float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);

    private static void AssertGrammar(string text, bool expected)
    {
      byte[] bytes = Encoding.UTF8.GetBytes(text);
      Assert.Equal(expected, FastDoubleParser.TryParseDoubleStrict(text.AsSpan(), out double d16));
      Assert.Equal(expected, FastDoubleParser.TryParseDoubleStrict(bytes.AsSpan(), out double d8));
      Assert.Equal(expected, FastFloatParser.TryParseFloatStrict(text.AsSpan(), out float f16));
      Assert.Equal(expected, FastFloatParser.TryParseFloatStrict(bytes.AsSpan(), out float f8));
      if (!expected)
      {
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(d16));
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(d8));
        Assert.Equal(0, FloatBits(f16));
        Assert.Equal(0, FloatBits(f8));
      }
    }

    private static void AssertValues(string text, double expectedDouble, float expectedFloat)
    {
      byte[] bytes = Encoding.UTF8.GetBytes(text);
      Assert.True(FastDoubleParser.TryParseDoubleStrict(text.AsSpan(), out double d16));
      Assert.True(FastDoubleParser.TryParseDoubleStrict(bytes.AsSpan(), out double d8));
      Assert.Equal(BitConverter.DoubleToInt64Bits(expectedDouble), BitConverter.DoubleToInt64Bits(d16));
      Assert.Equal(BitConverter.DoubleToInt64Bits(expectedDouble), BitConverter.DoubleToInt64Bits(d8));
      AssertFloats(text, expectedFloat);
    }

    private static void AssertFloats(string text, float expected)
    {
      byte[] bytes = Encoding.UTF8.GetBytes(text);
      Assert.True(FastFloatParser.TryParseFloatStrict(text.AsSpan(), out float f16));
      Assert.True(FastFloatParser.TryParseFloatStrict(bytes.AsSpan(), out float f8));
      Assert.Equal(FloatBits(expected), FloatBits(f16));
      Assert.Equal(FloatBits(expected), FloatBits(f8));
    }
  }
}
