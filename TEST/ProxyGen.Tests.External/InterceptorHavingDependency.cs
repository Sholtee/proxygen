using System.Data;

namespace Solti.Utils.Proxy.Tests.External
{
    public class InterceptorHavingDependency<TInterface>: InterfaceInterceptor<TInterface> where TInterface: class
    {
        public InterceptorHavingDependency(TInterface target, IDbConnection conn) : base(target) { }
    }
}
