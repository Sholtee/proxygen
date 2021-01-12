/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    using Attributes;
    using Primitives;

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

        [TestCase
        (
            @"
            using System.Collections.Generic;

            using Solti.Utils.Proxy;
            using Solti.Utils.Proxy.Attributes;
            using Solti.Utils.Proxy.Generators;

            [assembly: EmbedGeneratedType(typeof(ProxyGenerator<IList<int>, InterfaceInterceptor<IList<int>>>))]
            "
        )]
        [TestCase
        (
            @"
            using System.Collections;

            using Solti.Utils.Proxy;
            using Solti.Utils.Proxy.Attributes;
            using Solti.Utils.Proxy.Generators;

            [assembly: EmbedGeneratedType(typeof(DuckGenerator<IEnumerable, IEnumerable>))]
            "
        )]
        public void Execute_ShouldExtendTheOriginalSource(string src) 
        {
            Compilation compilation = CreateCompilation
            (
                src,
                typeof(EmbedGeneratedTypeAttribute).Assembly
            );

            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ProxyEmbedder());
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> _);

            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(2));
        }

        internal sealed class AssemblyLoader : IAnalyzerAssemblyLoader
        {
            public static AssemblyLoader Instance = new AssemblyLoader();

            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                return Assembly.LoadFrom(fullPath);
            }
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
        public void BuiltAssembly_ShouldContainTheProxy(Type generator) 
        {
            object generatorInst = generator
                .GetConstructor(Type.EmptyTypes)
                .ToStaticDelegate()
                .Invoke(new object[0]);

            IUnitSyntaxFactory syntaxFactory = (IUnitSyntaxFactory) generator
                .GetProperty("SyntaxFactory")
                .ToGetter()
                .Invoke(generatorInst);

            Assert.IsNotNull(EmbeddedGeneratorHolder.GetType(syntaxFactory.DefinedClasses.Single(), throwOnError: false));
        }
    }
}
