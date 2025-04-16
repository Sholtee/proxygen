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
using System.Runtime.InteropServices.ComTypes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    using Attributes;
    using Properties;
    using Primitives;
    using Proxy.Generators.Tests;
    using Proxy.Tests.EmbeddedTypes;
    using Proxy.Tests;

    public interface IMyService { }

    [TestFixture]
    public class ProxyEmbedderTests : CodeAnalysisTestsBase
    {
#if LEGACY_COMPILER
        [TestCase
        (
            @"
            using System.Collections.Generic;

            using Solti.Utils.Proxy;
            using Solti.Utils.Proxy.Attributes;
            using Solti.Utils.Proxy.Generators;

            [assembly: EmbedGeneratedType(typeof(ProxyGenerator<IList<int>, InterfaceInterceptor<IList<int>>>))]
            "
        ),
        TestCase
        (
            @"
            using System.Collections.Generic;

            using Solti.Utils.Proxy;
            using Solti.Utils.Proxy.Attributes;
            using Solti.Utils.Proxy.Generators;

            [assembly: EmbedGeneratedType(typeof(ProxyGenerator<IList<int>, InterfaceInterceptor<IList<int>>>)), EmbedGeneratedType(typeof(ProxyGenerator<IList<int>, InterfaceInterceptor<IList<int>>>))]
            [assembly: EmbedGeneratedType(typeof(ProxyGenerator<IList<int>, InterfaceInterceptor<IList<int>>>))]
            "
        )]
        public void GetAOTGenerators_ShouldReturnAllValidGeneratorsFromAnnotations(string src)
        {
            CSharpCompilation compilation = CreateCompilation
            (
                src,
                typeof(EmbedGeneratedTypeAttribute).Assembly
            );

            INamedTypeSymbol[] res = ProxyEmbedder.GetAOTGenerators(compilation).ToArray();

            Assert.That(res.Length, Is.EqualTo(1));
            Assert.That(res[0].ToDisplayString(), Is.EqualTo("Solti.Utils.Proxy.Generators.ProxyGenerator<System.Collections.Generic.IList<int>, Solti.Utils.Proxy.InterfaceInterceptor<System.Collections.Generic.IList<int>>>"));
        }
#endif
        public static GeneratorDriver CreateDriver(CSharpParseOptions opts)
        {
#if LEGACY_COMPILER
            return CSharpGeneratorDriver.Create(new ISourceGenerator[]{ new ProxyEmbedder() }, parseOptions: opts);
#else
            return CSharpGeneratorDriver.Create(new IIncrementalGenerator[] { new ProxyEmbedder() }.Select(GeneratorExtensions.AsSourceGenerator), parseOptions: opts);
#endif
        }

        public static IEnumerable<string> ValidSources1
        {
            get
            {
                yield return
                    @"
                    using System.Collections.Generic;

                    using Solti.Utils.Proxy;
                    using Solti.Utils.Proxy.Attributes;
                    using Solti.Utils.Proxy.Generators;

                    [assembly: EmbedGeneratedType(typeof(ProxyGenerator<IList<int>, InterfaceInterceptor<IList<int>>>))]
                    ";
                yield return
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
                    ";
                yield return
                    @"
                    using System;

                    using Solti.Utils.Proxy;
                    using Solti.Utils.Proxy.Attributes;
                    using Solti.Utils.Proxy.Generators;

                    [assembly: EmbedGeneratedType(typeof(ProxyGenerator<IGenericInterfaceHavingConstraint, InterfaceInterceptor<IGenericInterfaceHavingConstraint>>))]

                    public interface IGenericInterfaceHavingConstraint
                    {
                        void Foo<T>() where T : class, IDisposable;
                        void Bar<T, TT>() where T: new() where TT : struct;
                    }
                    ";
                yield return
                    @"
                    using System.Collections;

                    using Solti.Utils.Proxy;
                    using Solti.Utils.Proxy.Attributes;
                    using Solti.Utils.Proxy.Generators;

                    [assembly: EmbedGeneratedType(typeof(DuckGenerator<IEnumerable, IEnumerable>))]
                    ";
                yield return
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
                    ";
                yield return
                    @"
                    using System;

                    using Solti.Utils.Proxy;
                    using Solti.Utils.Proxy.Attributes;
                    using Solti.Utils.Proxy.Generators;

                    [assembly: EmbedGeneratedType(typeof(DuckGenerator<IGenericInterfaceHavingConstraint, GenericImplHavingConstraint>))]

                    public interface IGenericInterfaceHavingConstraint
                    {
                        void Foo<T>() where T : class, IDisposable;
                        void Bar<T, TT>() where T: new() where TT : struct;
                    }

                    public class GenericImplHavingConstraint
                    {
                        public void Foo<T>() where T : class, IDisposable {}
                        public void Bar<T, TT>() where T: new() where TT : struct {}
                    }
                    ";
            }
        }

        [Test]
        public void Execute_ShouldExtendTheOriginalSource([ValueSource(nameof(ValidSources1))] string src) 
        {
            Compilation compilation = CreateCompilation
            (
                src,
                typeof(EmbedGeneratedTypeAttribute).Assembly
            );

            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));
 
            GeneratorDriver driver = CreateDriver(null);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> diags);

            Assert.That(diags.Count(diag => diag.Id.StartsWith("PGE") && diag.Severity == DiagnosticSeverity.Warning), Is.EqualTo(0));
            Assert.That(diags.Count(diag => diag.Id.StartsWith("PGI") && diag.Severity == DiagnosticSeverity.Info), Is.EqualTo
            (
#if NET5_0_OR_GREATER
                1
#else
                2
#endif
            ));
            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(
#if NET5_0_OR_GREATER
                2
#else
                3
#endif
            ));
        }

        public static IEnumerable<(string, int)> ValidSources2
        {
            get
            {
                yield return
                (
                    @"
                    using System.Collections.Generic;

                    using Solti.Utils.Proxy;
                    using Solti.Utils.Proxy.Attributes;
                    using Solti.Utils.Proxy.Generators;

                    [assembly: EmbedGeneratedType(typeof(DuckGenerator<IMyInterface, MyImpl>))]

                    public interface IMyInterface
                    {
                    }

                    public class MyImpl 
                    {
                    } 
                    ",
                    3
                );
                yield return
                (
                    @"
                    [assembly: Solti.Utils.Proxy.Attributes.EmbedGeneratedType(typeof(Solti.Utils.Proxy.Generators.DuckGenerator<Foo.IMyInterface, Foo.MyImpl>))]

                    namespace System.Runtime.CompilerServices
                    {
                        using System;

                        internal sealed class ModuleInitializerAttribute : Attribute { }
                    }

                    namespace Foo
                    {
                        using System.Collections.Generic;

                        public interface IMyInterface
                        {
                        }

                        public class MyImpl 
                        {
                        }
                    }
                    ",
                    2
                );
            }
        }

        [Test]
        public void Execute_ShouldDefineTheModuleInitializerAttributeIfRequired([ValueSource(nameof(ValidSources2))] (string Src, int ExpectedTreeCount) ctx)
        {
            //
            // Ne a CreateCompilation()-t hasznaljuk mert az regisztralja a System.Private.CoreLib-et is (ami definialja a ModuleInitializerAttribute-t)
            //

            string[] fw = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet\\packs\\NETStandard.Library.Ref\\2.1.0\\ref\\netstandard2.1"), "*.dll");

            Compilation compilation = CSharpCompilation.Create
            (
                "cica",
                new[]
                {
                    CSharpSyntaxTree.ParseText(ctx.Src, new CSharpParseOptions())
                },
                fw.Append(typeof(EmbedGeneratedTypeAttribute).Assembly.Location).Select(asm => MetadataReference.CreateFromFile(asm)),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            Diagnostic[] errors = compilation
                .GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();
            if (errors.Any())
                throw new Exception("Bad source");

            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));

            GeneratorDriver driver = CreateDriver(null);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> diags);

            Assert.That(diags.Count(diag => diag.Id.StartsWith("PGE") && diag.Severity == DiagnosticSeverity.Warning), Is.EqualTo(0));
            Assert.That(diags.Count(diag => diag.Id.StartsWith("PGI") && diag.Severity == DiagnosticSeverity.Info), Is.EqualTo(ctx.ExpectedTreeCount - 1));
            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(ctx.ExpectedTreeCount));
        }

        [Test]
        public void Execute_ShouldRunOnlyIfRequired()
        {
            Compilation compilation = CreateCompilation
            (
                "namespace cica { class mica {} }",
                typeof(EmbedGeneratedTypeAttribute).Assembly
            );

            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));

            GeneratorDriver driver = CreateDriver(null);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> diags);

            Assert.That(diags.Count(diag => diag.Id.StartsWith("PGE") && diag.Severity == DiagnosticSeverity.Warning), Is.EqualTo(0));
            Assert.That(diags.Count(diag => diag.Id.StartsWith("PGI") && diag.Severity == DiagnosticSeverity.Info), Is.EqualTo(0));
            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));
        }

        public static IEnumerable<string> InvalidSources
        {
            get
            {
                yield return
                    @"
                    using System.Collections;

                    using Solti.Utils.Proxy;
                    using Solti.Utils.Proxy.Attributes;
                    using Solti.Utils.Proxy.Generators;

                    [assembly: EmbedGeneratedType(typeof(DuckGenerator<IEnumerable,>))]
                    ";
                yield return
                    @"
                    using Solti.Utils.Proxy;
                    using Solti.Utils.Proxy.Attributes;
                    using Solti.Utils.Proxy.Generators;

                    [assembly: EmbedGeneratedType(typeof(ProxyGenerator<IMyInterface, InterfaceInterceptor<IMyInterface>>))]

                    public interface IMyInterface
                    {
                        void Error(error);
                    }
                    ";
            }
        }

        [Test]
        public void Execute_ShouldHandleInvalidSyntax([ValueSource(nameof(InvalidSources))] string src)
        {
            Compilation compilation = CreateCompilation
            (
                src,
                new[] { typeof(EmbedGeneratedTypeAttribute).Assembly.Location },
                suppressErrors: true
            );

            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));

            GeneratorDriver driver = CreateDriver(null);
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

            GeneratorDriver driver = CreateDriver(null);
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
                PlatformAssemblies.References.Concat
                (
                    new[]
                    {
                        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                        MetadataReference.CreateFromFile(typeof(EmbedGeneratedTypeAttribute).Assembly.Location)
                    }
                ),
                new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            GeneratorDriver driver = CreateDriver(null);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> diags);

            Assert.That(diags.Any(diag => diag.Id == "PGE00" && diag.Severity == DiagnosticSeverity.Warning && diag.GetMessage() == SGResources.LNG_NOT_SUPPORTED));
            Assert.That(diags.Length, Is.EqualTo(1));
        }

        [Test]
        public void Execute_ShouldWarnOnUnsupportedLanguageVersion() 
        {
            Compilation compilation = CreateCompilation
            (
                @"
                using System.Collections;

                using Solti.Utils.Proxy;
                using Solti.Utils.Proxy.Attributes;
                using Solti.Utils.Proxy.Generators;

                [assembly: EmbedGeneratedType(typeof(DuckGenerator<IEnumerable, IEnumerable>))]
                ",
                new string[] { typeof(EmbedGeneratedTypeAttribute).Assembly.Location },
                Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6
            );

            GeneratorDriver driver = CreateDriver
            (
                new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp6)
            );
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> diags);

            Assert.That(diags.Any(diag => diag.Id == "PGE00" && diag.Severity == DiagnosticSeverity.Warning && diag.GetMessage() == SGResources.LNG_NOT_SUPPORTED));
            Assert.That(diags.Length, Is.EqualTo(1));
        }

        public static IEnumerable<Type> EmbeddedGenerators 
        {
            get 
            {
                foreach (var egta in typeof(EmbeddedTypeExposer).Assembly.GetCustomAttributes<EmbedGeneratedTypeAttribute>())
                {
                    yield return egta.Generator;
                }
            }
        }

        [TestCaseSource(nameof(EmbeddedGenerators))]
        public void BuiltAssembly_ShouldContainTheProxy(Type generator) 
        {
            Type generatedType = (Type) typeof(EmbeddedTypeExposer)
                .GetMethod(nameof(EmbeddedTypeExposer.GetGeneratedTypeByGenerator), BindingFlags.Public | BindingFlags.Static)
                .MakeGenericMethod(generator)
                .Invoke(null, null);

            Assert.That(generatedType.Assembly, Is.EqualTo(typeof(EmbeddedTypeExposer).Assembly));
        }

        public static IEnumerable<string> ProxyGeneration_AgainstRandomInterfaces_Params => RandomInterfaces<object>
            .Values
