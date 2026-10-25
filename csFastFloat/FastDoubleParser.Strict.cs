using csFastFloat.Constants;
using csFastFloat.Structures;
using System;
using System.Runtime.CompilerServices;

namespace csFastFloat
{
  public static unsafe partial class FastDoubleParser
  {
    /// <summary>
    /// Parses an entire span using ASCII decimal syntax:
    /// [+-]?([0-9]+(\.[0-9]*)?|\.[0-9]+)([eE][+-]?[0-9]+)?.
    /// Whitespace, grouping, NaN, Infinity and radix prefixes are not accepted.
    /// Overflow and underflow produce signed infinity and zero, respectively.
    /// </summary>
    /// <param name="s">The complete decimal input, already trimmed by the caller.</param>
    /// <param name="result">The correctly rounded value, or zero on failure.</param>
    /// <returns>Whether the entire input matches the decimal syntax.</returns>
    public static bool TryParseDoubleStrict(ReadOnlySpan<char> s, out double result)
    {
      result = 0;
      if (s.IsEmpty) { return false; }

      fixed (char* first = s)
      {
        char* p = first;
        char* last = first + s.Length;
        bool negative = *p == '-';
        if (*p == '-' || *p == '+') { ++p; }

        ulong mantissa = 0;
        int significantDigits = 0;
        char* integerStart = p;
        ScanStrictDigits(ref p, last, ref mantissa, ref significantDigits);
        bool hasDigits = p != integerStart;

        // Every integer through 2^53 is exactly representable. Negation also
        // preserves the sign of zero. Larger integers use the rounding engine.
        if (p == last && hasDigits && significantDigits <= 19 &&
            mantissa <= DoubleBinaryConstants.max_mantissa_fast_path)
        {
          result = negative ? -(double)mantissa : (double)mantissa;
          return true;
        }

        long fractionalDigits = 0;
        if (p != last && *p == '.')
        {
          char* fractionStart = ++p;
          ScanStrictDigits(ref p, last, ref mantissa, ref significantDigits);
          fractionalDigits = p - fractionStart;
          hasDigits |= fractionalDigits != 0;
        }
        if (!hasDigits) { return false; }

        long exponent = Math.Max(0, significantDigits - 19) - fractionalDigits;
        if (p != last && (*p == 'e' || *p == 'E'))
        {
          ++p;
          bool negativeExponent = p != last && *p == '-';
          if (p != last && (*p == '-' || *p == '+')) { ++p; }
          char* exponentStart = p;
          long explicitExponent = 0;
          // The bound exceeds every possible offset from the mantissa, even
          // for very long spans whose exponent cancels their digit count.
          long exponentLimit = (long)s.Length + 400;
          while (p != last && Utils.is_integer(*p, out uint digit))
          {
            if (explicitExponent < exponentLimit)
            {
              explicitExponent = explicitExponent * 10 + digit;
            }
            ++p;
          }
          if (p == exponentStart) { return false; }
          exponent += negativeExponent ? -explicitExponent : explicitExponent;
        }
        if (p != last) { return false; }

        ParsedNumberString number = new ParsedNumberString
        {
          mantissa = mantissa,
          exponent = exponent,
          negative = negative,
          too_many_digits = significantDigits > 19
        };
        result = ConvertNumber(first, last, number, '.');
        return true;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ScanStrictDigits(ref char* p, char* last, ref ulong mantissa, ref int significantDigits)
    {
      while (p != last)
      {
        // Avoid overflowing the 19-digit accumulator. Once full, chunks still
        // validate and count discarded digits without rereading the mantissa.
        if (last - p >= 8 && (significantDigits <= 11 || significantDigits >= 19) &&
            Utils.TryParseEightConsecutiveDigits(p, out uint chunk))
        {
          if (significantDigits <= 11) { mantissa = mantissa * 100000000 + chunk; }
          bool firstChunk = significantDigits == 0;
          significantDigits += 8;
          if (firstChunk && chunk < 10000000)
          {
            // Leading zeros do not consume accumulator capacity. This also
            // lets long zero runs advance eight characters at a time.
            for (uint place = 10000000; place != 0 && chunk < place; place /= 10)
            {
              --significantDigits;
            }
          }
          p += 8;
          continue;
        }
        if (!Utils.is_integer(*p, out uint digit)) { break; }
        if (significantDigits < 19) { mantissa = mantissa * 10 + digit; }
        if (mantissa != 0) { ++significantDigits; }
        ++p;
      }
    }
  }
}
