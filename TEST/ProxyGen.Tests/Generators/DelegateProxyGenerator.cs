/********************************************************************************
* DelegateProxyGenerator.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Generators.Tests
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public sealed class DelegateProxyGeneratorTests
    {
        public delegate int MyDelegate<T>(string a, ref T[] b, out object c);

        private sealed class Interceptor : IInterceptor
        {
            public IInvocationContext Context { get; private set; }

            public object Invoke(IInvocationContext context)
            {
                Context = context;
                return 1986;
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        private sealed class MyAnnotationAttribute : Attribute { }

        [Test]
        public async Task GeneratedProxy_ShouldHook()
        {
            Interceptor interceptor = new();
            MyDelegate<int> proxy = await DelegateProxyGenerator<MyDelegate<int>>.ActivateAsync(interceptor, (string a, ref int[] b, out object c) => { c = null; return 0; });

            int[] ar = [];
            Assert.That(proxy("cica", ref ar , out object x), Is.EqualTo(1986));
            Assert.That(interceptor.Context.Args[0], Is.EqualTo("cica"));
            Assert.That(interceptor.Context.Args[1], Is.SameAs(ar));
            Assert.That(interceptor.Context.Args[2], Is.Null);
        }

        [Test]
        public async Task GeneratedProxy_ShouldExposeTheTargetMember()
        {
            Interceptor interceptor = new();
            MyDelegate<int> proxy = await DelegateProxyGenerator<MyDelegate<int>>.ActivateAsync(interceptor, [MyAnnotation] (string a, ref int[] b, out object c) => { c = null; return 0; });

            int[] ar = [];
            proxy("cica", ref ar, out object x);

            Assert.That(interceptor.Context.Member.Method.GetCustomAttribute<MyAnnotationAttribute>(), Is.Not.Null);
        }
    }
}