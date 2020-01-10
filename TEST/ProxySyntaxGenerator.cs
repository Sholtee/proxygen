/********************************************************************************
* ProxySyntaxGenerator.cs                                                       *
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
    using static Internals.ProxySyntaxGeneratorBase;
    using static Internals.ProxySyntaxGenerator<ProxySyntaxGeneratorTestsBase.IFoo<int>, ProxySyntaxGeneratorTests.FooInterceptor>;

    [TestFixture]
    public sealed class ProxySyntaxGeneratorTests : ProxySyntaxGeneratorTestsBase
    {
        private static PropertyInfo Indexer { get; } = typeof(IList<int>).GetProperty("Item");

        internal class FooInterceptor : InterfaceInterceptor<IFoo<int>> // direkt internal
        {
            public FooInterceptor(IFoo<int> target) : base(target)
            {
            }
        }

        public static (MethodInfo Method, string Expected)[] MethodsToWhichTheArrayIsCreated = new[]
        {
            (Foo, "System.Object[] args = new System.Object[]{a, default(System.String), c};"),
            (Bar, "System.Object[] args = new System.Object[0];")
        };

        private Internals.ProxySyntaxGenerator<IFoo<int>, FooInterceptor> Generator { get; set; }

        [SetUp]
        public void Setup() => Generator = new Internals.ProxySyntaxGenerator<IFoo<int>, FooInterceptor>();

        [TestCaseSource(nameof(MethodsToWhichTheArrayIsCreated))]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments((MethodInfo Method, string Expected) para) =>
            Assert.That(CreateArgumentsArray(para.Method).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));

        [Test]
        public void AssignByRefParameters_ShouldAssignByRefParameters()
        {
            IReadOnlyList<ExpressionStatementSyntax> assignments = AssignByRefParameters(Foo, DeclareLocal<object[]>("args")).ToArray();

            Assert.That(assignments.Count, Is.EqualTo(2));
            Assert.That(assignments[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("b = (System.String)args[1];"));
            Assert.That(assignments[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("c = (TT)args[2];"));
        }

        public static (MethodInfo Method, int StatementCount, string Expected)[] InspectedMethods = new[]
        {
            (Foo, 3, "System.Reflection.MethodInfo currentMethod = Solti.Utils.Proxy.InterfaceInterceptor<Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorTestsBase.IFoo<System.Int32>>.MethodAccess(() => this.Target.Foo<TT>(a, out dummy_b, ref dummy_c));"),
            (Bar, 1, "System.Reflection.MethodInfo currentMethod = Solti.Utils.Proxy.InterfaceInterceptor<Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorTestsBase.IFoo<System.Int32>>.MethodAccess(() => this.Target.Bar());")
        };

        [TestCaseSource(nameof(InspectedMethods))]
        public void AcquireMethodInfo_ShouldGetAMethodInfoInstance((MethodInfo Method, int StatementCount, string Expected) data)
        {
            IReadOnlyList<StatementSyntax> statements = AcquireMethodInfo(data.Method, out _).ToArray();

            Assert.That(statements.Count, Is.EqualTo(data.StatementCount));
            Assert.That(statements.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo(data.Expected));
        }

        [Test]
        public void CallInvoke_ShouldCallTheInvokeMetehodOnThis() =>
            Assert.That
            (
                CallInvoke
                (
                    DeclareLocal<MethodInfo>("currentMethod"),
                    DeclareLocal<object[]>("args"),
                    DeclareLocal<MemberInfo>("extra")
                ).NormalizeWhitespace().ToFullString(), 
                Is.EqualTo("System.Object result = this.Invoke(currentMethod, args, extra);")
            );

        public static (Type Type, string Local, string Expected)[] ReturnTypes = new[]
        {
            (typeof(void), "@void", "return;"),
            (typeof(List<int>), "result", "return (System.Collections.Generic.List<System.Int32>)result;")
        };

        [TestCaseSource(nameof(ReturnTypes))]
        public void ReturnResult_ShouldCreateTheProperExpression((Type Type, string Local, string Expected) para) =>
            Assert.That(ReturnResult(para.Type, DeclareLocal<object>(para.Local)).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));

        public static (MethodInfo Method, string File)[] Methods = new[]
        {
            (Foo, "FooSrc.txt"),
            (Bar, "BarSrc.txt")
        };

        [TestCaseSource(nameof(Methods))]
        public void GenerateProxyMethod_Test((MethodInfo Method, string File) para) =>
            Assert.That(GenerateProxyMethod(para.Method).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText(para.File)));

        [Test]
        public void GenerateProxyProperty_Test() =>
            Assert.That(Generator.GenerateProxyProperty(Prop).Last().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("PropSrc.txt")));

        [Test]
        public void GenerateProxyIndexer_Test() =>
            Assert.That(Generator.GenerateProxyIndexer(Indexer, SyntaxFactory.IdentifierName($"F{Indexer.Name}")).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("IndexerSrc.txt")));

        [Test]
        public void GenerateProxyClass_Test() =>
            Assert.That(Generator.GenerateProxyClass().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("ClsSrc.txt")));

        public static (MethodInfo Method, string Expected)[] InvokedMethods = new[]
        {
            (Foo, "return this.Target.Foo<TT>(a, out b, ref c);"),
            (Bar, "{\n    this.Target.Bar();\n    return;\n}")
        };

        [TestCaseSource(nameof(InvokedMethods))]
        public void CallTargetAndReturn_ShouldInvokeTheTargetMethod((MethodInfo Method, string Expected) para) => 
            Assert.That(CallTargetAndReturn(para.Method).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(para.Expected));

        public static (PropertyInfo Property, string Expected)[] ReadProperties = new[]
        {
            (Prop, "return this.Target.Prop;"),
            (Indexer, "return this.Target[index];")
        };

        [TestCaseSource(nameof(ReadProperties))]
        public void ReadTargetAndReturn_ShouldReadTheGivenProperty((PropertyInfo Property, string Expected) para) =>
            Assert.That(ReadTargetAndReturn(para.Property).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));

        [Test]
        public void WriteTarget_ShouldWriteTheGivenProperty() =>
            Assert.That(WriteTarget(Prop).NormalizeWhitespace().ToFullString(), Is.EqualTo("this.Target.Prop = value;"));

        [Test]
        public void ShouldCallTarget_ShouldCreateAnIfStatement() =>
            Assert.That(ShouldCallTarget(DeclareLocal<object>("result"), SyntaxFactory.Block()).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("if (result == this.CALL_TARGET)\n{\n}"));

        [Test]
        public void GenerateProxyEvent_Test() =>
            Assert.That(SyntaxFactory.ClassDeclaration(Generator.GeneratedClassName).WithMembers(SyntaxFactory.List(Generator.GenerateProxyEvent(Event))).NormalizeWhitespace(eol: "\n").ToString(), Is.EqualTo(File.ReadAllText("EventSrc.txt")));
    }
}