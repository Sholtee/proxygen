/********************************************************************************
* InvocationContext.cs                                                          *
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
    public class InvocationContextTests
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
        public void Ctor_ShouldResolveIndexer() 
        {
            IDescendant2 i = null;

            InvocationContext cntx = new(Array.Empty<object>(), () => i[0] = default, MemberTypes.Property);

            PropertyInfo prop = (PropertyInfo) cntx.Member;
            Assert.That(prop.Name, Is.EqualTo("Item"));
            Assert.That(cntx.Method, Is.EqualTo(prop.SetMethod));
        }

        [Test]
        public void Ctor_ShouldResolveGenericIndexer() 
        {
            IList<string> i = null;

            InvocationContext cntx = new(Array.Empty<object>(), () => i[0] = default, MemberTypes.Property);

            PropertyInfo prop = (PropertyInfo) cntx.Member;
            Assert.That(prop.Name, Is.EqualTo("Item"));
            Assert.That(cntx.Method, Is.EqualTo(prop.SetMethod));
        }

        [Test]
        public void Ctor_ShouldResolveProperty()
        {
            IDescendant i = null;

            InvocationContext cntx = new(Array.Empty<object>(), () => _ =  i.Prop, MemberTypes.Property);

            PropertyInfo prop = (PropertyInfo) cntx.Member;
            Assert.That(prop.Name, Is.EqualTo("Prop"));
            Assert.That(cntx.Method, Is.EqualTo(prop.GetMethod));
        }

        [Test]
        public void Ctor_ShouldResolveEvent()
        {
            IDescendant i = null;

            InvocationContext cntx = new(Array.Empty<object>(), () => { i.Evt += _ => { }; return null; }, MemberTypes.Event);

            EventInfo evt = (EventInfo) cntx.Member;
            Assert.That(evt.Name, Is.EqualTo("Evt"));
            Assert.That(cntx.Method, Is.EqualTo(evt.AddMethod));
        }
    }
}
