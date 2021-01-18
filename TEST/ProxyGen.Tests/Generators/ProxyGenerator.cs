/********************************************************************************
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

[assembly: InternalsVisibleTo("Generated_3414F45DAAE29C6B1A60FC2BB1653B46")]

namespace Solti.Utils.Proxy.Generators.Tests
{
    using Generators;
    using Internals;
    using Primitives;
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
            public override object Invoke(MethodInfo targetMethod, object[] args, MemberInfo extra)
            {
                if (targetMethod.Name == nameof(Target.Hooked)) return 1986;
                return base.Invoke(targetMethod, args, extra);
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

        private static async Task<TInterface> CreateProxy<TInterface, TInterceptor>(params object[] paramz) where TInterceptor : InterfaceInterceptor<TInterface> where TInterface : class
        {
            Type generated = await ProxyGenerator<TInterface, TInterceptor>.GetGeneratedTypeAsync();

            ConstructorInfo ctor;

            ConstructorInfo[] ctors = generated.GetConstructors();

            ctor = ctors.Length == 1
                ? ctors[0]
                : generated.GetConstructor(paramz.Select(p => p.GetType()).ToArray());

            return (TInterface)ctor
                .ToStaticDelegate()
                .Invoke(paramz);
        }

        [Test]
        public async Task GeneratedProxy_ShouldHook()
        {
            IMyInterface proxy = await CreateProxy<IMyInterface, MyInterfaceProxy>(new MyClass());

            Assert.That(proxy.NotHooked(1), Is.EqualTo(1));
            Assert.That(proxy.Hooked(1), Is.EqualTo(1986));
        }

        [Test]
        public void GeneratedProxy_ShouldBeAccessibleParallelly() => Assert.DoesNotThrowAsync(() => Task.WhenAll(Enumerable
            .Repeat(0, 100)
            .Select(_ => CreateProxy<IMyInterface, MyInterfaceProxy>(new MyClass()))));

        [Test]
        public async Task GeneratedProxy_MayBeThreadSafe()
        {
            IMyInterface proxy = await CreateProxy<IMyInterface, ConcurrentInterfaceInterceptor<IMyInterface>>(new MyClass());

            Assert.DoesNotThrow(() => Parallel.For(1, 1000, _ => proxy.Hooked(0)));
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithComplexInterfaces()
        {
            IList<string> proxy = await CreateProxy<IList<string>, InterfaceInterceptor<IList<string>>>(new List<string>());

            proxy.Add("Cica");

            Assert.That(proxy.Count, Is.EqualTo(1));
            Assert.That(proxy[0], Is.EqualTo("Cica"));
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithTuples()
        {
            IList<(string Foo, object Bar)> proxy = await CreateProxy<IList<(string Foo, object Bar)>, InterfaceInterceptor<IList<(string Foo, object Bar)>>>(new List<(string Foo, object Bar)>());

            Assert.DoesNotThrow(() => proxy.Add(("...", 1)));
        }

        [Test]
        public void GeneratedProxy_ShouldWorkWithGenerics() =>
            Assert.DoesNotThrowAsync(() => CreateProxy<IList<List<object>>, InterfaceInterceptor<IList<List<object>>>>(new List<List<object>>()));

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithOverloadedProperties()
        {
            //
            // IEnumerator.Current, IEnumerator<string>.Current
            //

            using (IEnumerator<string> proxy = await CreateProxy<IEnumerator<string>, InterfaceInterceptor<IEnumerator<string>>>(new List<string> { "cica" }.GetEnumerator()))
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
                proxy = await CreateProxy<IList<int>, ListProxy>(src);

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
            public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
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
            IFoo proxy = await CreateProxy<IFoo, FooProxy>((object)null);

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

            public override object Invoke(MethodInfo method, object[] args, MemberInfo extra) => 1;
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithInternalTypes()
        {
            IInternalInterface proxy = await CreateProxy<IInternalInterface, InternalInterfaceProxy>();
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
            Assert.DoesNotThrowAsync(() => CreateProxy<IInterfaceContainingMembersHavingAccessibility, InterfaceInterceptor<IInterfaceContainingMembersHavingAccessibility>>((object)null));
#endif
        public class CallContext
        {
            public MethodInfo Method;
            public object[] Args;
            public MemberInfo Member;
        }

        public class ListProxyWithContext : InterfaceInterceptor<IList<object>>
        {
            public ListProxyWithContext() : base(new List<object>()) { }

            public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
            {
                Contexts.Add(new CallContext
                {
                    Method = method,
                    Args = args,
                    Member = extra
                });
                return base.Invoke(method, args, extra);
            }

            public List<CallContext> Contexts { get; } = new List<CallContext>();
        }

        [Test]
        public async Task GeneratedProxy_ShouldCallInvokeWithTheAppropriateArguments()
        {
            IList<object> proxy = await CreateProxy<IList<object>, ListProxyWithContext>();

            proxy.Add(100);
            _ = proxy.Count;

            ListProxyWithContext interceptor = (ListProxyWithContext)proxy;

            Assert.That(interceptor.Contexts.Count, Is.EqualTo(2));

            CallContext context = interceptor.Contexts[0];

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

            public override object Invoke(MethodInfo method, object[] args, MemberInfo extra) => args[0];
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithGenericMethods()
        {
            IInterfaceHavingGenericMethod proxy = await CreateProxy<IInterfaceHavingGenericMethod, InterfaceHavingGenericMethodProxy>();

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
            IBar proxy = await CreateProxy<IBar, InterfaceInterceptor<IBar>>(new BarExplicit());
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

            IEventSource proxy = await CreateProxy<IEventSource, InterfaceInterceptor<IEventSource>>(src);

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

            ICalculator calculator = await CreateProxy<ICalculator, CalculatorInterceptor>(mockCalculator.Object);
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

            public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
            {
                args[0] = 2;
                return base.Invoke(method, args, extra);
            }
        }

        public class InterceptorWithSealedInvoke : InterfaceInterceptor<IMyInterface>
        {
            public InterceptorWithSealedInvoke() : base(null) { }

            public sealed override object Invoke(MethodInfo method, object[] args, MemberInfo extra) => base.Invoke(method, args, extra);
        }

        [Test]
        public void ProxyGenerator_ShouldValidate()
        {
            Assert.ThrowsAsync<ArgumentException>(() => CreateProxy<object, InterfaceInterceptor<object>>());
            Assert.ThrowsAsync<InvalidOperationException>(() => CreateProxy<IMyInterface, AbstractInterceptor>());
            Assert.ThrowsAsync<InvalidOperationException>(() => CreateProxy<IMyInterface, SealedInterceptor>());
            Assert.ThrowsAsync<InvalidOperationException>(() => CreateProxy<IMyInterface, InterceptorWithPrivateCtor>());
            Assert.ThrowsAsync<InvalidOperationException>(() => CreateProxy<IMyInterface, InterceptorWithSealedInvoke>());
            Assert.ThrowsAsync<MemberAccessException>(() => CreateProxy<IMyInterface, PrivateInterceptor>());
        }

        [Test]
        public void ProxyGenerator_ShouldHandleInterceptorsWithMultipleCtors()
        {
            Assert.DoesNotThrowAsync(() => CreateProxy<IList<int>, InterceptorWithMultipleCtors>(new List<int>()));
            Assert.DoesNotThrowAsync(() => CreateProxy<IList<int>, InterceptorWithMultipleCtors>(new List<int>(), "cica"));
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
            Assert.DoesNotThrowAsync(() => CreateProxy<IInterfaceHavingNaughtyParameterNames, InterfaceInterceptor<IInterfaceHavingNaughtyParameterNames>>((object)null));

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
            Assert.DoesNotThrowAsync(() => CreateProxy<IByRef<Guid>, InterfaceInterceptor<IByRef<Guid>>>((object)null));

        [Test]
        public void ProxyGenerator_ShouldWorkWithByRefObjects() =>
            Assert.DoesNotThrowAsync(() => CreateProxy<IByRef<object>, InterfaceInterceptor<IByRef<object>>>((object)null));

        [Test]
        public void ProxyGenerator_ShouldWorkWithByRefArrays() =>
            Assert.DoesNotThrowAsync(() => CreateProxy<IByRef<object[]>, InterfaceInterceptor<IByRef<object[]>>>((object)null));

        [Test]
        public void ProxyGenerator_ShouldWorkWithInterceptorFromExternalLibrary() =>
            Assert.DoesNotThrowAsync(() => CreateProxy<IMyInterface, ExternalInterceptor<IMyInterface>>((object)null, (object)null));

        public static IEnumerable<Type> RandomInterfaces => Proxy.Tests.RandomInterfaces<object>.Values;

        [TestCaseSource(nameof(RandomInterfaces)), Parallelizable]
        public void ProxyGenerator_ShouldWorkWith(Type iface) => Assert.DoesNotThrow(() =>
            typeof(ProxyGenerator<,>)
                .MakeGenericType
                (
                    iface,
                    typeof(InterfaceInterceptor<>).MakeGenericType(iface)
                )
                .InvokeMember(nameof(ProxyGenerator<object, InterfaceInterceptor<object>>.GetGeneratedType), BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod, null, null, new object[0]));

        [Test]
        public void ProxyGenerator_ShouldCacheTheGeneratedAssemblyIfCacheDirectoryIsSet()
        {
            ITypeGenerator generator = new ProxyGenerator<IEnumerator<Guid>, InterfaceInterceptor<IEnumerator<Guid>>>();

            string tmpDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tmp");
            Directory.CreateDirectory(tmpDir);

            string cacheFile = Path.Combine(tmpDir, $"{generator.TypeResolutionStrategy.ContainingAssembly}.dll");

            if (File.Exists(cacheFile))
                File.Delete(cacheFile);

            ((RuntimeCompiledTypeResolutionStrategy)generator.TypeResolutionStrategy).CacheDir = tmpDir;
            generator.TypeResolutionStrategy.Resolve();

            Assert.That(File.Exists(cacheFile));
        }

        [Test]
        public void ProxyGenerator_ShouldUseTheCachedAssemblyIfTheCacheDirectoryIsSet()
        {
            ITypeGenerator generator = new ProxyGenerator<IEnumerator<object>, InterfaceInterceptor<IEnumerator<object>>>();

            string
                cacheDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                cacheFile = Path.Combine(
                    cacheDir,
                    $"{generator.TypeResolutionStrategy.ContainingAssembly}.dll");

            ((RuntimeCompiledTypeResolutionStrategy)generator.TypeResolutionStrategy).CacheDir = cacheDir;
            Type gt = generator.TypeResolutionStrategy.Resolve();

            Assert.That(gt.Assembly.Location, Is.EqualTo(cacheFile));
        }

        private const string WIRED_NAME = "Generated_D21B2B2F800C1FCEA906362887907084"; // amig a tipus nem valtozik addig ez sem valtozhat

        [Test]
        public void ProxyGenerator_ShouldGenerateUniqueAssemblyName()
        {
            Assert.AreEqual(WIRED_NAME, new ProxyGenerator<IList<int>, InterfaceInterceptor<IList<int>>>().TypeResolutionStrategy.ContainingAssembly);
            Assert.AreNotEqual(WIRED_NAME, new ProxyGenerator<IList<object>, InterfaceInterceptor<IList<object>>>().TypeResolutionStrategy.ContainingAssembly);
        }

        [Test]
        public void ProxyGenerator_ShouldAssembleTheProxyOnce() =>
            Assert.AreSame(ProxyGenerator<ICloneable, InterfaceInterceptor<ICloneable>>.GetGeneratedType(), ProxyGenerator<ICloneable, InterfaceInterceptor<ICloneable>>.GetGeneratedType());

        [Test]
        public async Task Target_MayAccessTheMostOuterEnclosingProxyInstance()
        {
            var target = new ProxyAccess();
            Assert.IsNull(target.Proxy);

            IList<object> 
                proxy1 = await CreateProxy<IList<object>, InterfaceInterceptor<IList<object>>>(target),
                proxy2 = await CreateProxy<IList<object>, InterfaceInterceptor<IList<object>>>(proxy1);

            Assert.AreSame(proxy2, target.Proxy);
        }

        public class ProxyAccess : List<object>, IProxyAccess<IList<object>>
        {
            public IList<object> Proxy { get; set; }
        }
    }
}
