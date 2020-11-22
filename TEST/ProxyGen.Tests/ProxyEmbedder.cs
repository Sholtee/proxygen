﻿/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

using Solti.Utils.Proxy;
using Solti.Utils.Proxy.Attributes;
using Solti.Utils.Proxy.Generators;

[assembly: 
    EmbedGeneratedType(typeof(IList<>)), 
    EmbedGeneratedType(typeof(ProxyGenerator<IList<int>, InterfaceInterceptor<IList<int>>>)),
    EmbedGeneratedType(typeof(ProxyGenerator<Solti.Utils.Proxy.Internals.Tests.IMyService, InterfaceInterceptor<Solti.Utils.Proxy.Internals.Tests.IMyService>>))
]

namespace Solti.Utils.Proxy.Internals.Tests
{
    public interface IMyService { }

    [TestFixture]
    public class ProxyEmbedderTests
    {
        [Test]
        public void GetAOTGenerators_ShouldReturnAllValidGeneratorsFromAnnotations() 
        {
            CSharpCompilation compilation = CSharpCompilation.Create
            (
                assemblyName: "cica",
                syntaxTrees: new[]
                {
                    CSharpSyntaxTree.ParseText
                    (
                        @"
                        using System.Collections.Generic;

                        using Solti.Utils.Proxy;
                        using Solti.Utils.Proxy.Attributes;
                        using Solti.Utils.Proxy.Generators;

                        [assembly: EmbedGeneratedType(typeof(IList<>)), EmbedGeneratedType(typeof(ProxyGenerator<IList<int>, InterfaceInterceptor<IList<int>>>))]
                        "
                    )
                },
                references: AppDomain
                    .CurrentDomain
                    .GetAssemblies()
                    .Where(asm => !asm.IsDynamic && !string.IsNullOrEmpty(asm.Location))
                    .Select(asm => MetadataReference.CreateFromFile(asm.Location)),
                options: CompilationOptionsFactory.Create()
            );

            var res = new ProxyEmbedder(compilation, default).GetAOTGenerators().ToArray();

            Assert.That(res.Length, Is.EqualTo(1));
            Assert.That(res[0].ToDisplayString(), Is.EqualTo("Solti.Utils.Proxy.Generators.ProxyGenerator<System.Collections.Generic.IList<int>, Solti.Utils.Proxy.InterfaceInterceptor<System.Collections.Generic.IList<int>>>"));
        }
    }
}
