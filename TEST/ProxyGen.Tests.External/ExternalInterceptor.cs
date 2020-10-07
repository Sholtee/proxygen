using System.Data;

namespace Solti.Utils.Proxy.Tests.External
{
    public class ExternalInterceptor<TInterface>: InterfaceInterceptor<TInterface> where TInterface: class
    {
        public ExternalInterceptor(TInterface target, IDbConnection conn) : base(target) { }
    }
}
