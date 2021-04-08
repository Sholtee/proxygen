/********************************************************************************
* InterfaceInterceptor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Tests
{
    [TestFixture]
    public class InterfaceInterceptorTests
    {
        private interface IBase 
        {
            int Prop { get; set; }
            event Action<EventArgs> Evt;
        }

        private interface IDescendant : IBase
        {
            void Foo();
        }

        private interface IDescendant2 : IList { }

        [Test]
        public void ResolveProperty_ShouldResolveIndexer() 
        {
            IDescendant2 i = null;

            PropertyInfo prop = InterfaceInterceptor<IDescendant2>.ResolveProperty(() => i[0] = default);
            Assert.That(prop.Name, Is.EqualTo("Item"));
        }

        [Test]
        public void ResolveProperty_ShouldResolveGenericIndexer() 
        {
            IList<string> i = null;

            PropertyInfo prop = InterfaceInterceptor<IList<string>>.ResolveProperty(() => i[0] = default);
            Assert.That(prop.Name, Is.EqualTo("Item"));
        }

        [Test]
        public void ResolveProperty_ShouldResolveProperty()
        {
            IDescendant i = null;

            PropertyInfo prop = InterfaceInterceptor<IDescendant>.ResolveProperty(() => i.Prop = default);
            Assert.That(prop.Name, Is.EqualTo("Prop"));
        }
    }
}
