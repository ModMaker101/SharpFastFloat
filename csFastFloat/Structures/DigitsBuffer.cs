using csFastFloat.Constants;
using System.Diagnostics;

namespace csFastFloat.Structures
{
  internal unsafe struct DigitsBuffer
  {
    private fixed byte _digits[(int)CalculationConstants.MaxDigits];

    public byte this[int index]
    {
      get => this[(uint)index];
      set => this[(uint)index] = value;
    }

    public byte this[uint index]
    {
      get
      {
        Debug.Assert(index < CalculationConstants.MaxDigits);
        return _digits[index];
      }

      set
      {
        Debug.Assert(index < CalculationConstants.MaxDigits);
        _digits[index] = value;
      }
    }
  }
}
