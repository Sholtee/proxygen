/********************************************************************************
* DuckSyntaxFactory.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Internals;
    using Properties;

    [TestFixture]
    public sealed class DuckSyntaxFactoryTests : SyntaxFactoryTestsBase
    {
        public sealed class BadFoo
        {
            public int Foo<TT>(int a, out string b, TT c) => (b = string.Empty).GetHashCode(); // nincs ref

            public int Prop { get; } // nincs setter

            // nincs esemeny
        }

        public sealed class GoodFoo<T>
        {
            public int Foo<TT>(int a, out string b, ref TT c) => (b = string.Empty).GetHashCode();

            public void Bar()
            {
            }

            public T Prop { get; set; }

#pragma warning disable 67  // impliciten hasznalva van
            public event TestDelegate<T> Event;
#pragma warning restore 67

        }

        private static DuckSyntaxFactory CreateGenerator<TInterface, TTarget>(string asm = null) => new DuckSyntaxFactory(MetadataTypeInfo.CreateFrom(typeof(TInterface)), MetadataTypeInfo.CreateFrom(typeof(TTarget)), asm, OutputType.Module);

        [Test]
        public void GenerateDuckMethod_ShouldThrowIfTheMethodNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => new DuckSyntaxFactory.MethodInterceptorFactory(CreateGenerator<IFoo<int>, BadFoo>()).Build(default),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckMethod_ShouldGenerateTheDesiredMethodIfSupported()
        {
            var fact = new DuckSyntaxFactory.MethodInterceptorFactory(CreateGenerator<IFoo<int>, GoodFoo<int>>());
            fact.Build(default);

            Assert.That(fact.Members.Any(m => m.NormalizeWhitespace(eol: "\n").ToFullString().Equals("[global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\nglobal::System.Int32 global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Foo<TT>(global::System.Int32 a, out global::System.String b, ref TT c) => this.Target.Foo<TT>(a, out b, ref c);")));
        }

        public class ExplicitFoo : IFoo<int>
        {
            int IFoo<int>.Prop { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            event TestDelegate<int> IFoo<int>.Event
            {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }
            void IFoo<int>.Bar() => throw new NotImplementedException();
            int IFoo<int>.Foo<TT>(int a, out string b, ref TT c) => throw new NotImplementedException();
        }

        [Test]
        public void GenerateDuckMethod_ShouldHandleExplicitImplementations()
        {
            var fact = new DuckSyntaxFactory.MethodInterceptorFactory(CreateGenerator<IFoo<int>, ExplicitFoo>());
            fact.Build(default);

            Assert.That(fact.Members.Any(m => m.NormalizeWhitespace(eol: "\n").ToFullString().Equals("[global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\nglobal::System.Int32 global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Foo<TT>(global::System.Int32 a, out global::System.String b, ref TT c) => ((global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>)this.Target).Foo<TT>(a, out b, ref c);")));
        }

        [Test]
        public void GenerateDuckProperty_ShouldThrowIfThePropertyNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => new DuckSyntaxFactory.PropertyInterceptorFactory(CreateGenerator<IFoo<int>, BadFoo>()).Build(default),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckProperty_ShouldGenerateTheDesiredPropertyIfSupported()
        {
            var fact = new DuckSyntaxFactory.PropertyInterceptorFactory(CreateGenerator<IFoo<int>, GoodFoo<int>>());
            fact.Build(default);

            Assert.That(fact.Members.Any(m => m.NormalizeWhitespace(eol: "\n").ToFullString().Equals("global::System.Int32 global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Prop\n{\n    [global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    get => this.Target.Prop;\n    [global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    set => this.Target.Prop = value;\n}")));
        }

        [Test]
        public void GenerateDuckProperty_ShouldThrowIfTheEventNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => new DuckSyntaxFactory.EventInterceptorFactory(CreateGenerator<IFoo<int>, BadFoo>()).Build(default),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckEvent_ShouldGenerateTheDesiredEventIfSupported()
        {
            var fact = new DuckSyntaxFactory.EventInterceptorFactory(CreateGenerator<IFoo<int>, GoodFoo<int>>());
            fact.Build(default);

            Assert.That(fact.Members.Any(m => m.NormalizeWhitespace(eol: "\n").ToFullString().Equals("event global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.TestDelegate<global::System.Int32> global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Event\n{\n    [global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    add => this.Target.Event += value;\n    [global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    remove => this.Target.Event -= value;\n}")));
        }

        [Test]
        public void GenerateDuckClass_ShouldGenerateTheDesiredClass()
        {
            var fact = CreateGenerator<IFoo<int>, GoodFoo<int>>();
            fact.Build(default);

            Assert.That(fact.Unit.NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("DuckClsSrc.txt")));
        }

        [Test]
        public void GenerateDuckProperty_ShouldThrowOnAmbiguousImplementation() =>
            Assert.Throws<AmbiguousMatchException>(() => new DuckSyntaxFactory.PropertyInterceptorFactory(CreateGenerator<IList<int>, List<int>>()).Build(default));
    }
}
