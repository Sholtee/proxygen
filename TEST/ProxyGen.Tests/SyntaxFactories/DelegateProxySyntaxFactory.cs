/********************************************************************************
* DelegateProxySyntaxFactory.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Internals;

    [TestFixture, Parallelizable(ParallelScope.All)]
#if LEGACY_COMPILER
    [Ignore("Roslyn v3 has different formatting rules than v4")]
#endif
    public sealed class DelegateProxySyntaxFactoryTests
    {
        public delegate int MyDelegate<T>(string a, ref T[] b, out object c);

        private static DelegateProxySyntaxFactory CreateSyntaxFactory(Type target, OutputType outputType) => new
        (
            MetadataTypeInfo.CreateFrom(target),
            SyntaxFactoryContext.Default with
            {
                AssemblyNameOverride = typeof(DelegateProxySyntaxFactory).Assembly.GetName().Name,
                OutputType = outputType
            }
        );

        [TestCase(typeof(Func<List<string>, int>), OutputType.Module, "FuncProxySrcModule.txt")]
        [TestCase(typeof(Func<List<string>, int>), OutputType.Unit, "FuncProxySrcUnit.txt")]
        [TestCase(typeof(MyDelegate<IList<long>>), OutputType.Module, "DelegateProxySrcModule.txt")]
        [TestCase(typeof(MyDelegate<IList<long>>), OutputType.Unit, "DelegateProxySrcUnit.txt")]
        public void ResolveUnit_ShouldGenerateTheDesiredUnit(Type target, int outputType, string fileName)
        {
            DelegateProxySyntaxFactory gen = CreateSyntaxFactory(target, (OutputType) outputType);

            Assert.That
            (
                gen
                    .ResolveUnit(null, default)
                    .Stringify("\n"),
                Is.EqualTo
                (
                    File
                        .ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName))
                        .Replace("\r", string.Empty)
                        .Replace("{version}", typeof(ClassProxySyntaxFactory)
                            .Assembly
                            .GetName()
                            .Version
                            .ToString())
                )
            );
        }
    }
}