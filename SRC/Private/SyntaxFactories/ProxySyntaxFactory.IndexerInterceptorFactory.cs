﻿/********************************************************************************
* ProxySyntaxFactory.IndexerInterceptorFactory.cs                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory<TInterface, TInterceptor> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        /// <summary>
        /// TResult IInterface.this[TParam1 p1, TPAram2 p2]                                         <br/>
        /// {                                                                                       <br/>
        ///     get                                                                                 <br/>
        ///     {                                                                                   <br/>
        ///         InvokeTarget = () => Target.Prop[p1, p2];                                       <br/>
        ///         PropertyInfo prop = ResolveProperty(InvokeTarget);                              <br/>
        ///         return (TResult) Invoke(prop.GetMethod, new System.Object[]{p1, p2}, prop);     <br/>
        ///     }                                                                                   <br/>
        ///     set                                                                                 <br/>
        ///     {                                                                                   <br/>
        ///         InvokeTarget = () =>                                                            <br/>
        ///         {                                                                               <br/>
        ///           Target.Prop[p1, p2] = value;                                                  <br/>
        ///           return null;                                                                  <br/>
        ///         };                                                                              <br/>
        ///         PropertyInfo prop = ResolveProperty(InvokeTarget);                              <br/>
        ///         Invoke(prop.SetMethod, new System.Object[]{ p1, p2, value }, prop);             <br/>
        ///     }                                                                                   <br/>
        /// }
        /// </summary>
        internal sealed class IndexerInterceptorFactory : PropertyInterceptorFactory
        {
            protected override bool IsCompatible(IPropertyInfo prop) => prop.Indices.Any();

            public override MemberDeclarationSyntax Build(IMemberInfo member)
            {
                IPropertyInfo prop = (IPropertyInfo) member;

                return DeclareIndexer
                (
                    property: prop,
                    getBody: Block
                    (
                        BuildGet(prop)
                    ),
                    setBody: Block
                    (
                        BuildSet(prop)
                    )
                );
            }
        }
    }
}