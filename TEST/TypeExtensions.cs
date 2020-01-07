/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    using Properties;

    [TestFixture]
    public sealed class TypeExtensionsTests
    {
        [Test]
        public void ListMembers_ShouldReturnOverLoadedMembers() 
        {
            MethodInfo[] methods = typeof(IEnumerable<string>).ListMembers(System.Reflection.TypeExtensions.GetMethods).ToArray();
            Assert.That(methods.Length, Is.EqualTo(2));
            Assert.That(methods.Contains(((MethodCallExpression) ((Expression<Action<IEnumerable<string>>>) (i => i.GetEnumerator())).Body).Method));
            Assert.That(methods.Contains(((MethodCallExpression) ((Expression<Action<System.Collections.IEnumerable>>)(i => i.GetEnumerator())).Body).Method));

            PropertyInfo[] properties = typeof(IEnumerator<string>).ListMembers(System.Reflection.TypeExtensions.GetProperties).ToArray();
            Assert.That(properties.Length, Is.EqualTo(2));
            Assert.That(properties.Contains((PropertyInfo) ((MemberExpression) ((Expression<Func<IEnumerator<string>, string>>)(i => i.Current)).Body).Member));
            Assert.That(properties.Contains((PropertyInfo) ((MemberExpression) ((Expression<Func<System.Collections.IEnumerator, object>>)(i => i.Current)).Body).Member));
        }

        [Test]
        public void ListMembers_ShouldReturnMembersFromTheWholeHierarchy()
        {
            MethodInfo[] methods = typeof(IGrandChild).ListMembers(System.Reflection.TypeExtensions.GetMethods).ToArray();
            Assert.That(methods.Length, Is.EqualTo(3));
            Assert.That(methods.Contains(((MethodCallExpression) ((Expression<Action<IParent>>)(i => i.Foo())).Body).Method));
            Assert.That(methods.Contains(((MethodCallExpression) ((Expression<Action<IChild>>)(i => i.Bar())).Body).Method));
            Assert.That(methods.Contains(((MethodCallExpression) ((Expression<Action<IGrandChild>>)(i => i.Baz())).Body).Method));
        }

        [TestCase(true, 3)]
        [TestCase(false, 1)]
        public void ListMembers_ShouldReturnNonPublicMembersIfNecessary(bool includeNonPublic, int expectedLength) 
        {
            PropertyInfo[] properties = typeof(Class).ListMembers(System.Reflection.TypeExtensions.GetProperties, includeNonPublic).ToArray();

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

        [TestCase(typeof(Object), "System.Object")]
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

        [Test]
        public void GetApplicableConstructor_ShouldNotThrowIfTheTypeHasOnlyOnePublicConstructor() 
        {
            Assert.DoesNotThrow(() => typeof(ClassWithPrivateCtor).GetApplicableConstructor());
            Assert.DoesNotThrow(() => typeof(object).GetApplicableConstructor());
        }

        private class ClassWithPrivateCtor 
        {
            public ClassWithPrivateCtor() 
            { 
            }

            private ClassWithPrivateCtor(int i) 
            { 
            }
        }

        [Test]
        public void GetApplicableConstructor_ShouldThrowIfTheTypeHasMoreThanOnePublicConstructor() =>
            Assert.Throws<InvalidOperationException>(() => typeof(List<int>).GetApplicableConstructor(), Resources.CONSTRUCTOR_AMBIGUITY);
    }
}