#if NET6_0_OR_GREATER
            // ref return values not supported
            .Except(new[] { typeof(ISpanFormattable) })
#endif
#if NET8_0_OR_GREATER
            // ref return values not supported
            .Except(new[] { typeof(IUtf8SpanFormattable) })
#endif
            .Select(static iface => iface.GetFriendlyName().Replace('{', '<').Replace('}', '>'));

        [TestCaseSource(nameof(ProxyGeneration_AgainstRandomInterfaces_Params))]
        public void ProxyGeneration_AgainstRandomInterfaces(string iface)
        {
            Compilation compilation = CreateCompilation
            (
                @$"
                    using Solti.Utils.Proxy;
                    using Solti.Utils.Proxy.Attributes;
                    using Solti.Utils.Proxy.Generators;

                    [assembly: EmbedGeneratedType(typeof(ProxyGenerator<{iface}, InterfaceInterceptor<{iface}>>))]

                ",
                typeof(EmbedGeneratedTypeAttribute).Assembly
            );

            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));

            GeneratorDriver driver = CreateDriver(null);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> diags);

            Assert.That(diags.Count(diag => diag.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error), Is.EqualTo(0));
        }

        public static IEnumerable<string> ProxyGeneration_AgainstRandomClasses_Params => ClassProxyGeneratorTests
            .GeneratedProxy_AgainstSystemType_Params
            .Select(static cls => cls.GetFriendlyName().Replace('{', '<').Replace('}', '>'));

        [TestCaseSource(nameof(ProxyGeneration_AgainstRandomClasses_Params))]
        public void ProxyGeneration_AgainstRandomClasses(string cls)
        {
            Compilation compilation = CreateCompilation
            (
                @$"
                    using Solti.Utils.Proxy;
                    using Solti.Utils.Proxy.Attributes;
                    using Solti.Utils.Proxy.Generators;

                    [assembly: EmbedGeneratedType(typeof(ProxyGenerator<{cls}>))]

                ",
                typeof(EmbedGeneratedTypeAttribute).Assembly
            );

            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));

            GeneratorDriver driver = CreateDriver(null);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> diags);

            Assert.That(diags.Count(diag => diag.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error), Is.EqualTo(0));
        }

        public static IEnumerable<string> DuckGeneration_AgainstRandomInterfaces_Params => RandomInterfaces<object>
            .Values
            // ambiguous match
            .Except(new[] { typeof(ITypeLib2), typeof(ITypeInfo2), typeof(IEnumerator<object>) })
            .Select(static iface => iface.GetFriendlyName().Replace('{', '<').Replace('}', '>'));

        [TestCaseSource(nameof(DuckGeneration_AgainstRandomInterfaces_Params))]
        public void DuckGeneration_AgainstRandomInterfaces(string iface)
        {
            Compilation compilation = CreateCompilation
            (
                @$"
                    using Solti.Utils.Proxy;
                    using Solti.Utils.Proxy.Attributes;
                    using Solti.Utils.Proxy.Generators;

                    [assembly: EmbedGeneratedType(typeof(DuckGenerator<{iface}, {iface}>))]

                ",
                typeof(EmbedGeneratedTypeAttribute).Assembly
            );

            Assert.That(compilation.SyntaxTrees.Count(), Is.EqualTo(1));

            GeneratorDriver driver = CreateDriver(null);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out ImmutableArray<Diagnostic> diags);

            Assert.That(diags.Count(diag => diag.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error), Is.EqualTo(0));
        }
    }
}
