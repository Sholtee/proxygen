/********************************************************************************
* ITypeInfoExtensionsTests.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public class ITypeInfoExtensionsTests: CodeAnalysisTestsBase
    {
        [TestCase(typeof(byte), "CF53DF959DD5A05087959B351908EE59")]
        [TestCase(typeof(byte*), "59244D3687A0B67E8579B590AD7769FF")]
        [TestCase(typeof(byte[]), "A658875CC6F8FA246F61EDE5E352B245")]
        [TestCase(typeof(List<byte>), "CEFC1D408B19E22E23055E20F796814F")]
        [TestCase(typeof(List<byte>[]), "E236570557474918386BAF1E6083FC57")]
        [TestCase(typeof(List<byte[]>), "6AA0E88BB32516572FF8F19F8D86B9B0")]
        [TestCase(typeof(List<>), "75A0EAD7A72DEBFBA629750329311D99")]
        public void GetMD5HashCode_ShouldGenerateUniqueHashCode(Type t, string hash) => 
            Assert.That(MetadataTypeInfo.CreateFrom(t).GetMD5HashCode(), Is.EqualTo(hash)); 

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
