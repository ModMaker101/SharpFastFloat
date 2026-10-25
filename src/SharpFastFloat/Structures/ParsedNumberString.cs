using System.Globalization;
using System.Runtime.CompilerServices;
namespace SharpFastFloat.Structures
{
  internal unsafe struct ParsedNumberString
  {
    internal long Exponent;
    internal ulong Mantissa;

    internal int CharactersConsumed;
    internal bool Negative;
    internal bool Valid;
    // Whether conversion needs a mantissa interval for discarded digits.
    // Regular scans are conservative; strict scans set this only for nonzero tails.
    internal bool TooManyDigits;

    // UTF-16 inputs involving SIMD within  eval_parse_eight_digits_simd when HAS_INTRINSICS

    internal static ParsedNumberString ParseNumberString(char* p, char* pend, NumberStyles expectedFormat = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
    {
      ParsedNumberString answer = new ParsedNumberString();

      answer.Valid = false;
      answer.TooManyDigits = false;
      char* pstart = p;
      answer.Negative = (*p == '-');
      if ((*p == '-') || (*p == '+'))
      {
        ++p;
        if (p == pend)
        {
          return answer;
        }
        if (!Utils.IsInteger(*p, out uint _) && (*p != decimalSeparator)) // culture info ?
        { // a  sign must be followed by an integer or the dot
          return answer;
        }
      }
      // Thousands separators (group separators) are accepted only when the
      // AllowThousands style is set and the separator differs from the
      // decimal separator. They are permitted between digits in the integer
      // part only.
      bool allowThousands = expectedFormat.IsSet(NumberStyles.AllowThousands)
                             && thousandsSeparator != decimalSeparator;
      char* startDigits = p;

      ulong i = 0; // an unsigned int avoids signed overflows (which are bad)
      long integerDigitCount = 0;

      // Keep short and grouped inputs on the existing scalar path. In the
      // ungrouped path, pointer subtraction counts digits without a per-digit
      // counter or group-separator checks.
      if (!allowThousands && pend - p >= 8)
      {
        while (pend - p >= 8 && Utils.TryParseEightConsecutiveDigits(p, out uint chunk))
        {
          i = i * 100000000 + chunk;
          p += 8;
        }
        while (p != pend && Utils.IsInteger(*p, out uint digit))
        {
          i = i * 10 + digit;
          ++p;
        }
        integerDigitCount = p - startDigits;
      }
      else
      {
        while (p != pend)
        {
          if (Utils.IsInteger(*p, out uint cMinus0))
          {
            // a multiplication by 10 is cheaper than an arbitrary integer
            // multiplication
            i = 10 * i + (ulong)cMinus0; // might overflow, we will handle the overflow later
            ++p;
            ++integerDigitCount;
          }
          else if (allowThousands && *p == thousandsSeparator
                   && integerDigitCount > 0
                   && (p + 1) != pend && Utils.IsInteger(*(p + 1), out uint _))
          {
            ++p; // skip a thousands separator that lies strictly between digits
          }
          else
          {
            break;
          }
        }
      }
      char* endOfIntegerPart = p;
      long digitCount = integerDigitCount;
      long exponent = 0;

      if ((p != pend) && (*p == decimalSeparator))
      {
        ++p;


#if HAS_INTRINSICS
        while ((p <= pend - 8) && Utils.TryParseEightConsecutiveDigitsSIMD(p, out uint tmp))
        {
          i = i * 100000000 + tmp;
          p += 8;
        }
#endif


        while ((p != pend) && Utils.IsInteger(*p, out uint cMinus0))
        {
          byte digit = (byte)cMinus0;
          ++p;
          i = i * 10 + digit; // in rare cases, this will overflow, but that's ok
        }
        exponent = endOfIntegerPart + 1 - p;
        digitCount -= exponent;
      }

      // we must have encountered at least one integer!
      if (digitCount == 0)
      {
        return answer;
      }
      long expNumber = 0;            // explicit exponential part
      if (expectedFormat.IsSet(NumberStyles.AllowExponent) && (p != pend) && (('e' == *p) || ('E' == *p)))
      {
        char* locationOfE = p;
        ++p;
        bool negExp = false;
        if ((p != pend) && ('-' == *p))
        {
          negExp = true;
          ++p;
        }
        else if ((p != pend) && ('+' == *p))
        {
          ++p;
        }
        if ((p == pend) || !Utils.IsInteger(*p, out uint _))
        {
          if (expectedFormat != NumberStyles.AllowDecimalPoint) // ce n'est pas ça !
          {
            // We are in error.
            return answer;
          }
          // Otherwise, we will be ignoring the 'e'.
          p = locationOfE;
        }
        else
        {
          while ((p != pend) && Utils.IsInteger(*p, out uint cMinus0))
          {
            byte digit = (byte)cMinus0;
            if (expNumber < 0x10000)
            {
              expNumber = 10 * expNumber + digit;
            }
            ++p;
          }
          if (negExp) { expNumber = -expNumber; }
          exponent += expNumber;
        }
      }
      else
      {
        // If it scientific and not fixed, we have to bail out.
        if ((expectedFormat.IsSet(NumberStyles.AllowExponent)) && !(expectedFormat.IsSet(NumberStyles.AllowDecimalPoint))) { return answer; }
      }
      answer.Valid = true;
      answer.CharactersConsumed = (int)(p - pstart);

      // If we frequently had to deal with long strings of digits,
      // we could extend our code by using a 128-bit integer instead
      // of a 64-bit integer. However, this is uncommon.
      //
      // We can deal with up to 19 digits.
      if (digitCount > 19)
      { // this is uncommon
        // It is possible that the integer had an overflow.
        // We have to handle the case where we have 0.0000somenumber.
        // We need to be mindful of the case where we only have zeroes...
        // E.g., 0.000000000...000.
        char* start = startDigits;
        while ((start != pend) && (*start == '0' || *start == decimalSeparator || (allowThousands && *start == thousandsSeparator)))
        {
          if (*start == '0') { digitCount--; }
          start++;
        }
        if (digitCount > 19)
        {
          answer.TooManyDigits = true;
          // Let us start again, this time, avoiding overflows.
          i = 0;
          p = startDigits;
          long reReadIntegerDigits = 0;
          const ulong MinimalNineteenDigitInteger = 1000000000000000000;
          while ((i < MinimalNineteenDigitInteger) && (p != pend))
          {
            if (Utils.IsInteger(*p, out uint cMinus0))
            {
              i = i * 10 + (ulong)cMinus0;
              ++p;
              ++reReadIntegerDigits;
            }
            else if (allowThousands && *p == thousandsSeparator)
            {
              ++p;
            }
            else { break; }
          }
          if (i >= MinimalNineteenDigitInteger)
          { // We have a big integers
            exponent = (integerDigitCount - reReadIntegerDigits) + expNumber;
          }
          else
          { // We have a value with a fractional component.
            p++; // skip the '.'
            char* firstAfterPeriod = p;
            while ((i < MinimalNineteenDigitInteger) && (p != pend) && Utils.IsInteger(*p, out uint cMinus0))
            {
              i = i * 10 + (ulong)cMinus0;
              ++p;
            }
            exponent = firstAfterPeriod - p + expNumber;
          }
          // We have now corrected both exponent and i, to a truncated value
        }
      }
      answer.Exponent = exponent;
      answer.Mantissa = i;
      return answer;
    }

    // UTF-8 / ASCII inputs.
    internal static ParsedNumberString ParseNumberString(byte* p, byte* pend, NumberStyles expectedFormat = NumberStyles.Float, byte decimalSeparator = (byte)'.', byte thousandsSeparator = (byte)',')
    {
      ParsedNumberString answer = new ParsedNumberString();

      answer.Valid = false;
      answer.TooManyDigits = false;
      byte* pstart = p;
      answer.Negative = (*p == '-');
      if ((*p == '-') || (*p == '+'))
      {
        ++p;
        if (p == pend)
        {
          return answer;
        }
        if (!Utils.IsInteger(*p, out uint _) && (*p != decimalSeparator))
        { // a  sign must be followed by an integer or the dot
          return answer;
        }
      }
      // Thousands separators (group separators) are accepted only when the
      // AllowThousands style is set and the separator differs from the
      // decimal separator. They are permitted between digits in the integer
      // part only.
      bool allowThousands = expectedFormat.IsSet(NumberStyles.AllowThousands)
                             && thousandsSeparator != decimalSeparator;
      byte* startDigits = p;

      ulong i = 0; // an unsigned int avoids signed overflows (which are bad)
      long integerDigitCount = 0;

      // Keep short and grouped inputs on the existing scalar path. In the
      // ungrouped path, pointer subtraction counts digits without a per-digit
      // counter or group-separator checks.
      if (!allowThousands && pend - p >= 8)
      {
        while (pend - p >= 8 && Utils.IsMadeOfEightDigitsFast(p))
        {
          i = i * 100000000 + Utils.ParseEightDigitsUnrolled(p);
          p += 8;
        }
        while (p != pend && Utils.IsInteger(*p, out uint digit))
        {
          i = i * 10 + digit;
          ++p;
        }
        integerDigitCount = p - startDigits;
      }
      else
      {
        while (p != pend)
        {
          if (Utils.IsInteger(*p, out uint digit))
          {
            // a multiplication by 10 is cheaper than an arbitrary integer
            // multiplication
            i = 10 * i + digit; // might overflow, we will handle the overflow later
            ++p;
            ++integerDigitCount;
          }
          else if (allowThousands && *p == thousandsSeparator
                   && integerDigitCount > 0
                   && (p + 1) != pend && Utils.IsInteger(*(p + 1), out uint _))
          {
            ++p; // skip a thousands separator that lies strictly between digits
          }
          else
          {
            break;
          }
        }
      }
      byte* endOfIntegerPart = p;
      long digitCount = integerDigitCount;
      long exponent = 0;
      if ((p != pend) && (*p == decimalSeparator))
      {
        ++p;
        if ((p + 8 <= pend) && Utils.IsMadeOfEightDigitsFast(p))
        {
          i = i * 100000000 + Utils.ParseEightDigitsUnrolled(p);
          p += 8;
          if ((p + 8 <= pend) && Utils.IsMadeOfEightDigitsFast(p))
          {
            i = i * 100000000 + Utils.ParseEightDigitsUnrolled(p);
            p += 8;
          }
        }
        while ((p != pend) && Utils.IsInteger(*p, out uint digit))
        {
          ++p;
          i = i * 10 + digit; // in rare cases, this will overflow, but that's ok
        }
        exponent = endOfIntegerPart + 1 - p;
        digitCount -= exponent;
      }
      // we must have encountered at least one integer!
      if (digitCount == 0)
      {
        return answer;
      }
      long expNumber = 0;            // explicit exponential part
      if (expectedFormat.IsSet(NumberStyles.AllowExponent) && (p != pend) && (('e' == *p) || ('E' == *p)))
      {
        byte* locationOfE = p;
        ++p;
        bool negExp = false;
        if ((p != pend) && ('-' == *p))
        {
          negExp = true;
          ++p;
        }
        else if ((p != pend) && ('+' == *p))
        {
          ++p;
        }
        if ((p == pend) || !Utils.IsInteger(*p, out uint _))
        {
          if (!expectedFormat.IsSet(NumberStyles.AllowDecimalPoint))
          {
            // We are in error.
            return answer;
          }
          // Otherwise, we will be ignoring the 'e'.
          p = locationOfE;
        }
        else
        {
          while ((p != pend) && Utils.IsInteger(*p, out uint cdigit))
          {
            if (expNumber < 0x10000)
            {
              expNumber = 10 * expNumber + cdigit;
            }
            ++p;
          }
          if (negExp) { expNumber = -expNumber; }
          exponent += expNumber;
        }
      }
      else
      {
        // If it scientific and not fixed, we have to bail out.
        if (expectedFormat.IsSet(NumberStyles.AllowExponent) && !expectedFormat.IsSet(NumberStyles.AllowDecimalPoint)) { return answer; }
      }
      answer.Valid = true;
      answer.CharactersConsumed = (int)(p - pstart);

      // If we frequently had to deal with long strings of digits,
      // we could extend our code by using a 128-bit integer instead
      // of a 64-bit integer. However, this is uncommon.
      //
      // We can deal with up to 19 digits.
      if (digitCount > 19)
      { // this is uncommon
        // It is possible that the integer had an overflow.
        // We have to handle the case where we have 0.0000somenumber.
        // We need to be mindful of the case where we only have zeroes...
        // E.g., 0.000000000...000.
        byte* start = startDigits;
        while ((start != pend) && (*start == '0' || *start == decimalSeparator || (allowThousands && *start == thousandsSeparator)))
        {
          if (*start == '0') { digitCount--; }
          start++;
        }
        if (digitCount > 19)
        {
          answer.TooManyDigits = true;
          // Let us start again, this time, avoiding overflows.
          i = 0;
          p = startDigits;
          long reReadIntegerDigits = 0;
          const ulong MinimalNineteenDigitInteger = 1000000000000000000;
          while ((i < MinimalNineteenDigitInteger) && (p != pend))
          {
            if (Utils.IsInteger(*p, out uint digit))
            {
              i = i * 10 + digit;
              ++p;
              ++reReadIntegerDigits;
            }
            else if (allowThousands && *p == thousandsSeparator)
            {
              ++p;
            }
            else { break; }
          }
          if (i >= MinimalNineteenDigitInteger)
          { // We have a big integers
            exponent = (integerDigitCount - reReadIntegerDigits) + expNumber;
          }
          else
          { // We have a value with a fractional component.
            p++; // skip the '.'
            byte* firstAfterPeriod = p;
            while ((i < MinimalNineteenDigitInteger) && (p != pend) && Utils.IsInteger(*p, out uint digit))
            {
              i = i * 10 + digit;
              ++p;
            }
            exponent = firstAfterPeriod - p + expNumber;
          }
          // We have now corrected both exponent and i, to a truncated value
        }
      }
      answer.Exponent = exponent;
      answer.Mantissa = i;
      return answer;
    }
  };
}
