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
using System.Threading;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

[assembly: InternalsVisibleTo("Solti.Utils.Proxy.Generators.Tests.ProxyGeneratorTests.InternalInterfaceProxy_Solti.Utils.Proxy.Generators.Tests.ProxyGeneratorTests.IInternalInterface_Proxy")]

namespace Solti.Utils.Proxy.Generators.Tests
{
    using Proxy.Tests.External;
    using Internals;
    using Generators;
    using Primitives;

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

            return (TInterface) ctor
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

            PropertyInfo prop = (PropertyInfo) MemberInfoExtensions.ExtractFrom(() => proxy.Count);

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

        public class EventSource: IEventSource
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

        [Test]
        public void ProxyGenerator_ShouldValidate() 
        {
            Assert.ThrowsAsync<InvalidOperationException>(() => CreateProxy<object, InterfaceInterceptor<object>>());
            Assert.ThrowsAsync<NotSupportedException>(() => CreateProxy<IMyInterface, AbstractInterceptor>());
            Assert.ThrowsAsync<InvalidOperationException>(() => CreateProxy<IMyInterface, SealedInterceptor>());
            Assert.ThrowsAsync<InvalidOperationException>(() => CreateProxy<IMyInterface, InterceptorWithPrivateCtor>());
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
            Assert.DoesNotThrowAsync(() => CreateProxy<IInterfaceHavingNaughtyParameterNames, InterfaceInterceptor<IInterfaceHavingNaughtyParameterNames>>((object) null));

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
            Assert.DoesNotThrowAsync(() => CreateProxy<IByRef<Guid>, InterfaceInterceptor<IByRef<Guid>>>((object) null));
        
        [Test]
        public void ProxyGenerator_ShouldWorkWithByRefObjects() =>
            Assert.DoesNotThrowAsync(() => CreateProxy<IByRef<object>, InterfaceInterceptor<IByRef<object>>>((object) null));

        [Test]
        public void ProxyGenerator_ShouldWorkWithByRefArrays() =>
            Assert.DoesNotThrowAsync(() => CreateProxy<IByRef<object[]>, InterfaceInterceptor<IByRef<object[]>>>((object) null));

        [Test]
        public void ProxyGenerator_ShouldWorkWithInterceptorFromExternalLibrary() =>
            Assert.DoesNotThrowAsync(() => CreateProxy<IMyInterface, ExternalInterceptor<IMyInterface>>((object) null, (object) null));

        public static IEnumerable<Type> RandomInterfaces => typeof(object)
            .Assembly
            .GetExportedTypes()
            .Where(t => t.IsInterface && !t.ContainsGenericParameters);

        [TestCaseSource(nameof(RandomInterfaces))]
        public void ProxyGenerator_ShouldWorkWith(Type iface) => Assert.DoesNotThrow(() =>
            typeof(ProxyGenerator<,>)
                .MakeGenericType
                (
                    iface,
                    typeof(InterfaceInterceptor<>).MakeGenericType(iface)
                )
                .InvokeMember(nameof(ProxyGenerator<object, InterfaceInterceptor<object>>.GetGeneratedType), BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod, null, null, new object[] { null }));

        [Test]
        public async Task ProxyGenerator_ShouldCacheTheGeneratedAssemblyIfCacheDirectoryIsSet() 
        {
            string tmpDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tmp");
            Directory.CreateDirectory(tmpDir);

            string cacheFile = Path.Combine(tmpDir, new ProxyGenerator<IEnumerator<Guid>, InterfaceInterceptor<IEnumerator<Guid>>>().CacheFileName);

            if (File.Exists(cacheFile))
                File.Delete(cacheFile);
           
            await ProxyGenerator<IEnumerator<Guid>, InterfaceInterceptor<IEnumerator<Guid>>>.GetGeneratedTypeAsync(tmpDir);

            Assert.That(File.Exists(cacheFile));               
        }

        [Test]
        public async Task ProxyGenerator_ShouldUseTheCachedAssemblyIfTheCacheDirectoryIsSet() 
        {
            string 
                cacheDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                cacheFile = Path.Combine(
                    cacheDir,
                    new ProxyGenerator<IEnumerator<object>, InterfaceInterceptor<IEnumerator<object>>>().CacheFileName);

            Type gt = await ProxyGenerator<IEnumerator<object>, InterfaceInterceptor<IEnumerator<object>>>.GetGeneratedTypeAsync(cacheDir);

            Assert.That(gt.Assembly.Location, Is.EqualTo(cacheFile));
        }
    }
}
