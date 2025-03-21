/********************************************************************************
* ClassSyntaxFactoryBase.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Internals;
    using Primitives;

    [TestFixture]
    public class ClassSyntaxFactoryBaseTests : SyntaxFactoryTestsBase
    {
        private sealed class ClassSyntaxFactory : ClassSyntaxFactoryBase
        {
            public ClassSyntaxFactory(ReferenceCollector referenceCollector) : base(referenceCollector, LanguageVersion.Latest) { }

            protected internal override IEnumerable<ITypeInfo> ResolveBases(object context) => throw new NotImplementedException();

            protected internal override string ResolveClassName(object context) => throw new NotImplementedException();

            protected internal override ClassDeclarationSyntax ResolveConstructor(ClassDeclarationSyntax cls, object context, IConstructorInfo ctor) => throw new NotImplementedException();

            protected internal override ClassDeclarationSyntax ResolveConstructors(ClassDeclarationSyntax cls, object context) => throw new NotImplementedException();

            protected internal override ClassDeclarationSyntax ResolveEvent(ClassDeclarationSyntax cls, object context, IEventInfo evt) => throw new NotImplementedException();

            protected internal override ClassDeclarationSyntax ResolveEvents(ClassDeclarationSyntax cls, object context) => throw new NotImplementedException();

            protected internal override ClassDeclarationSyntax ResolveMethod(ClassDeclarationSyntax cls, object context, IMethodInfo method) => throw new NotImplementedException();

            protected internal override ClassDeclarationSyntax ResolveMethods(ClassDeclarationSyntax cls, object context) => throw new NotImplementedException();

            protected internal override ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context) => throw new NotImplementedException();

            protected internal override ClassDeclarationSyntax ResolveProperty(ClassDeclarationSyntax cls, object context, IPropertyInfo property) => throw new NotImplementedException();
        }


        [TestCase(typeof(string[]), "global::System.String[] param;")]
        [TestCase(typeof(object), "global::System.Object param;")]
        public void ResolveLocal_ShouldDeclareTheDesiredLocalVariable(Type paramType, string expected) =>
            Assert.That(new ClassSyntaxFactory(default).ResolveLocal(MetadataTypeInfo.CreateFrom(paramType), "param").NormalizeWhitespace().ToFullString(), Is.EqualTo(expected));

        [TestCase(typeof(object), "global::System.Object")]
        [TestCase(typeof(int[]), "global::System.Int32[]")]
        [TestCase(typeof(int*), "global::System.Int32*")]
        [TestCase(typeof(int[,]), "global::System.Int32[, ]")]
        [TestCase(typeof(IEnumerable<>), "global::System.Collections.Generic.IEnumerable<T>")]
        [TestCase(typeof(IEnumerable<int[]>), "global::System.Collections.Generic.IEnumerable<global::System.Int32[]>")]
        [TestCase(typeof(IEnumerable<int>[]), "global::System.Collections.Generic.IEnumerable<global::System.Int32>[]")]
        [TestCase(typeof(IEnumerable<IEnumerable<string>>), "global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.IEnumerable<global::System.String>>")]
        [TestCase(typeof((string Foo, object Bar)), "global::System.ValueTuple<global::System.String, global::System.Object>")]
        public void ResolveType_ShouldHandleNonNestedTypes(Type type, string expected) =>
            Assert.That(new ClassSyntaxFactory(default).ResolveType(MetadataTypeInfo.CreateFrom(type)).NormalizeWhitespace().ToFullString(), Is.EqualTo(expected));

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

        private class CicaNestedDescendant : CicaNested<IAliasSymbol> { }

        [TestCase(typeof(Cica<>.Mica<>.Hajj), "global::Solti.Utils.Proxy.SyntaxFactories.Tests.Cica<T>.Mica<TT>.Hajj")]
        [TestCase(typeof(Cica<>.Mica.Hajj<,>), "global::Solti.Utils.Proxy.SyntaxFactories.Tests.Cica<T>.Mica.Hajj<TT, TTT>")]
        [TestCase(typeof(Cica<List<int>>.Mica<string>.Hajj), "global::Solti.Utils.Proxy.SyntaxFactories.Tests.Cica<global::System.Collections.Generic.List<global::System.Int32>>.Mica<global::System.String>.Hajj")]
        [TestCase(typeof(Cica<int>.Mica.Hajj<string, object>), "global::Solti.Utils.Proxy.SyntaxFactories.Tests.Cica<global::System.Int32>.Mica.Hajj<global::System.String, global::System.Object>")]
        [TestCase(typeof(CicaNested<>.Mica<>.Hajj), "global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.CicaNested<T>.Mica<TT>.Hajj")]
        [TestCase(typeof(CicaNested<>.Mica.Hajj<,>), "global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.CicaNested<T>.Mica.Hajj<TT, TTT>")]
        [TestCase(typeof(CicaNested<List<int>>.Mica<string>.Hajj), "global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.CicaNested<global::System.Collections.Generic.List<global::System.Int32>>.Mica<global::System.String>.Hajj")]
        [TestCase(typeof(CicaNested<int>.Mica.Hajj<string, object>), "global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.CicaNested<global::System.Int32>.Mica.Hajj<global::System.String, global::System.Object>")]
        [TestCase(typeof(CicaNestedDescendant.Mica), "global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.CicaNested<global::Microsoft.CodeAnalysis.IAliasSymbol>.Mica")]
        public void ResolveType_ShouldHandleNestedTypes(Type type, string expected) =>
            Assert.That(new ClassSyntaxFactory(default).ResolveType(MetadataTypeInfo.CreateFrom(type)).NormalizeWhitespace().ToFullString(), Is.EqualTo(expected));

        [Test]
        public void ResolveProperty_ShouldDeclareANewProperty() =>
            Assert.That(new ClassSyntaxFactory(default).ResolveProperty(Prop, Block(), Block()).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("global::System.Int32 global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Prop\n{\n    get\n    {\n    }\n\n    set\n    {\n    }\n}"));

        [Test]
        public void ResolveEvent_ShouldDeclareANewEvent() =>
            Assert.That(new ClassSyntaxFactory(default).ResolveEvent(Event, Block(), Block()).NormalizeWhitespace(eol: "\n").ToString(), Is.EqualTo("event global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.TestDelegate<global::System.Int32> global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Event\n{\n    add\n    {\n    }\n\n    remove\n    {\n    }\n}"));

        [Test]
        public void ResolveMethod_ShouldHandleParamsModifier() =>
            Assert.That(new ClassSyntaxFactory(default).ResolveMethod(MetadataMethodInfo.CreateFrom(MethodInfoExtractor.Extract<IParams>(i => i.Foo(default)))).NormalizeWhitespace().ToString(), Is.EqualTo("void global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.IParams.Foo(params global::System.Int32[] paramz)"));

        private interface IParams
        {
            void Foo(params int[] paramz);
        }

        [Test]
        public void ResolveMethod_ShouldSupportRefKeywords() =>
            // ref retval miatt a MemberInfoExtensions-s csoda itt nem jo
            Assert.That(new ClassSyntaxFactory(default).ResolveMethod(MetadataMethodInfo.CreateFrom(typeof(IRefInterface).GetMethod(nameof(IRefInterface.RefMethod), BindingFlags.Instance | BindingFlags.Public))).NormalizeWhitespace().ToString(), Is.EqualTo("ref global::System.Object global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.IRefInterface.RefMethod(in global::System.Object a, out global::System.Object b, ref global::System.Object c)"));

        [Test]
        public void InvokeMethod_ShouldSupportRefKeywords() =>
            Assert.That(new ClassSyntaxFactory(default).InvokeMethod(MetadataMethodInfo.CreateFrom(typeof(IRefInterface).GetMethod(nameof(IRefInterface.RefMethod), BindingFlags.Instance | BindingFlags.Public)), IdentifierName("target"), null, "a", "b", "c").NormalizeWhitespace().ToString(), Is.EqualTo("target.RefMethod(in a, out b, ref c)"));

        internal static void StaticGenericMethod<T>(T a) { }

        internal void GenericMethod<T>(T a) { }

        protected virtual void GenericVirtualMethodHavingConstraint<T>(T a) where T : IRefInterface, new() { }

        protected virtual int VirtualMethod(int i) => 0;

        public static (MethodInfo Method, string Expected)[] GenericMethods = new[]
        {
           (MethodInfoExtractor.Extract(() => StaticGenericMethod(0)), "global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.StaticGenericMethod<global::System.Int32>(a)"),
           (MethodInfoExtractor.Extract(() => StaticGenericMethod(0)).GetGenericMethodDefinition(),"global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.StaticGenericMethod<T>(a)"),
           (MethodInfoExtractor.Extract<ClassSyntaxFactoryBaseTests>(x => x.GenericMethod(0)).GetGenericMethodDefinition(), "this.GenericMethod<T>(a)"),
           (MethodInfoExtractor.Extract<ClassSyntaxFactoryBaseTests>(x => x.GenericMethod(0)), "this.GenericMethod<global::System.Int32>(a)")
        };

        [TestCaseSource(nameof(GenericMethods))]
        public void InvokeMethod_ShouldSupportGenerics((MethodInfo Method, string Expected) param) =>
            Assert.That(new ClassSyntaxFactory(default).InvokeMethod(MetadataMethodInfo.CreateFrom(param.Method), null, null, "a").NormalizeWhitespace().ToFullString(), Is.EqualTo(param.Expected));

        protected interface IRefInterface
        {
            ref object RefMethod(in object a, out object b, ref object c);
        }

        private class RefClass : IRefInterface
        {
            public ref object RefMethod(in object a, out object b, ref object c) => throw new NotImplementedException();
        }

        public static (MethodInfo Method, string Expected)[] MethodsHavingNullableRetVal = new[]
        {
            (MethodInfoExtractor.Extract<INullable>(i => i.Nullable()), "global::System.Nullable<global::System.Int32> global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.INullable.Nullable()"),
            (MethodInfoExtractor.Extract<INullable>(i => i.Object()), "global::System.Object global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.INullable.Object()")
        };

        [TestCaseSource(nameof(MethodsHavingNullableRetVal))]
        public void ResolveMethod_ShouldSupportNullables((MethodInfo Method, string Expected) param) =>
            Assert.That(new ClassSyntaxFactory(default).ResolveMethod(MetadataMethodInfo.CreateFrom(param.Method)).NormalizeWhitespace().ToString(), Is.EqualTo(param.Expected));

        public static (MethodInfo Method, string Expected)[] InterfaceMethodsToResolve = new[]
        {
            (MethodInfoExtractor.Extract<IGeneric>(i => i.Foo(0)).GetGenericMethodDefinition(), "T global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.IGeneric.Foo<T>(T b)"),
            (MethodInfoExtractor.Extract<INullable>(i => i.Object()), "global::System.Object global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.INullable.Object()"),
        };

        [TestCaseSource(nameof(InterfaceMethodsToResolve))]
        public void ResolveMethod_ShouldSupportInterfaceMethods((MethodInfo Method, string Expected) param) =>
            Assert.That(new ClassSyntaxFactory(default).ResolveMethod(MetadataMethodInfo.CreateFrom(param.Method)).NormalizeWhitespace().ToString(), Is.EqualTo(param.Expected));

        public static (MethodInfo Method, string Expected)[] ClassMethodsToResolve = new[]
        {
            (MethodInfoExtractor.Extract<ClassSyntaxFactoryBaseTests>(x => x.VirtualMethod(0)), "protected override global::System.Int32 VirtualMethod(global::System.Int32 i)"),
            (MethodInfoExtractor.Extract<ClassSyntaxFactoryBaseTests>(x => x.GenericMethod(0)).GetGenericMethodDefinition(), "internal new void GenericMethod<T>(T a)"),
            (MethodInfoExtractor.Extract<ClassSyntaxFactoryBaseTests>(x => x.GenericVirtualMethodHavingConstraint<RefClass>(null!)).GetGenericMethodDefinition(), "protected override void GenericVirtualMethodHavingConstraint<T>(T a)\r\n    where T : new(), global::Solti.Utils.Proxy.SyntaxFactories.Tests.ClassSyntaxFactoryBaseTests.IRefInterface")
        };

        [TestCaseSource(nameof(ClassMethodsToResolve))]
        public void ResolveMethod_ShouldSupportClassMethods((MethodInfo Method, string Expected) param) =>
            Assert.That(new ClassSyntaxFactory(default).ResolveMethod(MetadataMethodInfo.CreateFrom(param.Method)).NormalizeWhitespace().ToString(), Is.EqualTo(param.Expected));

        private interface INullable 
        {
            int? Nullable();
            object Object(); // nullable
        }

        private interface IGeneric
        {
            T Foo<T>(T b);
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