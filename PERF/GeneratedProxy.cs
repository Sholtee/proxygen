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

namespace Solti.Utils.Proxy.Perf
{
    using Generators;
    using static Consts;

    [MemoryDiagnoser]
    public class GeneratedProxy
    {
        private const string Param = "";

        private IInterface FInstance;

        private static async Task<TInterface> CreateProxy<TInterface, TInterceptor>(params object[] paramz) where TInterceptor : InterfaceInterceptor<TInterface> where TInterface : class =>
            (TInterface) Activator.CreateInstance(await ProxyGenerator<TInterface, TInterceptor>.GeneratedTypeAsync, paramz);

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
            public override object Invoke(MethodInfo method, object[] args, MemberInfo extra) => 0;
        }
        #endregion

        [GlobalSetup(Target = nameof(NoProxy))]
        public void SetupNoProxy() => FInstance = new Implementation();

        [GlobalSetup(Target = nameof(ProxyWithTarget))]
        public async Task SetupProxy() => FInstance = await CreateProxy<IInterface, InterfaceProxyWithTarget>(new Implementation());

        [GlobalSetup(Target = nameof(ProxyWithoutTarget))]
        public async Task SetupProxyWithoutTarget() => FInstance = await CreateProxy<IInterface, InterfaceProxyWithoutTarget>();

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void NoProxy()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FInstance.DoSomething(Param);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void ProxyWithTarget()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FInstance.DoSomething(Param);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void ProxyWithoutTarget()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                FInstance.DoSomething(Param);
            }
        }
    }
}
