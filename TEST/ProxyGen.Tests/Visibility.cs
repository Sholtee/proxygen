/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
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

        public static IEnumerable<(TestDelegate, string)> NotVisibleTypes
        {
            get
            {
                yield return (static () => Visibility.Check(MetadataTypeInfo.CreateFrom(typeof(IInternalInterface)), NonAnnotatedAssembly), Resources.IVT_REQUIRED);
                yield return (static () => Visibility.Check(MetadataTypeInfo.CreateFrom(typeof(IList<IInternalInterface>)), NonAnnotatedAssembly), Resources.IVT_REQUIRED);
                yield return (static () => Visibility.Check(MetadataTypeInfo.CreateFrom(typeof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass)), NonAnnotatedAssembly), Resources.IVT_REQUIRED);
                yield return (static () => Visibility.Check(MetadataTypeInfo.CreateFrom(typeof(PrivateClass)), NonAnnotatedAssembly), Resources.TYPE_NOT_VISIBLE);
                yield return (static () => Visibility.Check(MetadataTypeInfo.CreateFrom(typeof(PrivateClass)), AnnotatedAssembly), Resources.TYPE_NOT_VISIBLE);
                yield return (static () => Visibility.Check(MetadataTypeInfo.CreateFrom(typeof(IList<PrivateClass>)), NonAnnotatedAssembly), Resources.TYPE_NOT_VISIBLE);
                yield return (static () => Visibility.Check(MetadataTypeInfo.CreateFrom(typeof(IList<PrivateClass>)), AnnotatedAssembly), Resources.TYPE_NOT_VISIBLE);
            }
        }

        [Test]
        public void Check_ShouldThrowOnInvisibleTypes([ValueSource(nameof(NotVisibleTypes))] (TestDelegate, string) check) =>
            Assert.Throws<MemberAccessException>(check.Item1, check.Item2);

        public static IEnumerable<TestDelegate> VisibleTypes
        {
            get
            {
                yield return static () => Visibility.Check(MetadataTypeInfo.CreateFrom(typeof(IInternalInterface)), AnnotatedAssembly);
                yield return static () => Visibility.Check(MetadataTypeInfo.CreateFrom(typeof(IList<IInternalInterface>)), AnnotatedAssembly);
                yield return static () => Visibility.Check(MetadataTypeInfo.CreateFrom(typeof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass)), AnnotatedAssembly);
                yield return static () => Visibility.Check(MetadataTypeInfo.CreateFrom(typeof(IList<object>)), NonAnnotatedAssembly);
            }
        }

        [Test]
        public void Check_ShouldNotThrowOnVisibleTypes([ValueSource(nameof(VisibleTypes))] TestDelegate check) => Assert.DoesNotThrow(check);

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

        public static IEnumerable<(TestDelegate, string)> NotVisibleMembers
        {
            get
            {
                yield return (static () => Visibility.Check(MetadataMethodInfo.CreateFrom(typeof(TestClass).GetMethod(nameof(TestClass.InternalMethod), BindingFlags.Instance | BindingFlags.NonPublic)), NonAnnotatedAssembly), Resources.IVT_REQUIRED);
                yield return (static () => Visibility.Check(MetadataPropertyInfo.CreateFrom(typeof(TestClass).GetProperty(nameof(TestClass.InternalProtectedProperty))), NonAnnotatedAssembly, checkGet: false, checkSet: true), Resources.IVT_REQUIRED);
                yield return (static () => Visibility.Check(MetadataMethodInfo.CreateFrom(typeof(TestClass).GetMethod("ProtectedMethod", BindingFlags.Instance | BindingFlags.NonPublic)), NonAnnotatedAssembly), Resources.METHOD_NOT_VISIBLE);
                yield return (static () => Visibility.Check(MetadataMethodInfo.CreateFrom(typeof(TestClass).GetMethod("ProtectedMethod", BindingFlags.Instance | BindingFlags.NonPublic)), AnnotatedAssembly), Resources.METHOD_NOT_VISIBLE);
                yield return (static () => Visibility.Check(MetadataPropertyInfo.CreateFrom(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic)), NonAnnotatedAssembly, checkGet: true, checkSet: false), Resources.METHOD_NOT_VISIBLE);
                yield return (static () => Visibility.Check(MetadataPropertyInfo.CreateFrom(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic)), NonAnnotatedAssembly, checkGet: false, checkSet: true), Resources.METHOD_NOT_VISIBLE);
                yield return (static () => Visibility.Check(MetadataPropertyInfo.CreateFrom(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic)), AnnotatedAssembly, checkGet: true, checkSet: false), Resources.METHOD_NOT_VISIBLE);
                yield return (static () => Visibility.Check(MetadataPropertyInfo.CreateFrom(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic)), AnnotatedAssembly, checkGet: false, checkSet: true), Resources.METHOD_NOT_VISIBLE);
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
                yield return static () => Visibility.Check(MetadataPropertyInfo.CreateFrom(typeof(TestClass).GetProperty(nameof(TestClass.InternalProtectedProperty))), NonAnnotatedAssembly, checkGet: true, checkSet: false);
                yield return static () => Visibility.Check(MetadataPropertyInfo.CreateFrom(typeof(TestClass).GetProperty(nameof(TestClass.InternalProtectedProperty))), AnnotatedAssembly, checkGet: false, checkSet: true);
                yield return static () => Visibility.Check(MetadataEventInfo.CreateFrom(typeof(TestClass).GetEvent(nameof(TestClass.PublicEvent))), NonAnnotatedAssembly, checkAdd: true, checkRemove: true);
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
