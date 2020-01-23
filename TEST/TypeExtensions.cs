/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    using Properties;

    [TestFixture]
    public sealed class TypeExtensionsTests
    {
        [Test]
        public void ListMembers_ShouldReturnOverLoadedInterfaceMembers() 
        {
            MethodInfo[] methods = typeof(IEnumerable<string>).ListMembers<MethodInfo>().ToArray();
            Assert.That(methods.Length, Is.EqualTo(2));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IEnumerable<string>>(i => i.GetEnumerator())));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IEnumerable>(i => i.GetEnumerator())));

            PropertyInfo[] properties = typeof(IEnumerator<string>).ListMembers<PropertyInfo>().ToArray();
            Assert.That(properties.Length, Is.EqualTo(2));
            Assert.That(properties.Contains((PropertyInfo) MemberInfoExtensions.ExtractFrom<IEnumerator<string>>(i => i.Current)));
            Assert.That(properties.Contains((PropertyInfo) MemberInfoExtensions.ExtractFrom<IEnumerator>(i => i.Current)));
        }

        [Test]
        public void ListMembers_ShouldReturnExplicitImplementations() 
        {
            MethodInfo[] methods = typeof(List<int>).ListMembers<MethodInfo>(includeNonPublic: true).ToArray();

            Assert.That(methods.Length, Is.EqualTo(methods.Distinct().Count()));

            // Enumerator, IEnumerator, IEnumerator<int>
            Assert.That(methods.Where(m => m.Name.Contains(nameof(IEnumerable.GetEnumerator))).Count(), Is.EqualTo(3));

            PropertyInfo[] properties = typeof(List<int>).ListMembers<PropertyInfo>(includeNonPublic: true).ToArray();

            Assert.That(properties.Length, Is.EqualTo(properties.Distinct().Count()));

            // ICollection<T>.IsReadOnly, IList-ReadOnly
            Assert.That(properties.Where(prop => prop.Name.Contains(nameof(IList.IsReadOnly))).Count(), Is.EqualTo(2));
        }

        public interface IInterface 
        {
            void Bar();
        }

        public class NoughtyClass : IInterface
        {
            void IInterface.Bar(){}
            public void Bar() { }
        }

        [Test]
        public void ListMembers_ShouldReturnExplicitImplementations2() 
        {
            MethodInfo[] methods = typeof(NoughtyClass)
                .ListMembers<MethodInfo>(includeNonPublic: true)
                .Where(m => m.Name.Contains(nameof(IInterface.Bar)))
                .ToArray();

            Assert.That(methods.Count, Is.EqualTo(2));
        }

        [Test]
        public void ListMembers_ShouldReturnMembersFromTheWholeHierarchy()
        {
            MethodInfo[] methods = typeof(IGrandChild).ListMembers<MethodInfo>().ToArray();

            Assert.That(methods.Length, Is.EqualTo(3));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IParent>(i => i.Foo())));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IChild>(i => i.Bar())));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IGrandChild>(i => i.Baz())));
        }

        [TestCase(true, 3)]
        [TestCase(false, 1)]
        public void ListMembers_ShouldReturnNonPublicMembersIfNecessary(bool includeNonPublic, int expectedLength) 
        {
            PropertyInfo[] properties = typeof(Class).ListMembers<PropertyInfo>(includeNonPublic).ToArray();

            Assert.That(properties.Length, Is.EqualTo(expectedLength));
        }

        private class Class 
        {
            public class Nested { }

            public int PublicProperty { get; }
            internal int InternalProperty { get; }
            private int PrivateProperty { get; }

            private int[] ar = new[] { 1 }; 

            public ref int RefMethod() => ref ar[0];
        }

        private interface IParent 
        {
            void Foo();
        }

        private interface IChild : IParent 
        {
            void Bar();
        }

        private interface IGrandChild : IChild 
        {
            void Baz();
        }

        [TestCase(typeof(object), "System.Object")]
        [TestCase(typeof(List<>), "System.Collections.Generic.List")] // generic
        [TestCase(typeof(IParent), "IParent")] // nested
        public void GetFriendlyName_ShouldBeautifyTheTypeName(Type type, string expected) => Assert.AreEqual(expected, type.GetFriendlyName());

        [Test]
        public void GetFriendlyName_ShouldHandleRefTypes()
        {
            Type refType = typeof(Class).GetMethod(nameof(Class.RefMethod)).ReturnType; // System.Int32&

            Assert.AreEqual("System.Int32", refType.GetFriendlyName());
        }

        [TestCase(typeof(Class))]
        [TestCase(typeof(List<>))] // open generic type
        public void GetReferences_ShouldReturnAllReferencesOfTheContainingAssembly(Type type) =>
            Assert.That(type.GetReferences().SequenceEqual
            (
                new[] { type.Assembly }.Concat(type.Assembly.GetReferences())
            ));

        [Test]
        public void GetReferences_ShouldTakeGenericParametersIntoAccount() => Assert.That(typeof(List<Class>)
            .GetReferences()
            .OrderBy(r => r.FullName)
            .SequenceEqual
            (
                typeof(List<>)
                    .GetReferences()
                    .Concat
                    (
                        typeof(Class).GetReferences()
                    )
                    .Distinct()
                    .OrderBy(r => r.FullName))
            );

        [Test]
        public void GetParents_ShouldReturnTheParents() =>
            Assert.That(typeof(Class.Nested).GetParents().SequenceEqual(new[] { typeof(TypeExtensionsTests), typeof(Class) }));

        [Test]
        public void GetOwnGenericArguments_ShouldDoWhatTheNameSuggests() 
        {
            Type child = typeof(GenericParent<>.GenericChild<>);

            Assert.That(child.GetGenericArguments().Length, Is.EqualTo(2));
            Assert.That(child.GetOwnGenericArguments().SequenceEqual(child.GetGenericArguments().Skip(1)));
        }

        private class GenericParent<T>
        {
            public class GenericChild<TT>
            {
            }
        }
    }
}
