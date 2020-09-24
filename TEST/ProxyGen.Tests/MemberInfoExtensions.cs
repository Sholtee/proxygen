/********************************************************************************
* MemberInfoExtensions.cs                                                       *
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
    public class MemberInfoExtensionsTests
    {
        public static (MemberInfo Member, string Expected)[] Members = new[]
        {
            ((MemberInfo) typeof(MemberInfoExtensionsTests).GetMethod(nameof(GetFullName_ShouldDoWhatTheNameSuggests)), "Solti.Utils.Proxy.Internals.Tests.MemberInfoExtensionsTests.GetFullName_ShouldDoWhatTheNameSuggests"),
            ((MemberInfo) typeof(List<>).GetProperty(nameof(List<object>.Count)), "System.Collections.Generic.List<T>.Count"),
            ((MemberInfo) typeof(List<object>).GetProperty(nameof(List<object>.Count)), "System.Collections.Generic.List<System.Object>.Count")
        };

        [TestCaseSource(nameof(Members))]
        public void GetFullName_ShouldDoWhatTheNameSuggests((MemberInfo Member, string Expected) param) =>
            Assert.That(param.Member.GetFullName(), Is.EqualTo(param.Expected));

        public static (MemberInfo Member, bool IsStatic)[] StaticNonStatic = new[]
        {
            ((MemberInfo) typeof(AppDomain).GetProperty(nameof(AppDomain.CurrentDomain), BindingFlags.Static | BindingFlags.Public), true),
            ((MemberInfo) typeof(AppDomain).GetMethod(nameof(AppDomain.GetCurrentThreadId), BindingFlags.Static | BindingFlags.Public), true),
            ((MemberInfo) typeof(MemberInfoExtensionsTests).GetMethod(nameof(GetFullName_ShouldDoWhatTheNameSuggests)), false),
            ((MemberInfo) typeof(List<>).GetProperty(nameof(List<object>.Count)), false),
            ((MemberInfo) typeof(List<object>).GetProperty(nameof(List<object>.Count)), false)
        };

        [TestCaseSource(nameof(StaticNonStatic))]
        public void IsStatic_ShouldDoWhatTheNameSuggests((MemberInfo Member, bool IsStatic) param)
            => Assert.That(param.Member.IsStatic(), Is.EqualTo(param.IsStatic));

        private interface IFoo
        {
            int Method(ref string x);
            string Prop { get; set; }
        }

        private class SelectAttirubte: Attribute{}

        private class Foo : IFoo
        {
            [SelectAttirubte]
            string IFoo.Prop { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            [SelectAttirubte]
            int IFoo.Method(ref string x) => throw new NotImplementedException();
            [SelectAttirubte]
            public string Prop { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            [SelectAttirubte]
            public int Method(ref string x) => throw new NotImplementedException();
        }

        public static (MemberInfo, MemberInfo)[] FooMembers = new[] 
        {
            GetFooMemberPair<MethodInfo>(),
            GetFooMemberPair<PropertyInfo>()
        };

        private static (MemberInfo, MemberInfo) GetFooMemberPair<TMember>() where TMember : MemberInfo 
        {
            TMember[] members = typeof(Foo)
                .ListMembers<TMember>(includeNonPublic: true)
                .Where(member => member.GetCustomAttribute<SelectAttirubte>() != null)
                .ToArray();

            return (members[0], members[1]);
        }

        [TestCaseSource(nameof(FooMembers))]
        public void SignatureEquals_ShouldSupportExlicitImplementations((MemberInfo, MemberInfo) pair) =>
            Assert.That(pair.Item1.SignatureEquals(pair.Item2));
    }
}
