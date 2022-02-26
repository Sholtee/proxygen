﻿/********************************************************************************
* ProxyActivator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Internals.Tests
{
    [TestFixture]
    public class ProxyActivatorTests
    {
        [Test]
        public void Create_ShouldBuildAFactory() =>
            Assert.DoesNotThrow(() => ProxyActivator.Create(typeof(List<string>)));


        public static IEnumerable<ITuple> Paramz
        {
            get
            {
                yield return null;
                yield return Tuple.Create(1);
                yield return Tuple.Create((IEnumerable<string>) new string[] { "cica" });
            }
        }

        [Test]
        public void Factory_ShouldCreateANewInstance([ValueSource(nameof(Paramz))] ITuple paramz)
        {
            List<string> instance = (List<string>) ProxyActivator.Create(typeof(List<string>)).Invoke(paramz);
            Assert.IsNotNull(instance);
        }

        public static IEnumerable<ITuple> BadParamz
        {
            get
            {
                yield return Tuple.Create("cica");
            }
        }

        [Test]
        public void Factory_ShouldThrowOnInvalidParam([ValueSource(nameof(BadParamz))] ITuple paramz) =>
            Assert.Throws<MissingMemberException>(() => ProxyActivator.Create(typeof(List<string>)).Invoke(paramz));
    }
}