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
    using static Internals.DuckSyntaxFactory;

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
            MetadataTypeInfo.CreateFrom(typeof(DuckGenerator<TInterface, TTarget>))
        );

        [Test]
        public void GenerateDuckMethod_ShouldThrowIfTheMethodNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => new MethodInterceptorFactory(CreateGenerator<IFoo<int>, BadFoo>()).Build(default),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckMethod_ShouldGenerateTheDesiredMethodIfSupported()
        {
            var fact = new MethodInterceptorFactory(CreateGenerator<IFoo<int>, GoodFoo<int>>());
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
            var fact = new MethodInterceptorFactory(CreateGenerator<IFoo<int>, ExplicitFoo>());
            fact.Build(default);

            Assert.That(fact.Members.Any(m => m.NormalizeWhitespace(eol: "\n").ToFullString().Equals("[global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\nglobal::System.Int32 global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Foo<TT>(global::System.Int32 a, out global::System.String b, ref TT c) => ((global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>)this.Target).Foo<TT>(a, out b, ref c);")));
        }

        [Test]
        public void GenerateDuckProperty_ShouldThrowIfThePropertyNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => new PropertyInterceptorFactory(CreateGenerator<IFoo<int>, BadFoo>()).Build(default),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckProperty_ShouldGenerateTheDesiredPropertyIfSupported()
        {
            var fact = new PropertyInterceptorFactory(CreateGenerator<IFoo<int>, GoodFoo<int>>());
            fact.Build(default);

            Assert.That(fact.Members.Any(m => m.NormalizeWhitespace(eol: " ").ToFullString().Equals("global::System.Int32 global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Prop {[global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]     get => this.Target.Prop; [global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]     set => this.Target.Prop = value; }")));
        }

        [Test]
        public void GenerateDuckProperty_ShouldThrowIfTheEventNotSupported() =>
            Assert.Throws<MissingMemberException>
            (
                () => new EventInterceptorFactory(CreateGenerator<IFoo<int>, BadFoo>()).Build(default),
                Resources.MISSING_IMPLEMENTATION
            );

        [Test]
        public void GenerateDuckEvent_ShouldGenerateTheDesiredEventIfSupported()
        {
            var fact = new EventInterceptorFactory(CreateGenerator<IFoo<int>, GoodFoo<int>>());
            fact.Build(default);

            Assert.That(fact.Members.Any(m => m.NormalizeWhitespace(eol: " ").ToFullString().Equals("event global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.TestDelegate<global::System.Int32> global::Solti.Utils.Proxy.SyntaxFactories.Tests.SyntaxFactoryTestsBase.IFoo<global::System.Int32>.Event {[global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]     add => this.Target.Event += value; [global::System.Runtime.CompilerServices.MethodImplAttribute(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]     remove => this.Target.Event -= value; }")));
        }

        [Test]
        public void GenerateDuckClass_ShouldGenerateTheDesiredClass()
        {
            var fact = CreateGenerator<IFoo<int>, GoodFoo<int>>();
            fact.Build(default);

            Assert.That(fact.Unit.NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("DuckClsSrc.txt").Replace("{version}", typeof(DuckGenerator<,>).Assembly.GetName().Version.ToString())));
        }

        [Test]
        public void GenerateDuckProperty_ShouldThrowOnAmbiguousImplementation() =>
            Assert.Throws<AmbiguousMatchException>(() => new PropertyInterceptorFactory(CreateGenerator<IList<int>, List<int>>()).Build(default));

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

            IUnitSyntaxFactory
                fact1 = new DuckSyntaxFactory
                (
                    type1, 
                    type1, 
                    "cica", 
                    (OutputType) outputType, 
                    MetadataTypeInfo.CreateFrom(typeof(DuckGenerator<,>).MakeGenericType(type, type))
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
                    )
                );

            Assert.DoesNotThrow(() => fact1.Build());
            Assert.DoesNotThrow(() => fact2.Build());

            string
                src1 = fact1.Unit.NormalizeWhitespace().ToFullString(),
                src2 = fact2.Unit.NormalizeWhitespace().ToFullString();

            // Assert.AreEqual(src1, src2); // deklaraciok sorrendje nem biztos h azonos
            Assert.DoesNotThrow(() => CreateCompilation(src1, fact1.References.Select(asm => asm.Location)));
            Assert.DoesNotThrow(() => CreateCompilation(src2, fact2.References.Select(asm => asm.Location)));
        }

        public static IEnumerable<object> Factories
        {
            get
            {
                ITypeInfo iface = MetadataTypeInfo.CreateFrom(typeof(IComplex));

                var fact = new DuckSyntaxFactory
                (
                    iface, 
                    iface, 
                    "cica", 
                    OutputType.Module, 
                    MetadataTypeInfo.CreateFrom
                    (
                        typeof(DuckGenerator<,>).MakeGenericType(typeof(IComplex), typeof(IComplex))
                    )
                );

                yield return new ConstructorFactory(fact);
                yield return new MethodInterceptorFactory(fact);
                yield return new PropertyInterceptorFactory(fact);
                yield return new EventInterceptorFactory(fact);
            }
        }

        [TestCaseSource(nameof(Factories))]
        public void Factory_CanBeCancelled(object f)
        {
            ISyntaxFactory fact = (ISyntaxFactory) f;

            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();

                Assert.Throws<OperationCanceledException>(() => fact.Build(cancellation.Token));
            }
        }
    }
}
