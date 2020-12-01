/********************************************************************************
* Reflection.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public class ReflectionTests
    {
        [Test]
        public void EqualityComparison_ShouldWork() 
        {
            Assert.That(MetadataTypeInfo.CreateFrom(typeof(object)).Equals(MetadataTypeInfo.CreateFrom(typeof(object))));

            var set = new HashSet<ITypeInfo>();
            Assert.That(set.Add(MetadataTypeInfo.CreateFrom(typeof(object))));
            Assert.False(set.Add(MetadataTypeInfo.CreateFrom(typeof(object))));
        }

        private static CSharpCompilation CreateCompilation(string src, params Assembly[] additionalReferences) => CSharpCompilation.Create
        (
            "cica",
            new[]
            {
                CSharpSyntaxTree.ParseText(src)
            },
            Runtime.Assemblies.Concat(additionalReferences).Distinct().Select(asm => MetadataReference.CreateFromFile(asm.Location!))
        );

        [Test]
        public void AssemblyInfo_Test()
        {
            Assembly asm = typeof(ISyntaxFactory).Assembly;

            Compilation compilation = CreateCompilation("", asm);

            IAssemblyInfo
                asm1 = MetadataAssemblyInfo.CreateFrom(asm),
                asm2 = SymbolAssemblyInfo.CreateFrom((IAssemblySymbol) compilation.GetAssemblyOrModuleSymbol(compilation.References.Single(@ref => @ref.Display == asm.Location)), compilation);

            Assert.AreEqual(asm1.Name, asm2.Name);
            Assert.AreEqual(asm1.Location, asm2.Location);
            Assert.AreEqual(asm1.IsDynamic, asm2.IsDynamic);
            Assert.That(asm1.IsFriend(typeof(ReflectionTests).Assembly.GetName().Name));
            Assert.That(asm2.IsFriend(typeof(ReflectionTests).Assembly.GetName().Name));
        }
    }
}
