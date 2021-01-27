/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public sealed class TypeExtensionsTests
    {
        [Test]
        public void ListMethods_ShouldReturnOverLoadedInterfaceMethods() 
        {
            MethodInfo[] methods = typeof(IEnumerable<string>).ListMethods().ToArray();
            Assert.That(methods.Length, Is.EqualTo(2));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IEnumerable<string>>(i => i.GetEnumerator())));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IEnumerable>(i => i.GetEnumerator())));
        }

        [Test]
        public void ListProperties_ShouldReturnOverLoadedInterfaceProperties()
        {
            PropertyInfo[] properties = typeof(IEnumerator<string>).ListProperties().ToArray();
            Assert.That(properties.Length, Is.EqualTo(2));
            Assert.That(properties.Contains((PropertyInfo)MemberInfoExtensions.ExtractFrom<IEnumerator<string>>(i => i.Current)));
            Assert.That(properties.Contains((PropertyInfo)MemberInfoExtensions.ExtractFrom<IEnumerator>(i => i.Current)));
        }

        [Test]
        public void ListMethods_ShouldReturnExplicitImplementations() 
        {
            MethodInfo[] methods = typeof(List<int>).ListMethods().ToArray();

            Assert.That(methods.Length, Is.EqualTo(methods.Distinct().Count()));

            // Enumerator, IEnumerator, IEnumerator<int>
            Assert.That(methods.Where(m => m.Name.Contains(nameof(IEnumerable.GetEnumerator))).Count(), Is.EqualTo(3));
        }

        [Test]
        public void ListProperties_ShouldReturnExplicitImplementations()
        {
            PropertyInfo[] properties = typeof(List<int>).ListProperties().ToArray();

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
        public void ListMethods_ShouldReturnExplicitImplementations2() 
        {
            MethodInfo[] methods = typeof(NoughtyClass)
                .ListMethods()
                .Where(m => m.Name.Contains(nameof(IInterface.Bar)))
                .ToArray();

            Assert.That(methods.Count, Is.EqualTo(2));
        }

        [Test]
        public void ListMethods_ShouldReturnMethodsFromTheWholeHierarchy()
        {
            MethodInfo[] methods = typeof(IGrandChild).ListMethods().ToArray();

            Assert.That(methods.Length, Is.EqualTo(3));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IParent>(i => i.Foo())));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IChild>(i => i.Bar())));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IGrandChild>(i => i.Baz())));
        }

        private interface IIFace : IA, IB { }

        private interface IA: IC 
        {
            void Foo();
        }

        private interface IB: IC 
        {
            void Bar();
        }

        private interface IC 
        {
            void Baz();
        }

        [Test]
        public void ListMethods_ShouldDistinct() => Assert.That(typeof(IIFace).ListMethods().Count(), Is.EqualTo(3));

        private class Class 
        {
            public class Nested { }

            public int PublicProperty { get; }
            internal int InternalProperty { get; }
            private int PrivateProperty { get; }

            private readonly int[] ar = new[] { 1 }; 

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
