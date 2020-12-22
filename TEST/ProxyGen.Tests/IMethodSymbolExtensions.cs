/********************************************************************************
* IMethodSymbolExtensions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public class IMethodSymbolExtensionsTests: CodeAnalysisTestsBase
    {
        [TestCase("Foo", AccessModifiers.Public)]
        [TestCase("Bar", AccessModifiers.Explicit)]
        [TestCase("Baz", AccessModifiers.Public)]
        [TestCase("FooBar", AccessModifiers.Internal)]
        public void GetAccessModifiers_ShouldReturnTheCorrectAccessModifier(string name, AccessModifiers accessModifiers) 
        {
            CSharpCompilation compilation = CreateCompilation
            (@"
                interface IInterface 
                {
                    void Foo();
                    void Bar();
                }

                class MyClass: IInterface
                {
                    public void Foo() {}
                    void IInterface.Bar() {}
                    public void Baz() {}
                    internal void FooBar() {}
                }
            ");

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            IMethodSymbol[] methods = visitor
                .AllTypeSymbols
                .Single(t => t.Name == "MyClass")
                .GetMembers()
                .OfType<IMethodSymbol>()
                .ToArray();

            Assert.That(methods.Single(m => m.StrippedName() == name).GetAccessModifiers(), Is.EqualTo(accessModifiers));
        }

        [TestCase("get_Prop1", true)] // explicit
        [TestCase("get_Prop2", true)]
        [TestCase("Foo", false)]
        public void IsSpecial_ShouldReturnTrueForSpecialMethods(string name, bool isSpecial) 
        {
            CSharpCompilation compilation = CreateCompilation
            (@"
                interface IInterface 
                {
                    int Prop1 { get; }
                    int Prop2 { get; }
                }

                class MyClass: IInterface
                {
                    public void Foo() {}
                    int IInterface.Prop1 => 0;
                    public int Prop2 => 2;
                }
            ");

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            IMethodSymbol[] methods = visitor
                .AllTypeSymbols
                .Single(t => t.Name == "MyClass")
                .GetMembers()
                .OfType<IMethodSymbol>()
                .ToArray();

            Assert.That(methods.Single(m => m.StrippedName() == name).IsSpecial(), Is.EqualTo(isSpecial));
        }

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
            CSharpCompilation compilation = CreateCompilation(src);

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            IMethodSymbol
                a = visitor.AllTypeSymbols.Single(t => t.Name == "ClassA").ListMembers<IMethodSymbol>(includeNonPublic: true).Single(m => m.Name == "Foo"),
                b = visitor.AllTypeSymbols.Single(t => t.Name == "ClassB").ListMembers<IMethodSymbol>(includeNonPublic: true).Single(m => m.Name == "Foo");

            Assert.That(a.SignatureEquals(b), Is.EqualTo(equals));
        }
    }
}
