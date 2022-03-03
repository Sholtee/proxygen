/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Proxy.Perf
{
    using Generators;
    using Internals;

    [MemoryDiagnoser]
    public class ProxyGenerator
    {
        public class InterfaceProxy: InterfaceInterceptor<IInterface>
        {
            public InterfaceProxy(IInterface target) : base(target)
            {
            }
        }

        private static ProxyUnitSyntaxFactory SyntaxFactory { get; } = ProxyGenerator<IInterface, InterfaceProxy>.Instance.GetSyntaxFactory();

        [Benchmark]
        public void AssemblingProxyType() => TypeEmitter.Emit(SyntaxFactory, Guid.NewGuid().ToString(), default);
    }
}
