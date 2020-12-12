/********************************************************************************
* MetadataEventInfo.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal class MetadataEventInfo : IEventInfo
    {
        private EventInfo UnderLyingEvent { get; }

        public string Name => UnderLyingEvent.StrippedName();

        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= (AddMethod ?? RemoveMethod!).DeclaringType;

        private IMethodInfo? FAddMethod;
        public IMethodInfo AddMethod => FAddMethod ??= MetadataMethodInfo.CreateFrom(UnderLyingEvent.AddMethod);

        private IMethodInfo? FRemoveMethod;
        public IMethodInfo RemoveMethod => FRemoveMethod ??= MetadataMethodInfo.CreateFrom(UnderLyingEvent.RemoveMethod);

        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= MetadataTypeInfo.CreateFrom(UnderLyingEvent.EventHandlerType);

        public bool IsStatic => (AddMethod ?? RemoveMethod!).IsStatic;

        private MetadataEventInfo(EventInfo evt) => UnderLyingEvent = evt;

        public static IEventInfo CreateFrom(EventInfo evt) => new MetadataEventInfo(evt);

        public override bool Equals(object obj) => obj is MetadataEventInfo that && UnderLyingEvent.Equals(that.UnderLyingEvent);

        public override int GetHashCode() => UnderLyingEvent.GetHashCode();

        public override string ToString() => UnderLyingEvent.ToString();
    }
}
