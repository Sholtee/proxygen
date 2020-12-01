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

    [MemoryDiagnoser]
    public class DuckGenerator
    {
        public class Implementation 
        {
            public int DoSomething(string param) => param.GetHashCode();
        }

        [Benchmark]
        public void AssemblingDuckType() =>
            new DuckGenerator<IInterface, Implementation>().GenerateType(null, Guid.NewGuid().ToString());
    }
}
