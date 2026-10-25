using System;
using System.Runtime.CompilerServices;

namespace csFastFloat.Structures
{
  // Fixed ASCII decimal grammar shared by strict float/double entry points.
  // The original pointers remain with the caller for exact rounding fallback.
  internal static unsafe class StrictNumberScanner
  {
    // The short integer path avoids significant-digit and truncation
    // bookkeeping. Longer tokens retain up to 19 significant digits.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ParsedNumberString Parse(char* first, char* last)
    {
      ParsedNumberString number = new ParsedNumberString();
      if (first == last) { return number; }
      char* p = first;
      number.Negative = *p == '-';
      if (*p == '-' || *p == '+') { ++p; }

      ulong mantissa = 0;
      int significantDigits = 0;
      bool discardedNonZero = false;
      char* integerStart = p;
      if (last - p < 8)
      {
        while (p != last && Utils.IsInteger(*p, out uint digit))
        {
          mantissa = mantissa * 10 + digit;
          ++p;
        }
        if (p == last)
        {
          number.Valid = p != integerStart;
          number.Mantissa = mantissa;
          return number;
        }
        // There are fewer than eight characters in the entire remaining
        // token, so leading zeros cannot exhaust accumulator capacity.
        significantDigits = (int)(p - integerStart);
      }
      else
      {
        ScanDigits(ref p, last, ref mantissa, ref significantDigits, ref discardedNonZero);
      }
      bool hasDigits = p != integerStart;

      long fractionalDigits = 0;
      if (p != last && *p == '.')
      {
        char* fractionStart = ++p;
        ScanDigits(ref p, last, ref mantissa, ref significantDigits, ref discardedNonZero);
        fractionalDigits = p - fractionStart;
        hasDigits |= fractionalDigits != 0;
      }
      if (!hasDigits) { return number; }

      long exponent = Math.Max(0, significantDigits - 19) - fractionalDigits;
      if (p != last && (*p == 'e' || *p == 'E'))
      {
        ++p;
        bool negativeExponent = p != last && *p == '-';
        if (p != last && (*p == '-' || *p == '+')) { ++p; }
        char* exponentStart = p;
        long explicitExponent = 0;
        // This bound allows cancellation by any mantissa that fits in the
        // input span, while keeping arbitrarily large exponents bounded.
        long exponentLimit = last - first + 400;
        while (p != last && Utils.IsInteger(*p, out uint digit))
        {
          if (explicitExponent < exponentLimit) { explicitExponent = explicitExponent * 10 + digit; }
          ++p;
        }
        if (p == exponentStart) { return number; }
        exponent += negativeExponent ? -explicitExponent : explicitExponent;
      }
      if (p != last) { return number; }

      number.Valid = true;
      number.Mantissa = mantissa;
      number.Exponent = exponent;
      // All-zero discarded digits leave an exact coefficient, so the
      // conversion engine need not test the next mantissa or its interval.
      number.TooManyDigits = discardedNonZero;
      return number;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ScanDigits(ref char* p, char* last, ref ulong mantissa,
      ref int significantDigits, ref bool discardedNonZero)
    {
      while (p != last)
      {
        if (last - p >= 8 && (significantDigits <= 11 || significantDigits >= 19) &&
            Utils.TryParseEightConsecutiveDigits(p, out uint chunk))
        {
          AppendChunk(chunk, ref mantissa, ref significantDigits, ref discardedNonZero);
          p += 8;
          continue;
        }
        if (!Utils.IsInteger(*p, out uint digit)) { break; }
        if (significantDigits < 19) { mantissa = mantissa * 10 + digit; }
        else { discardedNonZero |= digit != 0; }
        if (mantissa != 0) { ++significantDigits; }
        ++p;
      }
    }

    // The short integer path avoids significant-digit and truncation
    // bookkeeping. Longer tokens retain up to 19 significant digits.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ParsedNumberString Parse(byte* first, byte* last)
    {
      ParsedNumberString number = new ParsedNumberString();
      if (first == last) { return number; }
      byte* p = first;
      number.Negative = *p == '-';
      if (*p == '-' || *p == '+') { ++p; }

      ulong mantissa = 0;
      int significantDigits = 0;
      bool discardedNonZero = false;
      byte* integerStart = p;
      if (last - p < 8)
      {
        while (p != last && Utils.IsInteger(*p, out uint digit))
        {
          mantissa = mantissa * 10 + digit;
          ++p;
        }
        if (p == last)
        {
          number.Valid = p != integerStart;
          number.Mantissa = mantissa;
          return number;
        }
        // There are fewer than eight characters in the entire remaining
        // token, so leading zeros cannot exhaust accumulator capacity.
        significantDigits = (int)(p - integerStart);
      }
      else
      {
        ScanDigits(ref p, last, ref mantissa, ref significantDigits, ref discardedNonZero);
      }
      bool hasDigits = p != integerStart;

      long fractionalDigits = 0;
      if (p != last && *p == '.')
      {
        byte* fractionStart = ++p;
        ScanDigits(ref p, last, ref mantissa, ref significantDigits, ref discardedNonZero);
        fractionalDigits = p - fractionStart;
        hasDigits |= fractionalDigits != 0;
      }
      if (!hasDigits) { return number; }

      long exponent = Math.Max(0, significantDigits - 19) - fractionalDigits;
      if (p != last && (*p == 'e' || *p == 'E'))
      {
        ++p;
        bool negativeExponent = p != last && *p == '-';
        if (p != last && (*p == '-' || *p == '+')) { ++p; }
        byte* exponentStart = p;
        long explicitExponent = 0;
        // This bound allows cancellation by any mantissa that fits in the
        // input span, while keeping arbitrarily large exponents bounded.
        long exponentLimit = last - first + 400;
        while (p != last && Utils.IsInteger(*p, out uint digit))
        {
          if (explicitExponent < exponentLimit) { explicitExponent = explicitExponent * 10 + digit; }
          ++p;
        }
        if (p == exponentStart) { return number; }
        exponent += negativeExponent ? -explicitExponent : explicitExponent;
      }
      if (p != last) { return number; }

      number.Valid = true;
      number.Mantissa = mantissa;
      number.Exponent = exponent;
      // All-zero discarded digits leave an exact coefficient, so the
      // conversion engine need not test the next mantissa or its interval.
      number.TooManyDigits = discardedNonZero;
      return number;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ScanDigits(ref byte* p, byte* last, ref ulong mantissa,
      ref int significantDigits, ref bool discardedNonZero)
    {
      while (p != last)
      {
        if (last - p >= 8 && (significantDigits <= 11 || significantDigits >= 19) &&
            Utils.IsMadeOfEightDigitsFast(p))
        {
          uint chunk = Utils.ParseEightDigitsUnrolled(p);
          AppendChunk(chunk, ref mantissa, ref significantDigits, ref discardedNonZero);
          p += 8;
          continue;
        }
        if (!Utils.IsInteger(*p, out uint digit)) { break; }
        if (significantDigits < 19) { mantissa = mantissa * 10 + digit; }
        else { discardedNonZero |= digit != 0; }
        if (mantissa != 0) { ++significantDigits; }
        ++p;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendChunk(uint chunk, ref ulong mantissa, ref int significantDigits, ref bool discardedNonZero)
    {
      if (significantDigits >= 19)
      {
        discardedNonZero |= chunk != 0;
        significantDigits += 8;
        return;
      }
      if (significantDigits == 0 && chunk == 0) { return; }
      mantissa = mantissa * 100000000 + chunk;
      bool firstChunk = significantDigits == 0;
      significantDigits += 8;
      if (firstChunk && chunk < 10000000)
      {
        for (uint place = 10000000; chunk < place; place /= 10) { --significantDigits; }
      }
    }
  }
}
