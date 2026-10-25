using System.Diagnostics.CodeAnalysis;

namespace csFastFloat.Structures
{
  internal struct AdjustedMantissa
  {
    internal ulong Mantissa;
    internal int Power2; // a negative value indicates an invalid result

    public static bool operator ==(AdjustedMantissa a, AdjustedMantissa b)
            => a.Mantissa == b.Mantissa && a.Power2 == b.Power2;

    public static bool operator !=(AdjustedMantissa a, AdjustedMantissa b)
       => a.Mantissa != b.Mantissa || a.Power2 != b.Power2;

    [ExcludeFromCodeCoverage]
    public override bool Equals(object obj) => base.Equals(obj);

    [ExcludeFromCodeCoverage]
    public override int GetHashCode() => base.GetHashCode();
  }
}
