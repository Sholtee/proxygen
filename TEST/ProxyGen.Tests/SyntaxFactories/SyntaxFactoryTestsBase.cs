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

        internal interface IFoo<T> // deliberately internal
        {
            int Foo<TT>(int a, out string b, ref TT c);
            void Bar();
            T Prop { get; set; }
            T this[int i] { get; set; }
            event TestDelegate<T> Event;
        }

        internal abstract class Foo<T>
        {
            public virtual T Prop { get; protected set; }
            public abstract T this[int i] { get; protected set; }
            public abstract event TestDelegate<T> Event;
        }

        protected interface IComplex
        {
            void Method();
            int Property { get; }
            event Action Event;
        }

        internal static IEventInfo InterfaceEvent { get; } = MetadataEventInfo.CreateFrom(typeof(IFoo<int>).GetEvent(nameof(IFoo<int>.Event)));

        internal static IEventInfo ClassEvent { get; } = MetadataEventInfo.CreateFrom(typeof(Foo<int>).GetEvent(nameof(Foo<int>.Event)));

        internal static IPropertyInfo InterfaceProp { get; } = MetadataPropertyInfo.CreateFrom(typeof(IFoo<int>).GetProperty(nameof(IFoo<int>.Prop)));

        internal static IPropertyInfo ClassProp { get; } = MetadataPropertyInfo.CreateFrom(typeof(Foo<int>).GetProperty(nameof(Foo<int>.Prop)));

        internal static IPropertyInfo InterfaceIndexer { get; } = MetadataPropertyInfo.CreateFrom(typeof(IFoo<int>).GetProperty("Item"));

        internal static IPropertyInfo ClassIndexer { get; } = MetadataPropertyInfo.CreateFrom(typeof(Foo<int>).GetProperty("Item"));

        internal static IMethodInfo FooMethod { get; } = MetadataMethodInfo.CreateFrom(typeof(IFoo<int>).GetMethod(nameof(IFoo<int>.Foo)));

        internal static IMethodInfo BarMethod { get; } = MetadataMethodInfo.CreateFrom(typeof(IFoo<int>).GetMethod(nameof(IFoo<int>.Bar)));
    }
}
