/********************************************************************************
* IChunkFactory.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IChunkFactory
    {
        bool ShouldUse(Compilation compilation);
        SourceCode GetSourceCode(in CancellationToken cancellation);
    }
}
