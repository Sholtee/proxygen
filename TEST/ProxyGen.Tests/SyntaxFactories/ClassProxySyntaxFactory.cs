/********************************************************************************
* ClassProxySyntaxFactory.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Internals;

    [TestFixture, Parallelizable(ParallelScope.All)]
#if LEGACY_COMPILER
    [Ignore("Roslyn v3 has different formatting rules than v4")]
#endif
    public sealed class ClassProxySyntaxFactoryTests : SyntaxFactoryTestsBase
    {
        private static ClassProxySyntaxFactory CreateSyntaxFactory(Type target, OutputType outputType) => new
        (
            MetadataTypeInfo.CreateFrom(target),
            SyntaxFactoryContext.Default with
            {
                OutputType = outputType,
                AssemblyNameOverride = typeof(ClassProxySyntaxFactoryTests).Assembly.GetName().Name
            }
        );

        [TestCase(typeof(Foo<int>), OutputType.Module, "ClsProxySrcModule.txt")]
        [TestCase(typeof(Foo<int>), OutputType.Unit, "ClsProxySrcUnit.txt")]
        public void ResolveUnit_ShouldGenerateTheDesiredUnit(Type target, int outputType, string fileName)
        {
            ClassProxySyntaxFactory gen = CreateSyntaxFactory(target, (OutputType) outputType);

            Assert.That
            (
                gen
                    .ResolveUnit(null, default)
                    .NormalizeWhitespace(eol: "\n")
                    .ToFullString(),
                Is.EqualTo
                (
                    File
                        .ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName))
                        .Replace("\r", string.Empty)
                        .Replace("{version}", typeof(ClassProxySyntaxFactory)
                            .Assembly
                            .GetName()
                            .Version
                            .ToString())
                )
            );
        }
    }
}