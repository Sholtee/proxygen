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
    [TestFixture]
    public class MethodBaseExtensionsTests
    {
        public static (MethodInfo Method, Type DeclaringType)[] Methods = new []
        {
            ((MethodInfo) MemberInfoExtensions.ExtractFrom<Dictionary<string, object>>(d => d.Add(default, default)), typeof(IDictionary<string, object>)),
            ((MethodInfo) MemberInfoExtensions.ExtractFrom<MyDictionary<string, object>>(d => d.Add(default, default)), typeof(IDictionary<string, object>)), // leszarmazott
            ((MethodInfo) MemberInfoExtensions.ExtractFrom<Dictionary<string, object>>(d => d.GetHashCode()), typeof(object)),
        };

        [TestCaseSource(nameof(Methods))]
        public void GetDeclaringType_ShouldDoWhatTheItsNameSays((MethodInfo Method, Type DeclaringType) data) =>
            Assert.That(data.Method.GetDeclaringType(), Is.EqualTo(data.DeclaringType));

        private class MyDictionary<TKey, TValue> : Dictionary<TKey, TValue> { }

        [TestCase(nameof(MyClass.Public), AccessModifiers.Public)]
        [TestCase("Protected", AccessModifiers.Protected)]
        [TestCase(nameof(MyClass.ProtectedInternal), AccessModifiers.Protected | AccessModifiers.Internal)]
        [TestCase(nameof(MyClass.Internal), AccessModifiers.Internal)]
        [TestCase("Solti.Utils.Proxy.Internals.Tests.MethodBaseExtensionsTests.IInterface.Explicit", AccessModifiers.Explicit)]
        [TestCase("Private", AccessModifiers.Private)]
        public void GetAccessModifiers_ShouldDoWhatTheItsNameSays(string name, int am) =>
            Assert.That(typeof(MyClass).ListMembers<MethodInfo>(includeNonPublic: true).Single(m => m.Name == name).GetAccessModifiers(), Is.EqualTo((AccessModifiers) am));

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
            private void Private() { }
        }
    }
}
