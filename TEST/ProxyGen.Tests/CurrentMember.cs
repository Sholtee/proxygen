/********************************************************************************
* CurrentMember.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public sealed class CurrentMemberTests
    {
        private class Base
        {
            public virtual ExtendedMemberInfo Prop => throw new NotImplementedException();
        }

        private class Derived : Base
        {
            public override ExtendedMemberInfo Prop
            {
                get
                {
                    ExtendedMemberInfo result = null;
                    return CurrentMember.GetBase(ref result)
                        ? result
                        : null;
                }
            }
        }

        [Test]
        public void GetBase_ShouldReturnTheBaseOfCallingMember()
        {
            ExtendedMemberInfo memberInfo = new Derived().Prop;

            Assert.IsNotNull(memberInfo);
            Assert.That(memberInfo.Member, Is.EqualTo(typeof(Base).GetProperty(nameof(Base.Prop))));
        }

        [Test]
        public void GetBase_ShouldThrowOnNonVirtualMember()
        {
            ExtendedMemberInfo result = null;
            Assert.Throws<InvalidOperationException>(() => CurrentMember.GetBase(ref result));
        }

        private interface IInterface1
        {
            ExtendedMemberInfo Prop { get; }

            void AmbiguousMethod();
        }

        private interface IInterface2
        {
            void AmbiguousMethod();
        }

        private class Implementation : IInterface1, IInterface2
        {
            public ExtendedMemberInfo Prop
            {
                get
                {
                    ExtendedMemberInfo result = null;
                    CurrentMember.GetImplementedInterfaceMethod(ref result);
                    return result;
                }
            }

            public void AmbiguousMethod()
            {
                ExtendedMemberInfo result = null;
                CurrentMember.GetImplementedInterfaceMethod(ref result);
            }

            public void NonInterfaceImplementation()
            {
                ExtendedMemberInfo result = null;
                CurrentMember.GetImplementedInterfaceMethod(ref result);
            }
        }

        [Test]
        public void GetImplementedInterfaceMethod_ShouldReturnTheInterfaceMethod()
        {
            ExtendedMemberInfo memberInfo = new Implementation().Prop;
            
            Assert.That(memberInfo, Is.Not.Null);
            Assert.That(memberInfo.Member, Is.EqualTo(typeof(IInterface1).GetProperty(nameof(IInterface1.Prop))));
        }

        [Test]
        public void GetImplementedInterfaceMethod_ShouldThrowOnAmbiguousMatch() =>
            Assert.Throws<InvalidOperationException>(new Implementation().AmbiguousMethod);

        [Test]
        public void GetImplementedInterfaceMethod_ShouldThrowOnNonInterfaceImplementation() =>
            Assert.Throws<InvalidOperationException>(new Implementation().NonInterfaceImplementation);
    }
}
