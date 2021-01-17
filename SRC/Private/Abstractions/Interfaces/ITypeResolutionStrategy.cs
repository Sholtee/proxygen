/********************************************************************************
* ITypeResolutionStrategy.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface ITypeResolutionStrategy
    {
        Type GeneratorType { get; }
        string ContainingAssembly { get; }
        bool ShouldUse { get; }
        Type Resolve(CancellationToken cancellation = default);
    }
}
