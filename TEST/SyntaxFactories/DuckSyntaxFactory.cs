/********************************************************************************
* DuckSyntaxFactory.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Internals;
    using Properties;

    [TestFixture]
    public sealed class DuckSyntaxFactoryTests : ProxySyntaxFactoryTestsBase
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

        private static Internals.DuckSyntaxFactory<IFoo<int>, T> GeneratorFor<T>() => new Internals.DuckSyntaxFactory<IFoo<int>, T>();

        [Test]
        public void GenerateDuckMethod_ShouldThrowIfTheMethodNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => GeneratorFor<BadFoo>().GenerateDuckMethod(Foo),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckMethod_ShouldGenerateTheDesiredMethodIfSupported() =>
            Assert.That(GeneratorFor<GoodFoo<int>>().GenerateDuckMethod(Foo).NormalizeWhitespace().ToFullString(), Is.EqualTo("[System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\r\nSystem.Int32 Solti.Utils.Proxy.SyntaxFactories.Tests.ProxySyntaxFactoryTestsBase.IFoo<System.Int32>.Foo<TT>(System.Int32 a, out System.String b, ref TT c) => this.Target.Foo<TT>(a, out b, ref c);"));

        [Test]
        public void GenerateDuckProperty_ShouldThrowIfThePropertyNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => GeneratorFor<BadFoo>().GenerateDuckProperty(Prop),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckProperty_ShouldGenerateTheDesiredPropertyIfSupported() =>
            Assert.That(GeneratorFor<GoodFoo<int>>().GenerateDuckProperty(Prop).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("System.Int32 Solti.Utils.Proxy.SyntaxFactories.Tests.ProxySyntaxFactoryTestsBase.IFoo<System.Int32>.Prop\n{\n    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    get => this.Target.Prop;\n    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    set => this.Target.Prop = value;\n}"));

        [Test]
        public void GenerateDuckProperty_ShouldThrowIfTheEventNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => GeneratorFor<BadFoo>().GenerateDuckEvent(Event),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckEvent_ShouldGenerateTheDesiredEventIfSupported() =>
            Assert.That(GeneratorFor<GoodFoo<int>>().GenerateDuckEvent(Event).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("event Solti.Utils.Proxy.SyntaxFactories.Tests.ProxySyntaxFactoryTestsBase.TestDelegate<System.Int32> Solti.Utils.Proxy.SyntaxFactories.Tests.ProxySyntaxFactoryTestsBase.IFoo<System.Int32>.Event\n{\n    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    add => this.Target.Event += value;\n    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    remove => this.Target.Event -= value;\n}"));

        private class Dummy
        {
        }

        [Test]
        public void GenerateProxyClass_ShouldThrowOnMissingImplementation()
        {
            var ex = Assert.Throws<AggregateException>(() => GeneratorFor<Dummy>().GenerateProxyClass());
            Assert.That(ex.InnerExceptions.Count, Is.EqualTo(4));
        }

        [Test]
        public void GenerateDuckClass_ShouldGenerateTheDesiredClass() =>
            Assert.That(GeneratorFor<GoodFoo<int>>().GenerateProxyClass().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("DuckClsSrc.txt")));

        [Test]
        public void GenerateDuckProperty_ShouldThrowOnAmbiguousImplementation() =>
            Assert.Throws<AmbiguousMatchException>(() => new DuckSyntaxFactory<IList<int>, List<int>>().GenerateDuckProperty((PropertyInfo) MemberInfoExtensions.ExtractFrom<IList<int>>(i => i.IsReadOnly)));
    }
}
