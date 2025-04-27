/********************************************************************************
* SyntaxFactoryBase.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class SyntaxFactoryBase(SyntaxFactoryContext context)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ILogger? FLogger;

        /// <summary>
        /// The context associated with this factory.
        /// </summary>
        public SyntaxFactoryContext Context { get; } = context;

        /// <summary>
        /// The class that is implemented by this factory. After compiling the code the caller can acquire the generated type using this property.
        /// </summary>
        public abstract string ExposedClass { get; }

        /// <summary>
        /// The assembly defined by this factory.
        /// </summary>
        public string ContainingAssembly => Context.AssemblyNameOverride ?? ExposedClass;

        /// <summary>
        /// The logger associated with this instance. Log scopes are created using the <see cref="ExposedClass"/> property
        /// </summary>
        public ILogger Logger => FLogger ??= Context.LoggerFactory.CreateLogger(ExposedClass);
    }
}