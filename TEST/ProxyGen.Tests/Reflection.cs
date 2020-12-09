/********************************************************************************
* Reflection.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public class ReflectionTests : IXxXSymbolExtensionsTestsBase
    {
        [Test]
        public void EqualityComparison_ShouldWork()
        {
            Assert.That(MetadataTypeInfo.CreateFrom(typeof(object)).Equals(MetadataTypeInfo.CreateFrom(typeof(object))));

            var set = new HashSet<ITypeInfo>();
            Assert.That(set.Add(MetadataTypeInfo.CreateFrom(typeof(object))));
            Assert.False(set.Add(MetadataTypeInfo.CreateFrom(typeof(object))));
        }

        [Test]
        public void AssemblyInfo_AbstractionTest()
        {
            Assembly asm = typeof(ISyntaxFactory).Assembly;

            Compilation compilation = CreateCompilation(string.Empty, asm);

            IAssemblyInfo
                asm1 = MetadataAssemblyInfo.CreateFrom(asm),
                asm2 = SymbolAssemblyInfo.CreateFrom((IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(compilation.References.Single(@ref => @ref.Display == asm.Location)), compilation);

            Assert.AreEqual(asm1.Name, asm2.Name);
            Assert.AreEqual(asm1.Location, asm2.Location);
            Assert.AreEqual(asm1.IsDynamic, asm2.IsDynamic);
            Assert.That(asm1.IsFriend(typeof(ReflectionTests).Assembly.GetName().Name));
            Assert.That(asm2.IsFriend(typeof(ReflectionTests).Assembly.GetName().Name));
        }

        [Test]
        public void TypeInfo_AbstractionTest([Values(typeof(void), typeof(int), typeof(int[]), typeof(int[,]), typeof((int Int, string String)), typeof(int*), typeof(List<>), typeof(List<object>), typeof(NestedGeneric<>), typeof(NestedGeneric<List<string>>))] Type type) 
        {
            Compilation compilation = CreateCompilation(string.Empty, type.Assembly);

            ITypeInfo
                type1 = MetadataTypeInfo.CreateFrom(type),
                type2 = SymbolTypeInfo.CreateFrom(SymbolTypeInfo.TypeInfoToSymbol(type1, compilation), compilation);

            AssertEquals(type1, type2);
            AssertEquals(type1.ElementType, type2.ElementType);
            AssertSequenceEquals(type1.Bases, type2.Bases);
            AssertSequenceEquals(type1.Interfaces.OrderBy(i => i.Name).ToArray(), type2.Interfaces.OrderBy(i => i.Name).ToArray());
            AssertSequenceEquals(type1.EnclosingTypes, type2.EnclosingTypes);

            void AssertEquals(ITypeInfo t1, ITypeInfo t2) 
            {
                if (t1 == null || t2 == null) 
                {
                    Assert.AreSame(t1, t2);
                    return;
                }

                Assert.AreEqual(t1.Name, t2.Name);
                Assert.AreEqual(t1.FullName, t2.FullName);
                Assert.AreEqual(t1.AssemblyQualifiedName, t2.AssemblyQualifiedName);
                Assert.AreEqual(t1.IsNested, t2.IsNested);
                Assert.AreEqual(t1.IsInterface, t2.IsInterface);
                Assert.AreEqual(t1.IsByRef, t2.IsByRef);
                Assert.AreEqual(t1.IsGenericParameter, t2.IsGenericParameter);
                Assert.AreEqual(t1.IsVoid, t2.IsVoid);
            }

            void AssertSequenceEquals(IReadOnlyList<ITypeInfo> l1, IReadOnlyList<ITypeInfo> l2) 
            {
                Assert.That(l1.Count, Is.EqualTo(l2.Count));

                for (int i = 0; i < l1.Count; i++)
                {
                    AssertEquals(l1[i], l2[i]);
                }
            }
        }

        private class NestedGeneric<T>
        {
            public class Nested<TT> { }
            public class Nested { }
        }

        public static IEnumerable<ITypeInfo> Generics
        {
            get
            {
                yield return MetadataTypeInfo.CreateFrom(typeof(List<>));

                Compilation compilation = CreateCompilation(string.Empty, typeof(List<>).Assembly);
                yield return SymbolTypeInfo.CreateFrom(compilation.GetTypeByMetadataName(typeof(List<>).FullName), compilation);
                
            }
        }

        [TestCaseSource(nameof(Generics))]
        public void GenericTypeInfo_CanBeSpecialized(IGenericTypeInfo type)
        {
            Assert.DoesNotThrow(() => type = (IGenericTypeInfo) type.Close(MetadataTypeInfo.CreateFrom(typeof(string))));
            Assert.That(type.FullName, Is.EqualTo("System.Collections.Generic.List`1"));
            Assert.That(type.GenericArguments.Single().FullName, Is.EqualTo("System.String"));
        }

        public static IEnumerable<(Type Type, string Name)> Types 
        {
            get 
            {
                yield return (typeof(int), "int");
                yield return (typeof(List<>), "System.Collections.Generic.List<T>");
                yield return (typeof(List<int>), "System.Collections.Generic.List<int>");
                yield return (typeof(List<List<string>>), "System.Collections.Generic.List<System.Collections.Generic.List<string>>");
                yield return (typeof(NestedGeneric<>), "Solti.Utils.Proxy.Internals.Tests.ReflectionTests.NestedGeneric<T>");
                yield return (typeof(NestedGeneric<List<string>>), "Solti.Utils.Proxy.Internals.Tests.ReflectionTests.NestedGeneric<System.Collections.Generic.List<string>>");
                yield return (typeof(NestedGeneric<int>.Nested), "Solti.Utils.Proxy.Internals.Tests.ReflectionTests.NestedGeneric<int>.Nested");
                yield return (typeof(NestedGeneric<int>.Nested<string>), "Solti.Utils.Proxy.Internals.Tests.ReflectionTests.NestedGeneric<int>.Nested<string>");
            }
        }

        [TestCaseSource(nameof(Types))]
        public void TypeInfoToSymbol_ShouldReturnTheDesiredSymbol((Type Type, string Name) data) 
        {
            Compilation compilation = CreateCompilation(string.Empty, data.Type.Assembly);

            ITypeSymbol resolved = SymbolTypeInfo.TypeInfoToSymbol(MetadataTypeInfo.CreateFrom(data.Type), compilation);

            Assert.That(resolved.ToString(), Is.EqualTo(data.Name));
        }

        [Test]
        public void TypeInfoToSymbol_ShouldThrowIfTheTypeNotFound() 
        {
            Compilation compilation = CreateCompilation(string.Empty);
            Assert.Throws<TypeLoadException>(() => SymbolTypeInfo.TypeInfoToSymbol(MetadataTypeInfo.CreateFrom(typeof(NestedGeneric<>)), compilation));
        }

        [TestCaseSource(nameof(Types))]
        public void TypeInfoToMetadata_ShouldReturnTheDesiredMetadata((Type Type, string _) data)
        {
            Type resolved = MetadataTypeInfo.TypeInfoToMetadata(MetadataTypeInfo.CreateFrom(data.Type));
            Assert.That(resolved, Is.EqualTo(data.Type));
        }
    }
}
