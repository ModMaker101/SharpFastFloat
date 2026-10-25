using SharpFastFloat.Structures;
using System;
using System.Numerics;
using Xunit;

namespace SharpFastFloat.Tests.Basic
{
  public class RoundingIntegerTests
  {
    [Fact]
    public unsafe void ArithmeticMatchesIndependentBigInteger()
    {
      ulong* storage = stackalloc ulong[RoundingInteger.Capacity];
      Random random = new Random(734);
      byte[] randomBytes = new byte[8];
      for (int trial = 0; trial < 100; ++trial)
      {
        RoundingInteger actual = new RoundingInteger(storage, ulong.MaxValue);
        BigInteger expected = ulong.MaxValue;
        for (int operation = 0; operation < 30; ++operation)
        {
          random.NextBytes(randomBytes);
          ulong value = unchecked((ulong)BitConverter.ToInt64(randomBytes, 0)) | 1;
          switch (operation % 3)
          {
            case 0: actual.Multiply(value); expected *= value; break;
            case 1: actual.Add(value); expected += value; break;
            default:
              int shift = random.Next(130);
              actual.ShiftLeft(shift);
              expected <<= shift;
              break;
          }
          AssertValue(ref actual, expected);
        }
      }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(26)]
    [InlineData(27)]
    [InlineData(28)]
    [InlineData(135)]
    [InlineData(1100)]
    public unsafe void PowersOfFiveMatchIndependentBigInteger(int exponent)
    {
      ulong* storage = stackalloc ulong[RoundingInteger.Capacity];
      RoundingInteger actual = new RoundingInteger(storage, 0x1FFFFFFFFFFFFFUL);
      actual.MultiplyPowerOfFive(exponent);
      AssertValue(ref actual, new BigInteger(0x1FFFFFFFFFFFFFUL) * BigInteger.Pow(5, exponent));
    }

    [Fact]
    public unsafe void CarryPropagatesAcrossAllOnesLimbs()
    {
      ulong* storage = stackalloc ulong[RoundingInteger.Capacity];
      RoundingInteger actual = new RoundingInteger(storage, ulong.MaxValue);
      actual.ShiftLeft(64);
      actual.Add(ulong.MaxValue);
      actual.Add(1);
      AssertValue(ref actual, BigInteger.One << 128);
    }

    private static unsafe void AssertValue(ref RoundingInteger actual, BigInteger expected)
    {
      byte[] bytes = expected.ToByteArray();
      int length = bytes.Length;
      while (length > 0 && bytes[length - 1] == 0) { --length; }
      ulong* storage = stackalloc ulong[RoundingInteger.Capacity];
      RoundingInteger reference = new RoundingInteger(storage, 0);
      reference.Length = (length + 7) / 8;
      for (int i = 0; i < reference.Length; ++i) { storage[i] = 0; }
      for (int i = 0; i < length; ++i) { storage[i / 8] |= (ulong)bytes[i] << (8 * (i % 8)); }
      Assert.Equal(0, actual.Compare(ref reference));
    }
  }
}
