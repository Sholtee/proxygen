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
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Generators;
    using Internals;
    using Properties;

    using static Internals.Tests.CodeAnalysisTestsBase;

    [TestFixture, Parallelizable(ParallelScope.All)]
#if LEGACY_COMPILER
    [Ignore("Roslyn v3 has different formatting rules than v4")]
#endif
    public sealed class DuckSyntaxFactoryTests : SyntaxFactoryTestsBase
    {
        private static ClassDeclarationSyntax GetDummyClass() => SyntaxFactory.ClassDeclaration("Dummy");

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

            public T this[int i] { get => default; set { } }
            public T Prop { get; set; }

#pragma warning disable 67  // impliciten hasznalva van
            public event TestDelegate<T> Event;
#pragma warning restore 67

        }

        private static DuckSyntaxFactory CreateGenerator<TInterface, TTarget>(string asm = null) where TInterface: class => new DuckSyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(typeof(TInterface)), 
            MetadataTypeInfo.CreateFrom(typeof(TTarget)),
            SyntaxFactoryContext.Default with
            {
                AssemblyNameOverride = asm ?? typeof(DuckSyntaxFactoryTests).Assembly.GetName().Name
            }
        );

        [Test]
        public void ResolveMethod_ShouldThrowIfTheMethodNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => CreateGenerator<IFoo<int>, BadFoo>().ResolveMethods(GetDummyClass(), default),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void ResolveMethod_ShouldGenerateTheDesiredMethodIfSupported() =>
            Assert.That(CreateGenerator<IFoo<int>, GoodFoo<int>>().ResolveMethods(GetDummyClass(), null).Members.Any(m => m.NormalizeWhitespace(eol: "\n").ToFullString().Equals("global::System.Int32 global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Foo<TT>(global::System.Int32 a, out global::System.String b, ref TT c) => (this.FTarget ?? throw new global::System.InvalidOperationException()).Foo<TT>(a, out b, ref c);")));

        public class ExplicitFoo : IFoo<int>
        {
            public int this[int i] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
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
        public void ResolveMethod_ShouldHandleExplicitImplementations()
        {
            MemberDeclarationSyntax m = CreateGenerator<IFoo<int>, ExplicitFoo>().ResolveMethods(GetDummyClass(), null).Members.Single(m => m is MethodDeclarationSyntax method && method.Identifier.Text.Contains("Foo"));

            Assert.That(m.NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("global::System.Int32 global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Foo<TT>(global::System.Int32 a, out global::System.String b, ref TT c) => ((global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>)(this.FTarget ?? throw new global::System.InvalidOperationException())).Foo<TT>(a, out b, ref c);"));
        }

        [Test]
        public void ResolveProperty_ShouldThrowIfThePropertyNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => CreateGenerator<IFoo<int>, BadFoo>().ResolveProperties(GetDummyClass(), default),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void ResolveProperty_ShouldGenerateTheDesiredPropertyIfSupported()
        {
            MemberDeclarationSyntax p = CreateGenerator<IFoo<int>, GoodFoo<int>>().ResolveProperties(GetDummyClass(), default).Members.Single(m => m is PropertyDeclarationSyntax p && p.Identifier.Text.Contains("Prop"));

            Assert.That(p.NormalizeWhitespace(eol: "\n").ToString(), Is.EqualTo("global::System.Int32 global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Prop { get => (this.FTarget ?? throw new global::System.InvalidOperationException()).Prop; set => (this.FTarget ?? throw new global::System.InvalidOperationException()).Prop = value; }"));
        }

        [Test]
        public void ResolveEvent_ShouldThrowIfTheEventNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => CreateGenerator<IFoo<int>, BadFoo>().ResolveEvents(GetDummyClass(), default),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void ResolveEvent_ShouldGenerateTheDesiredEventIfSupported()
        {
            MemberDeclarationSyntax e = CreateGenerator<IFoo<int>, GoodFoo<int>>().ResolveEvents(GetDummyClass(), null).Members.Single(m => m is EventDeclarationSyntax);

            Assert.That(e.NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("event global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.TestDelegate<global::System.Int32> global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Event { add => (this.FTarget ?? throw new global::System.InvalidOperationException()).Event += value; remove => (this.FTarget ?? throw new global::System.InvalidOperationException()).Event -= value; }"));
        }

        [Test]
        public void ResolveUnit_ShouldGenerateTheDesiredClass() =>
            Assert.That
            (
                CreateGenerator<IFoo<int>, GoodFoo<int>>()
                    .ResolveUnit(null, default)
                    .NormalizeWhitespace(eol: "\n")
                    .ToFullString(),
                Is.EqualTo
                (
                    File
                        .ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DuckClsSrc.txt"))
                        .Replace("\r", string.Empty)
                        .Replace("{version}", typeof(DuckGenerator<,>)
                            .Assembly
                            .GetName()
                            .Version
                            .ToString())
                )
            );

        [Test]
        public void ResolveProperty_ShouldThrowOnAmbiguousImplementation() =>
            Assert.Throws<AmbiguousMatchException>(() => CreateGenerator<IList<int>, List<int>>().ResolveProperties(GetDummyClass(), default));

        public static IEnumerable<Type> RandomInterfaces => Proxy.Tests.RandomInterfaces<string>
            .Values
#if NET8_0_OR_GREATER
            .Except(new[] { typeof(IParsable<string>), typeof(ISpanParsable<string>) })
#endif
            .Except(new[] { typeof(ITypeLib2), typeof(ITypeInfo2) });

        [Test]
        public void ResolveUnit_ShouldReturnTheSameValidSourceInCaseOfSymbolAndMetadata([ValueSource(nameof(RandomInterfaces))] Type type, [Values(OutputType.Module, OutputType.Unit)] int outputType)
        {
            Assembly[] refs = type
                .Assembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Concat([type.Assembly, typeof(DuckGenerator<,>).Assembly])
                .Distinct()
                .ToArray();

            Compilation compilation = CreateCompilation(string.Empty, refs);

            ITypeInfo
                type1 = MetadataTypeInfo.CreateFrom(type),
                type2 = SymbolTypeInfo.CreateFrom(type1.ToSymbol(compilation), compilation);

            DuckSyntaxFactory
                fact1 = new DuckSyntaxFactory
                (
                    type1,
                    type1,
                    SyntaxFactoryContext.Default with { OutputType = (OutputType) outputType }
                ),
                fact2 = new DuckSyntaxFactory
                (
                    type2, 
                    type2,
                    SyntaxFactoryContext.Default with { OutputType = (OutputType) outputType }
                );

            string
                src1 = fact1.ResolveUnit(null, default).NormalizeWhitespace().ToFullString(),
                src2 = fact2.ResolveUnit(null, default).NormalizeWhitespace().ToFullString();

            Assert.That(src1.SequenceEqual(src2));
        }

        [Test]
        public void Factory_CanBeCancelled()
        {
            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                Assert.Throws<OperationCanceledException>(() => CreateGenerator<IFoo<int>, GoodFoo<int>>().ResolveUnit(null, cancellation.Token));
            }
        }
    }
}
