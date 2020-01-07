/********************************************************************************
* ProxySyntaxGeneratorTestsBase.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.Proxy.Internals.Tests
{
    public class ProxySyntaxGeneratorTestsBase
    {
        internal delegate void TestDelegate<in T>(object sender, T eventArg);

        internal interface IFoo<T> // direkt internal
        {
            int Foo<TT>(int a, out string b, ref TT c);
            void Bar();
            T Prop { get; set; }
            event TestDelegate<T> Event;
        }

        protected static EventInfo Event { get; } = typeof(IFoo<int>).GetEvent(nameof(IFoo<int>.Event), BindingFlags.Public | BindingFlags.Instance);

        protected static PropertyInfo Prop { get; } = typeof(IFoo<int>).GetProperty(nameof(IFoo<int>.Prop));
    }
}
