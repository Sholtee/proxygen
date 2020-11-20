/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    using Abstractions;
    using Generators;

    [TestFixture]
    public class ProxyEmbedderTests
    {
        [Test]
        public void GetAttributes_ShouldReturnAllTheAttributes() 
        {
            IProxySyntaxFactory fact = new ProxyGenerator<IList<object>, InterfaceInterceptor<IList<object>>>().SyntaxFactory;

            (CompilationUnitSyntax unit, IReadOnlyCollection<MetadataReference> references, IReadOnlyCollection<Type> _) = fact.GetContext();

            CSharpCompilation compilation = CSharpCompilation.Create
            (
                assemblyName: fact.AssemblyName,
                syntaxTrees: new[]
                {
                    CSharpSyntaxTree.Create
                    (
                        root: unit
                    )
                },
                references: references,
                options: CompilationOptionsFactory.Create()
            );

            AttributeSyntax[] attributes = ProxyEmbedder.GetAttributes<AssemblyTitleAttribute>(compilation).ToArray();
            Assert.That(attributes.Length, Is.EqualTo(1));
        }
    }
}
