/********************************************************************************
* InterfaceProxySyntaxFactory.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Generators;
    using Internals;
    using static Internals.Tests.CodeAnalysisTestsBase;

    [TestFixture, Parallelizable(ParallelScope.All)]
    public sealed class InterfaceProxySyntaxFactoryTests : SyntaxFactoryTestsBase
    {
        private static ClassDeclarationSyntax GetDummyClass() => ClassDeclaration("Dummy");

        internal class FooInterceptor : InterfaceInterceptor<IFoo<int>> // direkt internal
        {
            public FooInterceptor(IFoo<int> target) : base(target)
            {
            }
        }

        public static (object Method, string Expected)[] MethodsToWhichTheArrayIsCreated = new[]
        {
            ((object) FooMethod, "global::System.Object[] args = new global::System.Object[]{a, default(global::System.String), c};"),
            (BarMethod, "global::System.Object[] args = new global::System.Object[0];")
        };

        private static InterfaceProxySyntaxFactory CreateSyntaxFactory(Type iface, Type interceptor, OutputType outputType) => new
        (
            MetadataTypeInfo.CreateFrom(iface),
            MetadataTypeInfo.CreateFrom(interceptor),
            typeof(InterfaceProxySyntaxFactoryTests).Assembly.GetName().Name,
            outputType,
            null
        );

        private static InterfaceProxySyntaxFactory CreateSyntaxFactory<TInterface, TInterceptor>(OutputType outputType) => CreateSyntaxFactory(typeof(TInterface), typeof(TInterceptor), outputType);

        [TestCaseSource(nameof(MethodsToWhichTheArrayIsCreated))]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments((object Method, string Expected) para) =>
            Assert.That(CreateSyntaxFactory<IFoo<int>, FooInterceptor>(OutputType.Module).ResolveArgumentsArray((IMethodInfo) para.Method).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));

        [Test]
        public void AssignByRefParameters_ShouldAssignByRefParameters()
        {
            InterfaceProxySyntaxFactory gen = CreateSyntaxFactory<IFoo<int>, FooInterceptor>(OutputType.Module);

            IReadOnlyList<ExpressionStatementSyntax> assignments = gen.AssignByRefParameters
            (
                FooMethod, gen.ResolveLocal<object[]>("args")
            ).ToArray();

            Assert.That(assignments.Count, Is.EqualTo(2));
            Assert.That(assignments[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("b = (global::System.String)args[1];"));
            Assert.That(assignments[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("c = (TT)args[2];"));
        }

        [Test]
        public void LocalArgs_ShouldBeDeclaredForEachArgument()
        {
            InterfaceProxySyntaxFactory gen = CreateSyntaxFactory<IFoo<int>, FooInterceptor>(OutputType.Module);

            IReadOnlyList<LocalDeclarationStatementSyntax> locals = gen.ResolveInvokeTargetLocals
            (
                Parameter
                (
                    identifier: Identifier("args")
                )
                .WithType
                (
                    gen.ResolveType<object[]>()
                ), 
                FooMethod
            ).ToArray();

            Assert.That(locals.Count, Is.EqualTo(3));
            Assert.That(locals[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("global::System.Int32 _a = (global::System.Int32)args[0];"));
            Assert.That(locals[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("global::System.String _b;"));
            Assert.That(locals[2].NormalizeWhitespace().ToFullString(), Is.EqualTo("TT _c = (TT)args[2];"));
        }

        [Test]
        public void ReassignArgsArray_ShouldCopyByRefArgumentsBack()
        {
            InterfaceProxySyntaxFactory gen = CreateSyntaxFactory<IFoo<int>, FooInterceptor>(OutputType.Module);

            ParameterSyntax args = Parameter
            (
                identifier: Identifier("args")
            )
            .WithType
            (
                gen.ResolveType<object[]>()
            );

            IEnumerable<StatementSyntax> assigns = gen.ReassignArgsArray
            (
                FooMethod,
                args,
                gen.ResolveInvokeTargetLocals(args, FooMethod).ToList()
            );

            Assert.That(assigns.Count, Is.EqualTo(2));
            Assert.That(assigns.First().NormalizeWhitespace().ToFullString(), Is.EqualTo("args[1] = (global::System.Object)_b;"));
            Assert.That(assigns.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("args[2] = (global::System.Object)_c;"));
        }

        [Test]
        public void ResolveInvokeTarget_ShouldCreateTheProperMethod()
        {
            InterfaceProxySyntaxFactory gen = CreateSyntaxFactory<IFoo<int>, FooInterceptor>(OutputType.Module);

            Assert.That(gen.ResolveInvokeTarget(FooMethod).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CallbackSrc.txt"))));
        }

        public static (Type Type, string Local, string Expected)[] ReturnTypes = new[]
        {
            (typeof(void), "@void", "return;"),
            (typeof(List<int>), "result", "return (global::System.Collections.Generic.List<global::System.Int32>)result;")
        };

        [TestCaseSource(nameof(ReturnTypes))]
        public void ReturnResult_ShouldCreateTheProperExpression((Type Type, string Local, string Expected) para)
        {
            InterfaceProxySyntaxFactory gen = CreateSyntaxFactory<IFoo<int>, FooInterceptor>(OutputType.Module);

            Assert.That(gen.ReturnResult(MetadataTypeInfo.CreateFrom(para.Type), gen.ResolveLocal<object>(para.Local)).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));
        }

        public static (object Method, string File)[] Methods = new[]
        {
            ((object) FooMethod, "FooSrc.txt"),
            (BarMethod, "BarSrc.txt")
        };

        [TestCaseSource(nameof(Methods))]
        public void ResolveMethod_ShouldGenerateTheProperInterceptor((object Method, string File) para)
        {
            SyntaxList<MemberDeclarationSyntax> methods = CreateSyntaxFactory<IFoo<int>, FooInterceptor>(OutputType.Module).ResolveMethods(GetDummyClass(), null).Members;

            Assert.That(methods.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, para.File)))));
        }

        [Test]
        public void ResolveProperty_ShouldGenerateTheProperInterceptor()
        {
            SyntaxList<MemberDeclarationSyntax> props = CreateSyntaxFactory<IFoo<int>, FooInterceptor>(OutputType.Module).ResolveProperties(GetDummyClass(), null).Members;

            Assert.That(props.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PropSrc.txt")))));
        }

        [Test]
        public void ResolveProperty_ShouldGenerateTheIndexerInterceptor()
        {
            SyntaxList<MemberDeclarationSyntax> props = new InterfaceProxySyntaxFactory
            (
                MetadataTypeInfo.CreateFrom(typeof(IList<int>)), 
                MetadataTypeInfo.CreateFrom(typeof(InterfaceInterceptor<IList<int>>)), 
                "cica", 
                OutputType.Module,
                null
            ).ResolveProperties(GetDummyClass(), null).Members;

            Assert.That(props.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IndexerSrc.txt")))));
        }

        [Test]
        public void GenerateProxyEvent_Test()
        {
            SyntaxList<MemberDeclarationSyntax> evts = CreateSyntaxFactory<IFoo<int>, FooInterceptor>(OutputType.Module).ResolveEvents(GetDummyClass(), null).Members;

            Assert.That(evts.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EventSrc.txt")))));
        }

        [TestCase(typeof(IFoo<int>), typeof(FooInterceptor), OutputType.Module, "IfaceProxySrcModule.txt")]
        [TestCase(typeof(IFoo<int>), typeof(FooInterceptor), OutputType.Unit, "IfaceProxySrcUnit.txt")]
#if !NETFRAMEWORK // The InterfaceInterceptionContext field gets different name when targeting .NET FW (TODO: Investigate why)
        [TestCase(typeof(IList), typeof(InterfaceInterceptor<IList, List<object>>), OutputType.Module, "IfaceProxyHavingTargetSrcModule.txt")]
#endif
        public void ResolveUnit_ShouldGenerateTheDesiredUnit(Type iface, Type interceptor, int outputType, string fileName)
        {
            InterfaceProxySyntaxFactory gen = CreateSyntaxFactory(iface, interceptor, (OutputType) outputType);

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
#if LEGACY_COMPILER
                        .Replace(") :", "):")
#endif
                        .Replace("{version}", typeof(ProxyGenerator<,>)
                            .Assembly
                            .GetName()
                            .Version
                            .ToString())
                )
            );
        }

        public static IEnumerable<Type> RandomInterfaces => Proxy
            .Tests
            .RandomInterfaces<string>
            .Values
#if NET8_0_OR_GREATER
            .Except(new[] { typeof(IParsable<string>), typeof(ISpanParsable<string>) })
#endif
#if NET6_0_OR_GREATER
            .Where(t => !t.GetMethods(BindingFlags.Instance | BindingFlags.Public).Any(m => m.GetParameters().Any(p => p.ParameterType.IsByRefLike)))
#endif
            ;

        [Test]
        public void ResolveUnit_ShouldReturnTheSameValidSourceInCaseOfSymbolAndMetadata([ValueSource(nameof(RandomInterfaces))] Type type, [Values(OutputType.Module, OutputType.Unit)] int outputType) 
        {
            Assembly[] refs = type
                .Assembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Concat(new[] { type.Assembly, typeof(InterfaceInterceptor<>).Assembly })
                .Distinct()
                .ToArray();

            Compilation compilation = CreateCompilation(string.Empty, refs);

            ITypeInfo
                type1 = MetadataTypeInfo.CreateFrom(type),
                type2 = SymbolTypeInfo.CreateFrom(type1.ToSymbol(compilation), compilation);

            IGenericTypeInfo
                interceptor1 = (IGenericTypeInfo) MetadataTypeInfo.CreateFrom(typeof(InterfaceInterceptor<>)),
                interceptor2 = (IGenericTypeInfo) SymbolTypeInfo.CreateFrom(compilation.GetTypeByMetadataName(typeof(InterfaceInterceptor<>).FullName), compilation);

            InterfaceProxySyntaxFactory
                fact1 = new InterfaceProxySyntaxFactory
                (
                    type1, 
                    interceptor1.Close(type1), 
                    "cica", 
                    (OutputType) outputType,
                    new ReferenceCollector()
                ),
                fact2 = new InterfaceProxySyntaxFactory
                (
                    type2, 
                    interceptor2.Close(type2), 
                    "cica", 
                    (OutputType) outputType,
                    new ReferenceCollector()
                );

            string
                src1 = fact1.ResolveUnit(null, default).NormalizeWhitespace().ToFullString(),
                src2 = fact2.ResolveUnit(null, default).NormalizeWhitespace().ToFullString();

            //
            // Deklaraciok sorrendje nem biztos h azonos ezert ez a csoda
            //
           
            string[]
                lines1 = src1.Split(Environment.NewLine.ToArray()).OrderBy(l => l).ToArray(),
                lines2 = src2.Split(Environment.NewLine.ToArray()).OrderBy(l => l).ToArray();

            Assert.That(lines1.Length, Is.EqualTo(lines2.Length));
            Assert.That(lines1.SequenceEqual(lines2));
        }

        [Test]
        public void Factory_CanBeCancelled()
        {
            InterfaceProxySyntaxFactory gen = CreateSyntaxFactory<IFoo<int>, FooInterceptor>(OutputType.Module);

            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                Assert.Throws<OperationCanceledException>(() => gen.ResolveUnit(null, cancellation.Token));
            }
        }
    }
}