using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TestcsFastFloat")]

namespace csFastFloat.Constants
{

  internal static class DoubleBinaryConstants
  {

    public const int MantissaExplicitBits = 52;

    public const int MinimumExponent = -1023;

    public const int InfinitePower = 0x7FF;

    public const int SignIndex = 63;

    public const int MinExponentFastPath = -22;

    public const int MaxExponentFastPath = 22;

    public const int MaxExponentRoundToEven = 23;

    public const int MinExponentRoundToEven = -4;

    public const ulong MaxMantissaFastPath = (ulong)2 << 52;

    public const int LargestPowerOfTen = 308;

    public const int SmallestPowerOfTen = -342;

    public const double NaN = double.NaN;

    public const double PositiveInfinity = double.PositiveInfinity;

    public const double NegativeInfinity = double.NegativeInfinity;

  }
}

