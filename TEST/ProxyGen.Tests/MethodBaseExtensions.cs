/********************************************************************************
* MethodBaseExtensions.cs                                                       *
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
    public class MethodBaseExtensionsTests
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
        [TestCase("Solti.Utils.Proxy.Internals.Tests.MethodBaseExtensionsTests.IInterface.Explicit", AccessModifiers.Explicit)]
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
    }
}
