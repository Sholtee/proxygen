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
    using static Internals.ProxySyntaxFactoryBase;
    using static Internals.ProxySyntaxFactory<ProxySyntaxFactoryTestsBase.IFoo<int>, ProxySyntaxFactoryTests.FooInterceptor>;

    [TestFixture]
    public sealed class ProxySyntaxFactoryTests : ProxySyntaxFactoryTestsBase
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

        private Internals.ProxySyntaxFactory<IFoo<int>, FooInterceptor> Generator { get; set; }

        [SetUp]
        public void Setup() => Generator = new Internals.ProxySyntaxFactory<IFoo<int>, FooInterceptor>();

        [TestCaseSource(nameof(MethodsToWhichTheArrayIsCreated))]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments((MethodInfo Method, string Expected) para) =>
            Assert.That(new MethodInterceptorFactory(para.Method).CreateArgumentsArray().NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));

        [Test]
        public void AssignByRefParameters_ShouldAssignByRefParameters()
        {
            IReadOnlyList<ExpressionStatementSyntax> assignments = new MethodInterceptorFactory(Foo).AssignByRefParameters().ToArray();

            Assert.That(assignments.Count, Is.EqualTo(2));
            Assert.That(assignments[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("b = (System.String)args[1];"));
            Assert.That(assignments[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("c = (TT)args[2];"));
        }

        [Test]
        public void LocalArgs_ShouldBeDeclaredForEachArgument()
        {
            IReadOnlyList<LocalDeclarationStatementSyntax> locals = new CallbackLambdaExpressionFactory(Foo, DeclareLocal<object[]>("args")).LocalArgs;

            Assert.That(locals.Count, Is.EqualTo(3));
            Assert.That(locals[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32 cb_a = (System.Int32)args[0];"));
            Assert.That(locals[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("System.String cb_b;"));
            Assert.That(locals[2].NormalizeWhitespace().ToFullString(), Is.EqualTo("TT cb_c = (TT)args[2];"));
        }

        [Test]
        public void CallTarget_ShouldStoreTheResult()
        {
            IReadOnlyList<StatementSyntax> result = new CallbackLambdaExpressionFactory(Foo, DeclareLocal<object[]>("args")).CallTarget().ToArray();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Single().NormalizeWhitespace().ToFullString(), Is.EqualTo("result = this.Target.Foo<TT>(cb_a, out cb_b, ref cb_c);"));
        }

        [Test]
        public void CallTarget_ShouldHandleVoidMethods()
        {
            IReadOnlyList<StatementSyntax> result = new CallbackLambdaExpressionFactory(Bar, DeclareLocal<object[]>("args")).CallTarget().ToArray();

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("result = null;"));
            Assert.That(result[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("this.Target.Bar();"));
        }

        [Test]
        public void ReassignArgsArray_ShouldCopyByRefArgumentsBack()
        {
            IReadOnlyList<StatementSyntax> assigns = new CallbackLambdaExpressionFactory(Foo, DeclareLocal<object[]>("args")).ReassignArgsArray().ToArray();

            Assert.That(assigns.Count, Is.EqualTo(2));
            Assert.That(assigns[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("args[1] = (System.Object)cb_b;"));
            Assert.That(assigns[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("args[2] = (System.Object)cb_c;"));
        }

        [Test]
        public void CallbackLambdaExpressionFactory_ShouldCreateTheProperLambda() =>
            Assert.That(new CallbackLambdaExpressionFactory(Foo, DeclareLocal<object[]>("args")).Build().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("CallbackSrc.txt")));

        public static (MethodInfo Method, int StatementCount, string Expected)[] InspectedMethods = new[]
        {
            (Foo, 3, "currentMethod = Solti.Utils.Proxy.InterfaceInterceptor<Solti.Utils.Proxy.SyntaxFactories.Tests.ProxySyntaxFactoryTestsBase.IFoo<System.Int32>>.ResolveMethod(() => this.Target.Foo<TT>(a, out dummy_b, ref dummy_c));"),
            (Bar, 1, "currentMethod = Solti.Utils.Proxy.InterfaceInterceptor<Solti.Utils.Proxy.SyntaxFactories.Tests.ProxySyntaxFactoryTestsBase.IFoo<System.Int32>>.ResolveMethod(() => this.Target.Bar());")
        };

        [TestCaseSource(nameof(InspectedMethods))]
        public void AcquireMethodInfo_ShouldGetAMethodInfoInstance((MethodInfo Method, int StatementCount, string Expected) para)
        {
            IReadOnlyList<StatementSyntax> statements = new MethodInterceptorFactory(para.Method).AcquireMethodInfo().ToArray();

            Assert.That(statements.Count, Is.EqualTo(para.StatementCount));
            Assert.That(statements.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo(para.Expected));
        }

        [Test]
        public void CallInvoke_ShouldCallTheInvokeMetehodOnThis() =>
            Assert.That
            (
                CallInvoke
                (
                    "result",
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
            Assert.That(new MethodInterceptorFactory(para.Method).Build().Last().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText(para.File)));

        [Test]
        public void GenerateProxyProperty_Test() =>
            Assert.That(new PropertyInterceptorFactory(Prop, new Internals.ProxySyntaxFactory<IFoo<int>, FooInterceptor>()).Build().Last().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("PropSrc.txt")));

        [Test]
        public void GenerateProxyIndexer_Test() =>
            Assert.That(new Internals.ProxySyntaxFactory<IList<int>, InterfaceInterceptor<IList<int>>>.IndexedPropertyInterceptorFactory(Indexer, new Internals.ProxySyntaxFactory<IList<int>, InterfaceInterceptor<IList<int>>>()).Build().Last().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("IndexerSrc.txt")));

        [Test]
        public void GenerateProxyClass_Test() =>
            Assert.That(Generator.GenerateProxyClass().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("ClsSrc.txt")));

        [Test]
        public void GenerateProxyEvent_Test() =>
            Assert.That(new EventInterceptorFactory(Event, new Internals.ProxySyntaxFactory<IFoo<int>, FooInterceptor>()).Build().Last().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("EventSrc.txt")));

        [Test]
        public void BuildPropertyGetter_ShouldCreateTheProperLambda() =>
            Assert.That(new PropertyInterceptorFactory(Prop, null).BuildPropertyGetter().NormalizeWhitespace().ToFullString(), Is.EqualTo("() => this.Target.Prop"));

        [Test]
        public void BuildPropertySetter_ShouldCreateTheProperLambda() =>
            Assert.That(new PropertyInterceptorFactory(Prop, null).BuildPropertySetter().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("() =>\n{\n    this.Target.Prop = value;\n    return null;\n}"));

        [Test]
        public void CallInvokeAndReturn_ShouldCallTheInvokeMethodAgainstTheDesiredProperty() =>
            Assert.That(new PropertyInterceptorFactory(Prop, new Internals.ProxySyntaxFactory<IFoo<int>, FooInterceptor>()).CallInvokeAndReturn().Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("return (System.Int32)this.Invoke(prop.GetMethod, new System.Object[0], prop);"));
        
        [Test]
        public void CallInvoke_ShouldCallTheInvokeMethodAgainstTheDesiredProperty() =>
            Assert.That(new PropertyInterceptorFactory(Prop, new Internals.ProxySyntaxFactory<IFoo<int>, FooInterceptor>()).CallInvoke().Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("this.Invoke(prop.SetMethod, new System.Object[]{value}, prop);"));
    }
}