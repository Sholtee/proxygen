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
        public delegate void TestDelegate<in T>(object sender, T eventArg);

        internal interface IFoo<T> // direkt internal
        {
            int Foo<TT>(int a, out string b, ref TT c);
            void Bar();
            T Prop { get; set; }
            event TestDelegate<T> Event;
        }

        protected static EventInfo Event { get; } = typeof(IFoo<int>).GetEvent(nameof(IFoo<int>.Event));

        protected static PropertyInfo Prop { get; } = typeof(IFoo<int>).GetProperty(nameof(IFoo<int>.Prop));

        protected static MethodInfo Foo { get; } = typeof(IFoo<int>).GetMethod(nameof(IFoo<int>.Foo));

        protected static MethodInfo Bar { get; } = typeof(IFoo<int>).GetMethod(nameof(IFoo<int>.Bar));
    }
}
