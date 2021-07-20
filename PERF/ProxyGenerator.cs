/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

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
        public void Setup() => TypeResolution = (RuntimeCompiledTypeResolutionStrategy) new ProxyGenerator<IInterface, InterfaceProxy>().SupportedResolutions.Single(res => res is RuntimeCompiledTypeResolutionStrategy);

        [Benchmark]
        public void AssemblingProxyType() => TypeResolution.TryResolve(Guid.NewGuid().ToString(), default);
    }
}
