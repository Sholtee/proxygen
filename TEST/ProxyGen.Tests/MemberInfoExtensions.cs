/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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

        public static (object Member, bool IsStatic)[] StaticNonStatic = new[]
        {
            ((object) MetadataPropertyInfo.CreateFrom(typeof(AppDomain).GetProperty(nameof(AppDomain.CurrentDomain), BindingFlags.Static | BindingFlags.Public)), true),
            (MetadataMethodInfo.CreateFrom(typeof(AppDomain).GetMethod(nameof(AppDomain.GetCurrentThreadId), BindingFlags.Static | BindingFlags.Public)), true),
            (MetadataMethodInfo.CreateFrom(typeof(MemberInfoExtensionsTests).GetMethod(nameof(GetFullName_ShouldDoWhatTheNameSuggests))), false),
            (MetadataPropertyInfo.CreateFrom(typeof(List<>).GetProperty(nameof(List<object>.Count))), false),
            (MetadataPropertyInfo.CreateFrom(typeof(List<object>).GetProperty(nameof(List<object>.Count))), false)
        };

        [TestCaseSource(nameof(StaticNonStatic))]
        public void IsStatic_ShouldDoWhatTheNameSuggests((object Member, bool IsStatic) param)
            => Assert.That(((IMemberInfo) param.Member).IsStatic, Is.EqualTo(param.IsStatic));

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
    }
}
