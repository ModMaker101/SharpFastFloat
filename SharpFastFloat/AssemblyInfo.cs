using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SharpFastFloat.Tests")]
[assembly: InternalsVisibleTo("SharpFastFloat.Benchmarks")]

#if NET8_0_OR_GREATER
[module: SkipLocalsInit]
#endif
