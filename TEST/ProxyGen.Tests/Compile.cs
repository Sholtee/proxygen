﻿/********************************************************************************
* Compile.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public class CompileTests
    {
        [Test]
        public void ToAssembly_ShouldThrowIfTheCompilationFailed() 
        {
            CompilationUnitSyntax unit = CompilationUnit().WithUsings
            (
                usings: SingletonList
                (
                    UsingDirective
                    (
                        name: IdentifierName("bad")
                    )
                )
            );

            Exception ex = Assert.Throws<Exception>(() => Compile.ToAssembly(unit, "cica", null, Array.Empty<MetadataReference>()));

            Assert.That(ex.Data["src"], Is.EqualTo("using bad;"));
            Assert.That(ex.Data["failures"], Is.Not.Empty);
        }
    }
}
