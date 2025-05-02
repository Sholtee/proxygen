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

        public static IEnumerable<object[]> NotVisibleTypes
        {
            get
            {
                yield return [typeof(IInternalInterface), NonAnnotatedAssembly, string.Format(Resources.IVT_REQUIRED, typeof(IInternalInterface), NonAnnotatedAssembly)];
                yield return [typeof(IList<IInternalInterface>), NonAnnotatedAssembly, string.Format(Resources.IVT_REQUIRED, typeof(IInternalInterface), NonAnnotatedAssembly)];
                yield return [typeof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass), NonAnnotatedAssembly, string.Format(Resources.IVT_REQUIRED, typeof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass), NonAnnotatedAssembly)];
                yield return [typeof(PrivateClass), NonAnnotatedAssembly, string.Format(Resources.TYPE_NOT_VISIBLE, typeof(PrivateClass))];
                yield return [typeof(PrivateClass), AnnotatedAssembly, string.Format(Resources.TYPE_NOT_VISIBLE, typeof(PrivateClass))];
                yield return [typeof(IList<PrivateClass>), NonAnnotatedAssembly, string.Format(Resources.TYPE_NOT_VISIBLE, typeof(PrivateClass))];
                yield return [typeof(IList<PrivateClass>), AnnotatedAssembly, string.Format(Resources.TYPE_NOT_VISIBLE, typeof(PrivateClass))];
                yield return [typeof(ProtectedClass), AnnotatedAssembly, string.Format(Resources.TYPE_NOT_VISIBLE, typeof(ProtectedClass))];
                yield return [typeof(ProtectedClass), NonAnnotatedAssembly, string.Format(Resources.TYPE_NOT_VISIBLE, typeof(ProtectedClass))];
                yield return [typeof(InternalProtectedClass), NonAnnotatedAssembly, string.Format(Resources.IVT_REQUIRED, typeof(InternalProtectedClass), NonAnnotatedAssembly)];
                yield return [typeof(PrivateProtectedClass), NonAnnotatedAssembly, string.Format(Resources.TYPE_NOT_VISIBLE, typeof(PrivateProtectedClass))];
                yield return [typeof(PrivateProtectedClass), AnnotatedAssembly, string.Format(Resources.TYPE_NOT_VISIBLE, typeof(PrivateProtectedClass))];
            }
        }

        [TestCaseSource(nameof(NotVisibleTypes))]
        public void Check_ShouldThrowOnInvisibleTypes(Type t, string asm, string msg)
        {
            MemberAccessException ex = Assert.Throws<MemberAccessException>(() => Visibility.Check(MetadataTypeInfo.CreateFrom(t), asm));
            Assert.That(ex.Message, Is.EqualTo(msg));
        }

        public static IEnumerable<object[]> VisibleTypes
        {
            get
            {
                yield return [typeof(IInternalInterface), AnnotatedAssembly];
                yield return [typeof(IList<IInternalInterface>), AnnotatedAssembly];
                yield return [typeof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass), AnnotatedAssembly];
                yield return [typeof(IList<object>), NonAnnotatedAssembly];
            }
        }

        [TestCaseSource(nameof(VisibleTypes))]
        public void Check_ShouldNotThrowOnVisibleTypes(Type t, string asm) =>
            Assert.DoesNotThrow(() => Visibility.Check(MetadataTypeInfo.CreateFrom(t), asm));

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

        protected class ProtectedClass
        {
        }

        internal protected class InternalProtectedClass
        {
        }

        private protected class PrivateProtectedClass
        {
        }

        public static IEnumerable<(TestDelegate, string)> NotVisibleMembers
        {
            get
            {
                yield return (static () => Visibility.Check(MetadataMethodInfo.CreateFrom(typeof(TestClass).GetMethod(nameof(TestClass.InternalMethod), BindingFlags.Instance | BindingFlags.NonPublic)), NonAnnotatedAssembly), Resources.IVT_REQUIRED);
                yield return (static () => Visibility.Check(MetadataPropertyInfo.CreateFrom(typeof(TestClass).GetProperty(nameof(TestClass.InternalProtectedProperty))), NonAnnotatedAssembly), Resources.IVT_REQUIRED);
                yield return (static () => Visibility.Check(MetadataMethodInfo.CreateFrom(typeof(TestClass).GetMethod("ProtectedMethod", BindingFlags.Instance | BindingFlags.NonPublic)), NonAnnotatedAssembly), Resources.METHOD_NOT_VISIBLE);
                yield return (static () => Visibility.Check(MetadataMethodInfo.CreateFrom(typeof(TestClass).GetMethod("ProtectedMethod", BindingFlags.Instance | BindingFlags.NonPublic)), AnnotatedAssembly), Resources.METHOD_NOT_VISIBLE);
                yield return (static () => Visibility.Check(MetadataPropertyInfo.CreateFrom(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic)), NonAnnotatedAssembly), Resources.METHOD_NOT_VISIBLE);
                yield return (static () => Visibility.Check(MetadataPropertyInfo.CreateFrom(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic)), AnnotatedAssembly), Resources.METHOD_NOT_VISIBLE);
            }
        }

        [Test]
        public void Check_ShouldThrowOnInvisibleMembers([ValueSource(nameof(NotVisibleMembers))] (TestDelegate, string) check) =>
            Assert.Throws<MemberAccessException>(check.Item1, check.Item2);

        public static IEnumerable<TestDelegate> VisibleMembers
        {
            get
            {
                yield return static () => Visibility.Check(MetadataMethodInfo.CreateFrom(typeof(TestClass).GetMethod(nameof(TestClass.InternalMethod), BindingFlags.Instance | BindingFlags.NonPublic)), AnnotatedAssembly);
                yield return static () => Visibility.Check(MetadataPropertyInfo.CreateFrom(typeof(TestClass).GetProperty(nameof(TestClass.InternalProtectedProperty))), NonAnnotatedAssembly, allowProtected: true);
                yield return static () => Visibility.Check(MetadataPropertyInfo.CreateFrom(typeof(TestClass).GetProperty(nameof(TestClass.InternalProtectedProperty))), AnnotatedAssembly, allowProtected: true);
                yield return static () => Visibility.Check(MetadataEventInfo.CreateFrom(typeof(TestClass).GetEvent(nameof(TestClass.PublicEvent))), NonAnnotatedAssembly);
            }
        }

        [Test]
        public void Check_ShouldNotThrowOnVisibleMembers([ValueSource(nameof(VisibleMembers))] TestDelegate check) => Assert.DoesNotThrow(check);

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
