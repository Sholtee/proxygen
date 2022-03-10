/********************************************************************************
* ICodeFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal interface ICodeFactory
    {
        public static class Registered
        {
            public static ICollection<ICodeFactory> Entries { get; } = new ConcurrentHashSet<ICodeFactory>();
        }

        bool ShouldUse(ITypeInfo generator);

        IEnumerable<SourceCode> GetSourceCodes(ITypeInfo generator, string? assembly, CancellationToken cancellation);

        //
        // Forrasgenerator nem bovitheti a mar meglevo referencia listat, szoval elvileg
        // a GetSourceCode() eleg is.
        //
    }
}
