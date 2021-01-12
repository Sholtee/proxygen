/********************************************************************************
* ITypeResolutionStrategy.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    using Abstractions;

    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface ITypeResolutionStrategy
    {
        OutputType Type { get; }
        ITypeGenerator Generator { get; }
        string AssemblyName { get; }
        bool ShouldUse { get; }
        Type Resolve(CancellationToken cancellation = default);
    }
}
