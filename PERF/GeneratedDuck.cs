/********************************************************************************
* GeneratedDuck.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.Proxy.Perf
{
    using Generators;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 1000000)]
    public class GeneratedDuck
    {
        private const string Param = "";

        private Implementation FOriginal;
        private IInterface FDuck;

        private static async Task<TInterface> CreateDuck<TInterface, TTarget>(TTarget target) where TInterface: class =>
            await DuckGenerator<TInterface, TTarget>.ActivateAsync(Tuple.Create(target));

        public class Implementation
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public int DoSomething(string param) => param.GetHashCode();
        }

        [GlobalSetup(Target = nameof(NoDuck))]
        public void SetupNoProxy() => FOriginal = new Implementation();

        [GlobalSetup(Target = nameof(Duck))]
        public async Task SetupThroughProxy() => FDuck = await CreateDuck<IInterface, Implementation>(new Implementation());

        [Benchmark(Baseline = true)]
        public void NoDuck() => FOriginal.DoSomething(Param);

        [Benchmark]
        public void Duck() => FDuck.DoSomething(Param);
    }
}
