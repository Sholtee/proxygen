/********************************************************************************
* SyntaxFactoryTestsBase.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.SyntaxFactories.Tests
{
    using Internals;

    public class SyntaxFactoryTestsBase
    {
        public delegate void TestDelegate<in T>(object sender, T eventArg);

        internal interface IFoo<T> // direkt internal
        {
            int Foo<TT>(int a, out string b, ref TT c);
            void Bar();
            T Prop { get; set; }
            event TestDelegate<T> Event;
        }

        protected interface IComplex
        {
            void Method();
            int Property { get; }
            event Action Event;
        }

        internal static IEventInfo Event { get; } = MetadataEventInfo.CreateFrom(typeof(IFoo<int>).GetEvent(nameof(IFoo<int>.Event)));

        internal static IPropertyInfo Prop { get; } = MetadataPropertyInfo.CreateFrom(typeof(IFoo<int>).GetProperty(nameof(IFoo<int>.Prop)));

        internal static IMethodInfo Foo { get; } = MetadataMethodInfo.CreateFrom(typeof(IFoo<int>).GetMethod(nameof(IFoo<int>.Foo)));

        internal static IMethodInfo Bar { get; } = MetadataMethodInfo.CreateFrom(typeof(IFoo<int>).GetMethod(nameof(IFoo<int>.Bar)));
    }
}
