/********************************************************************************
* ProxySyntaxFactory.InvokeFactory.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class ProxySyntaxFactory
    {
        /// <summary>
        /// public override object Invoke(MethodInfo method, object?[] args, MemberInfo extra) <br/>
        /// {                                                                                  <br/>
        ///     try                                                                            <br/>
        ///     {                                                                              <br/>
        ///         return base.Invoke(method, args, extra);                                   <br/>
        ///     } finally {                                                                    <br/>
        ///         InvokeTarget = null;                                                       <br/>
        ///     }                                                                              <br/>
        /// }
        /// </summary>
        internal sealed class InvokeFactory : ProxyMemberSyntaxFactory
        {
            public InvokeFactory(IProxyContext context) : base(context) { }

            protected override IEnumerable<MemberDeclarationSyntax> BuildMembers(CancellationToken cancellation)
            {
                cancellation.ThrowIfCancellationRequested();

                if (INVOKE.IsFinal)
                    throw new InvalidOperationException(Resources.SEALED_INVOKE);

                yield return OverrideMethod(INVOKE).WithBody
                (
                    SimpleBlock
                    (
                        TryStatement().WithBlock
                        (
                            SimpleBlock
                            (
                                ReturnStatement
                                (
                                    InvokeMethod(INVOKE, BaseExpression(), castTargetTo: null, arguments: INVOKE
                                        .Parameters
                                        .Select
                                        (
                                            para => Argument(IdentifierName(para.Name))
                                        )
                                        .ToArray())
                                )
                            )
                        )
                        .WithFinally
                        (
                            FinallyClause
                            (
                                SimpleBlock
                                (
                                    AssignCallback(LiteralExpression(SyntaxKind.NullLiteralExpression))
                                )
                            )
                        )
                    )
                );

                static BlockSyntax SimpleBlock(StatementSyntax statement) => Block
                (
                    SingletonList
                    (
                        statement
                    )
                );
            }
        }
    }
}