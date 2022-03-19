﻿/********************************************************************************
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

        [Test]
        public void Ctor_ShouldResolveMethod()
        {
            static object InvokeTarget(object target, object[] args)
            {
                ((IList) target).Clear();
                return null;
            }

            InvocationContext cntx = new InvocationContext(Array.Empty<object>(), InvokeTarget, MemberTypes.Method);

            MethodInfo met = (MethodInfo) cntx.Member;
            Assert.That(met.Name, Is.EqualTo(nameof(IList.Clear)));
        }

        [Test]
        public void Ctor_ShouldResolveIndexer() 
        {
            static object InvokeTarget(object target, object[] args)
            {
                ((IList) target)[0] = default;
                return null;
            }

            InvocationContext cntx = new InvocationContext(Array.Empty<object>(), InvokeTarget, MemberTypes.Property);

            PropertyInfo prop = (PropertyInfo) cntx.Member;
            Assert.That(prop.Name, Is.EqualTo("Item"));
            Assert.That(cntx.Method, Is.EqualTo(prop.SetMethod));
        }

        [Test]
        public void Ctor_ShouldResolveGenericIndexer() 
        {
            static object InvokeTarget(object target, object[] args)
            {
                ((IList<string>) target)[0] = default;
                return null;
            }

            InvocationContext cntx = new InvocationContext(Array.Empty<object>(), InvokeTarget, MemberTypes.Property);

            PropertyInfo prop = (PropertyInfo) cntx.Member;
            Assert.That(prop.Name, Is.EqualTo("Item"));
            Assert.That(cntx.Method, Is.EqualTo(prop.SetMethod));
        }

        [Test]
        public void Ctor_ShouldResolveProperty()
        {
            static object InvokeTarget(object target, object[] args)
            {
                return ((IDescendant) target).Prop;
            }

            InvocationContext cntx = new InvocationContext(Array.Empty<object>(), InvokeTarget, MemberTypes.Property);

            PropertyInfo prop = (PropertyInfo) cntx.Member;
            Assert.That(prop.Name, Is.EqualTo("Prop"));
            Assert.That(cntx.Method, Is.EqualTo(prop.GetMethod));
        }

        [Test]
        public void Ctor_ShouldResolveEvent()
        {
            static object InvokeTarget(object target, object[] args)
            {
                ((IDescendant) target).Evt += _ => { };
                return null;
            }

            InvocationContext cntx = new InvocationContext(Array.Empty<object>(), InvokeTarget, MemberTypes.Event);

            EventInfo evt = (EventInfo) cntx.Member;
            Assert.That(evt.Name, Is.EqualTo("Evt"));
            Assert.That(cntx.Method, Is.EqualTo(evt.AddMethod));
        }
    }
}
