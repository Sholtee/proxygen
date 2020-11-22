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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Internals;
    using static Internals.ProxySyntaxFactory<ProxySyntaxFactoryTestsBase.IFoo<int>, ProxySyntaxFactoryTests.FooInterceptor>;

    [TestFixture]
    public sealed class ProxySyntaxFactoryTests : ProxySyntaxFactoryTestsBase
    {
        private static IPropertyInfo Indexer { get; } = MetadataPropertyInfo.CreateFrom(typeof(IList<int>).GetProperty("Item"));

        internal class FooInterceptor : InterfaceInterceptor<IFoo<int>> // direkt internal
        {
            public FooInterceptor(IFoo<int> target) : base(target)
            {
            }
        }

        public static (object Method, string Expected)[] MethodsToWhichTheArrayIsCreated = new[]
        {
            ((object) Foo, "System.Object[] args = new System.Object[]{a, default(System.String), c};"),
            (Bar, "System.Object[] args = new System.Object[0];")
        };

        private Internals.ProxySyntaxFactory<IFoo<int>, FooInterceptor> Generator { get; set; }

        [SetUp]
        public void Setup() => Generator = new ProxySyntaxFactory<IFoo<int>, FooInterceptor>();

        [TestCaseSource(nameof(MethodsToWhichTheArrayIsCreated))]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments((object Method, string Expected) para) =>
            Assert.That(Generator.CreateArgumentsArray((IMethodInfo) para.Method).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));

        [Test]
        public void AssignByRefParameters_ShouldAssignByRefParameters()
        {
            IReadOnlyList<ExpressionStatementSyntax> assignments = new MethodInterceptorFactory().AssignByRefParameters(Foo.Parameters, Generator.DeclareLocal<object[]>("args")).ToArray();

            Assert.That(assignments.Count, Is.EqualTo(2));
            Assert.That(assignments[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("b = (System.String)args[1];"));
            Assert.That(assignments[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("c = (TT)args[2];"));
        }

        [Test]
        public void LocalArgs_ShouldBeDeclaredForEachArgument()
        {
            IReadOnlyList<LocalDeclarationStatementSyntax> locals = Generator.DeclareCallbackLocals(Generator.DeclareLocal<object[]>("args"), Foo.Parameters).ToArray();

            Assert.That(locals.Count, Is.EqualTo(3));
            Assert.That(locals[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32 cb_a = (System.Int32)args[0];"));
            Assert.That(locals[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("System.String cb_b;"));
            Assert.That(locals[2].NormalizeWhitespace().ToFullString(), Is.EqualTo("TT cb_c = (TT)args[2];"));
        }

        [Test]
        public void ReassignArgsArray_ShouldCopyByRefArgumentsBack()
        {
            IEnumerable<StatementSyntax> assigns = new MethodInterceptorFactory().ReassignArgsArray
            (
                Foo.Parameters,
                Generator.DeclareLocal<object[]>("args"),
                Generator.DeclareCallbackLocals(Generator.CreateArgumentsArray(Foo), Foo.Parameters)
            );

            Assert.That(assigns.Count, Is.EqualTo(2));
            Assert.That(assigns.First().NormalizeWhitespace().ToFullString(), Is.EqualTo("args[1] = (System.Object)cb_b;"));
            Assert.That(assigns.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("args[2] = (System.Object)cb_c;"));
        }

        [Test]
        public void BuildCallback_ShouldCreateTheProperLambda() =>
            Assert.That(new MethodInterceptorFactory().BuildCallback(Foo, Generator.DeclareLocal<object[]>("args")).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("CallbackSrc.txt")));

        public static (Type Type, string Local, string Expected)[] ReturnTypes = new[]
        {
            (typeof(void), "@void", "return;"),
            (typeof(List<int>), "result", "return (System.Collections.Generic.List<System.Int32>)result;")
        };

        [TestCaseSource(nameof(ReturnTypes))]
        public void ReturnResult_ShouldCreateTheProperExpression((Type Type, string Local, string Expected) para) =>
            Assert.That(Generator.ReturnResult(MetadataTypeInfo.CreateFrom(para.Type), Generator.DeclareLocal<object>(para.Local)).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));

        public static (object Method, string File)[] Methods = new[]
        {
            ((object) Foo, "FooSrc.txt"),
            (Bar, "BarSrc.txt")
        };

        [TestCaseSource(nameof(Methods))]
        public void GenerateProxyMethod_Test((object Method, string File) para) =>
            Assert.That(new MethodInterceptorFactory().Build((IMethodInfo) para.Method).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText(para.File)));

        [Test]
        public void GenerateProxyProperty_Test() =>
            Assert.That(new PropertyInterceptorFactory().Build(Prop).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("PropSrc.txt")));

        [Test]
        public void GenerateProxyIndexer_Test() =>
            Assert.That(new IndexerInterceptorFactory().Build(Indexer).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("IndexerSrc.txt")));

        [Test]
        public void GenerateProxyClass_Test() =>
            Assert.That(Generator.GetContext().Unit.NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("ClsSrc.txt")));

        [Test]
        public void GenerateProxyEvent_Test() =>
            Assert.That(new EventInterceptorFactory().Build(Event).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("EventSrc.txt")));
    }
}