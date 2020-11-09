using System;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Tests
{
    using Internals;

    [TestFixture]
    public class InterfaceInterceptorTests
    {
        private interface IBase 
        {
            int Prop { get; set; }
            event Action<EventArgs> Evt;
        }

        private interface IDescendant : IBase
        {
            void Foo();
        }

        [Test]
        public void ResolveMember_ShouldResolveTheMemberByToken() 
        {
            PropertyInfo prop = typeof(IDescendant).ListMembers<PropertyInfo>().Single(prop => prop.Name == nameof(IDescendant.Prop)); // GetProperty() nem mukodne mert az osbol kerdezzuk le
            Assert.AreSame(prop, InterfaceInterceptor<IDescendant>.ResolveMember(prop.MetadataToken));
        }
    }
}
