/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    using Abstractions;
    using Proxy.Attributes;

    public interface IMyService { }

    [TestFixture]
    public class ProxyEmbedderTests: CodeAnalysisTestsBase
    {
        [Test]
        public void GetAOTGenerators_ShouldReturnAllValidGeneratorsFromAnnotations() 
        {
            CSharpCompilation compilation = CreateCompilation
            (
                @"
                using System.Collections.Generic;

                using Solti.Utils.Proxy;
                using Solti.Utils.Proxy.Attributes;
                using Solti.Utils.Proxy.Generators;

                [assembly: EmbedGeneratedType(typeof(ProxyGenerator<IList<int>, InterfaceInterceptor<IList<int>>>))]
                ",
                typeof(EmbedGeneratedTypeAttribute).Assembly
            );

            INamedTypeSymbol[] res = ProxyEmbedder.GetAOTGenerators(compilation).ToArray();

            Assert.That(res.Length, Is.EqualTo(1));
            Assert.That(res[0].ToDisplayString(), Is.EqualTo("Solti.Utils.Proxy.Generators.ProxyGenerator<System.Collections.Generic.IList<int>, Solti.Utils.Proxy.InterfaceInterceptor<System.Collections.Generic.IList<int>>>"));
        }

        [Test]
        public void CreateDiagnosticAndLog_ShouldCreateALogFileForTheGivenException() 
        {
            Diagnostic diag = ProxyEmbedder.CreateDiagnosticAndLog(new InvalidOperationException(), Location.None);

            string path = Regex.Match(diag.ToString(), "Details stored in: ([\\w\\\\\\/ -:]+)$").Groups[1].Value;

            Assert.That(File.Exists(path));
        }

        private static readonly Assembly EmbeddedGeneratorHolder = Assembly.Load("Solti.Utils.Proxy.Tests.EmbeddedTypes");

        public static IEnumerable<Type> EmbeddedGenerators 
        {
            get 
            {
                foreach (var egta in EmbeddedGeneratorHolder.GetCustomAttributes<EmbedGeneratedTypeAttribute>())
                {
                    yield return egta.Generator;
                }
            }
        }

        [TestCaseSource(nameof(EmbeddedGenerators))]
        public void Execute_ShouldExtendTheSource(Type generator) 
        {
            var generatedType = (Type) generator.InvokeMember
            (
                "GetGeneratedType", 
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod, 
                null, 
                null, 
                new object[0]
            );

            //
            // A nem beagyazott "generatedType" nincs nevterben
            //

            Assert.IsNotNull(EmbeddedGeneratorHolder.GetType($"Proxies.{generatedType.Name}", throwOnError: false));
        }
    }
}
