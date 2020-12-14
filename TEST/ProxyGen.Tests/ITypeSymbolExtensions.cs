﻿/********************************************************************************
* ITypeSymbolExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public class ITypeSymbolExtensionsTests : IXxXSymbolExtensionsTestsBase
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
            CSharpCompilation compilation = CreateCompilation(string.Empty);

            INamedTypeSymbol tuple = compilation.CreateTupleTypeSymbol(new ITypeSymbol[] { compilation.GetSpecialType(SpecialType.System_String), compilation.GetSpecialType(SpecialType.System_Int32) }.ToImmutableArray(), new[] { "Cica", "Mica" }.ToImmutableArray());

            Assert.That(tuple.GetFriendlyName(), Is.EqualTo("System.ValueTuple"));
        }

        [Test]
        public void GetFriendlyName_ShouldWorkWithArrays()
        {
            CSharpCompilation compilation = CreateCompilation(string.Empty);

            ITypeSymbol ar = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Int32));
            
            Assert.That(ar.GetFriendlyName(), Is.EqualTo("System.Int32[]"));
        }

        [Test]
        public void GetFriendlyName_ShouldWorkWithNullables()
        {
            CSharpCompilation compilation = CreateCompilation(string.Empty);

            INamedTypeSymbol nullable = compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(compilation.GetSpecialType(SpecialType.System_Int32));

            Assert.That(nullable.GetFriendlyName(), Is.EqualTo("System.Nullable"));
        }

        private class Nested { }

        private class NestedGeneric<T> 
        {
            public class Nested { }
        }

        [TestCase(typeof(ITypeSymbolExtensionsTests))]
        [TestCase(typeof(Nested))]
        [TestCase(typeof(List<>))]
        [TestCase(typeof(NestedGeneric<>.Nested))]
        public void GetAssemblyQualifiedName_ShouldDoWhatTheNameSuggests(Type type)
        {
            Assembly asm = type.Assembly;

            CSharpCompilation compilation = CreateCompilation(string.Empty, asm);

            IAssemblySymbol asmSymbol = (IAssemblySymbol) compilation.GetAssemblyOrModuleSymbol(compilation.References.Single(@ref => @ref.Display == asm.Location));
            INamedTypeSymbol typeSymbol = asmSymbol.GetTypeByMetadataName(type.FullName);

            Assert.That(typeSymbol.GetAssemblyQualifiedName(), Is.EqualTo(type.AssemblyQualifiedName));
        }

        [Test]
        public void IsNested_ShouldReturnTrueIfTheTypeIsNested() 
        {
            CSharpCompilation compilation = CreateCompilation
            (@"
                public class MyClass<T>
                {
                    private class Nested {}
                }
            ");

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            ITypeSymbol ga = visitor.AllTypeSymbols.Single(t => t.Name == "MyClass").TypeArguments.Single();
            Assert.IsFalse(ga.IsNested());

            ITypeSymbol nested = visitor.AllTypeSymbols.Single(t => t.Name == "Nested");
            Assert.That(nested.IsNested());
        }

        [TestCase
        (@"
            using System.Collections.Generic;
            public class MyClass<T>: List<T>
            {
            }
        ", true)]
        [TestCase
        (@"
            using System.Collections.Generic;
            public class MyClass<T>: List<T[]>
            {
            }
        ", false)]
        [TestCase
        (@"
            using System.Collections.Generic;
            public class MyClass: List<object>
            {
            }
        ", false)]
        [TestCase
        (@"
            using System.Collections.Generic;
            public class MyClass: List<string>
            {
            }
        ", false)]
        [TestCase
        (@"
            using System.Collections.Generic;
            public class MyClass: List<List<string>>
            {
            }
        ", false)]
        public void IsGenericParameter_ShouldReturnTrueIfTheTypeIsGenericParameter(string src, bool isGP)
        {
            CSharpCompilation compilation = CreateCompilation(src);
           
            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            ITypeSymbol ga = visitor.AllTypeSymbols.Single(t => t.Name == "MyClass").BaseType.TypeArguments.Single();
            Assert.That(ga.IsGenericParameter(), Is.EqualTo(isGP));
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

        [TestCase
        (@"
            using System.Collections.Generic;
            class MyList: List<int[]>{}
        ", "System.Int32")]
        [TestCase
        (@"
            using System.Collections.Generic;
            class MyList: List<int*[]>{}
        ", "System.Int32")]
        [TestCase
        (@"
            using System.Collections.Generic;
            class MyList: List<int> {}
        ", null)]
        public void GetElementType_ShouldReturnTheProperElementType(string src, string element) 
        {
            CSharpCompilation compilation = CreateCompilation(src);

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            ITypeSymbol ga = visitor.AllTypeSymbols.Single(t => t.Name == "MyList").BaseType.TypeArguments.Single();

            Assert.That(ga.GetElementType(recurse: true)?.GetFriendlyName(), Is.EqualTo(element));
        }

        [Test]
        public void PassingByReference_ShouldNotAffectTheParameterType() // TODO: Ne ide
        {
            CSharpCompilation compilation = CreateCompilation
            (@"
                public class MyClass 
                {
                    void Foo(int a, ref int b) {}
                }
            ");

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            IMethodSymbol foo = (IMethodSymbol) visitor.AllTypeSymbols.Single(t => t.Name == "MyClass").GetMembers("Foo").Single();

            ITypeSymbol
                a = foo.Parameters[0].Type,
                b = foo.Parameters[0].Type;

            Assert.That(SymbolEqualityComparer.Default.Equals(a, b));
        }

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
        public void EqualsTo_ShouldCompareGenericParamsByTheirArity(string src, bool equals) 
        {
            CSharpCompilation compilation = CreateCompilation(src);

            var visitor = new FindAllTypesVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);

            ITypeSymbol
                a = visitor.AllTypeSymbols.Single(t => t.Name == "ClassA").ListMembers<IMethodSymbol>(includeNonPublic: true).Single(m => m.Name == "Foo").Parameters.Single().Type,
                b = visitor.AllTypeSymbols.Single(t => t.Name == "ClassB").ListMembers<IMethodSymbol>(includeNonPublic: true).Single(m => m.Name == "Foo").Parameters.Single().Type;

            Assert.That(a.EqualsTo(b), Is.EqualTo(equals));
        }
    }
}
