using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SharpFastFloat.Tests")]
[assembly: InternalsVisibleTo("SharpFastFloat.Benchmarks")]

#if NET5_0_OR_GREATER
[module: SkipLocalsInit]
#endif
