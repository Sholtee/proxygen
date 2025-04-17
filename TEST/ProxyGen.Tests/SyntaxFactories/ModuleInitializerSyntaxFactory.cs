/********************************************************************************
* ModuleInitializerSyntaxFactory.cs                                             *
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
    public class ModuleInitializerSyntaxFactoryTests
    {
        [Test]
        public void ResolveUnit_ShouldCreateTheDesiredUnit()
        {
            ModuleInitializerSyntaxFactory factory = new(SyntaxFactoryContext.Default with { OutputType = OutputType.Unit });

            Assert.That
            (
                factory.ResolveUnit(null, default).NormalizeWhitespace(eol: "\n").ToFullString(),
                Is.EqualTo
                (
                    File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModuleInitializerAttribute.txt")).Replace("{version}", typeof(ModuleInitializerSyntaxFactory).Assembly.GetName().Version.ToString())          
                )
            );
        }
    }
}