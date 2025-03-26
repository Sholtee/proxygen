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

        private class Derived(bool returnOverride) : Base
        {
            public override ExtendedMemberInfo Prop
            {
                get
                {
                    ExtendedMemberInfo result = null;
                    return CurrentMember.Get(ref result, returnOverride)
                        ? result
                        : null;
                }
            }
        }

        [Test]
        public void Get_ShouldReturnTheCallingMember()
        {
            ExtendedMemberInfo memberInfo = new Derived(false).Prop;
            
            Assert.IsNotNull(memberInfo);
            Assert.That(memberInfo.Member, Is.EqualTo(typeof(Derived).GetProperty(nameof(Derived.Prop))));
        }

        [Test]
        public void Get_ShouldReturnTheBaseOfCallingMember()
        {
            ExtendedMemberInfo memberInfo = new Derived(true).Prop;

            Assert.IsNotNull(memberInfo);
            Assert.That(memberInfo.Member, Is.EqualTo(typeof(Base).GetProperty(nameof(Base.Prop))));
        }

        [Test]
        public void Get_ShouldThrowOnNonVirtualMember()
        {
            ExtendedMemberInfo result = null;
            Assert.Throws<InvalidOperationException>(() => CurrentMember.Get(ref result, true));
        }
    }
}
