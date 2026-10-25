using csFastFloat.Constants;
using csFastFloat.Structures;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace csFastFloat
{


  /// <summary>
  /// This class is intented to parse double values from inputs such as string, readonlyspans  and char pointers
  /// There's two set of functions, one for UTF-16 encoding another for UTF-8 encoding
  /// This is a C# port of Daniel Lemire's fast_float library written in C++
  /// https://github.com/fastfloat/fast_float
  /// </summary>
  public static unsafe partial class FastDoubleParser
  {

    private static void ThrowArgumentException() => throw new ArgumentException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ExactPowerOfTen(long power)
    {
#if NET5_0_OR_GREATER
      Debug.Assert(power < CalculationConstants.PowersOfTenDouble.Length);
      ref double tableRef = ref MemoryMarshal.GetArrayDataReference(CalculationConstants.PowersOfTenDouble);
      return Unsafe.Add(ref tableRef, (nint)power);
#else
      return CalculationConstants.PowersOfTenDouble[power];
#endif

    }
    /// <summary>
    /// Resolve the adjusted mantissa back to its corresponding double value
    /// </summary>
    /// <param name="negative">bool:  true indicates a negative value should be returned</param>
    /// <param name="am">adjusted mantissa (mantissa and exponent)</param>
    /// <returns>double value corresponding</returns>
    internal static double ToFloat(bool negative, AdjustedMantissa am)
    {
      ulong word = am.Mantissa;
      word |= (ulong)(uint)(am.Power2) << DoubleBinaryConstants.MantissaExplicitBits;
      word = negative ? word | ((ulong)(1) << DoubleBinaryConstants.SignIndex) : word;

      return BitConverter.Int64BitsToDouble((long)word);
    }

    /// <summary>
    /// Clinger's fast path
    /// </summary>
    /// <param name="pns">Parsed info of the input</param>
    /// <returns></returns>
    internal static double FastPath(ParsedNumberString pns)
    {
      double value = pns.Mantissa;
      if (pns.Exponent < 0)
      {
        value /= ExactPowerOfTen(-pns.Exponent);
      }
      else
      {
        value *= ExactPowerOfTen(pns.Exponent);
      }
      if (pns.Negative) { value = -value; }
      return value;
    }


    /// <summary>
    /// Try parsing a double from a UTF-16 encoded string in the given number style
    /// </summary>
    /// <param name="s">input string</param>
    /// <param name="result">output double value</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>bool : true is sucessfuly parsed</returns>
    public static bool TryParseDouble(string s, out double result, NumberStyles styles = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
      => TryParseDouble(s.AsSpan(), out _, out result, styles, decimalSeparator, thousandsSeparator);

    /// <summary>
    /// Try parsing a double from a UTF-16 encoded string in the given number style, counting number of consumed characters
    /// </summary>
    /// <param name="s">input string</param>
    /// <param name="charactersConsumed">number of consumed characters while parsing </param>
    /// <param name="result">output double value</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>bool : true is sucessfuly parsed</returns>
    /// <returns></returns>
    public static bool TryParseDouble(string s, out int charactersConsumed, out double result, NumberStyles styles = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
    => TryParseDouble(s.AsSpan(), out charactersConsumed, out result, styles, decimalSeparator, thousandsSeparator);


    /// <summary>
    /// Try parsing a double from a UTF-16 encoded string in the given number style
    /// </summary>
    /// <param name="s">input as a readonly span</param>
    /// <param name="result">output double value</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>bool : true is sucessfuly parsed</returns>
    public static bool TryParseDouble(ReadOnlySpan<char> s, out double result, NumberStyles styles = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
    {

      fixed (char* pStart = s)
      {
        if (!TryParseNumber(pStart, pStart + (uint)s.Length, out _, out result, styles, decimalSeparator, thousandsSeparator))
        {
          return TryHandleInvalidInput(pStart, pStart + (uint)s.Length, out _, out result);
        }

        return true;
      }

    }

    /// <summary>
    /// Try parsing a double from a UTF-16 encoded span of chars in the given number style, counting number of consumed characters
    /// </summary>
    /// <param name="s">input as a readonly span</param>
    /// <param name="charactersConsumed">number of consumed characters while parsing </param>
    /// <param name="result">output double value</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>bool : true is sucessfuly parsed</returns>
    public static bool TryParseDouble(ReadOnlySpan<char> s, out int charactersConsumed, out double result, NumberStyles styles = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
    {
      fixed (char* pStart = s)
      {
        if (!TryParseNumber(pStart, pStart + (uint)s.Length, out charactersConsumed, out result, styles, decimalSeparator, thousandsSeparator))
        {
          return TryHandleInvalidInput(pStart, pStart + (uint)s.Length, out charactersConsumed, out result);
        }

        return true;
      }
    }



    /// <summary>
    /// Try parsing a double from a UTF-16 encoded input in the given number style
    /// </summary>
    /// <param name="first">char pointer to the begining of the string</param>
    /// <param name="last">char pointer to the end of the string</param>
    /// <param name="result">output double value</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>bool : true is sucessfuly parsed</returns>
    public static bool TryParseDouble(char* first, char* last, out double result, NumberStyles styles = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
    {

      if (!TryParseNumber(first, last, out _, out result, styles, decimalSeparator, thousandsSeparator))
      {
        return TryHandleInvalidInput(first, last, out _, out result);
      }

      return true;
    }


    /// <summary>
    /// Try parsing a double from a UTF-16 encoded input in the given number style, counting number of consumed characters
    /// </summary>
    /// <param name="first">char pointer to the begining of the string</param>
    /// <param name="last">char pointer to the end of the string</param>
    /// <param name="charactersConsumed">number of consumed characters while parsing </param>
    /// <param name="result">output double value</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>bool : true is sucessfuly parsed</returns>
    public static bool TryParseDouble(char* first, char* last, out int charactersConsumed, out double result, NumberStyles styles = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
    {
      if (!TryParseNumber(first, last, out charactersConsumed, out result, styles, decimalSeparator, thousandsSeparator))
      {
        return TryHandleInvalidInput(first, last, out charactersConsumed, out result);
      }

      return true;
    }




    /// <summary>
    /// Try parsing a double from a UTF-8 encoded span of bytes in the given number style
    /// </summary>
    /// <param name="s">input string as Readonly span</param>
    /// <param name="result">output double value</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>bool : true is sucessfuly parsed</returns>
    public static bool TryParseDouble(ReadOnlySpan<byte> s, out double result, NumberStyles styles = NumberStyles.Float, byte decimalSeparator = (byte)'.', byte thousandsSeparator = (byte)',')
    {
      fixed (byte* pStart = s)
      {
        if (!TryParseNumber(pStart, pStart + (uint)s.Length, out _, out result, styles, decimalSeparator, thousandsSeparator))
        {
          return TryHandleInvalidInput(pStart, pStart, out _, out result);
        }

        return true;
      }
    }
    /// <summary>
    /// Try parsing a double from a UTF-8 encoded span of bytes in the given number style, counting number of consumed characters
    /// </summary>
    /// <param name="s">input string as Readonly span</param>
    /// <param name="charactersConsumed">number of consumed characters while parsing </param>
    /// <param name="result">output double value</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>bool : true is sucessfuly parsed</returns>
    public static bool TryParseDouble(ReadOnlySpan<byte> s, out int charactersConsumed, out double result, NumberStyles styles = NumberStyles.Float, byte decimalSeparator = (byte)'.', byte thousandsSeparator = (byte)',')
    {
      fixed (byte* pStart = s)
      {
        if (!TryParseNumber(pStart, pStart + (uint)s.Length, out charactersConsumed, out result, styles, decimalSeparator, thousandsSeparator))
        {
          return TryHandleInvalidInput(pStart, pStart + (uint)s.Length, out _, out result);
        }

        return true;
      }
    }




    /// <summary>
    /// Try parsing a double from a UTF-8 encoded input in the given number style
    /// </summary>
    /// <param name="first">byte pointer to the begining of the string</param>
    /// <param name="last">byte pointer to the end of the string</param>
    /// <param name="result">output double value</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>bool : true is sucessfuly parsed</returns>
    public static bool TryParseDouble(byte* first, byte* last, out double result, NumberStyles styles = NumberStyles.Float, byte decimalSeparator = (byte)'.', byte thousandsSeparator = (byte)',')
    {
      if (!TryParseNumber(first, last, out _, out result, styles, decimalSeparator, thousandsSeparator))
      {
        return TryHandleInvalidInput(first, last, out _, out result);
      }

      return true;

    }

    /// <summary>
    /// Try parsing a double from a UTF-8 encoded input in the given number style, counting number of consumed characters
    /// </summary>
    /// <param name="first">byte pointer to the begining of the string</param>
    /// <param name="last">byte pointer to the end of the string</param>
    /// <param name="charactersConsumed">number of consumed characters while parsing </param>
    /// <param name="result">output double value</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>bool : true is sucessfuly parsed</returns>
    public static bool TryParseDouble(byte* first, byte* last, out int charactersConsumed, out double result, NumberStyles styles = NumberStyles.Float, byte decimalSeparator = (byte)'.', byte thousandsSeparator = (byte)',')
    {
      if (!TryParseNumber(first, last, out charactersConsumed, out result, styles, decimalSeparator, thousandsSeparator))
      {
        return TryHandleInvalidInput(first, last, out charactersConsumed, out result);
      }

      return true;

    }



    /// <summary>
    /// Parses double from a UTF-16 encoded string in the given number style
    /// </summary>
    /// <param name="s">input string</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>parsed double value </returns>
    public static double ParseDouble(string s, NumberStyles styles = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
    => ParseDouble(s, out int _, styles, decimalSeparator, thousandsSeparator);

    /// <summary>
    /// Parses double from a UTF-16 encoded string in the given number style counting number of characters consumed
    /// </summary>
    /// <param name="s">input string</param>
    /// <param name="charactersConsumed">number of character consumed while parsing</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>parsed double value </returns>
    public static double ParseDouble(string s, out int charactersConsumed, NumberStyles styles = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
    {
      if (s == null)
      {
        ThrowArgumentNull();
      }

      static void ThrowArgumentNull() => throw new ArgumentNullException(nameof(s));

      fixed (char* pStart = s)
      {
        if (!TryParseNumber(pStart, pStart + (uint)s.Length, out charactersConsumed, out double value, styles, decimalSeparator, thousandsSeparator))
        {
          if (!TryHandleInvalidInput(pStart, pStart + (uint)s.Length, out charactersConsumed, out value))
          {
            ThrowArgumentException();
          }
        }

        return value;
      }
    }


    /// <summary>
    /// Parses double from a UTF-16 encoded readonly span of chars in the given number style
    /// </summary>
    /// <param name="s">input as a readonly span of char</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>parsed double value </returns>
    public static double ParseDouble(ReadOnlySpan<char> s, NumberStyles styles = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
      => ParseDouble(s, out int _, styles, decimalSeparator, thousandsSeparator);

    /// <summary>
    /// Parses double from am UTF-16 encoded readonly span of chars in the given number style counting number of characters consumed
    /// </summary>
    /// <param name="s">input as a readonly span of char</param>
    /// <param name="charactersConsumed">number of character consumed while parsing</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>parsed double value </returns>
    public static double ParseDouble(ReadOnlySpan<char> s, out int charactersConsumed, NumberStyles styles = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
    {
      if (s.IsEmpty)
      {
        ThrowArgumentNull();
      }

      static void ThrowArgumentNull() => throw new ArgumentNullException(nameof(s));

      fixed (char* pStart = s)
      {
        if (!TryParseNumber(pStart, pStart + (uint)s.Length, out charactersConsumed, out double value, styles, decimalSeparator, thousandsSeparator))
        {
          if (!TryHandleInvalidInput(pStart, pStart + (uint)s.Length, out charactersConsumed, out value))
          {
            ThrowArgumentException();
          }
        }

        return value;
      }
    }

    /// <summary>
    /// Parses double from a UTF-16 encoded input in the given number style
    /// </summary>
    /// <param name="first">char pointer to the begining of the input</param>
    /// <param name="last">char pointer to the end of the input</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>parsed double value </returns>
    public static double ParseDouble(char* first, char* last, NumberStyles styles = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
      => ParseDouble(first, last, out int _, styles, decimalSeparator, thousandsSeparator);

    /// <summary>
    /// Parses double from a UTF-8 encoded readonly span of bytes  in the given number style
    /// </summary>
    /// <param name="s">input as a readonly span of bytes</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>parsed double value </returns>
    public static double ParseDouble(ReadOnlySpan<byte> s, NumberStyles styles = NumberStyles.Float, byte decimalSeparator = (byte)'.', byte thousandsSeparator = (byte)',')
      => ParseDouble(s, out int _, styles, decimalSeparator, thousandsSeparator);

    /// <summary>
    /// Parses double from a UTF-8 encoded readonly span of bytes in the given number style counting number of characters consumed
    /// </summary>
    /// <param name="s">input as a readonly span of bytes</param>
    /// <param name="charactersConsumed">number of character consumed while parsing</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>parsed double value </returns>
    public static double ParseDouble(ReadOnlySpan<byte> s, out int charactersConsumed, NumberStyles styles = NumberStyles.Float, byte decimalSeparator = (byte)'.', byte thousandsSeparator = (byte)',')
    {
      fixed (byte* pStart = s)
      {
        return ParseDouble(pStart, pStart + s.Length, out charactersConsumed, styles, decimalSeparator, thousandsSeparator);
      }
    }


    /// <summary>
    /// Parses double from a UTF-16 encoded input in the given number style counting number of characters consumed
    /// </summary>
    /// <param name="first">char pointer to the begining of the input</param>
    /// <param name="last">char pointer to the end of the input</param>
    /// <param name="charactersConsumed">number of character consumed while parsing</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>parsed double value </returns>
    public static double ParseDouble(char* first, char* last, out int charactersConsumed, NumberStyles styles = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
    {


      if (!TryParseNumber(first, last, out charactersConsumed, out double value, styles, decimalSeparator, thousandsSeparator))
      {
        if (!TryHandleInvalidInput(first, last, out charactersConsumed, out value))
        {
          ThrowArgumentException();
        }
      }

      return value;
    }


    /// <summary>
    /// Parses double from a UTF-8 encoded input in the given number style counting number of characters consumed
    /// </summary>
    /// <param name="first">byte pointer to the begining of the input</param>
    /// <param name="last">byte pointer to the end of the input</param>
    /// <param name="charactersConsumed">number of character consumed while parsing</param>
    /// <param name="styles">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <returns>parsed double value </returns>
    public static double ParseDouble(byte* first, byte* last, out int charactersConsumed, NumberStyles styles = NumberStyles.Float, byte decimalSeparator = (byte)'.', byte thousandsSeparator = (byte)',')
    {
      if (TryParseDouble(first, last, out charactersConsumed, out double result, styles, decimalSeparator, thousandsSeparator))
      {
        return result;
      }
      ThrowArgumentException();
      throw null;

    }

    /// <summary>
    /// Try to parse the input (UTF-16) and compute the double value
    /// </summary>
    /// <param name="first">char pointer to the begining of the input</param>
    /// <param name="last">char pointer to the end of the input</param>
    /// <param name="charactersConsumed">number of characters consumed while parsing</param>
    /// <param name="expectedFormat">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <param name="value">out : reference to double variable to hold the parsed value</param>
    /// <returns>double : parsed value</returns>


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryParseNumber(char* first, char* last, out int charactersConsumed, out double value, NumberStyles expectedFormat = NumberStyles.Float, char decimalSeparator = '.', char thousandsSeparator = ',')
    {
      int leadingSpaces = 0;
      charactersConsumed = 0;
      value = 0;

      while ((first != last) && Utils.IsAsciiSpace(*first))
      {
        first++;
        leadingSpaces++;
      }
      if (first == last)
      {
        return false;
      }
      ParsedNumberString pns = ParsedNumberString.ParseNumberString(first, last, expectedFormat, decimalSeparator, thousandsSeparator);
      if (!pns.Valid)
      {
        return false;
      }
      charactersConsumed = pns.CharactersConsumed + leadingSpaces;

      value = ConvertNumber(first, last, pns, decimalSeparator);
      return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ConvertNumber(char* first, char* last, ParsedNumberString pns, char decimalSeparator)
    {
      // Next is Clinger's fast path.
      if (DoubleBinaryConstants.MinExponentFastPath <= pns.Exponent && pns.Exponent <= DoubleBinaryConstants.MaxExponentFastPath && pns.Mantissa <= DoubleBinaryConstants.MaxMantissaFastPath && !pns.TooManyDigits)
      {
        return FastPath(pns);
      }

      AdjustedMantissa am = ComputeFloat(pns.Exponent, pns.Mantissa);
      if (pns.TooManyDigits)
      {
        if (am != ComputeFloat(pns.Exponent, pns.Mantissa + 1))
        {
          am.Power2 = -1; // value is invalid.
        }
      }
      // If we called compute_float<binary_format<T>>(pns.exponent, pns.mantissa) and we have an invalid power (am.power2 < 0),
      // then we need to go the long way around again. This is very uncommon.
      if (am.Power2 < 0) { am = ParseLongMantissa(first, last, decimalSeparator); }
      return ToFloat(pns.Negative, am);
    }
    /// <summary>
    /// Try to parse the input (UTF-8) and compute the double value
    /// </summary>
    /// <param name="first">byte pointer to the begining of the input</param>
    /// <param name="last">byte pointer to the end of the input</param>
    /// <param name="charactersConsumed">number of characters consumed while parsing</param>
    /// <param name="expectedFormat">allowed styles for the input string</param>
    /// <param name="decimalSeparator">decimal separator to be used</param>
    /// <param name="value">out : reference to double variable to hold the parsed value</param>
    /// <returns>double : parsed value</returns>

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryParseNumber(byte* first, byte* last, out int charactersConsumed, out double value, NumberStyles expectedFormat = NumberStyles.Float, byte decimalSeparator = (byte)'.', byte thousandsSeparator = (byte)',')
    {

      value = 0;
      charactersConsumed = 0;
      int leadingSpaces = 0;

      while ((first != last) && Utils.IsSpace(*first))
      {
        first++;
        leadingSpaces++;
      }
      if (first == last)
      {
        return false;
      }
      ParsedNumberString pns = ParsedNumberString.ParseNumberString(first, last, expectedFormat, decimalSeparator, thousandsSeparator);
      if (!pns.Valid)
      {
        return false;
      }
      charactersConsumed = pns.CharactersConsumed + leadingSpaces;

      value = ConvertNumber(first, last, pns, decimalSeparator);
      return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ConvertNumber(byte* first, byte* last, ParsedNumberString pns, byte decimalSeparator)
    {
      // Next is Clinger's fast path.
      if (DoubleBinaryConstants.MinExponentFastPath <= pns.Exponent && pns.Exponent <= DoubleBinaryConstants.MaxExponentFastPath && pns.Mantissa <= DoubleBinaryConstants.MaxMantissaFastPath && !pns.TooManyDigits)
      {
        return FastPath(pns);
      }

      AdjustedMantissa am = ComputeFloat(pns.Exponent, pns.Mantissa);
      if (pns.TooManyDigits)
      {
        if (am != ComputeFloat(pns.Exponent, pns.Mantissa + 1))
        {
          am.Power2 = -1; // value is invalid.
        }
      }
      // If we called compute_float<binary_format<T>>(pns.exponent, pns.mantissa) and we have an invalid power (am.power2 < 0),
      // then we need to go the long way around again. This is very uncommon.
      if (am.Power2 < 0) { am = ParseLongMantissa(first, last, decimalSeparator); }
      return ToFloat(pns.Negative, am);
    }


    /// <summary>
    /// Daniel Lemire's Fast-float algorithm
    /// please refer to https://arxiv.org/abs/2101.11408
    /// </summary>
    /// <param name="q">exponemt</param>
    /// <param name="w">decimal mantissa</param>
    /// <returns>Adjusted mantissa</returns>
    internal static AdjustedMantissa ComputeFloat(long q, ulong w)
    {
      AdjustedMantissa answer = new AdjustedMantissa();

      if ((w == 0) || (q < DoubleBinaryConstants.SmallestPowerOfTen))
      {
        // result should be zero
        return default;
      }
      if (q > DoubleBinaryConstants.LargestPowerOfTen)
      {
        // we want to get infinity:
        answer.Power2 = DoubleBinaryConstants.InfinitePower;
        answer.Mantissa = 0;
        return answer;
      }
      // At this point in time q is in [smallest_power_of_five, largest_power_of_five].

      // We want the most significant bit of i to be 1. Shift if needed.
      int lz = Utils.LeadingZeroCount(w);
      w <<= lz;

      // The required precision is mantissa_explicit_bits() + 3 because
      // 1. We need the implicit bit
      // 2. We need an extra bit for rounding purposes
      // 3. We might lose a bit due to the "upperbit" routine (result too small, requiring a shift)

      value128 product = Utils.ComputeProductApproximation(DoubleBinaryConstants.MantissaExplicitBits + 3, q, w);
      if (product.Low == 0xFFFFFFFFFFFFFFFF)
      { //  could guard it further
        // In some very rare cases, this could happen, in which case we might need a more accurate
        // computation that what we can provide cheaply. This is very, very unlikely.
        //
        bool insideSafeExponent = (q >= -27) && (q <= 55); // always good because 5**q <2**128 when q>=0,
                                                           // and otherwise, for q<0, we have 5**-q<2**64 and the 128-bit reciprocal allows for exact computation.
        if (!insideSafeExponent)
        {
          answer.Power2 = -1; // This (a negative value) indicates an error condition.
          return answer;
        }
      }
      // The "compute_product_approximation" function can be slightly slower than a branchless approach:
      // value128 product = compute_product(q, w);
      // but in practice, we can win big with the compute_product_approximation if its additional branch
      // is easily predicted. Which is best is data specific.
      int upperbit = (int)(product.High >> 63);

      answer.Mantissa = product.High >> (upperbit + 64 - DoubleBinaryConstants.MantissaExplicitBits - 3);

      answer.Power2 = Utils.Power((int)q) + upperbit - lz - DoubleBinaryConstants.MinimumExponent;
      if (answer.Power2 <= 0)
      { // we have a subnormal?
        // Here have that answer.power2 <= 0 so -answer.power2 >= 0
        if (-answer.Power2 + 1 >= 64)
        { // if we have more than 64 bits below the minimum exponent, you have a zero for sure.
          answer.Power2 = 0;
          answer.Mantissa = 0;
          // result should be zero
          return answer;
        }
        // next line is safe because -answer.power2 + 1 < 64
        answer.Mantissa >>= -answer.Power2 + 1;
        // Thankfully, we can't have both "round-to-even" and subnormals because
        // "round-to-even" only occurs for powers close to 0.
        answer.Mantissa += (answer.Mantissa & 1); // round up
        answer.Mantissa >>= 1;
        // There is a weird scenario where we don't have a subnormal but just.
        // Suppose we start with 2.2250738585072013e-308, we end up
        // with 0x3fffffffffffff x 2^-1023-53 which is technically subnormal
        // whereas 0x40000000000000 x 2^-1023-53  is normal. Now, we need to round
        // up 0x3fffffffffffff x 2^-1023-53  and once we do, we are no longer
        // subnormal, but we can only know this after rounding.
        // So we only declare a subnormal if we are smaller than the threshold.
        answer.Power2 = (answer.Mantissa < ((ulong)(1) << DoubleBinaryConstants.MantissaExplicitBits)) ? 0 : 1;
        return answer;
      }

      // usually, we round *up*, but if we fall right in between and and we have an
      // even basis, we need to round down
      // We are only concerned with the cases where 5**q fits in single 64-bit word.
      if ((product.Low <= 1) && (q >= DoubleBinaryConstants.MinExponentRoundToEven) && (q <= DoubleBinaryConstants.MaxExponentRoundToEven) &&
          ((answer.Mantissa & 3) == 1))
      { // we may fall between two floats!
        // To be in-between two floats we need that in doing
        //   answer.mantissa = product.high >> (upperbit + 64 - mantissa_explicit_bits() - 3);
        // ... we dropped out only zeroes. But if this happened, then we can go back!!!
        if ((answer.Mantissa << (upperbit + 64 - DoubleBinaryConstants.MantissaExplicitBits - 3)) == product.High)
        {
          answer.Mantissa &= ~(ulong)(1);          // flip it so that we do not round up
        }
      }

      answer.Mantissa += (answer.Mantissa & 1); // round up
      answer.Mantissa >>= 1;
      if (answer.Mantissa >= ((ulong)(2) << DoubleBinaryConstants.MantissaExplicitBits))
      {
        answer.Mantissa = ((ulong)(1) << DoubleBinaryConstants.MantissaExplicitBits);
        answer.Power2++; // undo previous addition
      }

      answer.Mantissa &= ~((ulong)(1) << DoubleBinaryConstants.MantissaExplicitBits);
      if (answer.Power2 >= DoubleBinaryConstants.InfinitePower)
      { // infinity
        answer.Power2 = DoubleBinaryConstants.InfinitePower;
        answer.Mantissa = 0;
      }
      return answer;
    }


    internal static AdjustedMantissa ComputeFloat(DecimalInfo d)
    {
      AdjustedMantissa answer = new AdjustedMantissa();
      if (d.NumDigits == 0)
      {
        // should be zero
        return default;
      }
      // At this point, going further, we can assume that d.NumDigits > 0.
      //
      // We want to guard against excessive decimal point values because
      // they can result in long running times. Indeed, we do
      // shifts by at most 60 bits. We have that log(10**400)/log(2**60) ~= 22
      // which is fine, but log(10**299995)/log(2**60) ~= 16609 which is not
      // fine (runs for a long time).
      //
      if (d.DecimalPoint < -324)
      {
        // We have something smaller than 1e-324 which is always zero
        // in binary64 and binary32.
        // It should be zero.
        return default;
      }
      else if (d.DecimalPoint >= 310)
      {
        // We have something at least as large as 0.1e310 which is
        // always infinite.
        answer.Power2 = DoubleBinaryConstants.InfinitePower;
        answer.Mantissa = 0;
        return answer;
      }
      const int MaxShift = 60;
      const uint NumPowers = 19;

      int exp2 = 0;
      while (d.DecimalPoint > 0)
      {
        uint n = (uint)(d.DecimalPoint);
        int shift = (n < NumPowers) ? CalculationConstants.GetPowers(n) : MaxShift;

        d.DecimalRightShift(shift);
        if (d.DecimalPoint < -CalculationConstants.DecimalPointRange)
        {
          // should be zero
          answer.Power2 = 0;
          answer.Mantissa = 0;
          return answer;
        }
        exp2 += shift;
      }
      // We shift left toward [1/2 ... 1].
      while (d.DecimalPoint <= 0)
      {
        int shift;
        if (d.DecimalPoint == 0)
        {
          if (d.Digits[0] >= 5)
          {
            break;
          }
          if (d.Digits[0] < 2)
          { shift = 2; }
          else { shift = 1; }
        }
        else
        {
          uint n = (uint)(-d.DecimalPoint);
          shift = (n < NumPowers) ? CalculationConstants.GetPowers(n) : MaxShift;
        }

        d.DecimalLeftShift(shift);

        if (d.DecimalPoint > CalculationConstants.DecimalPointRange)
        {
          // we want to get infinity:
          answer.Power2 = DoubleBinaryConstants.InfinitePower;
          answer.Mantissa = 0;
          return answer;
        }
        exp2 -= shift;
      }
      // We are now in the range [1/2 ... 1] but the binary format uses [1 ... 2].
      exp2--;

      int minExp = DoubleBinaryConstants.MinimumExponent;

      while ((minExp + 1) > exp2)
      {
        int n = (minExp + 1) - exp2;
        if (n > MaxShift)
        {
          n = MaxShift;
        }
        d.DecimalRightShift(n);
        exp2 += (int)(n);
      }
      if ((exp2 - minExp) >= DoubleBinaryConstants.InfinitePower)
      {
        answer.Power2 = DoubleBinaryConstants.InfinitePower;
        answer.Mantissa = 0;
        return answer;
      }

      int mantissaSizeInBits = DoubleBinaryConstants.MantissaExplicitBits + 1;
      d.DecimalLeftShift(mantissaSizeInBits);

      ulong mantissa = d.Round();
      // It is possible that we have an overflow, in which case we need
      // to shift back.
      if (mantissa >= ((ulong)(1) << mantissaSizeInBits))
      {
        d.DecimalRightShift(1);
        exp2 += 1;
        mantissa = d.Round();
        if ((exp2 - minExp) >= DoubleBinaryConstants.InfinitePower)
        {
          answer.Power2 = DoubleBinaryConstants.InfinitePower;
          answer.Mantissa = 0;
          return answer;
        }
      }
      answer.Power2 = exp2 - minExp;
      if (mantissa < ((ulong)(1) << DoubleBinaryConstants.MantissaExplicitBits)) { answer.Power2--; }
      answer.Mantissa = mantissa & (((ulong)(1) << DoubleBinaryConstants.MantissaExplicitBits) - 1);

      return answer;
    }

    // UTF-16 inputs
    internal static AdjustedMantissa ParseLongMantissa(char* first, char* last, char decimalSeparator)
    {
      DecimalInfo d = DecimalInfo.ParseDecimal(first, last, decimalSeparator);
      return ComputeFloat(d);
    }

    // UTF-8/ASCII inputs
    internal static AdjustedMantissa ParseLongMantissa(byte* first, byte* last, byte decimalSeparator)
    {
      DecimalInfo d = DecimalInfo.ParseDecimal(first, last, decimalSeparator);
      return ComputeFloat(d);
    }


    internal static bool TryHandleInvalidInput(char* first, char* last, out int charactersConsumed, out double result)
    {
      result = 0;
      charactersConsumed = 0;

      if (last - first >= 3)
      {
        if (Utils.Strncasecmp(first, "nan", 3))
        {
          charactersConsumed = 3;
          result = DoubleBinaryConstants.NaN;
          return true;
        }
        if (Utils.Strncasecmp(first, "inf", 3))
        {
          if ((last - first >= 8) && Utils.Strncasecmp(first, "infinity", 8))
          {
            charactersConsumed = 8;
            result = DoubleBinaryConstants.PositiveInfinity;
            return true;
          }
          charactersConsumed = 3;
          result = DoubleBinaryConstants.PositiveInfinity;
          return true;
        }
        if (last - first >= 4)
        {
          if (Utils.Strncasecmp(first, "+nan", 4) || Utils.Strncasecmp(first, "-nan", 4))
          {
            charactersConsumed = 4;
            result = DoubleBinaryConstants.NaN;
            return true;
          }
          if (Utils.Strncasecmp(first, "+inf", 4) ||
              Utils.Strncasecmp(first, "-inf", 4))
          {
            if ((last - first >= 9) && Utils.Strncasecmp(first + 1, "infinity", 8))
            {
              charactersConsumed = 9;
            }
            else
            {
              charactersConsumed = 4;
            }
            result = (first[0] == '-') ? DoubleBinaryConstants.NegativeInfinity : DoubleBinaryConstants.PositiveInfinity;
            return true;
          }
        }
      }
      else if (last - first >= 1)
      {
        if (Utils.Strncasecmp(first, "∞", 1))
        {
          charactersConsumed = 1;
          result = DoubleBinaryConstants.PositiveInfinity;
          return true;
        }
        if ((last - first >= 2) && Utils.Strncasecmp(first, "-∞", 2))
        {
          charactersConsumed = 2;
          result = DoubleBinaryConstants.NegativeInfinity;
          return true;
        }
      }

      return false;
    }

    internal static bool TryHandleInvalidInput(byte* first, byte* last, out int charactersConsumed, out double result)
    {
      // C# does not (yet) allow literal ASCII strings (it uses UTF-16), so
      // we need to use byte arrays.
      // "infinity"  string in ASCII, e.g., 105 = i
      ReadOnlySpan<byte> infinityString = new byte[] { 105, 110, 102, 105, 110, 105, 116, 121 };
      // "inf" string in ASCII
      ReadOnlySpan<byte> infString = new byte[] { 105, 110, 102 };
      // "+inf" string in ASCII
      ReadOnlySpan<byte> pinfString = new byte[] { 43, 105, 110, 102 };
      // "-inf" string in ASCII
      ReadOnlySpan<byte> minfString = new byte[] { 5, 105, 110, 102 };
      // "nan" string in ASCII
      ReadOnlySpan<byte> nanString = new byte[] { 110, 97, 110 };
      // "-nan" string in ASCII
      ReadOnlySpan<byte> mnanString = new byte[] { 45, 110, 97, 110 };
      // "+nan" string in ASCII
      ReadOnlySpan<byte> pnanString = new byte[] { 43, 110, 97, 110 };

      if (last - first >= 3)
      {
        if (Utils.Strncasecmp(first, nanString, 3))
        {
          charactersConsumed = 3;
          result = DoubleBinaryConstants.NaN; return true;
        }
        if (Utils.Strncasecmp(first, infString, 3))
        {
          if ((last - first >= 8) && Utils.Strncasecmp(first, infinityString, 8))
          {
            charactersConsumed = 8;
            result = DoubleBinaryConstants.PositiveInfinity; return true;
          }
          charactersConsumed = 3;
          result = DoubleBinaryConstants.PositiveInfinity; return true;
        }
        if (last - first >= 4)
        {
          if (Utils.Strncasecmp(first, pnanString, 4) || Utils.Strncasecmp(first, mnanString, 4))
          {
            charactersConsumed = 4;
            result = DoubleBinaryConstants.NaN; return true;
          }
          if (Utils.Strncasecmp(first, pinfString, 4) ||
              Utils.Strncasecmp(first, minfString, 4))
          {
            if ((last - first >= 9) && Utils.Strncasecmp(first + 1, infinityString, 8))
            {
              charactersConsumed = 9;
            }
            else
            {
              charactersConsumed = 4;
            }
            result = (first[0] == '-') ? DoubleBinaryConstants.NegativeInfinity : DoubleBinaryConstants.PositiveInfinity;
            return true;
          }
        }
      }
      charactersConsumed = 0;
      result = 0d; return false;
    }

  }



}
