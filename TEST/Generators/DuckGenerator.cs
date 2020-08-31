﻿/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

[assembly: InternalsVisibleTo("Solti.Utils.Proxy.Generators.Tests.DuckGeneratorTests.Internal_Solti.Utils.Proxy.Generators.Tests.DuckGeneratorTests.IInternal_Duck")]
[assembly: InternalsVisibleTo("<>f__AnonymousType0<System.Int32_System.String>_Solti.Utils.Proxy.Generators.Tests.DuckGeneratorTests.IProps_Duck")]

namespace Solti.Utils.Proxy.Generators.Tests
{
    [TestFixture]
    public sealed class DuckGeneratorTests
    {
        private static async Task<TInterface> CreateDuck<TInterface, TTarget>(TTarget target) where TInterface : class =>
            (TInterface) Activator.CreateInstance(await DuckGenerator<TInterface, TTarget>.GetGeneratedTypeAsync(), target);

        [Test]
        public async Task GeneratedDuck_ShouldWorkWithComplexInterfaces()
        {
            IList<int> proxy = await CreateDuck<IList<int>, IList<int>>(new List<int>());

            Assert.DoesNotThrow(() => proxy.Add(1986));

            Assert.That(proxy.Count, Is.EqualTo(1));
            Assert.That(proxy[0], Is.EqualTo(1986));
        }

        public interface IRef
        {
            ref object Foo(out string para);
        }

        public class Ref
        {
            private object FObject = new object();

            public ref object Foo(out string para)
            {
                para = "cica";

                return ref FObject;
            }
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
            Assert.ThrowsAsync<InvalidOperationException>(() => CreateDuck<object, object>(new object()));
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
            Assert.DoesNotThrowAsync(() => (Task) typeof(DuckGenerator<,>)
                .MakeGenericType(typeof(IProps), anon.GetType())
                .InvokeMember("GetGeneratedTypeAsync", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod, null, null, new object[] { default(CancellationToken) }));
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
        public async Task DuckGenerator_ShouldCacheTheGeneratedAssemblyIfCacheDirectoryIsSet()
        {
            string tmpDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tmp");
            Directory.CreateDirectory(tmpDir);

            DuckGenerator<IGeneric<Guid>, Generic<Guid>>.CacheDirectory = tmpDir;

            string cacheFile = new DuckGenerator<IGeneric<Guid>, Generic<Guid>>().CacheFile;

            if (File.Exists(cacheFile))
                File.Delete(cacheFile);

            await DuckGenerator<IGeneric<Guid>, Generic<Guid>>.GetGeneratedTypeAsync();

            Assert.That(File.Exists(cacheFile));
        }

        [Test]
        public async Task DuckGenerator_ShouldUseTheCachedAssemblyIfTheCacheDirectoryIsSet()
        {
            DuckGenerator<IGeneric<object>, Generic<object>>.CacheDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string cacheFile = new DuckGenerator<IGeneric<object>, Generic<object>>().CacheFile;

            Type gt = await DuckGenerator<IGeneric<object>, Generic<object>>.GetGeneratedTypeAsync();

            Assert.That(gt.Assembly.Location, Is.EqualTo(cacheFile));
        }
    }
}
