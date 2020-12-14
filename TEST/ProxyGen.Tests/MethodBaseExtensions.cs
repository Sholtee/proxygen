/********************************************************************************
* MethodBaseExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public class MethodBaseExtensionsTests
    {
        public static (MethodInfo Method, IEnumerable<Type> DeclaringTypes)[] Methods = new []
        {
            ((MethodInfo) MemberInfoExtensions.ExtractFrom<Dictionary<string, object>>(d => d.Add(default, default)), (IEnumerable<Type>) new[] {typeof(IDictionary<string, object>) }),
            ((MethodInfo) MemberInfoExtensions.ExtractFrom<MyDictionary<string, object>>(d => d.Add(default, default)), new[] {typeof(IDictionary<string, object>) }), // leszarmazott
            ((MethodInfo) MemberInfoExtensions.ExtractFrom<Dictionary<string, object>>(d => d.GetHashCode()), Array.Empty<Type>()),
        };

        [TestCaseSource(nameof(Methods))]
        public void GetDeclaringInterfaces_ShouldDoWhatItsNameSays((MethodInfo Method, IEnumerable<Type> DeclaringTypes) data) =>
            Assert.That(data.Method.GetDeclaringInterfaces().SequenceEqual(data.DeclaringTypes));

        private class MyDictionary<TKey, TValue> : Dictionary<TKey, TValue> { }

        [TestCase(nameof(MyClass.Public), AccessModifiers.Public)]
        [TestCase("Protected", AccessModifiers.Protected)]
        [TestCase(nameof(MyClass.ProtectedInternal), AccessModifiers.Protected | AccessModifiers.Internal)]
        [TestCase(nameof(MyClass.Internal), AccessModifiers.Internal)]
        [TestCase("Solti.Utils.Proxy.Internals.Tests.MethodBaseExtensionsTests.IInterface.Explicit", AccessModifiers.Explicit)]
        [TestCase("Private", AccessModifiers.Private)]
        public void GetAccessModifiers_ShouldDoWhatTheItsNameSays(string name, int am) =>
            Assert.That(typeof(MyClass).ListMembers<MethodInfo>(includeNonPublic: true).Single(m => m.Name == name).GetAccessModifiers(), Is.EqualTo((AccessModifiers) am));

        private interface IInterface 
        {
            void Explicit();
        }

        private class MyClass: IInterface
        {
            public void Public() { }
            protected void Protected() { }
            protected internal void ProtectedInternal() { }
            internal void Internal() { }
            void IInterface.Explicit() { }
            private void Private() { }
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
                .ToArray()
        );

        [TestCase
        (
            @"
                class ClassA<T> 
                {
                    void Foo(T a, int b) {}
                }

                class ClassB 
                {
                    void Foo<T>(T para, int b) {}
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                class ClassA<T> 
                {
                    T Foo(int a) => default(T);
                }

                class ClassB 
                {
                    T Foo<T>(int a) => default(T);
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
            false
        )]
        [TestCase
        (
            @"
                class ClassA<T> 
                {
                    void Foo(T a, int b) {}
                }

                class ClassB<TT> 
                {
                    void Foo(TT a, int b) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    void Foo(string para) {}
                }

                class ClassB
                {
                    void Foo(string para) {}
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
            false
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    void Foo<T>(T a, int b) {}
                }

                class ClassB 
                {
                    void Foo<TT>(TT a, int b) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    string Foo(int a) => null;
                }

                class ClassB 
                {
                    string Foo(int a) => null;
                }
            ",
            true
        )]
        public void SignatureEquals_ShouldReturnTrueOnEquality(string src, bool equals)
        {
            Assembly asm = Compile(src);

            MethodInfo
                a = asm.GetTypes().Single(t => t.Name.Contains("ClassA")).ListMembers<MethodInfo>(includeNonPublic: true).Single(m => m.Name == "Foo"),
                b = asm.GetTypes().Single(t => t.Name.Contains("ClassB")).ListMembers<MethodInfo>(includeNonPublic: true).Single(m => m.Name == "Foo");

            Assert.That(a.SignatureEquals(b), Is.EqualTo(equals));
        }
    }
}
