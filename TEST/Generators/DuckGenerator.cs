/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

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

        public void GeneratedProxy_ShouldHandleEvents() { }

        public void GeneratedProxy_ShouldWorkWithInternalTypes() { }

        public interface IBar
        {
            int Baz();
        }

        public interface IAnotherBar 
        {
            int Baz();
        }

        public class AnotherBarExplicit : IAnotherBar
        {
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
    }
}
