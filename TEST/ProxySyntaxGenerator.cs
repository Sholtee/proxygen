﻿/********************************************************************************
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

        public static (MethodInfo Method, string Expected)[] MethodsToWhichTheArrayIsCreated = new[]
        {
            (Foo, "System.Object[] args = new System.Object[]{a, default(System.String), c};"),
            (Bar, "System.Object[] args = new System.Object[0];")
        };

        [TestCaseSource(nameof(MethodsToWhichTheArrayIsCreated))]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments((MethodInfo Method, string Expected) para) =>
            Assert.That(CreateArgumentsArray(para.Method).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));

        [Test]
        public void AssignByRefParameters_ShouldAssignByRefParameters()
        {
            IReadOnlyList<ExpressionStatementSyntax> assignments = AssignByRefParameters(Foo, DeclareLocal<object[]>("args"));

            Assert.That(assignments.Count, Is.EqualTo(2));
            Assert.That(assignments[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("b = (System.String)args[1];"));
            Assert.That(assignments[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("c = (TT)args[2];"));
        }

        public static (MethodInfo Method, int StatementCount, string Expected)[] InspectedMethods = new[]
        {
            (Foo, 3, "System.Reflection.MethodInfo currentMethod = MethodAccess(() => Target.Foo(a, out dummy_b, ref dummy_c));"),
            (Bar, 1, "System.Reflection.MethodInfo currentMethod = MethodAccess(() => Target.Bar());")
        };

        [TestCaseSource(nameof(InspectedMethods))]
        public void AcquireMethodInfo_ShouldGetAMethodInfoInstance((MethodInfo Method, int StatementCount, string Expected) data)
        {
            IReadOnlyList<StatementSyntax> statements = AcquireMethodInfo(data.Method, out _);
            Assert.That(statements.Count, Is.EqualTo(data.StatementCount));
            Assert.That(statements.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo(data.Expected));
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

        public static (MethodInfo Method, string Expected)[] InvokedMethods = new[]
        {
            (Foo, "return Target.Foo(a, out b, ref c);"),
            (Bar, "{\n    Target.Bar();\n    return;\n}")
        };

        [TestCaseSource(nameof(InvokedMethods))]
        public void CallTargetAndReturn_ShouldInvokeTheTargetMethod((MethodInfo Method, string Expected) para) => 
            Assert.That(CallTargetAndReturn(para.Method).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(para.Expected));

        public static (PropertyInfo Property, string Expected)[] ReadProperties = new[]
        {
            (Prop, "return Target.Prop;"),
            (Indexer, "return Target[index];")
        };

        [TestCaseSource(nameof(ReadProperties))]
        public void ReadTargetAndReturn_ShouldReadTheGivenProperty((PropertyInfo Property, string Expected) para) =>
            Assert.That(ReadTargetAndReturn(para.Property).NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));

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