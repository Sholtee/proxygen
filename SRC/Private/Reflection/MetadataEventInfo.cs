/********************************************************************************
* MetadataEventInfo.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class MetadataEventInfo : IEventInfo
    {
        private EventInfo UnderlyingEvent { get; }

        public string Name => UnderlyingEvent.StrippedName();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= (AddMethod ?? RemoveMethod).DeclaringType;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IMethodInfo? FAddMethod;
        public IMethodInfo AddMethod => FAddMethod ??= MetadataMethodInfo.CreateFrom(UnderlyingEvent.AddMethod);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IMethodInfo? FRemoveMethod;
        public IMethodInfo RemoveMethod => FRemoveMethod ??= MetadataMethodInfo.CreateFrom(UnderlyingEvent.RemoveMethod);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= MetadataTypeInfo.CreateFrom(UnderlyingEvent.EventHandlerType);

        public bool IsStatic => (AddMethod ?? RemoveMethod).IsStatic;

        public bool IsAbstract => (AddMethod ?? RemoveMethod).IsAbstract;

        public bool IsVirtual => (AddMethod ?? RemoveMethod).IsVirtual;

        private MetadataEventInfo(EventInfo evt) => UnderlyingEvent = evt;

        public static IEventInfo CreateFrom(EventInfo evt) => new MetadataEventInfo(evt);

        public override bool Equals(object obj) => obj is MetadataEventInfo that && UnderlyingEvent.Equals(that.UnderlyingEvent);

        public override int GetHashCode() => UnderlyingEvent.GetHashCode();

        public override string ToString() => UnderlyingEvent.ToString();
    }
}
