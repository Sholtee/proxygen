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

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Generators;
    using Internals;

    using static Internals.Tests.CodeAnalysisTestsBase;

    [TestFixture]
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
            MetadataTypeInfo.CreateFrom(typeof(ProxyGenerator<IFoo<int>, FooInterceptor>)),
            null
        );


        [TestCaseSource(nameof(MethodsToWhichTheArrayIsCreated))]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments((object Method, string Expected) para) =>
            Assert.That(CreateGenerator(OutputType.Module).CreateArgumentsArray((IMethodInfo) para.Method).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));

        [Test]
        public void AssignByRefParameters_ShouldAssignByRefParameters()
        {
            ProxySyntaxFactory gen = CreateGenerator(OutputType.Module);

            IReadOnlyList<ExpressionStatementSyntax> assignments = gen.AssignByRefParameters
            (
                Foo.Parameters, gen.DeclareLocal<object[]>("args")
            ).ToArray();

            Assert.That(assignments.Count, Is.EqualTo(2));
            Assert.That(assignments[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("b = (global::System.String)args[1];"));
            Assert.That(assignments[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("c = (TT)args[2];"));
        }

        [Test]
        public void LocalArgs_ShouldBeDeclaredForEachArgument()
        {
            ProxySyntaxFactory gen = CreateGenerator(OutputType.Module);

            IReadOnlyList<LocalDeclarationStatementSyntax> locals = gen.DeclareCallbackLocals(gen.DeclareLocal<object[]>("args"), Foo.Parameters).ToArray();

            Assert.That(locals.Count, Is.EqualTo(3));
            Assert.That(locals[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("global::System.Int32 cb_a = (global::System.Int32)args[0];"));
            Assert.That(locals[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("global::System.String cb_b;"));
            Assert.That(locals[2].NormalizeWhitespace().ToFullString(), Is.EqualTo("TT cb_c = (TT)args[2];"));
        }

        [Test]
        public void ReassignArgsArray_ShouldCopyByRefArgumentsBack()
        {
            ProxySyntaxFactory gen = CreateGenerator(OutputType.Module);

            IEnumerable<StatementSyntax> assigns = gen.ReassignArgsArray
            (
                Foo.Parameters,
                gen.DeclareLocal<object[]>("args"),
                gen.DeclareCallbackLocals(gen.CreateArgumentsArray(Foo), Foo.Parameters)
            );

            Assert.That(assigns.Count, Is.EqualTo(2));
            Assert.That(assigns.First().NormalizeWhitespace().ToFullString(), Is.EqualTo("args[1] = (global::System.Object)cb_b;"));
            Assert.That(assigns.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("args[2] = (global::System.Object)cb_c;"));
        }

        [Test]
        public void BuildCallback_ShouldCreateTheProperLambda()
        {
            ProxySyntaxFactory gen = CreateGenerator(OutputType.Module);

            Assert.That(gen.BuildMethodInterceptorCallback(Foo, gen.DeclareLocal<object[]>("args")).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("CallbackSrc.txt")));
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

            Assert.That(gen.ReturnResult(MetadataTypeInfo.CreateFrom(para.Type), gen.DeclareLocal<object>(para.Local)).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));
        }

        public static (object Method, string File)[] Methods = new[]
        {
            ((object) Foo, "FooSrc.txt"),
            (Bar, "BarSrc.txt")
        };

        [TestCaseSource(nameof(Methods))]
        public void GenerateProxyMethod_Test((object Method, string File) para)
        {
            MethodDeclarationSyntax[] methods = CreateGenerator(OutputType.Module).ResolveMethods(null).ToArray();

            Assert.That(methods.Count, Is.EqualTo(3));
            Assert.That(methods.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText(para.File))));
        }

        [Test]
        public void GenerateProxyProperty_Test()
        {
            BasePropertyDeclarationSyntax[] props = CreateGenerator(OutputType.Module).ResolveProperties(null).ToArray();

            Assert.That(props.Count, Is.EqualTo(1));
            Assert.That(props.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText("PropSrc.txt"))));
        }

        [Test]
        public void GenerateProxyIndexer_Test()
        {
            BasePropertyDeclarationSyntax[] props = new ProxySyntaxFactory
            (
                MetadataTypeInfo.CreateFrom(typeof(IList<int>)), 
                MetadataTypeInfo.CreateFrom(typeof(InterfaceInterceptor<IList<int>>)), 
                "cica", 
                OutputType.Module, 
                MetadataTypeInfo.CreateFrom(typeof(ProxyGenerator<IList<int>, InterfaceInterceptor<IList<int>>>)),
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
        public void GenerateProxyClass_Test(int outputType, string fileName)
        {
            ProxySyntaxFactory gen = CreateGenerator((OutputType) outputType);

            Assert.That(gen.ResolveUnit(null, default).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText(fileName).Replace("{version}", typeof(ProxyGenerator<,>).Assembly.GetName().Version.ToString())));
        }

        public static IEnumerable<Type> RandomInterfaces => Proxy.Tests.RandomInterfaces<string>.Values;

        [Test]
        public void GenerateProxyClass_ShouldReturnTheSameValidSourceInCaseOfSymbolAndMetadata([ValueSource(nameof(RandomInterfaces))] Type type, [Values(OutputType.Module, OutputType.Unit)] int outputType) 
        {
            Assembly[] refs = type
                .Assembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Append(type.Assembly)
                .Append(typeof(InterfaceInterceptor<>).Assembly)
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
                    MetadataTypeInfo.CreateFrom
                    (
                        typeof(ProxyGenerator<,>).MakeGenericType
                        (
                            type, 
                            typeof(InterfaceInterceptor<>).MakeGenericType(type)
                        )
                    ),
                    new ReferenceCollector()
                ),
                fact2 = new ProxySyntaxFactory
                (
                    type2, 
                    interceptor2.Close(type2), 
                    "cica", 
                    (OutputType) outputType, 
                    SymbolTypeInfo.CreateFrom
                    (
                        compilation.GetTypeByMetadataName(typeof(ProxyGenerator<,>).FullName).Construct
                        (
                            type2.ToSymbol(compilation),
                            interceptor2.Close(type2).ToSymbol(compilation)
                        ), 
                        compilation
                    ),
                    new ReferenceCollector()
                );

            string
                src1 = fact1.ResolveUnit(null, default).NormalizeWhitespace().ToFullString(),
                src2 = fact2.ResolveUnit(null, default).NormalizeWhitespace().ToFullString();

            // Assert.AreEqual(src1, src2); // deklaraciok sorrendje nem biztos h azonos
            Assert.DoesNotThrow(() => CreateCompilation(src1, fact1.ReferenceCollector.References.Select(asm => asm.Location)));
            Assert.DoesNotThrow(() => CreateCompilation(src2, fact2.ReferenceCollector.References.Select(asm => asm.Location)));
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