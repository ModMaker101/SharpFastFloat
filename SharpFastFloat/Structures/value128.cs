using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SharpFastFloat.Tests")]

namespace SharpFastFloat.Structures
{
  internal struct value128
  {
    public ulong Low;
    public ulong High;

    public value128(ulong h, ulong l) : this()
    {
      High = h;
      Low = l;
    }
  }
}
