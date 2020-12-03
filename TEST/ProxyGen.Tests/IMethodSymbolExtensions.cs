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
    public class IMethodSymbolExtensionsTests: IXxXSymbolExtensionsTestsBase
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
    }
}
