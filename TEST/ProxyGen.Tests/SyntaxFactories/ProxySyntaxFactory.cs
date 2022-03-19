/********************************************************************************
* ProxySyntaxFactory.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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
    public sealed class ProxySyntaxFactoryTests : SyntaxFactoryTestsBase
    {
        internal class FooInterceptor : InterfaceInterceptor<IFoo<int>> // direkt internal
        {
            public FooInterceptor(IFoo<int> target) : base(target)
            {
            }
        }

        public static (object Method, string Expected)[] MethodsToWhichTheArrayIsCreated = new[]
        {
            ((object) Foo, "global::System.Object[] args = new global::System.Object[]{a, default(global::System.String), c};"),
            (Bar, "global::System.Object[] args = new global::System.Object[0];")
        };

        private static ProxySyntaxFactory CreateGenerator(OutputType outputType) => new ProxySyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(typeof(IFoo<int>)),
            MetadataTypeInfo.CreateFrom(typeof(FooInterceptor)),
            typeof(ProxySyntaxFactoryTests).Assembly.GetName().Name,
            outputType,
            null
        );

        [TestCaseSource(nameof(MethodsToWhichTheArrayIsCreated))]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments((object Method, string Expected) para) =>
            Assert.That(CreateGenerator(OutputType.Module).ResolveArgumentsArray((IMethodInfo) para.Method).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));

        [Test]
        public void AssignByRefParameters_ShouldAssignByRefParameters()
        {
            ProxySyntaxFactory gen = CreateGenerator(OutputType.Module);

            IReadOnlyList<ExpressionStatementSyntax> assignments = gen.AssignByRefParameters
            (
                Foo, gen.ResolveLocal<object[]>("args")
            ).ToArray();

            Assert.That(assignments.Count, Is.EqualTo(2));
            Assert.That(assignments[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("b = (global::System.String)args[1];"));
            Assert.That(assignments[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("c = (TT)args[2];"));
        }

        [Test]
        public void LocalArgs_ShouldBeDeclaredForEachArgument()
        {
            ProxySyntaxFactory gen = CreateGenerator(OutputType.Module);

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
                Foo
            ).ToArray();

            Assert.That(locals.Count, Is.EqualTo(3));
            Assert.That(locals[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("global::System.Int32 _a = (global::System.Int32)args[0];"));
            Assert.That(locals[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("global::System.String _b;"));
            Assert.That(locals[2].NormalizeWhitespace().ToFullString(), Is.EqualTo("TT _c = (TT)args[2];"));
        }

        [Test]
        public void ReassignArgsArray_ShouldCopyByRefArgumentsBack()
        {
            ProxySyntaxFactory gen = CreateGenerator(OutputType.Module);

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
                Foo,
                args,
                gen.ResolveInvokeTargetLocals(args, Foo)
            );

            Assert.That(assigns.Count, Is.EqualTo(2));
            Assert.That(assigns.First().NormalizeWhitespace().ToFullString(), Is.EqualTo("args[1] = (global::System.Object)_b;"));
            Assert.That(assigns.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("args[2] = (global::System.Object)_c;"));
        }

        [Test]
        public void ResolveInvokeTarget_ShouldCreateTheProperMethod()
        {
            ProxySyntaxFactory gen = CreateGenerator(OutputType.Module);

            Assert.That(gen.ResolveInvokeTarget(Foo).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("CallbackSrc.txt")));
        }

        public static (Type Type, string Local, string Expected)[] ReturnTypes = new[]
        {
            (typeof(void), "@void", "return;"),
            (typeof(List<int>), "result", "return (global::System.Collections.Generic.List<global::System.Int32>)result;")
        };

        [TestCaseSource(nameof(ReturnTypes))]
        public void ReturnResult_ShouldCreateTheProperExpression((Type Type, string Local, string Expected) para)
        {
            ProxySyntaxFactory gen = CreateGenerator(OutputType.Module);

            Assert.That(gen.ReturnResult(MetadataTypeInfo.CreateFrom(para.Type), gen.ResolveLocal<object>(para.Local)).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));
        }

        public static (object Method, string File)[] Methods = new[]
        {
            ((object) Foo, "FooSrc.txt"),
            (Bar, "BarSrc.txt")
        };

        [TestCaseSource(nameof(Methods))]
        public void ResolveMethod_ShouldGenerateTheProperInterceptor((object Method, string File) para)
        {
            MethodDeclarationSyntax[] methods = CreateGenerator(OutputType.Module).ResolveMethods(null).ToArray();

            Assert.That(methods.Count, Is.EqualTo(3));
            Assert.That(methods.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText(para.File))));
        }

        [Test]
        public void ResolveProperty_ShouldGenerateTheProperInterceptor()
        {
            BasePropertyDeclarationSyntax[] props = CreateGenerator(OutputType.Module).ResolveProperties(null).ToArray();

            Assert.That(props.Count, Is.EqualTo(1));
            Assert.That(props.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText("PropSrc.txt"))));
        }

        [Test]
        public void ResolveProperty_ShouldGenerateTheIndexerInterceptor()
        {
            BasePropertyDeclarationSyntax[] props = new ProxySyntaxFactory
            (
                MetadataTypeInfo.CreateFrom(typeof(IList<int>)), 
                MetadataTypeInfo.CreateFrom(typeof(InterfaceInterceptor<IList<int>>)), 
                "cica", 
                OutputType.Module,
                null
            ).ResolveProperties(null).ToArray();

            Assert.That(props.Count, Is.EqualTo(3));
            Assert.That(props.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText("IndexerSrc.txt"))));
        }

        [Test]
        public void GenerateProxyEvent_Test()
        {
            EventDeclarationSyntax[] evts = CreateGenerator(OutputType.Module).ResolveEvents(null).ToArray();

            Assert.That(evts.Count, Is.EqualTo(1));
            Assert.That(evts.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText("EventSrc.txt"))));
        }

        [TestCase(OutputType.Module, "ClsSrcModule.txt")]
        [TestCase(OutputType.Unit, "ClsSrcUnit.txt")]
        public void ResolveUnit_ShouldGenerateTheDesiredUnit(int outputType, string fileName)
        {
            ProxySyntaxFactory gen = CreateGenerator((OutputType) outputType);

            Assert.That(gen.ResolveUnit(null, default).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText(fileName).Replace("{version}", typeof(ProxyGenerator<,>).Assembly.GetName().Version.ToString())));
        }

        public static IEnumerable<Type> RandomInterfaces => Proxy
            .Tests
            .RandomInterfaces<string>
            .Values
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

            ProxySyntaxFactory
                fact1 = new ProxySyntaxFactory
                (
                    type1, 
                    interceptor1.Close(type1), 
                    "cica", 
                    (OutputType) outputType,
                    new ReferenceCollector()
                ),
                fact2 = new ProxySyntaxFactory
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
                lines1 = src1.Split(Environment.NewLine).OrderBy(l => l).ToArray(),
                lines2 = src2.Split(Environment.NewLine).OrderBy(l => l).ToArray();

            Assert.That(lines1.Length, Is.EqualTo(lines2.Length));
            Assert.That(lines1.SequenceEqual(lines2));
        }

        [Test]
        public void Factory_CanBeCancelled()
        {
            ProxySyntaxFactory gen = CreateGenerator(OutputType.Module);

            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                Assert.Throws<OperationCanceledException>(() => gen.ResolveUnit(null, cancellation.Token));
            }
        }
    }
}