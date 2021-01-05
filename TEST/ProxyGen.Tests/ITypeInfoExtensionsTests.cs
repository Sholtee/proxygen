/********************************************************************************
* ITypeInfoExtensionsTests.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public class ITypeInfoExtensionsTests: CodeAnalysisTestsBase
    {
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
                    public void Foo(T para) {}
                }

                class ClassB 
                {
                    public void Foo<T>(T para) {}
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                class ClassA<T> 
                {
                    public void Foo(T para) {}
                }

                class ClassB<T, TT> 
                {
                    public void Foo(TT para) {}
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                class ClassA<T> 
                {
                    public void Foo(T para) {}
                }

                class ClassB<T, TT> 
                {
                    public void Foo(T para) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA<T> 
                {
                    public void Foo(T para) {}
                }

                class ClassB<TT> 
                {
                    public void Foo(TT para) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    public void Foo<T>(T para) {}
                }

                class ClassB 
                {
                    public void Foo<T, TT>(TT para) {}
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    public void Foo<T>(T para) {}
                }

                class ClassB 
                {
                    public void Foo<T, TT>(T para) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    public void Foo<T>(T para) {}
                }

                class ClassB 
                {
                    public void Foo<TT>(TT para) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    public void Foo<T>(T[] para) {}
                }

                class ClassB 
                {
                    public void Foo<TT>(TT[] para) {}
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
                    public void Foo(List<int> para) {}
                }

                class ClassB 
                {
                    public void Foo(List<string> para) {}
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
                    public void Foo(List<string> para) {}
                }

                class ClassB 
                {
                    public void Foo(List<string> para) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    public unsafe void Foo(byte* para) {}
                }

                class ClassB 
                {
                    public void Foo(byte[] para) {}
                }
            ",
            false
        )]
        public void EqualsTo_ShouldCompare(string src, bool equals)
        {
            Assembly asm = Compile(src);

            ITypeInfo
                a1 = MetadataTypeInfo.CreateFrom(asm.GetTypes().Single(t => t.Name.Contains("ClassA")).ListMethods().Single(m => m.Name == "Foo").GetParameters().Single().ParameterType),
                b1 = MetadataTypeInfo.CreateFrom(asm.GetTypes().Single(t => t.Name.Contains("ClassB")).ListMethods().Single(m => m.Name == "Foo").GetParameters().Single().ParameterType);

            CSharpCompilation compilation = CreateCompilation(src);

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            ITypeInfo
                a2 = SymbolTypeInfo.CreateFrom(visitor.AllTypeSymbols.Single(t => t.Name == "ClassA").ListMethods().Single(m => m.Name == "Foo").Parameters.Single().Type, compilation),
                b2 = SymbolTypeInfo.CreateFrom(visitor.AllTypeSymbols.Single(t => t.Name == "ClassB").ListMethods().Single(m => m.Name == "Foo").Parameters.Single().Type, compilation);

            Assert.That(a1.EqualsTo(b1), Is.EqualTo(equals));
            Assert.That(a2.EqualsTo(b2), Is.EqualTo(equals));
            Assert.That(a1.EqualsTo(b2), Is.EqualTo(equals));
            Assert.That(a2.EqualsTo(b1), Is.EqualTo(equals));
        }
    }
}
