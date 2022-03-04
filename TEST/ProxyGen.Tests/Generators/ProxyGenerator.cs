﻿/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

[assembly: InternalsVisibleTo("Proxy_5C3FC6431B4D81D670187842F42EDDDE")]

namespace Solti.Utils.Proxy.Generators.Tests
{
    using Generators;
    using Internals;
    using Proxy.Tests.External;

    [TestFixture]
    public class ProxyGeneratorTests
    {
        public interface IMyInterface
        {
            int Hooked(in int val);
            int NotHooked(int val);
        }

        public class MyInterfaceProxy : InterfaceInterceptor<IMyInterface>
        {
            public override object Invoke(InvocationContext context)
            {
                if (context.Method.Name == nameof(Target.Hooked)) return 1986;
                return base.Invoke(context);
            }

            public MyInterfaceProxy(IMyInterface target) : base(target)
            {
            }
        }

        private sealed class MyClass : IMyInterface
        {
            public int Hooked(in int val)
            {
                return val;
            }

            public int NotHooked(int val)
            {
                return val;
            }
        }

        [Test]
        public async Task GeneratedProxy_ShouldHook()
        {
            IMyInterface proxy = await ProxyGenerator<IMyInterface, MyInterfaceProxy>.ActivateAsync(Tuple.Create((IMyInterface)new MyClass()));

            Assert.That(proxy.NotHooked(1), Is.EqualTo(1));
            Assert.That(proxy.Hooked(1), Is.EqualTo(1986));
        }

        [Test]
        public void GeneratedProxy_ShouldBeAccessibleParallelly() => Assert.DoesNotThrowAsync(() => Task.WhenAll(Enumerable
            .Repeat(0, 100)
            .Select(_ => ProxyGenerator<IMyInterface, MyInterfaceProxy>.ActivateAsync(Tuple.Create((IMyInterface)new MyClass())))));

