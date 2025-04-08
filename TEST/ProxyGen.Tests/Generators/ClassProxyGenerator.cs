/********************************************************************************
* ClassProxyGenerator.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Generators.Tests
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public sealed class ClassProxyGeneratorTests
    {
        public abstract class Foo(int myParam)
        {
            public abstract int Bar<T>(ref T x, out string y, in List<T> z) where T: struct;

            public virtual int Prop { get; protected set; }

            public virtual event Action Event;

            public int Param { get; } = myParam;
        }

        private sealed class FooInterceptor : IInterceptor
        {
            public IReadOnlyList<Type> GenericArguments { get; private set; }

            public object Invoke(IInvocationContext context)
            {
                GenericArguments = context.GenericArguments;
                return 1;
            }
        }

        [Test]
        public async Task GeneratedProxy_ShouldHook()
        {
            Foo proxy = await ProxyGenerator<Foo>.ActivateAsync(new FooInterceptor(), Tuple.Create(1986));

            int i = 0;

            Assert.That(proxy.Bar(ref i, out _, default), Is.EqualTo(1));
            Assert.That(proxy.Prop, Is.EqualTo(1));
        }

        [Test]
        public async Task GeneratedProxy_ShouldExposeTheGenericArguments()
        {
            FooInterceptor interceptor = new();
            Foo proxy = await ProxyGenerator<Foo>.ActivateAsync(interceptor, Tuple.Create(1986));

            int i = 0;
            proxy.Bar(ref i, out _, default);

            Assert.That(interceptor.GenericArguments.SequenceEqual([typeof(int)]));

            _ = proxy.Prop;
            Assert.That(interceptor.GenericArguments, Is.Empty);
        }
    }
}