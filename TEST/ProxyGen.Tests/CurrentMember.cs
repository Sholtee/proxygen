/********************************************************************************
* CurrentMember.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

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
    }
}
