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

        [Test]
        public void GenerateDuckMethod_ShouldThrowIfTheMethodNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => new DuckSyntaxFactory<IFoo<int>, BadFoo>.MethodInterceptorFactory(new DuckSyntaxFactory<IFoo<int>, BadFoo>()).Build(Foo),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckMethod_ShouldGenerateTheDesiredMethodIfSupported() =>
            Assert.That(new DuckSyntaxFactory<IFoo<int>, GoodFoo<int>>.MethodInterceptorFactory(new DuckSyntaxFactory<IFoo<int>, GoodFoo<int>>()).Build(Foo).NormalizeWhitespace().ToFullString(), Is.EqualTo("[System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\r\nSystem.Int32 Solti.Utils.Proxy.SyntaxFactories.Tests.ProxySyntaxFactoryTestsBase.IFoo<System.Int32>.Foo<TT>(System.Int32 a, out System.String b, ref TT c) => this.Target.Foo<TT>(a, out b, ref c);"));

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
            string dummyS;
            int dummyI = 0;

            MethodInfo foo = ((MethodInfo) MemberInfoExtensions.ExtractFrom<IFoo<int>>(i => i.Foo(0, out dummyS, ref dummyI))).GetGenericMethodDefinition();

            Assert.That(
                new DuckSyntaxFactory<IFoo<int>, ExplicitFoo>.MethodInterceptorFactory(new DuckSyntaxFactory<IFoo<int>, ExplicitFoo>()).Build(foo).NormalizeWhitespace(eol: "\n").ToFullString(), 
                Is.EqualTo("[System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\nSystem.Int32 Solti.Utils.Proxy.SyntaxFactories.Tests.ProxySyntaxFactoryTestsBase.IFoo<System.Int32>.Foo<TT>(System.Int32 a, out System.String b, ref TT c) => ((Solti.Utils.Proxy.SyntaxFactories.Tests.ProxySyntaxFactoryTestsBase.IFoo<System.Int32>)this.Target).Foo<TT>(a, out b, ref c);"));
        }

        [Test]
        public void GenerateDuckProperty_ShouldThrowIfThePropertyNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => new DuckSyntaxFactory<IFoo<int>, BadFoo>.PropertyInterceptorFactory(new DuckSyntaxFactory<IFoo<int>, BadFoo>()).Build(Prop),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckProperty_ShouldGenerateTheDesiredPropertyIfSupported() =>
            Assert.That(new DuckSyntaxFactory<IFoo<int>, GoodFoo<int>>.PropertyInterceptorFactory(new DuckSyntaxFactory<IFoo<int>, GoodFoo<int>>()).Build(Prop).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("System.Int32 Solti.Utils.Proxy.SyntaxFactories.Tests.ProxySyntaxFactoryTestsBase.IFoo<System.Int32>.Prop\n{\n    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    get => this.Target.Prop;\n    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    set => this.Target.Prop = value;\n}"));

        [Test]
        public void GenerateDuckProperty_ShouldThrowIfTheEventNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => new DuckSyntaxFactory<IFoo<int>, BadFoo>.EventInterceptorFactory(new DuckSyntaxFactory<IFoo<int>, BadFoo>()).Build(Event),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckEvent_ShouldGenerateTheDesiredEventIfSupported() =>
            Assert.That(new DuckSyntaxFactory<IFoo<int>, GoodFoo<int>>.EventInterceptorFactory(new DuckSyntaxFactory<IFoo<int>, GoodFoo<int>>()).Build(Event).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("event Solti.Utils.Proxy.SyntaxFactories.Tests.ProxySyntaxFactoryTestsBase.TestDelegate<System.Int32> Solti.Utils.Proxy.SyntaxFactories.Tests.ProxySyntaxFactoryTestsBase.IFoo<System.Int32>.Event\n{\n    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    add => this.Target.Event += value;\n    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    remove => this.Target.Event -= value;\n}"));

        [Test]
        public void GenerateDuckClass_ShouldGenerateTheDesiredClass() =>
            Assert.That(new DuckSyntaxFactory<IFoo<int>, GoodFoo<int>>().GenerateProxyClass().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("DuckClsSrc.txt")));

        [Test]
        public void GenerateDuckProperty_ShouldThrowOnAmbiguousImplementation() =>
            Assert.Throws<AmbiguousMatchException>(() => new DuckSyntaxFactory<IList<int>, List<int>>.PropertyInterceptorFactory(new DuckSyntaxFactory<IList<int>, List<int>>()).Build((PropertyInfo) MemberInfoExtensions.ExtractFrom<IList<int>>(i => i.IsReadOnly)));
    }
}
