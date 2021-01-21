/********************************************************************************
* HashCode.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
#if NETSTANDARD2_0
    internal sealed class HashCode
    {
        //
        // https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/exploration/records#characteristics-of-records
        //

        private sealed record Hasher
        {
            public Hasher? PreviousHasher { get; set; }
            public object? Value { get; set; }
        }

        private Hasher FHasher = new Hasher();

        public void Add(object? value) => FHasher = new Hasher
        {
            PreviousHasher = FHasher,
            Value = value
        };

        public int ToHashCode() => FHasher.GetHashCode();
    }
#endif
}
