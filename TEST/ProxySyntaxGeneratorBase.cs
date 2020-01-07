/********************************************************************************
* ProxySyntaxGeneratorBase.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    using static ProxySyntaxGeneratorBase;

    [TestFixture]
    public sealed class ProxySyntaxGeneratorBaseTests: ProxySyntaxGeneratorTestsBase
    {
        [Test]
        public void DeclareLocal_ShouldDeclareTheDesiredLocalVariable()
        {
            Assert.That(DeclareLocal<string[]>("paramz").NormalizeWhitespace().ToFullString(), Is.EqualTo("System.String[] paramz;"));
        }

        [Test]
        public void CreateType_ShouldCreateTheDesiredType()
        {
            Assert.That(CreateType<int[]>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32[]"));
            Assert.That(CreateType<IEnumerable<int[]>>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<System.Int32[]>"));
            Assert.That(CreateType<int[,]>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32[, ]"));
            Assert.That(CreateType(typeof(IEnumerable<>)).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<T>"));
            Assert.That(CreateType(typeof(IEnumerable<int>[])).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<System.Int32>[]"));
            Assert.That(CreateType<IEnumerable<IEnumerable<string>>>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<System.String>>"));
        }

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

        [Test]
        public void CreateType_ShouldHandleNestedTypes()
        {
            Assert.That(CreateType(typeof(Cica<>.Mica<>.Hajj)).NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.Proxy.Internals.Tests.Cica<T>.Mica<TT>.Hajj"));
            Assert.That(CreateType(typeof(Cica<>.Mica.Hajj<,>)).NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.Proxy.Internals.Tests.Cica<T>.Mica.Hajj<TT, TTT>"));
            Assert.That(CreateType<Cica<List<int>>.Mica<string>.Hajj>().NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.Proxy.Internals.Tests.Cica<System.Collections.Generic.List<System.Int32>>.Mica<System.String>.Hajj"));
            Assert.That(CreateType(typeof(Cica<int>.Mica.Hajj<string, object>)).NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.Proxy.Internals.Tests.Cica<System.Int32>.Mica.Hajj<System.String, System.Object>"));

            Assert.That(CreateType(typeof(CicaNested<>.Mica<>.Hajj)).NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.CicaNested<T>.Mica<TT>.Hajj"));
            Assert.That(CreateType(typeof(CicaNested<>.Mica.Hajj<,>)).NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.CicaNested<T>.Mica.Hajj<TT, TTT>"));
            Assert.That(CreateType<CicaNested<List<int>>.Mica<string>.Hajj>().NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.CicaNested<System.Collections.Generic.List<System.Int32>>.Mica<System.String>.Hajj"));
            Assert.That(CreateType(typeof(CicaNested<int>.Mica.Hajj<string, object>)).NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.CicaNested<System.Int32>.Mica.Hajj<System.String, System.Object>"));
        }

        [Test]
        public void DeclareProperty_ShouldDeclareTheDesiredProperty()
        {
            Assert.That(DeclareProperty(Prop, SyntaxFactory.Block(), SyntaxFactory.Block()).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("System.Int32 Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorTestsBase.IFoo<System.Int32>.Prop\n{\n    get\n    {\n    }\n\n    set\n    {\n    }\n}"));
        }

        [Test]
        public void DeclareField_ShouldDeclareAField()
        {
            Assert.That(DeclareField<EventInfo>("FEvent", SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression), SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword, SyntaxKind.ReadOnlyKeyword).NormalizeWhitespace().ToFullString(), Is.EqualTo("private static readonly System.Reflection.EventInfo FEvent = null;"));
        }

        [Test]
        public void DeclareEvent_ShouldDeclareTheDesiredEvent()
        {
            Assert.That(DeclareEvent(Event, SyntaxFactory.Block(), SyntaxFactory.Block()).NormalizeWhitespace(eol: "\n").ToString(), Is.EqualTo("event Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorTestsBase.TestDelegate<System.Int32> Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorTestsBase.IFoo<System.Int32>.Event\n{\n    add\n    {\n    }\n\n    remove\n    {\n    }\n}"));
        }

        [Test]
        public void DeclareMethod_ShouldHandleParamsModifier() 
        {
            Assert.That(DeclareMethod(typeof(IParams).GetMethod(nameof(IParams.Foo))).NormalizeWhitespace().ToString(), Is.EqualTo("void Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.IParams.Foo(params System.Int32[] paramz)"));
        }

        private interface IParams 
        {
            void Foo(params int[] paramz);
        }

        [Test]
        public void DeclareMethod_ShouldSupportRefKeywords() 
        {
            Assert.That(DeclareMethod(typeof(IRefInterface).GetMethod(nameof(IRefInterface.RefMethod), BindingFlags.Instance | BindingFlags.Public)).NormalizeWhitespace().ToString(), Is.EqualTo("void Solti.Utils.Proxy.Internals.Tests.ProxySyntaxGeneratorBaseTests.IRefInterface.RefMethod(in System.Object a, out System.Object b, ref System.Object c)"));
        }

        [Test]
        public void InvokeMethod_ShouldSupportRefKeywords() 
        {
            Assert.That(InvokeMethod(typeof(IRefInterface).GetMethod(nameof(IRefInterface.RefMethod), BindingFlags.Instance | BindingFlags.Public), "target", new[] {"a", "b", "c"}).NormalizeWhitespace().ToString(), Is.EqualTo("target.RefMethod(in a, out b, ref c)"));
        }

        private interface IRefInterface 
        {
            void RefMethod(in object a, out object b, ref object c);
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