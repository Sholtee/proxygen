/********************************************************************************
* ProxySyntaxGeneratorBase.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals.Tests
{
    using static ProxySyntaxGeneratorBase;

    [TestFixture]
    public sealed class ProxySyntaxGeneratorBaseTests : ProxySyntaxGeneratorTestsBase
    {
        [TestCase(typeof(string[]), "System.String[] param;")]
        [TestCase(typeof(object), "System.Object param;")]
        public void DeclareLocal_ShouldDeclareTheDesiredLocalVariable(Type paramType, string expected) =>
            Assert.That(DeclareLocal(paramType, "param").NormalizeWhitespace().ToFullString(), Is.EqualTo(expected));

        [TestCase(typeof(object), "System.Object")]
        [TestCase(typeof(int[]), "System.Int32[]")]
        [TestCase(typeof(int[,]), "System.Int32[, ]")]
        [TestCase(typeof(IEnumerable<>), "System.Collections.Generic.IEnumerable<T>")]
        [TestCase(typeof(IEnumerable<int[]>), "System.Collections.Generic.IEnumerable<System.Int32[]>")]
        [TestCase(typeof(IEnumerable<int>[]), "System.Collections.Generic.IEnumerable<System.Int32>[]")]
        [TestCase(typeof(IEnumerable<IEnumerable<string>>), "System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<System.String>>")]
        public void CreateType_ShouldHandleNonNestedTypes(Type type, string expected) =>
            Assert.That(CreateType(type).NormalizeWhitespace().ToFullString(), Is.EqualTo(expected));

        private class CicaNested<T>
        {
            public class Mica<TT>
            {
                public enum Hajj
                {
                }
            }
            public class Mica
            {
                public class Hajj<TT, TTT>
                {
                }
            }
        }

        [TestCase(typeof(Cica<>.Mica<>.Hajj), "Solti.Utils.Proxy.Internals.Tests.Cica<T>.Mica<TT>.Hajj")]
        [TestCase(typeof(Cica<>.Mica.Hajj<,>), "Solti.Utils.Proxy.Internals.Tests.Cica<T>.Mica.Hajj<TT, TTT>")]
        [TestCase(typeof(Cica<List<int>>.Mica<string>.Hajj), "Solti.Utils.Proxy.Internals.Tests.Cica<System.Collections.Generic.List<System.Int32>>.Mica<System.String>.Hajj")]
        [TestCase(typeof(Cica<int>.Mica.Hajj<string, object>), "Solti.Utils.Proxy.Internals.Tests.Cica<System.Int32>.Mica.Hajj<System.String, System.Object>")]
        [TestCase(typeof(CicaNested<>.Mica<>.Hajj), "Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.CicaNested<T>.Mica<TT>.Hajj")]
        [TestCase(typeof(CicaNested<>.Mica.Hajj<,>), "Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.CicaNested<T>.Mica.Hajj<TT, TTT>")]
        [TestCase(typeof(CicaNested<List<int>>.Mica<string>.Hajj), "Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.CicaNested<System.Collections.Generic.List<System.Int32>>.Mica<System.String>.Hajj")]
        [TestCase(typeof(CicaNested<int>.Mica.Hajj<string, object>), "Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.CicaNested<System.Int32>.Mica.Hajj<System.String, System.Object>")]
        public void CreateType_ShouldHandleNestedTypes(Type type, string expected) =>
            Assert.That(CreateType(type).NormalizeWhitespace().ToFullString(), Is.EqualTo(expected));


        [Test]
        public void DeclareProperty_ShouldDoWhatTheNameSuggests() =>
            Assert.That(DeclareProperty(Prop, SyntaxFactory.Block(), SyntaxFactory.Block()).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("System.Int32 Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorTestsBase.IFoo<System.Int32>.Prop\n{\n    get\n    {\n    }\n\n    set\n    {\n    }\n}"));

        [Test]
        public void DeclareField_ShouldDoWhatTheNameSuggests() =>
            Assert.That(DeclareField<EventInfo>("FEvent", SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression), SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword, SyntaxKind.ReadOnlyKeyword).NormalizeWhitespace().ToFullString(), Is.EqualTo("private static readonly System.Reflection.EventInfo FEvent = null;"));

        [Test]
        public void DeclareEvent_ShouldDoWhatTheNameSuggests() =>
            Assert.That(DeclareEvent(Event, SyntaxFactory.Block(), SyntaxFactory.Block()).NormalizeWhitespace(eol: "\n").ToString(), Is.EqualTo("event Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorTestsBase.TestDelegate<System.Int32> Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorTestsBase.IFoo<System.Int32>.Event\n{\n    add\n    {\n    }\n\n    remove\n    {\n    }\n}"));

        [Test]
        public void DeclareMethod_ShouldHandleParamsModifier() =>
            Assert.That(DeclareMethod(typeof(IParams).GetMethod(nameof(IParams.Foo))).NormalizeWhitespace().ToString(), Is.EqualTo("void Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.IParams.Foo(params System.Int32[] paramz)"));

        private interface IParams
        {
            void Foo(params int[] paramz);
        }

        [Test]
        public void DeclareMethod_ShouldSupportRefKeywords() =>
            Assert.That(DeclareMethod(typeof(IRefInterface).GetMethod(nameof(IRefInterface.RefMethod), BindingFlags.Instance | BindingFlags.Public)).NormalizeWhitespace().ToString(), Is.EqualTo("ref System.Object Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.IRefInterface.RefMethod(in System.Object a, out System.Object b, ref System.Object c)"));

        [Test]
        public void InvokeMethod_ShouldSupportRefKeywords() =>
            Assert.That(InvokeMethod(typeof(IRefInterface).GetMethod(nameof(IRefInterface.RefMethod), BindingFlags.Instance | BindingFlags.Public), IdentifierName("target"),  "a", "b", "c").NormalizeWhitespace().ToString(), Is.EqualTo("target.RefMethod(in a, out b, ref c)"));

        private static void GenericMethod<T>(T a) { }

        public static (MethodInfo Method, string Expected)[] GenericMethods = new[] 
        {
           (((MethodInfo) MemberInfoExtensions.ExtractFrom(() => GenericMethod(0))).GetGenericMethodDefinition(), "Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.GenericMethod<T>(a)"),
           ((MethodInfo) MemberInfoExtensions.ExtractFrom(() => GenericMethod(0)), "Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.GenericMethod<System.Int32>(a)")
        };

        [TestCaseSource(nameof(GenericMethods))]
        public void InvokeMethod_ShouldSupportGenerics((MethodInfo Method, string Expected) param) =>
            Assert.That(InvokeMethod(param.Method, null, "a").NormalizeWhitespace().ToFullString(), Is.EqualTo(param.Expected));

        private interface IRefInterface
        {
            ref object RefMethod(in object a, out object b, ref object c);
        }

        public static (MethodInfo Method, string Expected)[] MethodsHavingNullableRetVal = new[]
        {
            (typeof(INullable).GetMethod(nameof(INullable.Nullable), BindingFlags.Instance | BindingFlags.Public), "System.Nullable<System.Int32> Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.INullable.Nullable()"),
            (typeof(INullable).GetMethod(nameof(INullable.Object), BindingFlags.Instance | BindingFlags.Public), "System.Object Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.INullable.Object()")
        };

        [TestCaseSource(nameof(MethodsHavingNullableRetVal))]
        public void DeclareMethod_ShouldSupportNullables((MethodInfo Method, string Expected) param) =>
            Assert.That(DeclareMethod(param.Method).NormalizeWhitespace().ToString(), Is.EqualTo(param.Expected));

        private interface INullable 
        {
            int? Nullable();
            object Object(); // nullable
        }
    }

    internal class Cica<T> // NE nested legyen
    {
        public class Mica<TT>
        {
            public enum Hajj
            {
            }
        }
        public class Mica
        {
            public class Hajj<TT, TTT>
            {
            }
        }
    }
}