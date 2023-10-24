/********************************************************************************
* Reflection.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class ReflectionTests : CodeAnalysisTestsBase
    {
        [Test]
        public void EqualityComparison_ShouldWork()
        {
            Assert.That(MetadataTypeInfo.CreateFrom(typeof(object)).Equals(MetadataTypeInfo.CreateFrom(typeof(object))));

            var set = new HashSet<ITypeInfo>();
            Assert.That(set.Add(MetadataTypeInfo.CreateFrom(typeof(object))));
            Assert.False(set.Add(MetadataTypeInfo.CreateFrom(typeof(object))));
        }

        public static IEnumerable<object> FriendAsms 
        {
            get 
            {
                yield return MetadataAssemblyInfo.CreateFrom(typeof(ClassSyntaxFactoryBase).Assembly);
                yield return CreateTI(typeof(ClassSyntaxFactoryBase).Assembly);
                yield return MetadataAssemblyInfo.CreateFrom(typeof(ReflectionTests).Assembly);
                yield return CreateTI(typeof(ReflectionTests).Assembly);
            }
        }

        private static IAssemblyInfo CreateTI(Assembly asm)
        {
            Compilation compilation = CreateCompilation(string.Empty, asm);

            return SymbolAssemblyInfo.CreateFrom((IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(compilation.References.Single(@ref => @ref.Display == asm.Location)), compilation);
        }

        [TestCaseSource(nameof(FriendAsms))]
        public void AssemblyInfo_FriendshipTest(object asm) =>
            Assert.That(((IAssemblyInfo) asm).IsFriend(typeof(ReflectionTests).Assembly.GetName().Name));

        private HashSet<string> FProcessedTypes;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            FProcessedTypes = new HashSet<string>
            {
#if NETCOREAPP3_1
                "System.Collections.Generic.NonRandomizedStringEqualityComparer"
#endif
#if NET6_0
                "System.UIntPtr"
#endif
            };
        }

       // [TestCase("System.UIntPtr, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")]
        public void TypeInfo_AbstractionTest(string type) => TypeInfo_AbstractionTest(Type.GetType(type, throwOnError: true));

        [Test]
        public void TypeInfo_AbstractionTest([Values(
            typeof(void), 
            typeof(object), 
            typeof(int),
            typeof(int[]), 
            typeof(int[,]),
            typeof((int Int, string String)), 
            typeof(int*),
            typeof(nint),
            typeof(nint[]),
            typeof(DateTime),
            typeof(List<>),
            typeof(Span<int>),
            typeof(ReadOnlySpan<int>),
            typeof(List<object>), 
            typeof(NestedGeneric<>), 
            typeof(NestedGeneric<List<string>>), 
            typeof(InterfaceInterceptor<>),
            typeof(Generators.ProxyGenerator<,>), 
            typeof(System.ComponentModel.Component), // van esemenye
            typeof(DuckBase<>), 
            typeof(HasInternal))] Type type) 
        {
            Assembly[] refs = type
                .Assembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Append(type.Assembly)
                .Distinct()
                .ToArray();

            Compilation compilation = CreateCompilation(string.Empty, refs);

            ITypeInfo
                type1 = MetadataTypeInfo.CreateFrom(type),
                type2 = SymbolTypeInfo.CreateFrom(type1.ToSymbol(compilation), compilation);

            AssertEqualsT(type1, type2);

            void AssertEqualsT(ITypeInfo t1, ITypeInfo t2) 
            {
                if (t1 == null || t2 == null)
                {
                    Assert.AreSame(t1, t2);
                    return;
                }

                Assert.AreEqual(t1.Name, t2.Name);
                Assert.AreEqual(t1.QualifiedName, t2.QualifiedName);
                Assert.AreEqual(t1.AssemblyQualifiedName, t2.AssemblyQualifiedName);
                Assert.AreEqual(t1.IsNested, t2.IsNested);
                Assert.AreEqual(t1.AccessModifiers, t2.AccessModifiers);
                Assert.AreEqual(t1.IsClass, t2.IsClass);
                Assert.AreEqual(t1.IsAbstract, t2.IsAbstract);
                Assert.AreEqual(t1.IsFinal, t2.IsFinal);
                Assert.AreEqual(t1.IsInterface, t2.IsInterface);
                Assert.AreEqual(t1.RefType, t2.RefType);
                Assert.AreEqual(t1.IsGenericParameter, t2.IsGenericParameter);
                Assert.AreEqual(t1.IsVoid, t2.IsVoid);

                if (!FProcessedTypes.Add(t1.Name) || t1.DeclaringAssembly.Name.Contains("CodeAnalysis"))
                    return;

                AssertEqualsA(t1.DeclaringAssembly, t2.DeclaringAssembly);
                AssertEqualsT(t1.ElementType, t2.ElementType);
                AssertEqualsT(t1.EnclosingType, t2.EnclosingType);
                AssertEqualsT(t1.BaseType, t2.BaseType);
                AssertEqualsN(t1.ContainingMember, t2.ContainingMember);
                AssertSequenceEqualsT(t1.Interfaces.OrderBy(i => i.Name).ToArray(), t2.Interfaces.OrderBy(i => i.Name).ToArray());
                AssertSequenceEqualsM(OrderCtors(t1).ToArray(), OrderCtors(t2).ToArray());
                AssertSequenceEqualsM(OrderMethods(t1).ToArray(), OrderMethods(t2).ToArray());
                AssertSequenceEqualsPr(OrderProperties(t1).ToArray(), OrderProperties(t2).ToArray());
                AssertSequenceEqualsE(t1.Events.OrderBy(e => e.Name).ToArray(), t2.Events.OrderBy(e => e.Name).ToArray());

                IGenericTypeInfo
                    gt1 = t1 as IGenericTypeInfo,
                    gt2 = t2 as IGenericTypeInfo;

                Assert.AreEqual(gt1 != null, gt2 != null);
                if (gt1 != null)
                {
                    Assert.AreEqual(gt1.IsGenericDefinition, gt2.IsGenericDefinition);
                    Assert.AreEqual(gt1.IsGenericParameter, gt2.IsGenericParameter);

                    AssertSequenceEqualsT(gt1.GenericArguments, gt2.GenericArguments);
                }

                IEnumerable<IMethodInfo> OrderCtors(ITypeInfo t) => t
                    .Constructors
                    .OrderBy(m => m.AccessModifiers)
                    .ThenBy(m => string.Join(string.Empty, m.Parameters.Select(p => p.Type.Name)));

                IEnumerable<IMethodInfo> OrderMethods(ITypeInfo t) => t
                    .Methods
                    .OrderBy(m => m.Name)
                    .ThenBy(m => m.AccessModifiers)
                    .ThenBy(m => m.IsStatic)     
                    .ThenBy(m => (m as IGenericMethodInfo)?.GenericArguments.Count ?? 0)
                    .ThenBy(m => string.Join("_", m.Parameters.Select(p => $"{p.Type.Name}_{p.Type.RefType}")))
                    .ThenBy(m => m.ReturnValue.Type.Name);

                IEnumerable<IPropertyInfo> OrderProperties(ITypeInfo t) => t
                    .Properties
                    .OrderBy(p => p.Name)
                    .ThenBy(p => p.DeclaringType.Name);
            }

            void AssertEqualsA(IAssemblyInfo a1, IAssemblyInfo a2) 
            {
                Assert.AreEqual(a1.Name, a2.Name);
                Assert.AreEqual(a1.Location, a2.Location);
                Assert.AreEqual(a1.IsDynamic, a2.IsDynamic);
            }

            void AssertEqualsN(IHasName hn1, IHasName hn2)
            {
                if (hn1 is null)
                    Assert.IsNull(hn2);

                else if (hn1 is ITypeInfo t1)
                {
                    var t2 = hn2 as ITypeInfo;

                    Assert.NotNull(t2);
                    AssertEqualsT(t1, t2);
                }

                else if (hn1 is IMethodInfo m1)
                {
                    var m2 = hn2 as IMethodInfo;

                    Assert.NotNull(m2);
                    AssertEqualsM(m1, m2);
                }

                else Assert.Fail();
            }

            void AssertEqualsMP(IParameterInfo p1, IParameterInfo p2)
            {
                if (p1 == null || p2 == null)
                {
                    Assert.AreSame(p1, p2);
                    return;
                }

                // Assert.AreEqual(p1.Name, p2.Name); // Roslyn szereti atnevezni a parametereket nem tudom miert
                Assert.AreEqual(p1.Kind, p2.Kind);
                AssertEqualsT(p1.Type, p2.Type);
            }

            void AssertEqualsM(IMethodInfo m1, IMethodInfo m2)
            {
                if (m1 == null || m2 == null)
                {
                    Assert.AreSame(m1, m2);
                    return;
                }

                Assert.AreEqual(m1.Name, m2.Name);
                Assert.AreEqual(m1.IsSpecial, m2.IsSpecial);
                Assert.AreEqual(m1.IsStatic, m2.IsStatic);
                Assert.AreEqual(m1.IsAbstract, m2.IsAbstract);
                Assert.AreEqual(m1.AccessModifiers, m2.AccessModifiers);

                AssertEqualsT(m1.DeclaringType, m2.DeclaringType);
                AssertSequenceEqualsT(m1.DeclaringInterfaces.OrderBy(i => i.Name).ToArray(), m2.DeclaringInterfaces.OrderBy(i => i.Name).ToArray());
                AssertSequenceEqualsP(m1.Parameters, m2.Parameters);
                AssertEqualsMP(m1.ReturnValue, m2.ReturnValue);

                IGenericMethodInfo
                    gm1 = m1 as IGenericMethodInfo,
                    gm2 = m2 as IGenericMethodInfo;

                Assert.AreEqual(gm1 != null, gm2 != null);
                if (gm1 != null)
                    AssertSequenceEqualsT(gm1.GenericArguments, gm2.GenericArguments);
            }

            void AssertEqualsPr(IPropertyInfo p1, IPropertyInfo p2) 
            {
                if (p1 == null || p2 == null)
                {
                    Assert.AreSame(p1, p2);
                    return;
                }

                Assert.AreEqual(p1.Name, p2.Name);
                Assert.AreEqual(p1.IsStatic, p2.IsStatic);
                Assert.AreEqual(p1.IsAbstract, p2.IsAbstract);

                AssertEqualsT(p1.Type, p2.Type);
                AssertEqualsT(p1.DeclaringType, p2.DeclaringType);
                AssertSequenceEqualsP(p1.Indices, p2.Indices);
                AssertEqualsM(p1.GetMethod, p2.GetMethod);
                AssertEqualsM(p1.SetMethod, p2.SetMethod);
            }

            void AssertEqualsE(IEventInfo e1, IEventInfo e2)
            {
                if (e1 == null || e2 == null)
                {
                    Assert.AreSame(e1, e2);
                    return;
                }

                Assert.AreEqual(e1.Name, e2.Name);
                Assert.AreEqual(e1.IsStatic, e2.IsStatic);
                Assert.AreEqual(e1.IsAbstract, e2.IsAbstract);

                AssertEqualsT(e1.Type, e2.Type);
                AssertEqualsT(e1.DeclaringType, e2.DeclaringType);
                AssertEqualsM(e1.AddMethod, e2.AddMethod);
                AssertEqualsM(e1.RemoveMethod, e2.RemoveMethod);
            }

            void AssertSequenceEqualsT(IReadOnlyList<ITypeInfo> l1, IReadOnlyList<ITypeInfo> l2) 
            {
                Assert.AreEqual(l1.Count, l2.Count);

                for (int i = 0; i < l1.Count; i++)
                {
                    AssertEqualsT(l1[i], l2[i]);
                }
            }

            void AssertSequenceEqualsP(IReadOnlyList<IParameterInfo> l1, IReadOnlyList<IParameterInfo> l2)
            {
                Assert.AreEqual(l1.Count, l2.Count);

                for (int i = 0; i < l1.Count; i++)
                {
                    AssertEqualsMP(l1[i], l2[i]);
                }
            }

            void AssertSequenceEqualsM(IReadOnlyList<IMethodInfo> l1, IReadOnlyList<IMethodInfo> l2)
            {
                Assert.AreEqual(l1.Count, l2.Count);

                for (int i = 0; i < l1.Count; i++)
                {
                    AssertEqualsM(l1[i], l2[i]);
                }
            }

            void AssertSequenceEqualsPr(IReadOnlyList<IPropertyInfo> l1, IReadOnlyList<IPropertyInfo> l2)
            {
                Assert.AreEqual(l1.Count, l2.Count);

                for (int i = 0; i < l1.Count; i++)
                {
                    AssertEqualsPr(l1[i], l2[i]);
                }
            }

            void AssertSequenceEqualsE(IReadOnlyList<IEventInfo> l1, IReadOnlyList<IEventInfo> l2)
            {
                Assert.AreEqual(l1.Count, l2.Count);

                for (int i = 0; i < l1.Count; i++)
                {
                    AssertEqualsE(l1[i], l2[i]);
                }
            }
        }

        public static IEnumerable<Type> SystemTypes 
        {
            get => typeof(object).Assembly.GetTypes().Where(t => t.IsPublic);
        }

        [TestCaseSource(nameof(SystemTypes)), Parallelizable]
        public void TypeInfo_AbstractionTestAgainstSystemType(Type t)
        {
            TypeInfo_AbstractionTest(t);
        }

        public class NestedGeneric<T>
        {
            public class Nested<TT> { }
            public class Nested { }
        }

        public class HasInternal 
        {
            internal void Foo() { }
        }

        public static IEnumerable<object> Generics
        {
            get
            {
                yield return MetadataTypeInfo.CreateFrom(typeof(List<>));

                Compilation compilation = CreateCompilation(string.Empty, typeof(List<>).Assembly);
                yield return SymbolTypeInfo.CreateFrom(compilation.GetTypeByMetadataName(typeof(List<>).FullName), compilation);
                
            }
        }

        [TestCaseSource(nameof(Generics))]
        public void GenericTypeInfo_CanBeSpecialized(object t)
        {
            IGenericTypeInfo type = (IGenericTypeInfo) t;

            Assert.DoesNotThrow(() => type = type.Close(MetadataTypeInfo.CreateFrom(typeof(string))));
            Assert.That(type.QualifiedName, Is.EqualTo("System.Collections.Generic.List`1"));
            Assert.That(type.GenericArguments.Single().QualifiedName, Is.EqualTo("System.String"));
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

            ITypeSymbol resolved = MetadataTypeInfo.CreateFrom(data.Type).ToSymbol(compilation);

            Assert.That(resolved.ToString(), Is.EqualTo(data.Name));
        }

        [Test]
        public void TypeInfoToSymbol_ShouldThrowIfTheTypeNotFound() 
        {
            Compilation compilation = CreateCompilation(string.Empty);
            Assert.Throws<TypeLoadException>(() => MetadataTypeInfo.CreateFrom(typeof(NestedGeneric<>)).ToSymbol(compilation));
        }

        [TestCaseSource(nameof(Types))]
        public void TypeInfoToMetadata_ShouldReturnTheDesiredMetadata((Type Type, string _) data)
        {
            Type resolved = MetadataTypeInfo.CreateFrom(data.Type).ToMetadata();
            Assert.That(resolved, Is.EqualTo(data.Type));
        }
    }
}
