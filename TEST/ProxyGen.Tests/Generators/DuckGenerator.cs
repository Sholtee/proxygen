/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

using NUnit.Framework;

[assembly: InternalsVisibleTo("Duck_6191D0BB1603D9ADCE5DC9C7263A20D7")]
[assembly: InternalsVisibleTo("Duck_D794BCF6F9BF1A73E2F1353F68AD23BB")]

namespace Solti.Utils.Proxy.Generators.Tests
{
    using Internals;

    [TestFixture, Parallelizable(ParallelScope.All)]
    public sealed class DuckGeneratorTests
    {
        private static Task<TInterface> CreateDuck<TInterface, TTarget>(TTarget target) where TInterface : class =>
            DuckGenerator<TInterface, TTarget>.ActivateAsync(Tuple.Create(target));

        [Test]
        public async Task GeneratedDuck_ShouldWorkWithComplexInterfaces()
        {
            IList<int> proxy = await CreateDuck<IList<int>, IList<int>>(new List<int>());

            Assert.DoesNotThrow(() => proxy.Add(1986));

            Assert.That(proxy.Count, Is.EqualTo(1));
            Assert.That(proxy[0], Is.EqualTo(1986));
        }

        public interface IGeneric
        {
            T Foo<T, TT>(T a, TT b);
        }

        public class Generic 
        {
            public B Foo<B, C>(B a, C b) => default;
        }

        [Test]
        public void GeneratedDuck_ShouldWorkWithGenerics() => Assert.DoesNotThrowAsync(() => CreateDuck<IGeneric, Generic>(new Generic()));

        public interface IRef
        {
            ref object Foo(out string para);

            ref readonly object Bar();
        }

        public class Ref
        {
            private object FObject = new object();

            public ref object Foo(out string para)
            {
                para = "cica";

                return ref FObject;
            }

            public ref readonly object Bar() => ref FObject;
        }

        [Test]
        public async Task GeneratedProxy_ShouldHandleRefs()
        {
            IRef proxy = await CreateDuck<IRef, Ref>(new Ref());

            string para = null;

            Assert.DoesNotThrow(() => proxy.Foo(out para));
            Assert.That(para, Is.EqualTo("cica"));
        }

        public interface IEventSource
        {
            event EventHandler Event;
        }

        public class EventSource
        {
            public event EventHandler Event;
            public void Raise() => Event.Invoke(this, null);
        }

        [Test]
        public void GeneratedProxy_ShouldBeAccessibleParallelly() => Assert.DoesNotThrowAsync(() => Task.WhenAll(100.Times(() => CreateDuck<IRef, Ref>(new Ref()))));

        [Test]
        public async Task GeneratedProxy_ShouldHandleEvents()
        {
            var src = new EventSource();

            IEventSource proxy = await CreateDuck<IEventSource, EventSource>(src);

            int callCount = 0;
            proxy.Event += (s, a) => callCount++;

            src.Raise();

            Assert.That(callCount, Is.EqualTo(1));
        }

        internal interface IInternal
        {
            void Foo();
        }

        internal class Internal
        {
            internal void Foo() { }
        }

        [Test]
        public void GeneratedProxy_ShouldWorkWithInternalTypes() =>
            Assert.DoesNotThrowAsync(() => CreateDuck<IInternal, Internal>(new Internal()));

        public interface IBar
        {
            string Foo { get; }
            int Baz();
        }

        public interface IAnotherBar
        {
            string Foo { get; }
            int Baz();
        }

        public class AnotherBarExplicit : IAnotherBar
        {
            string IAnotherBar.Foo => "cica";
            int IAnotherBar.Baz() => 1986;
        }

        [Test]
        public async Task GeneratedProxy_ShouldWorkWithExplicitImplementations()
        {
            IBar proxy = await CreateDuck<IBar, AnotherBarExplicit>(new AnotherBarExplicit());
            Assert.That(proxy.Baz(), Is.EqualTo(1986));

            proxy = await CreateDuck<IBar, IAnotherBar>(new AnotherBarExplicit());
            Assert.That(proxy.Baz(), Is.EqualTo(1986));
        }

        private class Private : IBar
        {
            public int Baz() => 0;
            public string Foo { get; }
        }

        [Test]
        public void DuckGenerator_ShouldValidate()
        {
            Assert.ThrowsAsync<ArgumentException>(() => CreateDuck<object, object>(new object()));
            Assert.ThrowsAsync<MemberAccessException>(() => CreateDuck<IBar, Private>(new Private()));
        }

