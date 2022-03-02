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
using NUnit.Framework;

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Generators;
    using Internals;
    using Properties;

    using static Internals.Tests.CodeAnalysisTestsBase;

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

        private static DuckSyntaxFactory CreateGenerator<TInterface, TTarget>(string asm = null) where TInterface: class => new DuckSyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(typeof(TInterface)), 
            MetadataTypeInfo.CreateFrom(typeof(TTarget)),
            asm ?? typeof(DuckSyntaxFactoryTests).Assembly.GetName().Name,
            OutputType.Module, 
            MetadataTypeInfo.CreateFrom(typeof(DuckGenerator<TInterface, TTarget>)),
            null
        );

        [Test]
        public void GenerateDuckMethod_ShouldThrowIfTheMethodNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => CreateGenerator<IFoo<int>, BadFoo>().ResolveMethods(default).ToList(),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckMethod_ShouldGenerateTheDesiredMethodIfSupported()
        {
            Assert.That(CreateGenerator<IFoo<int>, GoodFoo<int>>().ResolveMethods(null).Any(m => m.NormalizeWhitespace(eol: "\n").ToFullString().Equals("[global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\nglobal::System.Int32 global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Foo<TT>(global::System.Int32 a, out global::System.String b, ref TT c) => this.Target.Foo<TT>(a, out b, ref c);")));
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
            Assert.That(CreateGenerator<IFoo<int>, ExplicitFoo>().ResolveMethods(null!).Any(m => m.NormalizeWhitespace(eol: "\n").ToFullString().Equals("[global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\nglobal::System.Int32 global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Foo<TT>(global::System.Int32 a, out global::System.String b, ref TT c) => ((global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>)this.Target).Foo<TT>(a, out b, ref c);")));
        }

        [Test]
        public void GenerateDuckProperty_ShouldThrowIfThePropertyNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => CreateGenerator<IFoo<int>, BadFoo>().ResolveProperties(default).ToList(),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckProperty_ShouldGenerateTheDesiredPropertyIfSupported()
        {
            Assert.That(CreateGenerator<IFoo<int>, GoodFoo<int>>().ResolveProperties(default).Any(m => m.NormalizeWhitespace(eol: " ").ToFullString().Equals("global::System.Int32 global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Prop {[global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]     get => this.Target.Prop; [global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]     set => this.Target.Prop = value; }")));
        }

        [Test]
        public void GenerateDuckProperty_ShouldThrowIfTheEventNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => CreateGenerator<IFoo<int>, BadFoo>().ResolveEvents(default).ToList(),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckEvent_ShouldGenerateTheDesiredEventIfSupported()
        {
            Assert.That(CreateGenerator<IFoo<int>, GoodFoo<int>>().ResolveEvents(null).Any(m => m.NormalizeWhitespace(eol: " ").ToFullString().Equals("event global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.TestDelegate<global::System.Int32> global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Event {[global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]     add => this.Target.Event += value; [global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]     remove => this.Target.Event -= value; }")));
        }

        [Test]
        public void GenerateDuckClass_ShouldGenerateTheDesiredClass()
        {
            Assert.That(CreateGenerator<IFoo<int>, GoodFoo<int>>().ResolveUnit(null, default).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("DuckClsSrc.txt").Replace("{version}", typeof(DuckGenerator<,>).Assembly.GetName().Version.ToString())));
        }

        [Test]
        public void GenerateDuckProperty_ShouldThrowOnAmbiguousImplementation() =>
            Assert.Throws<AmbiguousMatchException>(() => CreateGenerator<IList<int>, List<int>>().ResolveProperties(default).ToList());

        public static IEnumerable<Type> RandomInterfaces => Proxy.Tests.RandomInterfaces<string>.Values.Except(new[] { typeof(ITypeLib2), typeof(ITypeInfo2) });

        [Test]
        public void GenerateDuckClass_ShouldReturnTheSameValidSourceInCaseOfSymbolAndMetadata([ValueSource(nameof(RandomInterfaces))] Type type, [Values(OutputType.Module, OutputType.Unit)] int outputType)
        {
            Assembly[] refs = type
                .Assembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Append(type.Assembly)
                .Append(typeof(DuckBase<>).Assembly)
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
                    "cica", 
                    (OutputType) outputType, 
                    MetadataTypeInfo.CreateFrom(typeof(DuckGenerator<,>).MakeGenericType(type, type)),
                    new ReferenceCollector()
                ),
                fact2 = new DuckSyntaxFactory
                (
                    type2, 
                    type2, 
                    "cica", 
                    (OutputType) outputType, 
                    SymbolTypeInfo.CreateFrom
                    (
                        compilation
                            .GetTypeByMetadataName(typeof(DuckGenerator<,>).FullName).Construct
                            (
                                type2.ToSymbol(compilation), 
                                type2.ToSymbol(compilation)
                            ), 
                        compilation
                    ),
                    new ReferenceCollector()
                );

            string
                src1 = fact1.ResolveUnit(null, default).NormalizeWhitespace().ToFullString(),
                src2 = fact2.ResolveUnit(null, default).NormalizeWhitespace().ToFullString();

            // Assert.AreEqual(src1, src2); // deklaraciok sorrendje nem biztos h azonos
            Assert.DoesNotThrow(() => CreateCompilation(src1, fact1.ReferenceCollector.References.Select(asm => asm.Location)));
            Assert.DoesNotThrow(() => CreateCompilation(src2, fact2.ReferenceCollector.References.Select(asm => asm.Location)));
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
