/********************************************************************************
* DuckGenerator.cs                                                              *
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
    public class DuckGenerator
    {
        public class Implementation
        {
            public int DoSomething(string param) => param.GetHashCode();
        }

        internal RuntimeCompiledTypeResolutionStrategy TypeResolution { get; set; }

        [GlobalSetup]
        public void Setup() => TypeResolution = (RuntimeCompiledTypeResolutionStrategy) new DuckGenerator<IInterface, Implementation>().SupportedResolutions.Single(res => res is RuntimeCompiledTypeResolutionStrategy);

        [Benchmark]
        public void AssemblingProxyType() => TypeResolution.TryResolve(Guid.NewGuid().ToString(), default);
    }
}
