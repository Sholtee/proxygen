/********************************************************************************
* GeneratedProxy.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.Proxy.Perf
{
    using Generators;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 5000000)]
    public class GeneratedProxy
    {
        private const string Param = "";

        private IInterface FInstance;

        private static async Task<TInterface> CreateProxy<TInterface, TInterceptor>(ITuple paramz) where TInterceptor : InterfaceInterceptor<TInterface> where TInterface : class =>
            await ProxyGenerator<TInterface, TInterceptor>.ActivateAsync(paramz);

        #region Helper classes
        public class Implementation : IInterface
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            int IInterface.DoSomething(string param) => 0;
        }

        public class InterfaceProxyWithTarget : InterfaceInterceptor<IInterface>
        {
            public InterfaceProxyWithTarget(IInterface target) : base(target)
            {
            }
        }

        public class InterfaceProxyWithoutTarget : InterfaceInterceptor<IInterface>
        {
            public InterfaceProxyWithoutTarget() : base(null)
            {
            }
            public override object Invoke(InterfaceInvocationContext context) => 0;
        }

        public class DispatchProxyWithTarget : DispatchProxy // DispatchProxy cannot support target
        {
            protected override object Invoke(MethodInfo targetMethod, object[] args) => 0;
        }
        #endregion

        [GlobalSetup(Target = nameof(NoProxy))]
        public void SetupNoProxy() => FInstance = new Implementation();

        [GlobalSetup(Target = nameof(ProxyWithTarget))]
        public async Task SetupProxy() => FInstance = await CreateProxy<IInterface, InterfaceProxyWithTarget>(Tuple.Create((IInterface) new Implementation()));

        [GlobalSetup(Target = nameof(ProxyWithoutTarget))]
        public async Task SetupProxyWithoutTarget() => FInstance = await CreateProxy<IInterface, InterfaceProxyWithoutTarget>(null);

        [GlobalSetup(Target = nameof(DispatchProxyWithoutTarget))]
        public void SetupDispatchProxyWithoutTarget() => FInstance = DispatchProxy.Create<IInterface, DispatchProxyWithTarget>();

        [Benchmark(Baseline = true)]
        public int NoProxy() => FInstance.DoSomething(Param);

        [Benchmark]
        public int ProxyWithTarget() => FInstance.DoSomething(Param);

        [Benchmark]
        public int ProxyWithoutTarget() => FInstance.DoSomething(Param);

        [Benchmark]
        public int DispatchProxyWithoutTarget() => FInstance.DoSomething(Param);
    }
}
