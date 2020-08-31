/********************************************************************************
* GeneratedDuck.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Proxy.Perf
{
    using Generators;
    using static Consts;

    [MemoryDiagnoser]
    public class GeneratedDuck
    {
        private const string Param = "";

        private Implementation FOriginal;
        private IInterface FDuck;

        private static async Task<TInterface> CreateDuck<TInterface, TTarget>(TTarget target) where TInterface: class =>
            (TInterface) Activator.CreateInstance(await DuckGenerator<TInterface, TTarget>.GeneratedTypeAsync, target);

        public class Implementation
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public int DoSomething(string param) => param.GetHashCode();
        }

        [GlobalSetup(Target = nameof(NoDuck))]
        public void SetupNoProxy() => FOriginal = new Implementation();

        [GlobalSetup(Target = nameof(Duck))]
        public async Task SetupThroughProxy() => FDuck = await CreateDuck<IInterface, Implementation>(new Implementation());

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void NoDuck()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FOriginal.DoSomething(Param);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Duck()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FDuck.DoSomething(Param);
            }
        }
    }
}
