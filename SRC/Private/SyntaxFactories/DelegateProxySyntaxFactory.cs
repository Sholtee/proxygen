/********************************************************************************
* DelegateProxySyntaxFactory.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal sealed partial class DelegateProxySyntaxFactory : ProxyUnitSyntaxFactory
    {
        private const string INVOKE_METHOD_NAME = nameof(Action.Invoke);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IMethodInfo FInvokeDelegate;

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ClassDeclarationSyntax> ResolveClasses(object context, CancellationToken cancellation)
        {
            yield return ResolveClass(context, cancellation);
        }

        public override string ExposedClass => $"DelegateProxy_{TargetType!.GetMD5HashCode()}";

        #if DEBUG
        internal
        #endif
        protected override IReadOnlyList<ITypeInfo> Bases =>
        [
            MetadataTypeInfo.CreateFrom(typeof(IDelegateWrapper)),
            ..base.Bases
        ];

        public DelegateProxySyntaxFactory(ITypeInfo targetType, SyntaxFactoryContext context) : base(targetType, context)
        {
            if (!targetType.Flags.HasFlag(TypeInfoFlags.IsDelegate))
                throw new ArgumentException(Resources.NOT_A_DELEGATE, nameof(targetType));

            if (targetType is IGenericTypeInfo generic && generic.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_TARGET, nameof(targetType));

            FInvokeDelegate = targetType.Methods.Single(static m => m.Name == INVOKE_METHOD_NAME);

            //
            // "ref return"s not supported
            //

            if (FInvokeDelegate.ReturnValue.Kind >= ParameterKind.Ref)
                throw new NotSupportedException(Resources.REF_VALUE);
        }
    }
}