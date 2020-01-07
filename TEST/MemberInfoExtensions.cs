/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public class MemberInfoExtensionsTests
    {
        [Test]
        public void GetFullName_ShouldDoWhatTheNameSuggests() 
        {
            Assert.That(GetType().GetMethod(nameof(GetFullName_ShouldDoWhatTheNameSuggests)).GetFullName(), Is.EqualTo("Solti.Utils.Proxy.Internals.Tests.MemberInfoExtensionsTests.GetFullName_ShouldDoWhatTheNameSuggests"));
            Assert.That(typeof(List<>).GetProperty(nameof(List<object>.Count)).GetFullName(), Is.EqualTo("System.Collections.Generic.List<T>.Count"));
            Assert.That(typeof(List<object>).GetProperty(nameof(List<object>.Count)).GetFullName(), Is.EqualTo("System.Collections.Generic.List<System.Object>.Count"));
        }
    }
}
