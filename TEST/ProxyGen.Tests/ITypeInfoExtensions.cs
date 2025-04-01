/********************************************************************************
* ITypeInfoExtensions.cs                                                        *
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
        [TestCase(typeof(byte*), "19F35337AE3BF432B0EC2DC12F27ED0F")]
        [TestCase(typeof(byte[]), "74D7FF138EB16B14B959AC16874C4C64")]
        [TestCase(typeof(List<byte>), "E47C10415CD484DC9D8AC1F5705BE431")]
        [TestCase(typeof(List<byte>[]), "FC025B03ACEA197B07AB94FEFA629795")]
        [TestCase(typeof(List<byte[]>), "6135BB1118B7D3F9E84FB096B33F4723")]
        [TestCase(typeof(List<>), "793EB15224B0ECBB3E765880825D540A")]
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
            true
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
            Assembly asm = Compile(src, customConfig: comp => comp.WithOptions(((CSharpCompilationOptions) comp.Options).WithAllowUnsafe(true)));

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
