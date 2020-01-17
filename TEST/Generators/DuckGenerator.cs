/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using NUnit.Framework;

[assembly: InternalsVisibleTo("Solti.Utils.Proxy.Generators.Tests.DuckGeneratorTests.Internal_Solti.Utils.Proxy.Generators.Tests.DuckGeneratorTests.IInternal_Duck")]

namespace Solti.Utils.Proxy.Generators.Tests
{
    [TestFixture]
    public sealed class DuckGeneratorTests
    {
        private static TInterface CreateDuck<TInterface, TTarget>(TTarget target) where TInterface : class =>
            (TInterface) Activator.CreateInstance(DuckGenerator<TInterface, TTarget>.GeneratedType, target);

        [Test]
        public void GeneratedDuck_ShouldWorkWithComplexInterfaces()
        {
            IList<int> proxy = CreateDuck<IList<int>, IList<int>>(new List<int>());

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
        public void GeneratedProxy_ShouldHandleRefs()
        {
            IRef proxy = CreateDuck<IRef, Ref>(new Ref());

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
        public void GeneratedProxy_ShouldHandleEvents() 
        {
            var src = new EventSource();

            IEventSource proxy = CreateDuck<IEventSource, EventSource>(src);

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
            Assert.DoesNotThrow(() => CreateDuck<IInternal, Internal>(new Internal()));

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
        public void GeneratedProxy_ShouldWorkWithExplicitImplementations()
        {
            IBar proxy = CreateDuck<IBar, AnotherBarExplicit>(new AnotherBarExplicit());
            Assert.That(proxy.Baz(), Is.EqualTo(1986));

            proxy = CreateDuck<IBar, IAnotherBar>(new AnotherBarExplicit());
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
            Assert.Throws<InvalidOperationException>(() => CreateDuck<object, object>(new object()));
            Assert.Throws<MemberAccessException>(() => CreateDuck<IBar, Private>(new Private()));
        }

        public class MyBar 
        {
            public int Bar() => 0;
            public int Baz() => 0;
        }

        [Test]
        public void DuckGenerator_ShouldDistinguishByName() =>
            Assert.DoesNotThrow(() => _ = DuckGenerator<IBar, MyBar>.GeneratedType);       
    }
}
