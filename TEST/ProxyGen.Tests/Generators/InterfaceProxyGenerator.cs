/********************************************************************************
* InterfaceProxyGenerator.cs                                                    *
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

[assembly: 
    InternalsVisibleTo("Proxy_0CCCBCC05D2DAEF1EA398B60806CCA29")
#if NET5_0_OR_GREATER
    , InternalsVisibleTo("Proxy_0D009316AFE8DB66C191DEB867B3B850")
#endif
]

namespace Solti.Utils.Proxy.Generators.Tests
{
    using Generators;
    using Internals;
    using Primitives;

    [TestFixture, Parallelizable(ParallelScope.All)]
    public sealed class InterfaceProxyGeneratorTests
    {
        public interface IMyInterface
        {
            int Hooked(in int val);
            int NotHooked(int val);
        }

        public class MyInterceptor : IInterceptor
        {
            public object Invoke(IInvocationContext context)
            {
                if (context.Member.Method.Name == nameof(IMyInterface.Hooked)) return 1986;
                return context.Dispatch();
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
            IMyInterface proxy = await InterfaceProxyGenerator<IMyInterface>.ActivateAsync(new MyInterceptor(), new MyClass());

            Assert.That(proxy.NotHooked(1), Is.EqualTo(1));
            Assert.That(proxy.Hooked(1), Is.EqualTo(1986));
        }

        [Test]
        public void GeneratedProxy_ShouldBeAccessibleParallelly() => Assert.DoesNotThrowAsync(() => Task.WhenAll(100.Times(() => InterfaceProxyGenerator<IMyInterface>.ActivateAsync(new MyInterceptor(), new MyClass()))));

        [Test]
        public async Task GeneratedProxy_MayBeThreadSafe()
        {
            IMyInterface proxy = await InterfaceProxyGenerator<IMyInterface>.ActivateAsync(new MyInterceptor(), new MyClass());

            Assert.DoesNotThrow(() => Parallel.For(1, 1000, _ => proxy.Hooked(0)));
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithComplexInterfaces()
        {
            IList<string> proxy = await InterfaceProxyGenerator<IList<string>>.ActivateAsync(new MyInterceptor(), new List<string>());

            proxy.Add("Cica");

            Assert.That(proxy.Count, Is.EqualTo(1));
            Assert.That(proxy[0], Is.EqualTo("Cica"));
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithTuples()
        {
            IList<(string Foo, object Bar)> proxy = await InterfaceProxyGenerator<IList<(string Foo, object Bar)>>.ActivateAsync(new MyInterceptor(), new List<(string Foo, object Bar)>());

            Assert.DoesNotThrow(() => proxy.Add(("...", 1)));
        }

        [Test]
        public void GeneratedProxy_ShouldWorkWithGenerics() =>
            Assert.DoesNotThrowAsync(() => InterfaceProxyGenerator<IList<List<object>>>.ActivateAsync(new MyInterceptor(), new List<List<object>>()));

        public interface IMyGenericInterfaceHavingConstraint
        {
            void Foo<T>(List<T> values) where T : class, IDisposable;
            void Bar<T, TT>() where T : new() where TT : struct;
        }

        [Test]
        public void GeneratedProxy_ShouldWorkWithGenericsHavingConstraint() =>
            Assert.DoesNotThrowAsync(() => InterfaceProxyGenerator<IMyGenericInterfaceHavingConstraint>.ActivateAsync(new MyInterceptor()));

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithOverloadedProperties()
        {
            //
            // IEnumerator.Current, IEnumerator<string>.Current
            //

            using (IEnumerator<string> proxy = await InterfaceProxyGenerator<IEnumerator<string>>.ActivateAsync(new MyInterceptor(), new List<string> { "cica" }.GetEnumerator()))
            {
                Assert.That(proxy.MoveNext);
                Assert.That(proxy.Current, Is.EqualTo("cica"));
            }
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithIndexers()
        {
            IList<int>
                src = new List<int>(),
                proxy = await InterfaceProxyGenerator<IList<int>>.ActivateAsync(new MyInterceptor(), src);

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

        public class FooInterceptor : IInterceptor
        {
            public object Invoke(IInvocationContext context) => 1;
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithRefParameters()
        {
            IFoo proxy = await InterfaceProxyGenerator<IFoo>.ActivateAsync(new FooInterceptor());

            string x = string.Empty;

            Assert.That(proxy.Foo(0, out var _, ref x), Is.EqualTo(1));
        }

        internal interface IInternalInterface
        {
            int Foo();
        }

        internal class InternalInterfaceInterceptor : IInterceptor
        {
            public object Invoke(IInvocationContext context) => 1;
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithInternalTypes()
        {
            IInternalInterface proxy = await InterfaceProxyGenerator<IInternalInterface>.ActivateAsync(new InternalInterfaceInterceptor());
            Assert.That(proxy.Foo(), Is.EqualTo(1));
        }
#if NET5_0_OR_GREATER
        public interface IInterfaceContainingMembersHavingAccessibility
        {
            public void Foo();
            internal void Baz() { } // TODO: FEXME: ez torzs nelkul is valid de akkor a forditas elhasal
        }

        [Test]
        public void GeneratedProxy_ShouldWorkWithInterfaceMembersHavingAccessibility() =>
            Assert.DoesNotThrowAsync(() => InterfaceProxyGenerator<IInterfaceContainingMembersHavingAccessibility>.ActivateAsync(new MyInterceptor()));
#endif
        public class InterceptorPersistingContext : IInterceptor
        {
            public object Invoke(IInvocationContext context)
            {
                Contexts.Add(context);
                return context.Dispatch();
            }

            public List<IInvocationContext> Contexts { get; } = [];
        }

        [Test]
        public async Task GeneratedProxy_ShouldPassTheProperMethodInfo()
        {
            InterceptorPersistingContext interceptor = new();

            IList<int> proxy = await InterfaceProxyGenerator<IList<int>>.ActivateAsync(interceptor, new List<int>());
            proxy.Add(100);

            IInvocationContext context = interceptor.Contexts[0];

            Assert.That(context.Args.Length, Is.EqualTo(1));
            Assert.That(context.Args[0], Is.EqualTo(100));
            Assert.That(context.Member.Method, Is.EqualTo(MethodInfoExtractor.Extract(() => proxy.Add(default))));
            Assert.That(context.Member.Member, Is.EqualTo(context.Member.Method));
            Assert.That(context.GenericArguments, Is.Empty);
        }

        public interface IMyInterfaceHavingGenericMethod
        {
            T GenericMethod<T>(T val);
        }

        public sealed class MyInterfaceHavingGenericMethodImpl : IMyInterfaceHavingGenericMethod
        {
            public T GenericMethod<T>(T val) => val;
        }

        [Test]
        public async Task GeneratedProxy_ShouldPassTheProperGenericMethodInfo()
        {
            InterceptorPersistingContext interceptor = new();

            IMyInterfaceHavingGenericMethod proxy = await InterfaceProxyGenerator<IMyInterfaceHavingGenericMethod>.ActivateAsync
            (
                interceptor,
                new MyInterfaceHavingGenericMethodImpl()
            );
            proxy.GenericMethod(100);

            IInvocationContext context = interceptor.Contexts[0];

            Assert.That(context.Args.Length, Is.EqualTo(1));
            Assert.That(context.Args[0], Is.EqualTo(100));
            Assert.That(context.Member.Method, Is.EqualTo(MethodInfoExtractor.Extract(() => proxy.GenericMethod(100)).GetGenericMethodDefinition()));
            Assert.That(context.Member.Member, Is.EqualTo(context.Member.Method));
            Assert.That(context.GenericArguments, Is.EquivalentTo(new Type[] {typeof(int)}));
        }

        [Test]
        public async Task GeneratedProxy_ShouldPassTheProperPropertyInfo()
        {
            InterceptorPersistingContext interceptor = new();

            IList<int> proxy = await InterfaceProxyGenerator<IList<int>>.ActivateAsync(interceptor, new List<int>());

            //
            // IList.Count IS "inherited" from ICollection
            //

            _ = proxy.Count;

            IInvocationContext context = interceptor.Contexts[0];

            Assert.That(context.Args.Length, Is.EqualTo(0));

            PropertyInfo prop = PropertyInfoExtractor.Extract(() => proxy.Count);

            Assert.That(context.Member.Method, Is.EqualTo(prop.GetMethod));
            Assert.That(context.Member.Member, Is.EqualTo(prop));
            Assert.That(context.GenericArguments, Is.Empty);
        }

        [Test]
        public async Task GeneratedProxy_ShouldPassTheProperEventInfo()
        {
            InterceptorPersistingContext interceptor = new();

            IEventSource proxy = await InterfaceProxyGenerator<IEventSource>.ActivateAsync(interceptor, new EventSource());

            proxy.Event += null;

            IInvocationContext context = interceptor.Contexts[0];

            Assert.That(context.Args.Length, Is.EqualTo(1));

            EventInfo evt = typeof(IEventSource).GetEvent("Event");

            Assert.That(context.Member.Method, Is.EqualTo(evt.AddMethod));
            Assert.That(context.Member.Member, Is.EqualTo(evt));
            Assert.That(context.GenericArguments, Is.Empty);
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithGenericMethods()
        {
            IMyInterfaceHavingGenericMethod proxy = await InterfaceProxyGenerator<IMyInterfaceHavingGenericMethod>.ActivateAsync
            (
                new MyInterceptor(),
                new MyInterfaceHavingGenericMethodImpl()
            );

            Assert.That(proxy.GenericMethod(10), Is.EqualTo(10));
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
            IBar proxy = await InterfaceProxyGenerator<IBar>.ActivateAsync(new MyInterceptor(), new BarExplicit());
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

            IEventSource proxy = await InterfaceProxyGenerator<IEventSource>.ActivateAsync(new MyInterceptor(), src);

            int callCount = 0;
            proxy.Event += (s, a) => callCount++;

            src.Raise();

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public async Task GeneratedProxy_ShouldBeAbleToModifyTheInputArguments()
        {
            var mockCalculator = new Mock<ICalculator>(MockBehavior.Strict);
            mockCalculator
                .Setup(calc => calc.Add(2, 1))
                .Returns<int, int>((a, b) => a + b);

            ICalculator calculator = await InterfaceProxyGenerator<ICalculator>.ActivateAsync(new CalculatorInterceptor(), mockCalculator.Object);
            calculator.Add(0, 1); // elso parameter direkt 0

            mockCalculator.Verify(calc => calc.Add(2, 1), Times.Once);
        }

        public interface ICalculator
        {
            int Add(int a, int b);
        }

        public class CalculatorInterceptor : IInterceptor
        {
            public object Invoke(IInvocationContext context)
            {
                context.Args[0] = 2;
                return context.Dispatch();
            }
        }

        [Test]
        public void ProxyGenerator_ShouldHandleIdentifierNameCollision() =>
            Assert.DoesNotThrowAsync(() => InterfaceProxyGenerator<IInterfaceHavingNaughtyParameterNames>.GetGeneratedTypeAsync());

        public interface IInterfaceHavingNaughtyParameterNames
        {
            //
            // "result" and "args" are known identifiers in the generated type
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
            Assert.DoesNotThrowAsync(() => InterfaceProxyGenerator<IByRef<Guid>>.GetGeneratedTypeAsync());

        [Test]
        public void ProxyGenerator_ShouldWorkWithByRefObjects() =>
            Assert.DoesNotThrowAsync(() => InterfaceProxyGenerator<IByRef<object>>.GetGeneratedTypeAsync());

        [Test]
        public void ProxyGenerator_ShouldWorkWithByRefArrays() =>
            Assert.DoesNotThrowAsync(() => InterfaceProxyGenerator<IByRef<object[]>>.GetGeneratedTypeAsync());

        //
        // RandomInterfaces generikusa ne "object" legyen mert akkor tartalmazni fogja IEnumerator<object>-t
        // amit viszont ProxyGenerator_ShouldUseTheCachedAssemblyIfTheCacheDirectoryIsSet() is hasznal ezert
        // faszan osszeakadhatnak
        //

        public static IEnumerable<Type> RandomInterfaces => Proxy.Tests.RandomInterfaces<string>
            .Values
#if NET8_0_OR_GREATER
            .Except(new[] { typeof(IParsable<string>), typeof(ISpanParsable<string>) })
#endif
#if NET5_0_OR_GREATER
            .Where(iface => !iface
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Any(m => m.ReturnType.IsByRef || m.GetParameters()
                    .Any(p => p.ParameterType.IsByRefLike)))
#endif
#if NETFRAMEWORK
            .Where(iface => !iface.Name.StartsWith("_"))
#endif
            ;

        [TestCaseSource(nameof(RandomInterfaces)), Parallelizable]
        public void ProxyGenerator_ShouldWorkWith(Type iface) =>
            Assert.DoesNotThrow(() => new InterfaceProxyGenerator(iface).GetGeneratedType());

        [Test]
        public void ProxyGenerator_ShouldCacheTheGeneratedAssemblyIfCacheDirectoryIsSet()
        {
            Generator generator = InterfaceProxyGenerator<IEnumerator<Guid>>.Instance;

            string tmpDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tmp");
            Directory.CreateDirectory(tmpDir);

            string cacheFile = Path.Combine(tmpDir, $"{generator.GetDefaultAssemblyName()}.dll");

            if (File.Exists(cacheFile))
                File.Delete(cacheFile);

            generator.EmitAsync
            (
                SyntaxFactoryContext.Default with
                {
                    Config = new Config
                    (
                        new DictionaryConfigReader
                        (
                            new Dictionary<string, string>
                            {
                                [nameof(Config.AssemblyCacheDir)] = tmpDir
                            }
                        )
                    ),
                    ReferenceCollector = new ReferenceCollector()
                },
                default
            ).GetAwaiter().GetResult();

            Assert.That(File.Exists(cacheFile));
        }

        [
            Test
#if NETFRAMEWORK
            , Ignore(".NET Framework cannot load assembly targeting .NET Core")
#endif
        ]
        public async Task ProxyGenerator_ShouldUseTheCachedAssemblyIfTheCacheDirectoryIsSet()
        {
            Generator generator = InterfaceProxyGenerator<IEnumerator<object>>.Instance;

            string
                cacheDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                cacheFile = Path.Combine(cacheDir, $"{generator.GetDefaultAssemblyName()}.dll");

            Type gt = 
            (
                await generator.EmitAsync
                (
                    SyntaxFactoryContext.Default with
                    {
                        Config = new Config
                        (
                            new DictionaryConfigReader
                            (
                                new Dictionary<string, string>
                                {
                                    [nameof(Config.AssemblyCacheDir)] = cacheDir
                                }
                            )
                        ),
                        ReferenceCollector = new ReferenceCollector()
                    },
                    default
                )
            ).Type;

            Assert.That(gt.Assembly.Location, Is.EqualTo(cacheFile));
        }

        private const string WIRED_NAME = "Proxy_DDD429D5DBEE9FCDEB315DB6AFDC6605"; // amig a tipus nem valtozik addig ez sem valtozhat

        [Test]
        public void ProxyGenerator_ShouldGenerateUniqueAssemblyName()
        {
            Assert.AreEqual(WIRED_NAME, InterfaceProxyGenerator<IList<int>>.GetGeneratedType().Assembly.GetName().Name);
            Assert.AreNotEqual(WIRED_NAME, InterfaceProxyGenerator<IList<object>>.GetGeneratedType().Assembly.GetName().Name);
        }

        [Test]
        public void ProxyGenerator_ShouldAssembleTheProxyOnce() =>
            Assert.AreSame(InterfaceProxyGenerator<ICloneable>.GetGeneratedType(), InterfaceProxyGenerator<ICloneable>.GetGeneratedType());

        [Test]
        public void ProxyGenerator_ShouldAssembleTheProxyOnce2() =>
            Assert.AreSame(InterfaceProxyGenerator<IQueryable>.GetGeneratedType(), new InterfaceProxyGenerator(typeof(IQueryable)).GetGeneratedType());

        public interface IRefReturn
        {
            ref object Foo();
        }

        [Test]
        public void ProxyGenerator_ShouldThrowOnRefReturnValues() =>
            Assert.Throws<NotSupportedException>(() => InterfaceProxyGenerator<IRefReturn>.GetGeneratedType());

        public interface IRefStructUsage
        {
            void Foo(Span<int> para);
        }

        [
            Test
#if NETFRAMEWORK
            , Ignore("Ref structures are not supported in .NET Framework")
#endif
        ]
        public void ProxyGenerator_ShouldThrowOnRefStructs() =>
            Assert.Throws<NotSupportedException>(() => InterfaceProxyGenerator<IRefStructUsage>.GetGeneratedType());

        public interface IBase
        {
            void Foo();
        }

        public interface IDescendant : IBase
        {
            new void Foo();
        }

        [Test]
        public void ProxyGenerator_ShouldHandleOverrides() =>
            Assert.DoesNotThrow(() => InterfaceProxyGenerator<IDescendant>.GetGeneratedType());
    }
}
