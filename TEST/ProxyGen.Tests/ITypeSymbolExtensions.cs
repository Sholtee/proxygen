/********************************************************************************
* INamedTypeSymbolExtensions.cs                                                 *
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
    public class INamedTypeSymbolExtensionsTests: IXxXSymbolExtensionsTestsBase
    {
        [Test]
        public void GetFriendlyName_ShouldNotReturnNamespaceForNestedTypes()
        {
            CSharpCompilation compilation = CreateCompilation
            (@"
                namespace Cica.Mica
                {
                    public class GenericParent<T>
                    {
                        public class GenericChild<TT>
                        {
                        }
                    }
                }
            ");

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            INamedTypeSymbol 
                parent = visitor.AllTypeSymbols.Single(m => m.Name == "GenericParent"),
                child = visitor.AllTypeSymbols.Single(m => m.Name == "GenericChild");

            Assert.That(parent.GetFriendlyName(), Is.EqualTo("Cica.Mica.GenericParent"));
            Assert.That(child.GetFriendlyName(), Is.EqualTo("GenericChild"));
        }

        [Test]
        public void GetFriendlyName_ShouldWorkWithTuples() 
        {
            CSharpCompilation compilation = CreateCompilation
            (@"
                public interface IMyInterface: IList<(string Cica, int Mica)>
                {
                }
            ");

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            INamedTypeSymbol tuple = (INamedTypeSymbol) visitor.AllTypeSymbols.Single(m => m.Name == "IMyInterface").Interfaces[0].TypeArguments.Single();

            Assert.That(tuple.GetFriendlyName(), Is.EqualTo("System.ValueTuple"));
        }

        [TestCase
        (@"
            public interface IFoo 
            {
                void Foo();
            }

            public interface IBar: IFoo
            {
                void Bar();
            }
        ", "IBar")]
        [TestCase
        (@"
            public abstract class FooCls 
            {
                public abstract void Foo();
            }

            public abstract class BarCls: FooCls
            {
                public abstract void Bar();
            }
        ", "BarCls")]
        [TestCase
        (@"
            public class FooCls 
            {
                public virtual void Foo() {}
            }

            public class BarCls: FooCls
            {
                public override void Foo() {}
                public void Bar() {}
            }
        ", "BarCls")]
        public void ListMembers_ShouldReturnMembersFromTheWholeHierarchy(string src, string cls)
        {
            CSharpCompilation compilation = CreateCompilation(src);

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            INamedTypeSymbol bar = visitor.AllTypeSymbols.Single(t => t.Name == cls);

            IMethodSymbol[] methods = bar.ListMembers<IMethodSymbol>().ToArray();

            Assert.That(methods.Count(m => m.Name == "Foo"), Is.EqualTo(1));
            Assert.That(methods.Count(m => m.Name == "Bar"), Is.EqualTo(1));
        }
    }
}
