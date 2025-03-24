/********************************************************************************
* IMethodInfoExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public class IMethodInfoExtensionsTests: CodeAnalysisTestsBase
    {
        [TestCase
        (
            @"
                class ClassA<T> 
                {
                    public void Foo(T a, int b) {}
                }

                class ClassB 
                {
                    public void Foo<T>(T para, int b) {}
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                class ClassA<T> 
                {
                    public T Foo(int a) => default(T);
                }

                class ClassB 
                {
                    public T Foo<T>(int a) => default(T);
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
                    public void Foo(T a, int b) {}
                }

                class ClassB<TT> 
                {
                    public void Foo(TT a, int b) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    public void Foo(string para) {}
                }

                class ClassB
                {
                    public void Foo(string s) {}
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
            false
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    public void Foo<T>(T a, int b) {}
                }

                class ClassB 
                {
                    public void Foo<TT>(TT a, int b) {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    public void Foo<T>(T a, int b) where T: new() {}
                }

                class ClassB 
                {
                    public void Foo<TT>(TT a, int b) where TT: new() {}
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    public void Foo<T>(T a, int b) where T: new() {}
                }

                class ClassB 
                {
                    public void Foo<TT>(TT a, int b) {}
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    public void Foo<T>(T a, int b) where T: new() {}
                }

                class ClassB 
                {
                    public void Foo<T>(T a, int b) where T: struct {}
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    public string Foo(int a) => null;
                }

                class ClassB 
                {
                    public string Foo(int a) => null;
                }
            ",
            true
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    public string Foo(ref int a) => null;
                }

                class ClassB 
                {
                    public string Foo(int a) => null;
                }
            ",
            false
        )]
        [TestCase
        (
            @"
                class ClassA
                {
                    private string cica;
                    public ref string Foo(int a) => ref cica;
                }

                class ClassB 
                {
                    public string Foo(int a) => null;
                }
            ",
            false
        )]
        public void SignatureEquals_ShouldReturnTrueOnSignatureEquality(string src, bool equals) 
        {
            Assembly asm = Compile(src);

            IMethodInfo
                a1 = MetadataMethodInfo.CreateFrom(asm.GetTypes().Single(t => t.Name.Contains("ClassA")).ListMethods().Single(m => m.Name == "Foo")),
                b1 = MetadataMethodInfo.CreateFrom(asm.GetTypes().Single(t => t.Name.Contains("ClassB")).ListMethods().Single(m => m.Name == "Foo"));

            CSharpCompilation compilation = CreateCompilation(src);

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            IMethodInfo
                a2 = SymbolMethodInfo.CreateFrom(visitor.AllTypeSymbols.Single(t => t.Name == "ClassA").ListMethods().Single(m => m.Name == "Foo"), compilation),
                b2 = SymbolMethodInfo.CreateFrom(visitor.AllTypeSymbols.Single(t => t.Name == "ClassB").ListMethods().Single(m => m.Name == "Foo"), compilation);

            Assert.That(a1.SignatureEquals(b1), Is.EqualTo(equals));
            Assert.That(a2.SignatureEquals(b2), Is.EqualTo(equals));
            Assert.That(a1.SignatureEquals(b2), Is.EqualTo(equals));
            Assert.That(a2.SignatureEquals(b1), Is.EqualTo(equals));
        }

        [TestCase
        (
            @"
                public class BaseClass
                {
                   public override string ToString()
                   {
                      return ""Base"";
                   }
                }

                public class DerivedClass : BaseClass
                {
                   public override string ToString()
                   {
                      return ""Derived"";
                   }
                }
            ",
            "ToString"
        )]
        [TestCase
        (
            @"
                public abstract class BaseClass
                {
                   public abstract string Foo();
                }

                public class DerivedClass : BaseClass
                {
                   public override string Foo()
                   {
                      return ""Derived"";
                   }
                }
            ",
            "Foo"
        )]
        public void GetBaseMethod_ShouldReturnTheImmediateBaseThatWasOverridden(string src, string methodName)
        {
            Assembly asm = Compile(src);

            IMethodInfo
                a1 = MetadataMethodInfo.CreateFrom(asm.GetTypes().Single(t => t.Name.Contains("BaseClass")).ListMethods().Single(m => m.Name == methodName)),
                b1 = MetadataMethodInfo.CreateFrom(asm.GetTypes().Single(t => t.Name.Contains("DerivedClass")).ListMethods().Single(m => m.Name == methodName));

            CSharpCompilation compilation = CreateCompilation(src);

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            IMethodInfo
                a2 = SymbolMethodInfo.CreateFrom(visitor.AllTypeSymbols.Single(t => t.Name == "BaseClass").ListMethods().Single(m => m.Name == methodName), compilation),
                b2 = SymbolMethodInfo.CreateFrom(visitor.AllTypeSymbols.Single(t => t.Name == "DerivedClass").ListMethods().Single(m => m.Name == methodName), compilation);

            Assert.That(b1.GetBaseMethod(), Is.EqualTo(a1));
            Assert.That(b2.GetBaseMethod(), Is.EqualTo(a2));
        }
    }
}
