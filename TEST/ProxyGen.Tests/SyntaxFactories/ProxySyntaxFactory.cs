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
    using Internals;

    using static Internals.Tests.CodeAnalysisTestsBase;
    using static Internals.ProxySyntaxFactory;

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

        private ProxySyntaxFactory Generator { get; set; }

        [SetUp]
        public void Setup() => Generator = new ProxySyntaxFactory(MetadataTypeInfo.CreateFrom(typeof(IFoo<int>)), MetadataTypeInfo.CreateFrom(typeof(FooInterceptor)), typeof(ProxySyntaxFactoryTests).Assembly.GetName().Name, OutputType.Module);

        private class NonAbstractProxyMemberSyntaxFactory : ProxyMemberSyntaxFactory
        {
            public NonAbstractProxyMemberSyntaxFactory(IProxyContext context) : base(context) { }
            protected override IEnumerable<MemberDeclarationSyntax> BuildMembers(CancellationToken cancellation) => throw new NotImplementedException();
        }

        [TestCaseSource(nameof(MethodsToWhichTheArrayIsCreated))]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments((object Method, string Expected) para) =>
            Assert.That(new NonAbstractProxyMemberSyntaxFactory(Generator).CreateArgumentsArray((IMethodInfo) para.Method).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));

        [Test]
        public void AssignByRefParameters_ShouldAssignByRefParameters()
        {
            var fact = new MethodInterceptorFactory(Generator);

            IReadOnlyList<ExpressionStatementSyntax> assignments = fact.AssignByRefParameters
            (
                Foo.Parameters, fact.DeclareLocal<object[]>("args")
            ).ToArray();

            Assert.That(assignments.Count, Is.EqualTo(2));
            Assert.That(assignments[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("b = (global::System.String)args[1];"));
            Assert.That(assignments[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("c = (TT)args[2];"));
        }

        [Test]
        public void LocalArgs_ShouldBeDeclaredForEachArgument()
        {
            var fact = new NonAbstractProxyMemberSyntaxFactory(Generator);

            IReadOnlyList<LocalDeclarationStatementSyntax> locals = fact.DeclareCallbackLocals(fact.DeclareLocal<object[]>("args"), Foo.Parameters).ToArray();

            Assert.That(locals.Count, Is.EqualTo(3));
            Assert.That(locals[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("global::System.Int32 cb_a = (global::System.Int32)args[0];"));
            Assert.That(locals[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("global::System.String cb_b;"));
            Assert.That(locals[2].NormalizeWhitespace().ToFullString(), Is.EqualTo("TT cb_c = (TT)args[2];"));
        }

        [Test]
        public void ReassignArgsArray_ShouldCopyByRefArgumentsBack()
        {
            var fact = new MethodInterceptorFactory(Generator);

            IEnumerable<StatementSyntax> assigns = fact.ReassignArgsArray
            (
                Foo.Parameters,
                fact.DeclareLocal<object[]>("args"),
                fact.DeclareCallbackLocals(fact.CreateArgumentsArray(Foo), Foo.Parameters)
            );

            Assert.That(assigns.Count, Is.EqualTo(2));
            Assert.That(assigns.First().NormalizeWhitespace().ToFullString(), Is.EqualTo("args[1] = (global::System.Object)cb_b;"));
            Assert.That(assigns.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("args[2] = (global::System.Object)cb_c;"));
        }

        [Test]
        public void BuildCallback_ShouldCreateTheProperLambda()
        {
            var fact = new MethodInterceptorFactory(Generator);

            Assert.That(fact.BuildCallback(Foo, fact.DeclareLocal<object[]>("args")).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("CallbackSrc.txt")));
        }

        public static (Type Type, string Local, string Expected)[] ReturnTypes = new[]
        {
            (typeof(void), "@void", "return;"),
            (typeof(List<int>), "result", "return (global::System.Collections.Generic.List<global::System.Int32>)result;")
        };

        [TestCaseSource(nameof(ReturnTypes))]
        public void ReturnResult_ShouldCreateTheProperExpression((Type Type, string Local, string Expected) para)
        {
            var fact = new NonAbstractProxyMemberSyntaxFactory(Generator);

            Assert.That(fact.ReturnResult(MetadataTypeInfo.CreateFrom(para.Type), fact.DeclareLocal<object>(para.Local)).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));
        }

        public static (object Method, string File)[] Methods = new[]
        {
            ((object) Foo, "FooSrc.txt"),
            (Bar, "BarSrc.txt")
        };

        [TestCaseSource(nameof(Methods))]
        public void GenerateProxyMethod_Test((object Method, string File) para)
        {
            var fact = new MethodInterceptorFactory(Generator);
            fact.Build(default);

            Assert.That(fact.Members, Is.Not.Null);
            Assert.That(fact.Members.Count, Is.EqualTo(2));
            Assert.That(fact.Members.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText(para.File))));
        }

        [Test]
        public void GenerateProxyProperty_Test()
        {
            var fact = new PropertyInterceptorFactory(Generator);
            fact.Build(default);

            Assert.That(fact.Members, Is.Not.Null);
            Assert.That(fact.Members.Count, Is.EqualTo(1));
            Assert.That(fact.Members.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText("PropSrc.txt"))));
        }

        [Test]
        public void GenerateProxyIndexer_Test()
        {
            var fact = new PropertyInterceptorFactory(new ProxySyntaxFactory(MetadataTypeInfo.CreateFrom(typeof(IList<int>)), MetadataTypeInfo.CreateFrom(typeof(InterfaceInterceptor<IList<int>>)), "cica", OutputType.Module));
            fact.Build(default);

            Assert.That(fact.Members, Is.Not.Null);
            Assert.That(fact.Members.Count, Is.EqualTo(3));
            Assert.That(fact.Members.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText("IndexerSrc.txt"))));
        }

        [Test]
        public void GenerateProxyEvent_Test()
        {
            var fact = new EventInterceptorFactory(Generator);
            fact.Build(default);

            Assert.That(fact.Members, Is.Not.Null);
            Assert.That(fact.Members.Count, Is.EqualTo(1));
            Assert.That(fact.Members.Any(member => member.NormalizeWhitespace(eol: "\n").ToFullString().Equals(File.ReadAllText("EventSrc.txt"))));
        }

        [Test]
        public void GenerateProxyClass_Test()
        {
            Generator.Build(default);

            Assert.That(Generator.Unit.NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("ClsSrc.txt")));
        }

        public static IEnumerable<Type> RandomInterfaces => Proxy.Tests.RandomInterfaces<string>.Values;

        [Test]
        public void GenerateProxyClass_ShouldReturnTheSameValidSourceInCaseOfSymbolAndMetadata([ValueSource(nameof(RandomInterfaces))] Type type, [Values(OutputType.Module, OutputType.Unit)] OutputType outputType) 
        {
            Assembly[] refs = type
                .Assembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Append(type.Assembly)
                .Distinct()
                .ToArray();

            Compilation compilation = CreateCompilation(string.Empty, refs);

            ITypeInfo
                type1 = MetadataTypeInfo.CreateFrom(type),
                type2 = SymbolTypeInfo.CreateFrom(SymbolTypeInfo.TypeInfoToSymbol(type1, compilation), compilation);

            IGenericTypeInfo
                interceptor = (IGenericTypeInfo) MetadataTypeInfo.CreateFrom(typeof(InterfaceInterceptor<>));

            IUnitSyntaxFactory
                fact1 = new ProxySyntaxFactory(type1, (ITypeInfo) interceptor.Close(type1), "cica", outputType),
                fact2 = new ProxySyntaxFactory(type2, (ITypeInfo) interceptor.Close(type2), "cica", outputType);

            Assert.DoesNotThrow(() => fact1.Build());
            Assert.DoesNotThrow(() => fact2.Build());

            string
                src1 = fact1.Unit.NormalizeWhitespace().ToFullString(),
                src2 = fact2.Unit.NormalizeWhitespace().ToFullString();

            // Assert.AreEqual(src1, src2); // deklaraciok sorrendje nem biztos h azonos
            Assert.DoesNotThrow(() => CreateCompilation(src1, fact1.References.Select(asm => asm.Location)));
            Assert.DoesNotThrow(() => CreateCompilation(src2, fact2.References.Select(asm => asm.Location)));
        }

        public static IEnumerable<ISyntaxFactory> Factories 
        {
            get 
            {
                ITypeInfo
                    iface = MetadataTypeInfo.CreateFrom(typeof(IComplex)),
                    interceptor = MetadataTypeInfo.CreateFrom(typeof(InterfaceInterceptor<IComplex>));

                var fact = new ProxySyntaxFactory(iface, interceptor, "cica", OutputType.Module);

                yield return new ConstructorFactory(fact);
                yield return new InvokeFactory(fact);
                yield return new MethodInterceptorFactory(fact);
                yield return new PropertyInterceptorFactory(fact);
                yield return new EventInterceptorFactory(fact);
            }
        }

        [TestCaseSource(nameof(Factories))]
        public void Factory_CanBeCancelled(ISyntaxFactory fact)
        {
            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                Assert.Throws<OperationCanceledException>(() => fact.Build(cancellation.Token));
            }
        }
    }
}