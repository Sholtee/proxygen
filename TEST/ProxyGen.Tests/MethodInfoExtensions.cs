/********************************************************************************
* MethodInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    using Primitives;

    [TestFixture]
    public class MethodInfoExtensionsTests
    {
        public static (MethodInfo Method, IEnumerable<Type> DeclaringTypes)[] Methods = new []
        {
            (MethodInfoExtractor.Extract<Dictionary<string, object>>(d => d.Add(default, default)), (IEnumerable<Type>) new[] {typeof(IDictionary<string, object>) }),
            (MethodInfoExtractor.Extract<MyDictionary<string, object>>(d => d.Add(default, default)), new[] {typeof(IDictionary<string, object>) }), // leszarmazott
            (MethodInfoExtractor.Extract< Dictionary<string, object>>(d => d.GetHashCode()), Array.Empty<Type>()),
        };

        [TestCaseSource(nameof(Methods))]
        public void GetDeclaringInterfaces_ShouldDoWhatItsNameSays((MethodInfo Method, IEnumerable<Type> DeclaringTypes) data) =>
            Assert.That(data.Method.GetDeclaringInterfaces().SequenceEqual(data.DeclaringTypes));

        private class MyDictionary<TKey, TValue> : Dictionary<TKey, TValue> { }

        [TestCase(nameof(MyClass.Public), AccessModifiers.Public)]
        [TestCase("Protected", AccessModifiers.Protected)]
        [TestCase(nameof(MyClass.ProtectedInternal), AccessModifiers.Protected | AccessModifiers.Internal)]
        [TestCase(nameof(MyClass.Internal), AccessModifiers.Internal)]
        [TestCase("Solti.Utils.Proxy.Internals.Tests.MethodInfoExtensionsTests.IInterface.Explicit", AccessModifiers.Explicit)]
        public void GetAccessModifiers_ShouldDoWhatTheItsNameSays(string name, int am) =>
            Assert.That(typeof(MyClass).ListMethods().Single(m => m.Name == name).GetAccessModifiers(), Is.EqualTo((AccessModifiers) am));

        private interface IInterface 
        {
            void Explicit();
        }

        private class MyClass: IInterface
        {
            public void Public() { }
            protected void Protected() { }
            protected internal void ProtectedInternal() { }
            internal void Internal() { }
            void IInterface.Explicit() { }
        }

        private class BaseClass1
        {
            public override string ToString()
            {
                return "Base";
            }
        }

        private class DerivedClass1 : BaseClass1
        {
            public override string ToString()
            {
                return "Derived";
            }
        }

        private class DerivedClass2 : BaseClass2
        {
        }

        private class BaseClass2
        {
            public virtual string GenericMethod<T>(T x)
            {
                return "Base";
            }
        }

        private class DerivedClass3 : BaseClass2
        {
            public override string GenericMethod<T>(T x)
            {
                return "Derived";
            }
        }

        private class BaseClass3<T>
        {
            public virtual string GenericMethod(T x)
            {
                return "Base";
            }
        }

        private class DerivedClass4<T> : BaseClass3<T>
        {
            public override string GenericMethod(T x)
            {
                return "Derived";
            }
        }

        public static IEnumerable<object[]> GetOverriddenMethod_ShouldReturnTheImmediateBaseDeclaration_Params
        {
            get
            {
                yield return [typeof(DerivedClass1).GetMethod(nameof(DerivedClass1.ToString)), typeof(BaseClass1).GetMethod(nameof(BaseClass1.ToString))];
                yield return [typeof(DerivedClass2).GetMethod(nameof(DerivedClass2.ToString)), typeof(object).GetMethod(nameof(object.ToString))];
                yield return [typeof(DerivedClass3).GetMethod(nameof(DerivedClass3.GenericMethod)), typeof(BaseClass2).GetMethod(nameof(BaseClass2.GenericMethod))];
                yield return [typeof(DerivedClass4<>).GetMethod(nameof(DerivedClass4<int>.GenericMethod)), typeof(DerivedClass4<>).GetMethod(nameof(DerivedClass4<int>.GenericMethod)).GetBaseDefinition()];
            }
        }

        [TestCaseSource(nameof(GetOverriddenMethod_ShouldReturnTheImmediateBaseDeclaration_Params))]
        public void GetOverriddenMethod_ShouldReturnTheImmediateBaseDeclaration(MethodInfo original, MethodInfo expected) => 
            Assert.That(original.GetOverriddenMethod(), Is.EqualTo(expected));

        public static IEnumerable<MethodInfo> GetOverriddenMethod_AgainstSystemTypes_Params
        {
            get
            {
                HashSet<MethodInfo> returned = [];

                foreach (Type t in typeof(object).Assembly.GetTypes().Where(t => t.IsClass))
                {
                    foreach (MethodInfo method in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        MethodInfo @base = method.GetBaseDefinition();
                        if (@base is not null && @base != method && returned.Add(@base))
                            yield return method;
                    }
                }
            }
        }

        [TestCaseSource(nameof(GetOverriddenMethod_AgainstSystemTypes_Params))]
        public void GetOverriddenMethod_AgainstSystemTypes(MethodInfo @override) =>
            Assert.That(@override.GetOverriddenMethod(), Is.Not.Null);
    }
}
