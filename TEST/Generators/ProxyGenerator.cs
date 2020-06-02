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

using NUnit.Framework;

[assembly: InternalsVisibleTo("Solti.Utils.Proxy.Generators.Tests.ProxyGeneratorTests.InternalInterfaceProxy_Solti.Utils.Proxy.Generators.Tests.ProxyGeneratorTests.IInternalInterface_Proxy")]

namespace Solti.Utils.Proxy.Generators.Tests
{
    using Internals;
    using Generators;
    
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

        private static TInterface CreateProxy<TInterface, TInterceptor>(params object[] paramz) where TInterceptor : InterfaceInterceptor<TInterface> where TInterface : class =>
            (TInterface) Activator.CreateInstance(ProxyGenerator<TInterface, TInterceptor>.GeneratedType, paramz);

        [Test]
        public void GeneratedProxy_ShouldHook()
        {
            IMyInterface proxy = CreateProxy<IMyInterface, MyInterfaceProxy>(new MyClass());

            Assert.That(proxy.NotHooked(1), Is.EqualTo(1));
            Assert.That(proxy.Hooked(1), Is.EqualTo(1986));
        }

        [Test]
        public void GeneratedProxy_ShouldWorkWithComplexInterfaces()
        {
            IList<string> proxy = CreateProxy<IList<string>, InterfaceInterceptor<IList<string>>>(new List<string>());

            proxy.Add("Cica");

            Assert.That(proxy.Count, Is.EqualTo(1));
            Assert.That(proxy[0], Is.EqualTo("Cica"));
        }

        [Test]
        public void GeneratedProxy_ShouldWorkWithOverloadedProperties()
        {
            //
            // IEnumerator.Current, IEnumerator<string>.Current
            //

            using (IEnumerator<string> proxy = CreateProxy<IEnumerator<string>, InterfaceInterceptor<IEnumerator<string>>>(new List<string> { "cica" }.GetEnumerator()))
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
        public void GeneratedProxy_ShouldWorkWithIndexers()
        {
            IList<int>
                src = new List<int>(),
                proxy = CreateProxy<IList<int>, ListProxy>(src);

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
        public void GeneratedProxy_ShouldWorkWithRefParameters()
        {
            IFoo proxy = CreateProxy<IFoo, FooProxy>((object)null);

            string x = string.Empty;

            Assert.That(proxy.Foo(0, out var a, ref x), Is.EqualTo(1));
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
        public void GeneratedProxy_ShouldWorkWithInternalTypes()
        {
            IInternalInterface proxy = CreateProxy<IInternalInterface, InternalInterfaceProxy>();
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
        public void GeneratedProxy_ShouldCallInvokeWithTheAppropriateArguments()
        {
            IList<object> proxy = CreateProxy<IList<object>, ListProxyWithContext>();

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
        public void GeneratedProxy_ShouldWorkWithGenericMethods() 
        {
            IInterfaceHavingGenericMethod proxy = CreateProxy<IInterfaceHavingGenericMethod, InterfaceHavingGenericMethodProxy>();

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
        public void GeneratedProxy_ShouldWorkWithExplicitImplementations() 
        {
            IBar proxy = CreateProxy<IBar, InterfaceInterceptor<IBar>>(new BarExplicit());
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
        public void GeneratedProxy_ShouldHandleEvents()
        {
            var src = new EventSource();

            IEventSource proxy = CreateProxy<IEventSource, InterfaceInterceptor<IEventSource>>(src);

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
        public void ProxyGenerator_ShouldValidate() 
        {
            Assert.Throws<InvalidOperationException>(() => CreateProxy<object, InterfaceInterceptor<object>>());
            Assert.Throws<NotSupportedException>(() => CreateProxy<IMyInterface, AbstractInterceptor>());
            Assert.Throws<InvalidOperationException>(() => CreateProxy<IMyInterface, SealedInterceptor>());
            Assert.Throws<InvalidOperationException>(() => CreateProxy<IMyInterface, InterceptorWithPrivateCtor>());
            Assert.Throws<MemberAccessException>(() => CreateProxy<IMyInterface, PrivateInterceptor>());
        }

        [Test]
        public void ProxyGenerator_ShouldHandleInterceptorsWithMultipleCtors() 
        {
            Assert.DoesNotThrow(() => CreateProxy<IList<int>, InterceptorWithMultipleCtors>(new List<int>()));
            Assert.DoesNotThrow(() => CreateProxy<IList<int>, InterceptorWithMultipleCtors>(new List<int>(), "cica"));
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
            Assert.DoesNotThrow(() => CreateProxy<IInterfaceHavingNaughtyParameterNames, InterfaceInterceptor<IInterfaceHavingNaughtyParameterNames>>((object) null));

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
            Assert.DoesNotThrow(() => CreateProxy<IByRef<Guid>, InterfaceInterceptor<IByRef<Guid>>>((object) null));
        
        [Test]
        public void ProxyGenerator_ShouldWorkWithByRefObjects() =>
            Assert.DoesNotThrow(() => CreateProxy<IByRef<object>, InterfaceInterceptor<IByRef<object>>>((object) null));

        [Test]
        public void ProxyGenerator_ShouldWorkWithByRefArrays() =>
            Assert.DoesNotThrow(() => CreateProxy<IByRef<object[]>, InterfaceInterceptor<IByRef<object[]>>>((object) null));

        public static IEnumerable<Type> RandomInterfaces => typeof(object)
            .Assembly
            .GetExportedTypes()
            .Where(t => t.IsInterface && !t.ContainsGenericParameters);

        [TestCaseSource(nameof(RandomInterfaces))]
        public void ProxyGenerator_ShouldWorkWith(Type iface) =>
            typeof(ProxyGenerator<,>)
                .MakeGenericType
                (
                    iface,
                    typeof(InterfaceInterceptor<>).MakeGenericType(iface)
                )
                .GetProperty("GeneratedType", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .GetValue(null);

        [Test]
        public void ProxyGenerator_ShouldCacheTheGeneratedAssemblyIfCacheDirectoryIsSet() 
        {
            string tmpDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tmp");
            Directory.CreateDirectory(tmpDir);

            ProxyGenerator<IEnumerator<object>, InterfaceInterceptor<IEnumerator<object>>>.CacheDirectory = tmpDir;

            string cacheFile = new ProxyGenerator<IEnumerator<object>, InterfaceInterceptor<IEnumerator<object>>>().CacheFile;
            cacheFile = Path.Combine(tmpDir, cacheFile);

            if (File.Exists(cacheFile))
                File.Delete(cacheFile);
           
            _ = ProxyGenerator<IEnumerator<object>, InterfaceInterceptor<IEnumerator<object>>>.GeneratedType;

            Assert.That(File.Exists(cacheFile));               
        }
    }
}
