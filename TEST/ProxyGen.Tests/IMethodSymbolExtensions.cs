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
        public void GetAccessModifiers_ShouldReturnTheCorrectAccessModifier(string name, int am) 
        {
            AccessModifiers accessModifiers = (AccessModifiers) am;

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
    }
}
