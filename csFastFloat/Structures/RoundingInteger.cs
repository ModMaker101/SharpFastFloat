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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace csFastFloat.Structures
{
  // Stack-backed, little-endian limbs for exact decimal/binary midpoint comparisons.
  // Adapted from fast_float's bigint.h (MIT license). The bounded coefficient and
  // powers needed by binary32/binary64 fit within 4000 bits; reserve 4096.
  internal unsafe ref struct RoundingInteger
  {
    internal const int Capacity = 64;
    private readonly ulong* _limbs;
    internal int Length;

    internal RoundingInteger(ulong* limbs, ulong value)
    {
      _limbs = limbs;
      Length = value == 0 ? 0 : 1;
      if (Length != 0) { _limbs[0] = value; }
    }

    internal void Multiply(ulong value)
    {
      ulong carry = 0;
      for (int i = 0; i < Length; ++i)
      {
        value128 product = Utils.FullMultiplication(_limbs[i], value);
        ulong low = product.Low + carry;
        carry = product.High + (low < product.Low ? 1UL : 0UL);
        _limbs[i] = low;
      }
      if (carry != 0) { Append(carry); }
    }

    internal void Add(ulong value)
    {
      for (int i = 0; value != 0 && i < Length; ++i)
      {
        ulong previous = _limbs[i];
        _limbs[i] += value;
        value = _limbs[i] < previous ? 1UL : 0UL;
      }
      if (value != 0) { Append(value); }
    }

    internal void MultiplyPowerOfFive(int exponent)
    {
      const ulong FiveTo27 = 7450580596923828125;
      while (exponent >= 27)
      {
        Multiply(FiveTo27);
        exponent -= 27;
      }
      ulong power = 1;
      for (int i = 0; i < exponent; ++i) { power *= 5; }
      if (power != 1) { Multiply(power); }
    }

    internal void ShiftLeft(int shift)
    {
      Debug.Assert(shift >= 0);
      if (Length == 0 || shift == 0) { return; }
      int words = shift / 64;
      int bits = shift % 64;
      if (bits != 0)
      {
        ulong carry = 0;
        for (int i = 0; i < Length; ++i)
        {
          ulong previous = _limbs[i];
          _limbs[i] = (previous << bits) | carry;
          carry = previous >> (64 - bits);
        }
        if (carry != 0) { Append(carry); }
      }
      Debug.Assert(Length + words <= Capacity);
      for (int i = Length - 1; i >= 0; --i) { _limbs[i + words] = _limbs[i]; }
      for (int i = 0; i < words; ++i) { _limbs[i] = 0; }
      Length += words;
    }

    internal int Compare(ref RoundingInteger other)
    {
      if (Length != other.Length) { return Length > other.Length ? 1 : -1; }
      for (int i = Length - 1; i >= 0; --i)
      {
        if (_limbs[i] != other._limbs[i]) { return _limbs[i] > other._limbs[i] ? 1 : -1; }
      }
      return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Append(ulong value)
    {
      Debug.Assert(Length < Capacity);
      _limbs[Length++] = value;
    }
  }
}
