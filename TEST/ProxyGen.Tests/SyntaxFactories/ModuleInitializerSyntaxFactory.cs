/********************************************************************************
* ModuleInitializerSyntaxFactory.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.IO;

using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Internals;

    [TestFixture]
    public class ModuleInitializerSyntaxFactoryTests
    {
        [Test]
        public void ResolveUnit_ShouldCreateTheDesiredUnit()
        {
            ModuleInitializerSyntaxFactory factory = new ModuleInitializerSyntaxFactory(OutputType.Unit, null);

            Assert.That(factory.ResolveUnit(null, default).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("ModuleInitializerAttribute.txt")));
        }
    }
}