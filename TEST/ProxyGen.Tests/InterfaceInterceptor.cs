using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Tests
{
    using Internals;

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
        public void ResolveMember_ShouldResolveTheMemberByToken() 
        {
            PropertyInfo prop = typeof(IDescendant).ListMembers<PropertyInfo>().Single(x => x.Name == nameof(IDescendant.Prop)); // GetProperty() nem mukodne mert az osbol kerdezzuk le
            Assert.AreSame(prop, InterfaceInterceptor<IDescendant>.ResolveMember(prop.MetadataToken));
        }

        [Test]
        public void ResolveProperty_ShouldResolveIndexer() 
        {
            IDescendant2 i = null;

            PropertyInfo prop = InterfaceInterceptor<IDescendant2>.ResolvePropertySet(() => i[0] = default);
            Assert.That(prop.Name, Is.EqualTo("Item"));
        }

        [Test]
        public void ResolveProperty_ShouldResolveGenericIndexer() 
        {
            IList<string> i = null;

            PropertyInfo prop = InterfaceInterceptor<IList<string>>.ResolvePropertySet(() => i[0] = default);
            Assert.That(prop.Name, Is.EqualTo("Item"));
        }

        [Test]
        public void ResolveProperty_ShouldResolveProperty()
        {
            IDescendant i = null;

            PropertyInfo prop = InterfaceInterceptor<IDescendant>.ResolvePropertySet(() => i.Prop = default);
            Assert.That(prop.Name, Is.EqualTo("Prop"));
        }
    }
}
