/********************************************************************************
* IChunkFactory.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IChunkFactory
    {
        public static class Registered
        {
            public static ICollection<IChunkFactory> Entries { get; } = new ConcurrentHashSet<IChunkFactory>();
        }

        bool ShouldUse(IRuntimeContext context, string? assembly);

        SourceCode GetSourceCode(in CancellationToken cancellation);
    }
}