        public class MyBar
        {
            public int Bar() => 0;
            public int Baz() => 0;
            public string Foo { get; }
        }

        [Test]
        public void DuckGenerator_ShouldDistinguishByName() =>
            Assert.DoesNotThrowAsync(() => DuckGenerator<IBar, MyBar>.GetGeneratedTypeAsync());

        [Test]
        public void DuckGenerator_ShouldThrowOnAmbiguousImplementation() =>
            Assert.ThrowsAsync<AmbiguousMatchException>(() => DuckGenerator<IBar, MultipleBaz>.GetGeneratedTypeAsync());

        public class MultipleBaz : IBar
        {
            string IBar.Foo => throw new NotImplementedException();

            int IBar.Baz() => throw new NotImplementedException();

            public int Baz() => throw new NotImplementedException();
        }

        [Test]
        public void DuckGenerator_ShouldWorkWithAnonimObjects() 
        {
            var anon = new 
            {
                Cica = 1,
                Kutya = "Dénes"
            };

            // anonim objektumok mindig internal-ok
            Assert.DoesNotThrow(() => new DuckGenerator(typeof(IProps), anon.GetType()).GetGeneratedType());
        }

        public interface IProps 
        {
            int Cica { get; }
            string Kutya { get; }
        }

        [Test]
        public void DuckGenerator_ShouldWorkWithGenericTypes() =>
            Assert.DoesNotThrowAsync(() => DuckGenerator<IGeneric<int>, Generic<int>>.GetGeneratedTypeAsync());

        public interface IGeneric<T> { T Foo(); }

        public class Generic<T> 
        {
            public T Foo() => default;
        }

        [Test]
        public void DuckGenerator_ShouldCacheTheGeneratedAssemblyIfCacheDirectoryIsSet()
        {
            Generator generator = DuckGenerator<IGeneric<Guid>, Generic<Guid>>.Instance;

            string tmpDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tmp");
            Directory.CreateDirectory(tmpDir);

            string cacheFile = Path.Combine(tmpDir, $"{generator.GetDefaultAssemblyName()}.dll");

            if (File.Exists(cacheFile))
                File.Delete(cacheFile);

            generator.Emit(default, tmpDir, default);

            Assert.That(File.Exists(cacheFile));
        }

        [
            Test
#if NETFRAMEWORK
            , Ignore(".NET Framework cannot load assembly targeting .NET Core")
#endif
        ]
        public void DuckGenerator_ShouldUseTheCachedAssemblyIfTheCacheDirectoryIsSet()
        {
            Generator generator = DuckGenerator<IGeneric<object>, Generic<object>>.Instance;
            
            string
                cacheDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                cacheFile = Path.Combine(cacheDir, $"{generator.GetDefaultAssemblyName()}.dll");

            Type gt = generator.Emit(default, cacheDir, default);

            Assert.That(gt.Assembly.Location, Is.EqualTo(cacheFile));
        }

        public static IEnumerable<Type> RandomInterfaces => Proxy.Tests.RandomInterfaces<string>
            .Values
            .Except(new[] { typeof(ITypeLib2), typeof(ITypeInfo2) })
#if NET8_0_OR_GREATER
            .Except(new[] { typeof(IParsable<string>), typeof(ISpanParsable<string>) })
#endif
#if NETFRAMEWORK
            .Where(iface => !iface.Name.StartsWith("_"))
#endif
            ;

        [TestCaseSource(nameof(RandomInterfaces))]
        public void DuckGenerator_ShouldWorkWith(Type iface) =>
            Assert.DoesNotThrow(() => new DuckGenerator(iface, iface).GetGeneratedType());

        [Test]
        public void DuckGenerator_ShouldAssembleTheProxyOnce() =>
            Assert.AreSame(DuckGenerator<ICloneable, ICloneable>.GetGeneratedType(), DuckGenerator<ICloneable, ICloneable>.GetGeneratedType());

        [Test]
        public void DuckGenerator_ShouldAssembleTheProxyOnce2() =>
            Assert.AreSame(DuckGenerator<IQueryable, IQueryable>.GetGeneratedType(), new DuckGenerator(typeof(IQueryable), typeof(IQueryable)).GetGeneratedType());

#if NET8_0_OR_GREATER
        [Test]
        public void DuckGenerator_ShouldThrowInStaticAbstractMember() =>
            Assert.Throws<NotSupportedException>(() => new DuckGenerator(typeof(IUtf8SpanParsable<int>), typeof(IUtf8SpanParsable<int>)).GetGeneratedType());
#endif
    }
}
