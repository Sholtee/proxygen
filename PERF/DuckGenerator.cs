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
    using static Consts;

    [MemoryDiagnoser]
    public class DuckGenerator
    {
        public class Implementation 
        {
            public int DoSomething(string param) => param.GetHashCode();
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void AssemblingProxyType()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                new DuckGenerator<IInterface, Implementation>().GenerateType(Guid.NewGuid().ToString());
            }
        }
    }
}
