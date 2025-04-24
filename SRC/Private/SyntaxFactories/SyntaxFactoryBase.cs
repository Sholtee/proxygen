/********************************************************************************
* SyntaxFactoryBase.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class SyntaxFactoryBase: IDisposable
    {
        protected SyntaxFactoryBase(SyntaxFactoryContext context)
        {
            Context = context;
            ContainingAssembly = Context.AssemblyNameOverride ?? ExposedClass;
            Logger = Context.LoggerFactory.CreateLogger(ExposedClass);
        }

        /// <summary>
        /// The context associated with this factory.
        /// </summary>
        public SyntaxFactoryContext Context { get; }

        /// <summary>
        /// The class that is implemented by this factory. After compiling the code the caller can acquire the generated type using this property.
        /// </summary>
        public abstract string ExposedClass { get; }

        /// <summary>
        /// The assembly defined by this factory.
        /// </summary>
        public string ContainingAssembly { get; }

        /// <summary>
        /// The logger associated with this instance. Log scopes are created using the <see cref="ExposedClass"/> property
        /// </summary>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// Disposes this object. Since this is an internal class we won't implement the disposable pattern.
        /// </summary>
        public virtual void Dispose()
        {
            Logger?.Dispose();
            Logger = null!;
        }
    }
}