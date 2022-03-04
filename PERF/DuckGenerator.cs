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

        [Benchmark]
        public void AssemblingProxyType() => TypeEmitter.Emit
        (
            DuckGenerator<IInterface, Implementation>
                .Instance
                .GetSyntaxFactory(Guid.NewGuid().ToString()),
            null, 
            default
        );
    }
}
