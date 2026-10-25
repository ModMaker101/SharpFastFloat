using SharpFastFloat.Constants;
using SharpFastFloat.Structures;
using System;
using System.Runtime.CompilerServices;

namespace SharpFastFloat
{
  public static unsafe partial class FastDoubleParser
  {
    /// <summary>
    /// Parses an entire UTF-16 span using fixed decimal syntax and ASCII digits.
    /// Accepts an optional sign, decimal point and exponent. Whitespace, grouping,
    /// NaN, Infinity and radix prefixes are rejected. Overflow produces signed
    /// infinity; underflow and zero preserve the sign.
    /// </summary>
    /// <param name="s">The complete decimal input, already trimmed by the caller.</param>
    /// <param name="result">The correctly rounded value, or zero on failure.</param>
    /// <returns>Whether the entire input matches the decimal syntax.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseDoubleStrict(ReadOnlySpan<char> s, out double result)
    {
      result = 0;
      // A one-character decimal token can only be an ASCII digit.
      if (s.Length == 1)
      {
        if (!Utils.IsInteger(s[0], out uint digit)) { return false; }
        result = digit;
        return true;
      }
      fixed (char* first = s)
      {
        char* last = first + s.Length;
        ParsedNumberString number = StrictNumberScanner.Parse(first, last);
        if (!number.Valid) { return false; }
        if (number.Exponent == 0 && !number.TooManyDigits && number.Mantissa <= DoubleBinaryConstants.MaxMantissaFastPath)
        {
          result = number.Negative ? -(double)number.Mantissa : (double)number.Mantissa;
          return true;
        }
        result = ConvertNumber(first, last, number, '.');
        return true;
      }
    }

    /// <summary>
    /// Parses an entire UTF-8/ASCII span using fixed decimal syntax and ASCII digits.
    /// Accepts an optional sign, decimal point and exponent. Whitespace, grouping,
    /// NaN, Infinity and radix prefixes are rejected. Overflow produces signed
    /// infinity; underflow and zero preserve the sign.
    /// </summary>
    /// <param name="s">The complete decimal input, already trimmed by the caller.</param>
    /// <param name="result">The correctly rounded value, or zero on failure.</param>
    /// <returns>Whether the entire input matches the decimal syntax.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseDoubleStrict(ReadOnlySpan<byte> s, out double result)
    {
      result = 0;
      // A one-character decimal token can only be an ASCII digit.
      if (s.Length == 1)
      {
        if (!Utils.IsInteger(s[0], out uint digit)) { return false; }
        result = digit;
        return true;
      }
      fixed (byte* first = s)
      {
        byte* last = first + s.Length;
        ParsedNumberString number = StrictNumberScanner.Parse(first, last);
        if (!number.Valid) { return false; }
        if (number.Exponent == 0 && !number.TooManyDigits && number.Mantissa <= DoubleBinaryConstants.MaxMantissaFastPath)
        {
          result = number.Negative ? -(double)number.Mantissa : (double)number.Mantissa;
          return true;
        }
        result = ConvertNumber(first, last, number, (byte)'.');
        return true;
      }
    }
  }
}
