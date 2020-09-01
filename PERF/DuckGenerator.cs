/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading.Tasks;

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
        public async Task AssemblingDuckType() =>
            await new DuckGenerator<IInterface, Implementation>().GenerateTypeAsync(Guid.NewGuid().ToString());
    }
}
