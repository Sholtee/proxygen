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
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
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
        public void GetEnclosingTypes_ShouldReturnTheParentTypes() =>
            Assert.That(typeof(Class.Nested).GetEnclosingTypes().SequenceEqual(new[] { typeof(TypeExtensionsTests), typeof(Class) }));

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

        private static Assembly Compile(string src, params Assembly[] additionalReferences) => Internals.Compile.ToAssembly
        (
            CSharpSyntaxTree.ParseText(src).GetCompilationUnitRoot(),
            Guid.NewGuid().ToString(),
            null,
            Runtime
                .Assemblies
                .Concat(additionalReferences)
                .Select(@ref => MetadataReference.CreateFromFile(@ref.Location))
                .ToArray(),
            allowUnsafe: true
        );

        [TestCase
        (
            @"
                class ClassA<T> 
                {
                    void Foo(T para) {}
                }

                class ClassB 
                {
                    void Foo<T>(T para) {}
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                class ClassA<T> 
                {
                    void Foo(T para) {}
                }

                class ClassB<T, TT> 
                {
                    void Foo(TT para) {}
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                class ClassA<T> 
                {
                    void Foo(T para) {}
                }

                class ClassB<T, TT> 
                {
                    void Foo(T para) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA<T> 
                {
                    void Foo(T para) {}
                }

                class ClassB<TT> 
                {
                    void Foo(TT para) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    void Foo<T>(T para) {}
                }

                class ClassB 
                {
                    void Foo<T, TT>(TT para) {}
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    void Foo<T>(T para) {}
                }

                class ClassB 
                {
                    void Foo<T, TT>(T para) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    void Foo<T>(T para) {}
                }

                class ClassB 
                {
                    void Foo<TT>(TT para) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    void Foo<T>(T[] para) {}
                }

                class ClassB 
                {
                    void Foo<TT>(TT[] para) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                using System.Collections.Generic;
                class ClassA
                {
                    void Foo(List<int> para) {}
                }

                class ClassB 
                {
                    void Foo(List<string> para) {}
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                using System.Collections.Generic;
                class ClassA
                {
                    void Foo(List<string> para) {}
                }

                class ClassB 
                {
                    void Foo(List<string> para) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    unsafe void Foo(byte* para) {}
                }

                class ClassB 
                {
                    void Foo(byte[] para) {}
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    unsafe void Foo(ref byte cica) {}
                }

                class ClassB 
                {
                    void Foo(ref byte kutya) {}
                }
            ",
            true
        )]
        public void EqualsTo_ShouldCompareGenericParamsByTheirArity(string src, bool equals)
        {
            Assembly asm = Compile(src);

            Type
                a = asm.GetTypes().Single(t => t.Name.Contains("ClassA")).ListMembers<MethodInfo>(includeNonPublic: true).Single(m => m.Name == "Foo").GetParameters().Single().ParameterType,
                b = asm.GetTypes().Single(t => t.Name.Contains("ClassB")).ListMembers<MethodInfo>(includeNonPublic: true).Single(m => m.Name == "Foo").GetParameters().Single().ParameterType;

            Assert.That(a.EqualsTo(b), Is.EqualTo(equals));
        }
    }
}
