/********************************************************************************
* ProxySyntaxGenerator.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    using static ProxySyntaxGeneratorBase;
    using static ProxySyntaxGenerator<ProxySyntaxGeneratorTestsBase.IFoo<int>, ProxySyntaxGeneratorTests.FooInterceptor>;

    [TestFixture]
    public sealed class ProxySyntaxGeneratorTests : ProxySyntaxGeneratorTestsBase
    {
        private static MethodInfo GetMethod(string name) => typeof(IFoo<int>).GetMethod(name);

        private static MethodInfo Foo { get; } = GetMethod(nameof(IFoo<int>.Foo));

        private static MethodInfo Bar { get; } = GetMethod(nameof(IFoo<int>.Bar));

        private static PropertyInfo Indexer { get; } = typeof(IList<int>).GetProperty("Item");

        internal class FooInterceptor : InterfaceInterceptor<IFoo<int>> // direkt internal
        {
            public FooInterceptor(IFoo<int> target) : base(target)
            {
            }
        }

        [Test]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments()
        {
            Assert.That(CreateArgumentsArray(Foo).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Object[] args = new System.Object[]{a, default(System.String), c};"));

            Assert.That(CreateArgumentsArray(Bar).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Object[] args = new System.Object[0];"));
        }

        [Test]
        public void AssignByRefParameters_ShouldAssignByRefParameters()
        {
            IReadOnlyList<ExpressionStatementSyntax> assignments = AssignByRefParameters(Foo, DeclareLocal<object[]>("args"));

            Assert.That(assignments.Count, Is.EqualTo(2));
            Assert.That(assignments[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("b = (System.String)args[1];"));
            Assert.That(assignments[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("c = (TT)args[2];"));
        }

        [Test]
        public void AcquireMethodInfo_ShouldGetAMethodInfoInstance()
        {
            IReadOnlyList<StatementSyntax> statements = AcquireMethodInfo(Foo, out _);
            Assert.That(statements.Count, Is.EqualTo(3));
            Assert.That(statements.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Reflection.MethodInfo currentMethod = MethodAccess(() => Target.Foo(a, out dummy_b, ref dummy_c));"));

            statements = AcquireMethodInfo(Bar, out _);
            Assert.That(statements.Count, Is.EqualTo(1));
            Assert.That(statements.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Reflection.MethodInfo currentMethod = MethodAccess(() => Target.Bar());"));
        }

        [Test]
        public void CallInvoke_ShouldCallTheInvokeMetehodOnThis()
        {
            LocalDeclarationStatementSyntax
                currentMethod = DeclareLocal<MethodInfo>(nameof(currentMethod)),
                args = DeclareLocal<object[]>(nameof(args));

            Assert.That(CallInvoke
            (
                currentMethod,
                args
            ).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Object result = Invoke(currentMethod, args);"));
        }


        [Test]
        public void ReturnResult_ShouldCreateTheProperExpression()
        {
            Assert.That(ReturnResult(typeof(void), DeclareLocal<object>("@void")).NormalizeWhitespace().ToFullString(), Is.EqualTo("return;"));
            Assert.That(ReturnResult(typeof(List<int>), DeclareLocal<object>("result")).NormalizeWhitespace().ToFullString(), Is.EqualTo("return (System.Collections.Generic.List<System.Int32>)result;"));
        }

        [Test]
        public void GenerateProxyMethod_Test()
        {
            Assert.That(GenerateProxyMethod(Foo).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("FooSrc.txt")));
            Assert.That(GenerateProxyMethod(Bar).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("BarSrc.txt")));
        }

        [Test]
        public void GenerateProxyProperty_Test()
        {
            Assert.That(GenerateProxyProperty(Prop).Last().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("PropSrc.txt")));
        }

        [Test]
        public void GenerateProxyIndexer_Test()
        {
            Assert.That(GenerateProxyIndexer(Indexer, SyntaxFactory.IdentifierName($"F{Indexer.Name}")).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("IndexerSrc.txt")));
        }

        [Test]
        public void GenerateProxyClass_Test()
        {
            Assert.That(new ProxySyntaxGenerator<IFoo<int>, FooInterceptor>().GenerateProxyClass().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("ClsSrc.txt")));
        }

        [Test]
        public void CallTargetAndReturn_ShouldInvokeTheTargetMethod()
        {
            Assert.That(CallTargetAndReturn(Foo).NormalizeWhitespace().ToFullString(), Is.EqualTo("return Target.Foo(a, out b, ref c);"));
            Assert.That(CallTargetAndReturn(Bar).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("{\n    Target.Bar();\n    return;\n}"));
        }

        [Test]
        public void ReadTargetAndReturn_ShouldReadTheGivenProperty()
        {
            Assert.That(ReadTargetAndReturn(Prop).NormalizeWhitespace().ToFullString(), Is.EqualTo("return Target.Prop;"));
            Assert.That(ReadTargetAndReturn(Indexer).NormalizeWhitespace().ToFullString(), Is.EqualTo("return Target[index];"));
        }

        [Test]
        public void WriteTarget_ShouldWriteTheGivenProperty()
        {
            Assert.That(WriteTarget(Prop).NormalizeWhitespace().ToFullString(), Is.EqualTo("Target.Prop = value;"));
        }

        [Test]
        public void ShouldCallTarget_ShouldCreateAnIfStatement()
        {
            Assert.That(ShouldCallTarget(DeclareLocal<object>("result"), SyntaxFactory.Block()).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("if (result == CALL_TARGET)\n{\n}"));
        }

        [Test]
        public void GenerateProxyEvent_Test()
        {
            Assert.That(SyntaxFactory.ClassDeclaration("Test").WithMembers(SyntaxFactory.List(GenerateProxyEvent(Event))).NormalizeWhitespace(eol: "\n").ToString(), Is.EqualTo(File.ReadAllText("EventSrc.txt")));
        }
    }
}