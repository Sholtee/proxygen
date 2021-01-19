/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    using Attributes;
    using Primitives;
    using Properties;

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
        public void LogException_ShouldCreateALogFileForTheGivenException() 
        {
            string logFile = ProxyEmbedder.LogException(new Exception(), default);

            Assert.That(File.Exists(logFile));
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
            using System.Collections.Generic;

            using Solti.Utils.Proxy;
            using Solti.Utils.Proxy.Attributes;
            using Solti.Utils.Proxy.Generators;

            [assembly: EmbedGeneratedType(typeof(ProxyGenerator<MyNamespace.IMyInterface, InterfaceInterceptor<MyNamespace.IMyInterface>>))]

            namespace MyNamespace
            {
                internal interface IMyInterface // nem kell InternalsVisibleTo mert ugyanabban a szerelvenybe lesz a proxy agyazva
                {
                    void Foo();
                }
            }
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
        [TestCase
        (
            @"
            using System.Collections.Generic;

            using Solti.Utils.Proxy;
            using Solti.Utils.Proxy.Attributes;
            using Solti.Utils.Proxy.Generators;

            [assembly: EmbedGeneratedType(typeof(DuckGenerator<IMyInterface, MyImpl>))]

            public interface IMyInterface
            {
                void Foo();
            }

            public class MyImpl 
            {
                internal void Foo() {} // nem kell InternalsVisibleTo mert ugyanabban a szerelvenybe lesz a proxy agyazva
            } 
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
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> diags);

            Assert.That(diags.Count(diag => diag.Id.StartsWith("PGE") && diag.Severity == DiagnosticSeverity.Warning), Is.EqualTo(0));
            Assert.That(diags.Count(diag => diag.Id.StartsWith("PGI") && diag.Severity == DiagnosticSeverity.Info), Is.EqualTo(1));
            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(2));
        }

        [Test]
        public void Execute_ShouldHandleInvalidSyntax()
        {
            Compilation compilation = CreateCompilation
            (
                @"
                using System.Collections;

                using Solti.Utils.Proxy;
                using Solti.Utils.Proxy.Attributes;
                using Solti.Utils.Proxy.Generators;

                [assembly: EmbedGeneratedType(typeof(DuckGenerator<IEnumerable,>))]
                ",
                new[] { typeof(EmbedGeneratedTypeAttribute).Assembly.Location },
                suppressErrors: true
            );

            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ProxyEmbedder());
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> diags);

            Assert.That(diags, Is.Empty);
            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Execute_ShouldReportDiagnosticOnError() 
        {
            Compilation compilation = CreateCompilation
            (
                @"
                using System.Collections;

                using Solti.Utils.Proxy;
                using Solti.Utils.Proxy.Attributes;
                using Solti.Utils.Proxy.Generators;

                [assembly: EmbedGeneratedType(typeof(DuckGenerator<IEnumerable, object>))]
                ",
                new[] { typeof(EmbedGeneratedTypeAttribute).Assembly.Location },
                suppressErrors: true
            );

            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ProxyEmbedder());
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> diags);

            Assert.That(diags.Any(diag => diag.Id.StartsWith("PGE") && diag.Severity == DiagnosticSeverity.Warning && diag.GetMessage().Contains(string.Format(Resources.MISSING_IMPLEMENTATION, nameof(IEnumerable.GetEnumerator)))));
            Assert.That(diags.Length, Is.EqualTo(1));
            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Execute_ShouldWarnOnNonCSharCompilation() 
        {
            Compilation compilation = VisualBasicCompilation.Create
            (
                "cica",
                new[]
                {
                    VisualBasicSyntaxTree.ParseText
                    (
                        @"
                        Imports System.Collections

                        Imports Solti.Utils.Proxy
                        Imports Solti.Utils.Proxy.Attributes
                        Imports Solti.Utils.Proxy.Generators

                        <Assembly:EmbedGeneratedType(GetType(DuckGenerator(Of IEnumerable, IEnumerable)))>
                        "
                    )
                },
                Runtime
                    .Assemblies
                    .Select(asm => asm.Location)
                    .Append(typeof(EmbedGeneratedTypeAttribute).Assembly.Location)
                    .Distinct()
                    .Select(location => MetadataReference.CreateFromFile(location)),
                new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ProxyEmbedder());
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> diags);

            Assert.That(diags.Any(diag => diag.Id == "PGE00" && diag.Severity == DiagnosticSeverity.Warning && diag.GetMessage() == SGResources.LNG_NOT_SUPPORTED));
            Assert.That(diags.Length, Is.EqualTo(1));
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

            ITypeResolution typeResolutionStrategy = (ITypeResolution) generator
                .GetProperty("TypeResolutionStrategy")
                .ToGetter()
                .Invoke(generatorInst);

            Assert.IsNotNull(EmbeddedGeneratorHolder.GetType(typeResolutionStrategy.ClassName, throwOnError: false));
        }
    }
}
