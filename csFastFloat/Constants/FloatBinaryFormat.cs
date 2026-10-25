using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TestcsFastFloat")]

namespace csFastFloat.Constants
{
  internal static class FloatBinaryConstants
  {
    public const int MantissaExplicitBits = 23;

    public const int MinimumExponent = -127;

    public const int InfinitePower = 0xFF;

    public const int SignIndex = 31;

    public const int MinExponentFastPath = -10;

    public const int MaxExponentFastPath = 10;

    public const int MaxExponentRoundToEven = 10;

    public const int MinExponentRoundToEven = -17;

    public const ulong MaxMantissaFastPath = (ulong)2 << MantissaExplicitBits;

    public const int LargestPowerOfTen = 38;

    public const int SmallestPowerOfTen = -65;

    public const float NaN = float.NaN;

    public const float PositiveInfinity = float.PositiveInfinity;

    public const float NegativeInfinity = float.NegativeInfinity;

  }
}
