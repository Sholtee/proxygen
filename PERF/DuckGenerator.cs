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

        private static ProxyUnitSyntaxFactory SyntaxFactory { get; } = DuckGenerator<IInterface, Implementation>.Instance.GetSyntaxFactory();

        [Benchmark]
        public void AssemblingProxyType() => TypeEmitter.Emit(SyntaxFactory, Guid.NewGuid().ToString(), default);
    }
}
