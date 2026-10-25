/*
MIT License

Copyright (c) 2021 The fast_float authors

Permission is hereby granted, free of charge, to any
person obtaining a copy of this software and associated
documentation files (the "Software"), to deal in the
Software without restriction, including without
limitation the rights to use, copy, modify, merge,
publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software
is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice
shall be included in all copies or substantial portions
of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT
SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SharpFastFloat.Structures
{
  // Adapted from fast_float's digit_comparison.h (MIT license): compare bounded
  // decimal digits with the exact binary midpoint using stack-backed integers.
  // The caller already has adjacent rounded endpoints for its 19-digit interval,
  // so it can select between those endpoints without recomputing an approximation.
  internal static unsafe class ExactDigitComparison
  {
#if NET8_0_OR_GREATER
    // Each limb is written before it enters Length; unused stack storage is never read.
    [SkipLocalsInit]
#endif
    internal static AdjustedMantissa Round(char* first, char* last,
      ParsedNumberString number, AdjustedMantissa lower, AdjustedMantissa upper,
      int mantissaBits, int minimumExponent, int decimalSeparator, int groupingSeparator)
    {
      // The retained 19-digit interval is narrower than one ULP. Only use
      // midpoint selection when both rounded endpoints prove adjacency; keep
      // the existing fallback for any unresolved conversion-engine result.
      ulong lowerBits = lower.Mantissa | ((ulong)(uint)lower.Power2 << mantissaBits);
      ulong upperBits = upper.Mantissa | ((ulong)(uint)upper.Power2 << mantissaBits);
      if (lower.Power2 < 0 || upper.Power2 < 0 || upperBits != lowerBits + 1)
      {
        return new AdjustedMantissa { Power2 = -1 };
      }
      ulong* realStorage = stackalloc ulong[RoundingInteger.Capacity];
      ulong* halfwayStorage = stackalloc ulong[RoundingInteger.Capacity];
      RoundingInteger real = new RoundingInteger(realStorage, 0);
      int digits = ParseMantissa(first, last, mantissaBits == 52 ? 769 : 114,
        decimalSeparator, groupingSeparator, ref real);
      return Compare(ref real, halfwayStorage, digits, number, lower, upper,
        mantissaBits, minimumExponent);
    }

    private static int ParseMantissa(char* p, char* last, int maxDigits,
      int decimalSeparator, int groupingSeparator, ref RoundingInteger result)
    {
      if (p != last && (*p == '-' || *p == '+')) { ++p; }
      int digits = 0;
      int chunkDigits = 0;
      ulong chunk = 0;
      ulong power = 1;
      bool fractional = false;
      bool truncated = false;
      while (p != last)
      {
        if (digits != 0 && chunkDigits <= 11 && maxDigits - digits >= 8 && last - p >= 8 &&
            Utils.TryParseEightConsecutiveDigits(p, out uint eight))
        {
          chunk = chunk * 100000000 + eight;
          power *= 100000000;
          chunkDigits += 8;
          digits += 8;
          p += 8;
          if (chunkDigits == 19)
          {
            result.Multiply(power);
            result.Add(chunk);
            chunk = 0;
            chunkDigits = 0;
            power = 1;
          }
          continue;
        }
        if (Utils.IsInteger(*p, out uint digit))
        {
          ++p;
          if (digits == 0 && digit == 0) { continue; }
          if (digits == maxDigits)
          {
            truncated |= digit != 0;
            continue;
          }
          chunk = chunk * 10 + digit;
          power *= 10;
          ++digits;
          if (++chunkDigits == 19)
          {
            result.Multiply(power);
            result.Add(chunk);
            chunk = 0;
            chunkDigits = 0;
            power = 1;
          }
        }
        else if (!fractional && *p == decimalSeparator)
        {
          fractional = true;
          ++p;
        }
        else if (!fractional && *p == groupingSeparator)
        {
          ++p;
        }
        else { break; }
      }
      if (chunkDigits != 0)
      {
        result.Multiply(power);
        result.Add(chunk);
      }
      // Preserve which side of any binary midpoint a truncated suffix lies on.
      // Appending a sticky 1 avoids turning a run of 9s into a false midpoint.
      if (truncated)
      {
        result.Multiply(10);
        result.Add(1);
        ++digits;
      }
      return digits;
    }

#if NET8_0_OR_GREATER
    // Each limb is written before it enters Length; unused stack storage is never read.
    [SkipLocalsInit]
#endif
    internal static AdjustedMantissa Round(byte* first, byte* last,
      ParsedNumberString number, AdjustedMantissa lower, AdjustedMantissa upper,
      int mantissaBits, int minimumExponent, int decimalSeparator, int groupingSeparator)
    {
      // The retained 19-digit interval is narrower than one ULP. Only use
      // midpoint selection when both rounded endpoints prove adjacency; keep
      // the existing fallback for any unresolved conversion-engine result.
      ulong lowerBits = lower.Mantissa | ((ulong)(uint)lower.Power2 << mantissaBits);
      ulong upperBits = upper.Mantissa | ((ulong)(uint)upper.Power2 << mantissaBits);
      if (lower.Power2 < 0 || upper.Power2 < 0 || upperBits != lowerBits + 1)
      {
        return new AdjustedMantissa { Power2 = -1 };
      }
      ulong* realStorage = stackalloc ulong[RoundingInteger.Capacity];
      ulong* halfwayStorage = stackalloc ulong[RoundingInteger.Capacity];
      RoundingInteger real = new RoundingInteger(realStorage, 0);
      int digits = ParseMantissa(first, last, mantissaBits == 52 ? 769 : 114,
        decimalSeparator, groupingSeparator, ref real);
      return Compare(ref real, halfwayStorage, digits, number, lower, upper,
        mantissaBits, minimumExponent);
    }

    private static int ParseMantissa(byte* p, byte* last, int maxDigits,
      int decimalSeparator, int groupingSeparator, ref RoundingInteger result)
    {
      if (p != last && (*p == '-' || *p == '+')) { ++p; }
      int digits = 0;
      int chunkDigits = 0;
      ulong chunk = 0;
      ulong power = 1;
      bool fractional = false;
      bool truncated = false;
      while (p != last)
      {
        if (digits != 0 && chunkDigits <= 11 && maxDigits - digits >= 8 && last - p >= 8 &&
            Utils.IsMadeOfEightDigitsFast(p))
        {
          uint eight = Utils.ParseEightDigitsUnrolled(p);
          chunk = chunk * 100000000 + eight;
          power *= 100000000;
          chunkDigits += 8;
          digits += 8;
          p += 8;
          if (chunkDigits == 19)
          {
            result.Multiply(power);
            result.Add(chunk);
            chunk = 0;
            chunkDigits = 0;
            power = 1;
          }
          continue;
        }
        if (Utils.IsInteger(*p, out uint digit))
        {
          ++p;
          if (digits == 0 && digit == 0) { continue; }
          if (digits == maxDigits)
          {
            truncated |= digit != 0;
            continue;
          }
          chunk = chunk * 10 + digit;
          power *= 10;
          ++digits;
          if (++chunkDigits == 19)
          {
            result.Multiply(power);
            result.Add(chunk);
            chunk = 0;
            chunkDigits = 0;
            power = 1;
          }
        }
        else if (!fractional && *p == decimalSeparator)
        {
          fractional = true;
          ++p;
        }
        else if (!fractional && *p == groupingSeparator)
        {
          ++p;
        }
        else { break; }
      }
      if (chunkDigits != 0)
      {
        result.Multiply(power);
        result.Add(chunk);
      }
      // Preserve which side of any binary midpoint a truncated suffix lies on.
      // Appending a sticky 1 avoids turning a run of 9s into a false midpoint.
      if (truncated)
      {
        result.Multiply(10);
        result.Add(1);
        ++digits;
      }
      return digits;
    }

    private static AdjustedMantissa Compare(ref RoundingInteger real,
      ulong* halfwayStorage, int digits, ParsedNumberString number,
      AdjustedMantissa lower, AdjustedMantissa upper, int mantissaBits, int minimumExponent)
    {
      // Scientific exponent is unchanged by truncation in the scanner. Count
      // the scanner coefficient's digits rather than re-reading a huge exponent.
      ulong coefficient = number.Mantissa;
      long exponent = number.Exponent;
      while (coefficient >= 10000) { coefficient /= 10000; exponent += 4; }
      while (coefficient >= 10) { coefficient /= 10; ++exponent; }
      exponent += 1 - digits;
      Debug.Assert(exponent >= -1200 && exponent <= 400);

      ulong halfway = lower.Mantissa;
      if (lower.Power2 != 0) { halfway |= 1UL << mantissaBits; }
      halfway = halfway * 2 + 1;
      int binaryExponent = Math.Max(1, lower.Power2) + minimumExponent - mantissaBits - 1;
      RoundingInteger theoretical = new RoundingInteger(halfwayStorage, halfway);
      if (exponent >= 0) { real.MultiplyPowerOfFive((int)exponent); }
      else { theoretical.MultiplyPowerOfFive((int)-exponent); }
      int shift = binaryExponent - (int)exponent;
      if (shift > 0) { theoretical.ShiftLeft(shift); }
      else if (shift < 0) { real.ShiftLeft(-shift); }
      int order = real.Compare(ref theoretical);
      return order > 0 || (order == 0 && (lower.Mantissa & 1) != 0) ? upper : lower;
    }
  }
}
