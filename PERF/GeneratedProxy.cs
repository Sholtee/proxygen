/********************************************************************************
* GeneratedProxy.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
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

        #region Helper classes
        public class Implementation : IInterface
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            int IInterface.DoSomething(string param) => 0;
        }

        private sealed class InterceptorCallingTheTarget : IInterceptor
        {
            public object Invoke(IInvocationContext context) => context.Dispatch();
        }

        private sealed class InterceptorNotCallingTheTarget: IInterceptor
        {
            public object Invoke(IInvocationContext context) => 1;
        }

        public class DispatchProxyWithTarget : DispatchProxy // DispatchProxy cannot support target
        {
            protected override object Invoke(MethodInfo targetMethod, object[] args) => 0;
        }
        #endregion

        [GlobalSetup(Target = nameof(NoProxy))]
        public void SetupNoProxy() => FInstance = new Implementation();

        [GlobalSetup(Target = nameof(ProxyWithTarget))]
        public async Task SetupProxy() => FInstance = await InterfaceProxyGenerator<IInterface>.ActivateAsync(new InterceptorCallingTheTarget(), new Implementation());

        [GlobalSetup(Target = nameof(ProxyWithoutTarget))]
        public async Task SetupProxyWithoutTarget() => FInstance = await InterfaceProxyGenerator<IInterface>.ActivateAsync(new InterceptorNotCallingTheTarget());

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
