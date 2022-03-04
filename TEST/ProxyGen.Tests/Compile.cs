/********************************************************************************
* Compile.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;

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

            Exception ex = Assert.Throws<InvalidOperationException>(() => Compile.ToAssembly(unit, "cica", null, Array.Empty<MetadataReference>()));

            Assert.That(ex.Data["src"], Is.EqualTo("using bad;"));
            Assert.That(ex.Data["failures"], Is.Not.Empty);
        }

        public static IEnumerable<string> PlatformAsmsDirs
        {
            get 
            {
                yield return null;
                yield return Environment.ExpandEnvironmentVariables("%USERPROFILE%\\.nuget\\packages\\netstandard.library\\2.0.0\\build\\netstandard2.0\\ref");
            }
        }

        [Test]
        public void GetPlatformAssemblies_ShouldReturnTheDesiredAsms([ValueSource(nameof(PlatformAsmsDirs))] string platformAsmDir) =>
            Assert.That
            (
                File.Exists
                (
                    Compile.GetPlatformAssemblies(platformAsmDir, new string[] { "netstandard.dll" }).Single().Display
                )
            );
    }
}