        [Test]
        public async Task GeneratedProxy_MayBeThreadSafe()
        {
            IMyInterface proxy = await ProxyGenerator<IMyInterface, InterfaceInterceptor<IMyInterface>>.ActivateAsync(Tuple.Create((IMyInterface)new MyClass()));

            Assert.DoesNotThrow(() => Parallel.For(1, 1000, _ => proxy.Hooked(0)));
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithComplexInterfaces()
        {
            IList<string> proxy = await ProxyGenerator<IList<string>, InterfaceInterceptor<IList<string>>>.ActivateAsync(Tuple.Create((IList<string>)new List<string>()));

            proxy.Add("Cica");

            Assert.That(proxy.Count, Is.EqualTo(1));
            Assert.That(proxy[0], Is.EqualTo("Cica"));
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithTuples()
        {
            IList<(string Foo, object Bar)> proxy = await ProxyGenerator<IList<(string Foo, object Bar)>, InterfaceInterceptor<IList<(string Foo, object Bar)>>>.ActivateAsync(Tuple.Create((IList<(string Foo, object Bar)>)new List<(string Foo, object Bar)>()));

            Assert.DoesNotThrow(() => proxy.Add(("...", 1)));
        }

        [Test]
        public void GeneratedProxy_ShouldWorkWithGenerics() =>
            Assert.DoesNotThrowAsync(() => ProxyGenerator<IList<List<object>>, InterfaceInterceptor<IList<List<object>>>>.ActivateAsync(Tuple.Create((IList<List<object>>) new List<List<object>>())));

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithOverloadedProperties()
        {
            //
            // IEnumerator.Current, IEnumerator<string>.Current
            //

            using (IEnumerator<string> proxy = await ProxyGenerator<IEnumerator<string>, InterfaceInterceptor<IEnumerator<string>>>.ActivateAsync(Tuple.Create((IEnumerator<string>) new List<string> { "cica" }.GetEnumerator())))
            {
                Assert.That(proxy.MoveNext);
                Assert.That(proxy.Current, Is.EqualTo("cica"));
            }
        }

        public class ListProxy : InterfaceInterceptor<IList<int>>
        {
            public ListProxy(IList<int> target) : base(target)
            {
            }
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithIndexers()
        {
            IList<int>
                src = new List<int>(),
                proxy = await ProxyGenerator<IList<int>, ListProxy>.ActivateAsync(Tuple.Create(src));

            proxy.Add(1986);

            Assert.That(proxy.Count, Is.EqualTo(1));
            Assert.That(proxy[0], Is.EqualTo(1986));

            proxy[0]++;
            Assert.That(src[0], Is.EqualTo(1987));
        }

        public interface IFoo
        {
            int Foo<T>(int a, out string b, ref T c);
        }

        public class FooProxy : InterfaceInterceptor<IFoo>
        {
            public override object Invoke(InvocationContext context)
            {
                return 1;
            }

            public FooProxy(IFoo target) : base(target)
            {
            }
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithRefParameters()
        {
            IFoo proxy = await ProxyGenerator<IFoo, FooProxy>.ActivateAsync(Tuple.Create((IFoo) null));

            string x = string.Empty;

            Assert.That(proxy.Foo(0, out var _, ref x), Is.EqualTo(1));
        }

        internal interface IInternalInterface
        {
            int Foo();
        }

        internal class InternalInterfaceProxy : InterfaceInterceptor<IInternalInterface>
        {
            public InternalInterfaceProxy() : base(null) { }

            public override object Invoke(InvocationContext context) => 1;
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithInternalTypes()
        {
            IInternalInterface proxy = await ProxyGenerator<IInternalInterface, InternalInterfaceProxy>.ActivateAsync(null);
            Assert.That(proxy.Foo(), Is.EqualTo(1));
        }
#if !NETCOREAPP2_2
        public interface IInterfaceContainingMembersHavingAccessibility
        {
            public void Foo();
            protected void Bar() { } // TODO: FEXME: ez torzs nelkul is valid de akkor a forditas elhasal
            internal void Baz() { } // TODO: FEXME: ez torzs nelkul is valid de akkor a forditas elhasal
            private void FooBar() { } // muszaj legyen torzse
        }

        [Test]
        public void GeneratedProxy_ShouldWorkWithInterfaceMembersHavingAccessibility() =>
            Assert.DoesNotThrowAsync(() => ProxyGenerator<IInterfaceContainingMembersHavingAccessibility, InterfaceInterceptor<IInterfaceContainingMembersHavingAccessibility>>.ActivateAsync(Tuple.Create((IInterfaceContainingMembersHavingAccessibility) null)));
#endif
        public class ListProxyWithContext : InterfaceInterceptor<IList<object>>
        {
            public ListProxyWithContext() : base(new List<object>()) { }

            public override object Invoke(InvocationContext context)
            {
                Contexts.Add(context);
                return base.Invoke(context);
            }

            public List<InvocationContext> Contexts { get; } = new List<InvocationContext>();
        }

        [Test]
        public async Task GeneratedProxy_ShouldCallInvokeWithTheAppropriateArguments()
        {
            IList<object> proxy = await ProxyGenerator<IList<object>, ListProxyWithContext>.ActivateAsync(null);

            proxy.Add(100);
            _ = proxy.Count;

            ListProxyWithContext interceptor = (ListProxyWithContext)proxy;

            Assert.That(interceptor.Contexts.Count, Is.EqualTo(2));

            InvocationContext context = interceptor.Contexts[0];

            Assert.That(context.Args.Length, Is.EqualTo(1));
            Assert.That(context.Args[0], Is.EqualTo(100));
            Assert.That(context.Method, Is.EqualTo(MemberInfoExtensions.ExtractFrom(() => proxy.Add(default))));

            context = interceptor.Contexts[1];

            Assert.That(context.Args.Length, Is.EqualTo(0));

            PropertyInfo prop = (PropertyInfo)MemberInfoExtensions.ExtractFrom(() => proxy.Count);

            Assert.That(context.Method, Is.EqualTo(prop.GetMethod));
            Assert.That(context.Member, Is.EqualTo(prop));
        }

        public interface IInterfaceHavingGenericMethod
        {
            T GenericMethod<T>(in T a, object b);
        }

        public class InterfaceHavingGenericMethodProxy : InterfaceInterceptor<IInterfaceHavingGenericMethod>
        {
            public InterfaceHavingGenericMethodProxy() : base(null) { }

            public override object Invoke(InvocationContext context) => context.Args[0];
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithGenericMethods()
        {
            IInterfaceHavingGenericMethod proxy = await ProxyGenerator<IInterfaceHavingGenericMethod, InterfaceHavingGenericMethodProxy>.ActivateAsync(null);

            Assert.That(proxy.GenericMethod(10, null), Is.EqualTo(10));
        }

        public interface IBar
        {
            int Baz();
        }

        public class BarExplicit : IBar
        {
            int IBar.Baz() => 1986;
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithExplicitImplementations()
        {
            IBar proxy = await ProxyGenerator<IBar, InterfaceInterceptor<IBar>>.ActivateAsync(Tuple.Create((IBar) new BarExplicit()));
            Assert.That(proxy.Baz(), Is.EqualTo(1986));
        }

        public interface IEventSource
        {
            event EventHandler Event;
        }

        public class EventSource : IEventSource
        {
            public event EventHandler Event;
            public void Raise() => Event.Invoke(this, null);
        }

        [Test]
        public async Task GeneratedProxy_ShouldHandleEvents()
        {
            var src = new EventSource();

            IEventSource proxy = await ProxyGenerator<IEventSource, InterfaceInterceptor<IEventSource>>.ActivateAsync(Tuple.Create((IEventSource) src));

            int callCount = 0;
            proxy.Event += (s, a) => callCount++;

            src.Raise();

            Assert.That(callCount, Is.EqualTo(1));
        }

        public abstract class AbstractInterceptor : InterfaceInterceptor<IMyInterface>
        {
            public AbstractInterceptor() : base(null) { }
        }

        public class InterceptorWithPrivateCtor : InterfaceInterceptor<IMyInterface>
        {
            private InterceptorWithPrivateCtor() : base(null) { }
        }

        private class PrivateInterceptor : InterfaceInterceptor<IMyInterface>
        {
            public PrivateInterceptor() : base(null) { }
        }

        public sealed class SealedInterceptor : InterfaceInterceptor<IMyInterface>
        {
            public SealedInterceptor() : base(null) { }
        }

        [Test]
        public async Task GeneratedProxy_ShouldBeAbleToModifyTheInputArguments()
        {
            var mockCalculator = new Mock<ICalculator>(MockBehavior.Strict);
            mockCalculator
                .Setup(calc => calc.Add(2, 1))
                .Returns<int, int>((a, b) => a + b);

            ICalculator calculator = await ProxyGenerator<ICalculator, CalculatorInterceptor>.ActivateAsync(Tuple.Create(mockCalculator.Object));
            calculator.Add(0, 1); // elso parameter direkt 0

            mockCalculator.Verify(calc => calc.Add(2, 1), Times.Once);
        }

        public interface ICalculator
        {
            int Add(int a, int b);
        }

        public class CalculatorInterceptor : InterfaceInterceptor<ICalculator>
        {
            public CalculatorInterceptor(ICalculator target) : base(target)
            {
            }

            public override object Invoke(InvocationContext context)
            {
                context.Args[0] = 2;
                return base.Invoke(context);
            }
        }

        [Test]
        public void ProxyGenerator_ShouldValidate()
        {
            Assert.Throws<ArgumentException>(() => ProxyGenerator<object, InterfaceInterceptor<object>>.GetGeneratedType());
            Assert.Throws<InvalidOperationException>(() => ProxyGenerator<IMyInterface, AbstractInterceptor>.GetGeneratedType());
            Assert.Throws<InvalidOperationException>(() => ProxyGenerator<IMyInterface, SealedInterceptor>.GetGeneratedType());
            Assert.Throws<InvalidOperationException>(() => ProxyGenerator<IMyInterface, InterceptorWithPrivateCtor>.GetGeneratedType());
            Assert.Throws<MemberAccessException>(() => ProxyGenerator<IMyInterface, PrivateInterceptor>.GetGeneratedType());
        }

        [Test]
        public void ProxyGenerator_ShouldHandleInterceptorsWithMultipleCtors()
        {
            Assert.DoesNotThrowAsync(() => ProxyGenerator<IList<int>, InterceptorWithMultipleCtors>.ActivateAsync(Tuple.Create((IList<int>) new List<int>())));
            Assert.DoesNotThrowAsync(() => ProxyGenerator<IList<int>, InterceptorWithMultipleCtors>.ActivateAsync(Tuple.Create((IList<int>) new List<int>(), "cica")));
        }

        public class InterceptorWithMultipleCtors : InterfaceInterceptor<IList<int>>
        {
            public InterceptorWithMultipleCtors(IList<int> target) : base(target)
            {
            }

            public InterceptorWithMultipleCtors(IList<int> target, string cica) : this(target)
            {
            }
        }

        [Test]
        public void ProxyGenerator_ShouldHandleIdentifierNameCollision() =>
            Assert.DoesNotThrowAsync(() => ProxyGenerator<IInterfaceHavingNaughtyParameterNames, InterfaceInterceptor<IInterfaceHavingNaughtyParameterNames>>.GetGeneratedTypeAsync());

        public interface IInterfaceHavingNaughtyParameterNames
        {
            //
            // Mindket nev hasznalva van belsoleg
            //

            void Foo(int result, object[] args);
        }

        public interface IByRef<T>
        {
            void In(in T p);
            void Out(out T p);
            void Ref(ref T p);
        }

        [Test]
        public void ProxyGenerator_ShouldWorkWithByRefStructs() =>
            Assert.DoesNotThrowAsync(() => ProxyGenerator<IByRef<Guid>, InterfaceInterceptor<IByRef<Guid>>>.GetGeneratedTypeAsync());

        [Test]
        public void ProxyGenerator_ShouldWorkWithByRefObjects() =>
            Assert.DoesNotThrowAsync(() => ProxyGenerator<IByRef<object>, InterfaceInterceptor<IByRef<object>>>.GetGeneratedTypeAsync());

        [Test]
        public void ProxyGenerator_ShouldWorkWithByRefArrays() =>
            Assert.DoesNotThrowAsync(() => ProxyGenerator<IByRef<object[]>, InterfaceInterceptor<IByRef<object[]>>>.GetGeneratedTypeAsync());

        [Test]
        public void ProxyGenerator_ShouldWorkWithInterceptorFromExternalLibrary() =>
            Assert.DoesNotThrowAsync(() => ProxyGenerator<IMyInterface, ExternalInterceptor<IMyInterface>>.GetGeneratedTypeAsync());

        public static IEnumerable<Type> RandomInterfaces => Proxy.Tests.RandomInterfaces<object>.Values;

        [TestCaseSource(nameof(RandomInterfaces)), Parallelizable]
        public void ProxyGenerator_ShouldWorkWith(Type iface) =>
            Assert.DoesNotThrow(() => new ProxyGenerator(iface, typeof(InterfaceInterceptor<>).MakeGenericType(iface)).GetGeneratedType());

        [Test]
        public void ProxyGenerator_ShouldCacheTheGeneratedAssemblyIfCacheDirectoryIsSet()
        {
            Generator generator = ProxyGenerator<IEnumerator<Guid>, InterfaceInterceptor<IEnumerator<Guid>>>.Instance;

            string tmpDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tmp");
            Directory.CreateDirectory(tmpDir);

            string cacheFile = Path.Combine(tmpDir, $"{generator.GetSyntaxFactory(null).ContainingAssembly}.dll");

            if (File.Exists(cacheFile))
                File.Delete(cacheFile);

            generator.GetGeneratedTypeAsyncInternal(tmpDir).Wait();

            Assert.That(File.Exists(cacheFile));
        }

        [Test]
        public void ProxyGenerator_ShouldUseTheCachedAssemblyIfTheCacheDirectoryIsSet()
        {
            Generator generator = ProxyGenerator<IEnumerator<object>, InterfaceInterceptor<IEnumerator<object>>>.Instance;

            string
                cacheDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                cacheFile = Path.Combine(
                    cacheDir,
                    $"{generator.GetSyntaxFactory(null).ContainingAssembly}.dll");

            Assembly.LoadFile(cacheFile);

            Type gt = generator.GetGeneratedTypeAsyncInternal(cacheDir).Result;

            Assert.That(gt.Assembly.Location, Is.EqualTo(cacheFile));
        }

        private const string WIRED_NAME = "Proxy_C0A8B48E74900773E53774AC260C0EF7"; // amig a tipus nem valtozik addig ez sem valtozhat

        [Test]
        public void ProxyGenerator_ShouldGenerateUniqueAssemblyName()
        {
            Assert.AreEqual(WIRED_NAME, ProxyGenerator<IList<int>, InterfaceInterceptor<IList<int>>>.GetGeneratedType().Assembly.GetName().Name);
            Assert.AreNotEqual(WIRED_NAME, ProxyGenerator<IList<object>, InterfaceInterceptor<IList<object>>>.GetGeneratedType().Assembly.GetName().Name);
        }

        [Test]
        public void ProxyGenerator_ShouldAssembleTheProxyOnce() =>
            Assert.AreSame(ProxyGenerator<ICloneable, InterfaceInterceptor<ICloneable>>.GetGeneratedType(), ProxyGenerator<ICloneable, InterfaceInterceptor<ICloneable>>.GetGeneratedType());

        [Test]
        public void ProxyGenerator_ShouldAssembleTheProxyOnce2() =>
            Assert.AreSame(ProxyGenerator<IQueryable, InterfaceInterceptor<IQueryable>>.GetGeneratedType(), new ProxyGenerator(typeof(IQueryable), typeof(InterfaceInterceptor<IQueryable>)).GetGeneratedType());

        [Test]
        public async Task Target_MayAccessTheMostOuterEnclosingProxyInstance()
        {
            var target = new ProxyAccess();
            Assert.IsNull(target.Proxy);

            IList<object> 
                proxy1 = await ProxyGenerator<IList<object>, InterfaceInterceptor<IList<object>>>.ActivateAsync(Tuple.Create((IList<object>) target)),
                proxy2 = await ProxyGenerator<IList<object>, InterfaceInterceptor<IList<object>>>.ActivateAsync(Tuple.Create(proxy1));

            Assert.AreSame(proxy2, target.Proxy);
        }

        public class ProxyAccess : List<object>, IProxyAccess<IList<object>>
        {
            public IList<object> Proxy { get; set; }
        }
    }
}
