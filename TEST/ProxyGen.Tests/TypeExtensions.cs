/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    using Primitives.Patterns;
    using Proxy.Tests.External;

    [TestFixture]
    public sealed class TypeExtensionsTests
    {
        [Test]
        public void ListMembers_ShouldReturnOverLoadedInterfaceMembers() 
        {
            MethodInfo[] methods = typeof(IEnumerable<string>).ListMembers<MethodInfo>().ToArray();
            Assert.That(methods.Length, Is.EqualTo(2));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IEnumerable<string>>(i => i.GetEnumerator())));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IEnumerable>(i => i.GetEnumerator())));

            PropertyInfo[] properties = typeof(IEnumerator<string>).ListMembers<PropertyInfo>().ToArray();
            Assert.That(properties.Length, Is.EqualTo(2));
            Assert.That(properties.Contains((PropertyInfo) MemberInfoExtensions.ExtractFrom<IEnumerator<string>>(i => i.Current)));
            Assert.That(properties.Contains((PropertyInfo) MemberInfoExtensions.ExtractFrom<IEnumerator>(i => i.Current)));
        }

        [Test]
        public void ListMembers_ShouldReturnExplicitImplementations() 
        {
            MethodInfo[] methods = typeof(List<int>).ListMembers<MethodInfo>(includeNonPublic: true).ToArray();

            Assert.That(methods.Length, Is.EqualTo(methods.Distinct().Count()));

            // Enumerator, IEnumerator, IEnumerator<int>
            Assert.That(methods.Where(m => m.Name.Contains(nameof(IEnumerable.GetEnumerator))).Count(), Is.EqualTo(3));

            PropertyInfo[] properties = typeof(List<int>).ListMembers<PropertyInfo>(includeNonPublic: true).ToArray();

            Assert.That(properties.Length, Is.EqualTo(properties.Distinct().Count()));

            // ICollection<T>.IsReadOnly, IList-ReadOnly
            Assert.That(properties.Where(prop => prop.Name.Contains(nameof(IList.IsReadOnly))).Count(), Is.EqualTo(2));
        }

        public interface IInterface 
        {
            void Bar();
        }

        public class NoughtyClass : IInterface
        {
            void IInterface.Bar(){}
            public void Bar() { }
        }

        [Test]
        public void ListMembers_ShouldReturnExplicitImplementations2() 
        {
            MethodInfo[] methods = typeof(NoughtyClass)
                .ListMembers<MethodInfo>(includeNonPublic: true)
                .Where(m => m.Name.Contains(nameof(IInterface.Bar)))
                .ToArray();

            Assert.That(methods.Count, Is.EqualTo(2));
        }

        [Test]
        public void ListMembers_ShouldReturnMembersFromTheWholeHierarchy()
        {
            MethodInfo[] methods = typeof(IGrandChild).ListMembers<MethodInfo>().ToArray();

            Assert.That(methods.Length, Is.EqualTo(3));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IParent>(i => i.Foo())));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IChild>(i => i.Bar())));
            Assert.That(methods.Contains((MethodInfo) MemberInfoExtensions.ExtractFrom<IGrandChild>(i => i.Baz())));
        }

        [TestCase(true, 3)]
        [TestCase(false, 1)]
        public void ListMembers_ShouldReturnNonPublicMembersIfNecessary(bool includeNonPublic, int expectedLength) 
        {
            PropertyInfo[] properties = typeof(Class).ListMembers<PropertyInfo>(includeNonPublic).ToArray();

            Assert.That(properties.Length, Is.EqualTo(expectedLength));
        }

        private class Class 
        {
            public class Nested { }

            public int PublicProperty { get; }
            internal int InternalProperty { get; }
            private int PrivateProperty { get; }

            private readonly int[] ar = new[] { 1 }; 

            public ref int RefMethod() => ref ar[0];
        }

        private interface IParent 
        {
            void Foo();
        }

        private interface IChild : IParent 
        {
            void Bar();
        }

        private interface IGrandChild : IChild 
        {
            void Baz();
        }

        [TestCase(typeof(object), "System.Object")]
        [TestCase(typeof(List<>), "System.Collections.Generic.List")] // generic
        [TestCase(typeof(IParent), "IParent")] // nested
        public void GetFriendlyName_ShouldBeautifyTheTypeName(Type type, string expected) => Assert.AreEqual(expected, type.GetFriendlyName());

        [Test]
        public void GetFriendlyName_ShouldHandleRefTypes()
        {
            Type refType = typeof(Class).GetMethod(nameof(Class.RefMethod)).ReturnType; // System.Int32&

            Assert.AreEqual("System.Int32", refType.GetFriendlyName());
        }

        private class MyInterceptor : InterfaceInterceptor<IDbConnection> 
        {
            public MyInterceptor() : base(null) { }
        }

        public static IEnumerable<(Type, IEnumerable<Assembly>)> TypeReferences 
        {
            get 
            {
                yield return (typeof(InterfaceInterceptor<>), new[] { typeof(InterfaceInterceptor<>).Assembly, typeof(object).Assembly });
                yield return (typeof(InterfaceInterceptor<IInterface>), new[] { typeof(TypeExtensionsTests).Assembly, typeof(InterfaceInterceptor<>).Assembly, typeof(object).Assembly });
                yield return (typeof(ExternalInterceptor<IInterface>), new[] { typeof(TypeExtensionsTests).Assembly, typeof(ExternalInterceptor<>).Assembly, typeof(object).Assembly, typeof(InterfaceInterceptor<>).Assembly });
                yield return (typeof(MyInterceptor), new[] { typeof(TypeExtensionsTests).Assembly, typeof(InterfaceInterceptor<>).Assembly, typeof(object).Assembly, typeof(IDbConnection).Assembly });
                yield return (typeof(Disposable), new[] { typeof(Disposable).Assembly, typeof(object).Assembly });
                yield return (typeof(IList), new[] { typeof(IList).Assembly });
                yield return (typeof(IDisposableEx), new[] { typeof(IDisposableEx).Assembly, typeof(IDisposable).Assembly
#if NETCOREAPP2_2
                    , typeof(IAsyncDisposable).Assembly
#endif
                });
                yield return (typeof(IList<>), new[] { typeof(IList<>).Assembly });
                yield return (typeof(IList<int>), new[] { typeof(IList<>).Assembly });
                yield return (typeof(IList<IDisposableEx>), new[] { typeof(IList<>).Assembly, typeof(IDisposableEx).Assembly 
#if NETCOREAPP2_2
                    , typeof(IAsyncDisposable).Assembly
#endif           
                });
            }
        }

        [TestCaseSource(nameof(TypeReferences))]
        public void GetBasicReferences_ShouldReturnTheDeclaringAssemblyOfTheSourceTypeAndItsAncestors((Type Type, IEnumerable<Assembly> References) data) 
        {
            IEnumerable<Assembly>
                refs = data.References.OrderBy(x => x.FullName),
                returned = data.Type.GetBasicReferences().OrderBy(x => x.FullName);

            Assert.That(returned.SequenceEqual(refs));
        }

        private class DummyClass_1 
        {
            public DummyClass_1(IDisposable p) { }
            public List<int> Method(IDbConnection conn) => null;
            public IComponent Prop { get; }
        }

        public interface IInterface_1
        {
        }

        public interface IInterface_2
        {
            IInterface_1 Interface1 { get; }
        }

        public interface IInterface_3<T>
        {
            IInterface_1 Interface1 { get; }
        }

        public class DummyClass_2 : InterfaceInterceptor<IInterface_2>
        {
            public DummyClass_2(IInterface_3<int> dependency, IInterface_2 target) : base(target)
            {
                Dependency = dependency;
            }

            public IInterface_3<int> Dependency { get; }
        }

        public static IEnumerable<(Type, IEnumerable<Assembly>)> TypeReferences2
        {
            get
            {
                yield return (typeof(DummyClass_1), new[] { typeof(DummyClass_1).Assembly, typeof(IDisposable).Assembly, typeof(IDbConnection).Assembly, typeof(IComponent).Assembly });
                yield return (typeof(DummyClass_2), new[] { typeof(DummyClass_2).Assembly, typeof(InterfaceInterceptor<>).Assembly, typeof(int).Assembly});
                yield return (typeof(IList<IDbConnection>), new[] { typeof(IList<>).Assembly, typeof(IDbConnection).Assembly });
                yield return (typeof(ExternalInterceptor<>), new[] { typeof(ExternalInterceptor<>).Assembly, typeof(InterfaceInterceptor<>).Assembly, typeof(object).Assembly, typeof(IDbConnection).Assembly });
            }
        }

        [TestCaseSource(nameof(TypeReferences2))]
        public void GetReferences_ShouldReturnTheDeclaringAssemblyOfAllTheMemberParameters((Type Type, IEnumerable<Assembly> References) data) 
        {
            IEnumerable<Assembly>
                refs = data.References.OrderBy(x => x.FullName),
                returned = data.Type.GetReferences().OrderBy(x => x.FullName);

            Assert.That(returned.SequenceEqual(refs));
        }

        private class SelfReferencingClass 
        {
            public SelfReferencingClass SelfReference { get; }
        }

        private interface ISelfReferencingIface : IComposite<ISelfReferencingIface> { }

        public static IEnumerable<(Type, IEnumerable<Assembly>)> TypeReferences3
        {
            get
            {
                yield return (typeof(SelfReferencingClass), new[] { typeof(SelfReferencingClass).Assembly, typeof(Object).Assembly });
                yield return (typeof(ISelfReferencingIface), new[] 
                { 
                    typeof(IComposite<>).Assembly, 
                    typeof(ISelfReferencingIface).Assembly, 
                    typeof(Object).Assembly 
#if NETCOREAPP2_2
                    , typeof(IAsyncDisposable).Assembly
#endif    
                });
            }
        }

        [TestCaseSource(nameof(TypeReferences3))]
        public void GetReferences_ShouldHandleSelfReferences((Type Type, IEnumerable<Assembly> References) data) => Assert.That(data.Type.GetReferences().OrderBy(x => x.FullName).SequenceEqual(data.References.OrderBy(x => x.FullName)));

        [Test]
        public void GetEnclosingTypes_ShouldReturnTheParentTypes() =>
            Assert.That(typeof(Class.Nested).GetEnclosingTypes().SequenceEqual(new[] { typeof(TypeExtensionsTests), typeof(Class) }));

        [Test]
        public void GetOwnGenericArguments_ShouldDoWhatTheNameSuggests() 
        {
            Type child = typeof(GenericParent<>.GenericChild<>);

            Assert.That(child.GetGenericArguments().Length, Is.EqualTo(2));
            Assert.That(child.GetOwnGenericArguments().SequenceEqual(child.GetGenericArguments().Skip(1)));
        }

        private class GenericParent<T>
        {
            public class GenericChild<TT>
            {
            }
        }
    }
}
