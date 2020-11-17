/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

using NUnit.Framework;

[assembly: InternalsVisibleTo(Solti.Utils.Proxy.Internals.Tests.VisibilityTests.AnnotatedAssembly)]

namespace Solti.Utils.Proxy.Internals.Tests
{
    using Properties;

    [TestFixture]
    public class VisibilityTests
    {
        public const string
            NonAnnotatedAssembly = nameof(NonAnnotatedAssembly),
            AnnotatedAssembly = nameof(AnnotatedAssembly);

        [Test]
        public void Check_ShouldThrowIfTheTypeNotVisible()
        {
            Assert.Throws<MemberAccessException>(() => Visibility.Check(typeof(IInternalInterface), NonAnnotatedAssembly), Resources.TYPE_NOT_VISIBLE);
            Assert.DoesNotThrow(() => Visibility.Check(typeof(IInternalInterface), AnnotatedAssembly));
            Assert.Throws<MemberAccessException>(() => Visibility.Check(typeof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass), NonAnnotatedAssembly), Resources.TYPE_NOT_VISIBLE);
            Assert.DoesNotThrow(() => Visibility.Check(typeof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass), AnnotatedAssembly));
            Assert.Throws<MemberAccessException>(() => Visibility.Check(typeof(PrivateClass), NonAnnotatedAssembly), Resources.TYPE_NOT_VISIBLE);
            Assert.Throws<MemberAccessException>(() => Visibility.Check(typeof(PrivateClass), AnnotatedAssembly), Resources.TYPE_NOT_VISIBLE);
           // Assert.DoesNotThrow(() => Visibility.Check(typeof(IList<>), NonAnnotatedAssembly));
            Assert.DoesNotThrow(() => Visibility.Check(typeof(IList<object>), NonAnnotatedAssembly));
        }

        internal interface IInternalInterface { }

        public class PublicClassWithInternalMethodAndNestedType
        {
            internal class InternalNestedClass
            {
            }
        }

        private class PrivateClass
        {
        }

        [Test]
        public void Check_ShouldThrowIfTheMemberNotVisible()
        {
            Assert.Throws<MemberAccessException>(() => Visibility.Check(typeof(TestClass).GetMethod(nameof(TestClass.InternalMethod), BindingFlags.Instance | BindingFlags.NonPublic), NonAnnotatedAssembly), Resources.IVT_REQUIRED);
            Assert.DoesNotThrow(() => Visibility.Check(typeof(TestClass).GetMethod(nameof(TestClass.InternalMethod), BindingFlags.Instance | BindingFlags.NonPublic), AnnotatedAssembly));

            Assert.DoesNotThrow(() => Visibility.Check(typeof(TestClass).GetProperty(nameof(TestClass.InternalProtectedProperty)), NonAnnotatedAssembly, checkGet: true, checkSet: false));
            Assert.Throws<MemberAccessException>(() => Visibility.Check(typeof(TestClass).GetProperty(nameof(TestClass.InternalProtectedProperty)), NonAnnotatedAssembly, checkGet: false, checkSet: true), Resources.IVT_REQUIRED);
            Assert.DoesNotThrow(() => Visibility.Check(typeof(TestClass).GetProperty(nameof(TestClass.InternalProtectedProperty)), AnnotatedAssembly, checkGet: false, checkSet: true));

            Assert.DoesNotThrow(() => Visibility.Check(typeof(TestClass).GetEvent(nameof(TestClass.PublicEvent)), NonAnnotatedAssembly, checkAdd: true, checkRemove: true));

            Assert.Throws<MemberAccessException>(() => Visibility.Check(typeof(TestClass).GetMethod("ProtectedMethod", BindingFlags.Instance | BindingFlags.NonPublic), NonAnnotatedAssembly), Resources.METHOD_NOT_VISIBLE);
            Assert.Throws<MemberAccessException>(() => Visibility.Check(typeof(TestClass).GetMethod("ProtectedMethod", BindingFlags.Instance | BindingFlags.NonPublic), AnnotatedAssembly), Resources.METHOD_NOT_VISIBLE);

            Assert.Throws<MemberAccessException>(() => Visibility.Check(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic), NonAnnotatedAssembly, checkGet: true, checkSet: false), Resources.METHOD_NOT_VISIBLE);
            Assert.Throws<MemberAccessException>(() => Visibility.Check(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic), NonAnnotatedAssembly, checkGet: false, checkSet: true), Resources.METHOD_NOT_VISIBLE);
            Assert.Throws<MemberAccessException>(() => Visibility.Check(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic), AnnotatedAssembly, checkGet: true, checkSet: false), Resources.METHOD_NOT_VISIBLE);
            Assert.Throws<MemberAccessException>(() => Visibility.Check(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic), AnnotatedAssembly, checkGet: false, checkSet: true), Resources.METHOD_NOT_VISIBLE);
        }

        public class TestClass 
        {
            internal void InternalMethod() { }
            public int InternalProtectedProperty { get; internal protected set; }
            public event EventHandler PublicEvent;
            protected void ProtectedMethod() { }
            private int PrivateProperty { get; set; }
        }
    }
}
