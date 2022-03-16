/********************************************************************************
* Delegate.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public class DelegateTests
    {
        private static Func<int> GetLambda(IList<int> lst) => () => lst[0];

        [Test]
        public void UnderlyingMethodOfDelegate_ShouldBeIndependentFromTheEnclosedVariables()
        {
            Assert.AreSame(GetLambda(new List<int>()).Method, GetLambda(null).Method);
            Assert.AreSame(GetLambda(new List<int>()).Method, GetLambda(new List<int>()).Method);
        }
    }
}
