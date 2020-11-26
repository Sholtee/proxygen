﻿/********************************************************************************
* MemberSyntaxFactory.cs                                                        *
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

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Internals;

    [TestFixture]
    public sealed class MemberSyntaxFactoryTests : SyntaxFactoryTestsBase
    {
        [TestCase(typeof(string[]), "System.String[] param;")]
        [TestCase(typeof(object), "System.Object param;")]
        public void DeclareLocal_ShouldDeclareTheDesiredLocalVariable(Type paramType, string expected) =>
            Assert.That(new MemberSyntaxFactory(default).DeclareLocal(MetadataTypeInfo.CreateFrom(paramType), "param").NormalizeWhitespace().ToFullString(), Is.EqualTo(expected));

        [TestCase(typeof(object), "System.Object")]
        [TestCase(typeof(int[]), "System.Int32[]")]
        [TestCase(typeof(int[,]), "System.Int32[, ]")]
        [TestCase(typeof(IEnumerable<>), "System.Collections.Generic.IEnumerable<T>")]
        [TestCase(typeof(IEnumerable<int[]>), "System.Collections.Generic.IEnumerable<System.Int32[]>")]
        [TestCase(typeof(IEnumerable<int>[]), "System.Collections.Generic.IEnumerable<System.Int32>[]")]
        [TestCase(typeof(IEnumerable<IEnumerable<string>>), "System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<System.String>>")]
        public void CreateType_ShouldHandleNonNestedTypes(Type type, string expected) =>
            Assert.That(new MemberSyntaxFactory(default).CreateType(MetadataTypeInfo.CreateFrom(type)).NormalizeWhitespace().ToFullString(), Is.EqualTo(expected));

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

        [TestCase(typeof(Cica<>.Mica<>.Hajj), "Solti.Utils.Proxy.SyntaxFactories.Tests.Cica<T>.Mica<TT>.Hajj")]
        [TestCase(typeof(Cica<>.Mica.Hajj<,>), "Solti.Utils.Proxy.SyntaxFactories.Tests.Cica<T>.Mica.Hajj<TT, TTT>")]
        [TestCase(typeof(Cica<List<int>>.Mica<string>.Hajj), "Solti.Utils.Proxy.SyntaxFactories.Tests.Cica<System.Collections.Generic.List<System.Int32>>.Mica<System.String>.Hajj")]
        [TestCase(typeof(Cica<int>.Mica.Hajj<string, object>), "Solti.Utils.Proxy.SyntaxFactories.Tests.Cica<System.Int32>.Mica.Hajj<System.String, System.Object>")]
        [TestCase(typeof(CicaNested<>.Mica<>.Hajj), "Solti.Utils.Proxy.SyntaxFactories.Tests.MemberSyntaxFactoryTests.CicaNested<T>.Mica<TT>.Hajj")]
        [TestCase(typeof(CicaNested<>.Mica.Hajj<,>), "Solti.Utils.Proxy.SyntaxFactories.Tests.MemberSyntaxFactoryTests.CicaNested<T>.Mica.Hajj<TT, TTT>")]
        [TestCase(typeof(CicaNested<List<int>>.Mica<string>.Hajj), "Solti.Utils.Proxy.SyntaxFactories.Tests.MemberSyntaxFactoryTests.CicaNested<System.Collections.Generic.List<System.Int32>>.Mica<System.String>.Hajj")]
        [TestCase(typeof(CicaNested<int>.Mica.Hajj<string, object>), "Solti.Utils.Proxy.SyntaxFactories.Tests.MemberSyntaxFactoryTests.CicaNested<System.Int32>.Mica.Hajj<System.String, System.Object>")]
        public void CreateType_ShouldHandleNestedTypes(Type type, string expected) =>
            Assert.That(new MemberSyntaxFactory(default).CreateType(MetadataTypeInfo.CreateFrom(type)).NormalizeWhitespace().ToFullString(), Is.EqualTo(expected));

        [Test]
        public void DeclareProperty_ShouldDoWhatTheNameSuggests() =>
            Assert.That(new MemberSyntaxFactory(default).DeclareProperty(Prop, Block(), Block()).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("System.Int32 Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<System.Int32>.Prop\n{\n    get\n    {\n    }\n\n    set\n    {\n    }\n}"));

        [Test]
        public void DeclareEvent_ShouldDoWhatTheNameSuggests() =>
            Assert.That(new MemberSyntaxFactory(default).DeclareEvent(Event, Block(), Block()).NormalizeWhitespace(eol: "\n").ToString(), Is.EqualTo("event Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.TestDelegate<System.Int32> Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<System.Int32>.Event\n{\n    add\n    {\n    }\n\n    remove\n    {\n    }\n}"));

        [Test]
        public void DeclareMethod_ShouldHandleParamsModifier() =>
            Assert.That(new MemberSyntaxFactory(default).DeclareMethod(MetadataMethodInfo.CreateFrom((MethodInfo) MemberInfoExtensions.ExtractFrom<IParams>(i => i.Foo(default)))).NormalizeWhitespace().ToString(), Is.EqualTo("void Solti.Utils.Proxy.SyntaxFactories.Tests.MemberSyntaxFactoryTests.IParams.Foo(params System.Int32[] paramz)"));

        private interface IParams
        {
            void Foo(params int[] paramz);
        }

        [Test]
        public void DeclareMethod_ShouldSupportRefKeywords() =>
            // ref retval miatt a MemberInfoExtensions-s csoda itt nem jo
            Assert.That(new MemberSyntaxFactory(default).DeclareMethod(MetadataMethodInfo.CreateFrom(typeof(IRefInterface).GetMethod(nameof(IRefInterface.RefMethod), BindingFlags.Instance | BindingFlags.Public))).NormalizeWhitespace().ToString(), Is.EqualTo("ref System.Object Solti.Utils.Proxy.SyntaxFactories.Tests.MemberSyntaxFactoryTests.IRefInterface.RefMethod(in System.Object a, out System.Object b, ref System.Object c)"));

        [Test]
        public void InvokeMethod_ShouldSupportRefKeywords() =>
            Assert.That(new MemberSyntaxFactory(default).InvokeMethod(MetadataMethodInfo.CreateFrom(typeof(IRefInterface).GetMethod(nameof(IRefInterface.RefMethod), BindingFlags.Instance | BindingFlags.Public)), IdentifierName("target"),  null, "a", "b", "c").NormalizeWhitespace().ToString(), Is.EqualTo("target.RefMethod(in a, out b, ref c)"));

        private static void GenericMethod<T>(T a) { }

        public static (MethodInfo Method, string Expected)[] GenericMethods = new[] 
        {
           (((MethodInfo) MemberInfoExtensions.ExtractFrom(() => GenericMethod(0))).GetGenericMethodDefinition(), "Solti.Utils.Proxy.SyntaxFactories.Tests.MemberSyntaxFactoryTests.GenericMethod<T>(a)"),
           ( (MethodInfo) MemberInfoExtensions.ExtractFrom(() => GenericMethod(0)), "Solti.Utils.Proxy.SyntaxFactories.Tests.MemberSyntaxFactoryTests.GenericMethod<System.Int32>(a)")
        };

        [TestCaseSource(nameof(GenericMethods))]
        public void InvokeMethod_ShouldSupportGenerics((MethodInfo Method, string Expected) param) =>
            Assert.That(new MemberSyntaxFactory(default).InvokeMethod(MetadataMethodInfo.CreateFrom(param.Method), null, null, "a").NormalizeWhitespace().ToFullString(), Is.EqualTo(param.Expected));

        private interface IRefInterface
        {
            ref object RefMethod(in object a, out object b, ref object c);
        }

        public static (MethodInfo Method, string Expected)[] MethodsHavingNullableRetVal = new[]
        {
            ((MethodInfo) MemberInfoExtensions.ExtractFrom<INullable>(i => i.Nullable()), "System.Nullable<System.Int32> Solti.Utils.Proxy.SyntaxFactories.Tests.MemberSyntaxFactoryTests.INullable.Nullable()"),
            ((MethodInfo) MemberInfoExtensions.ExtractFrom<INullable>(i => i.Object()), "System.Object Solti.Utils.Proxy.SyntaxFactories.Tests.MemberSyntaxFactoryTests.INullable.Object()")
        };

        [TestCaseSource(nameof(MethodsHavingNullableRetVal))]
        public void DeclareMethod_ShouldSupportNullables((MethodInfo Method, string Expected) param) =>
            Assert.That(new MemberSyntaxFactory(default).DeclareMethod(MetadataMethodInfo.CreateFrom(param.Method)).NormalizeWhitespace().ToString(), Is.EqualTo(param.Expected));

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