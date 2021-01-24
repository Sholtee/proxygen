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

        internal RuntimeCompiledTypeResolutionStrategy TypeResolution { get; set; }

        [GlobalSetup]
        public void Setup() => TypeResolution = (RuntimeCompiledTypeResolutionStrategy) ((ITypeGenerator) new ProxyGenerator<IInterface, InterfaceProxy>()).TypeResolutionStrategy;

        [Benchmark]
        public void AssemblingProxyType()
        {
            TypeResolution.ContainingAssembly = Guid.NewGuid().ToString();
            TypeResolution.Resolve(default);
        }
    }
}
