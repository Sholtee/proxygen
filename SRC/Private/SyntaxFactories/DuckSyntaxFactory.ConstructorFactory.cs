﻿/********************************************************************************
* DuckSyntaxFactory.ConstructorFactory.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory
    {
        internal sealed class ConstructorFactory : MemberSyntaxFactory
        {
            public DuckSyntaxFactory Owner { get; }

            public ConstructorFactory(DuckSyntaxFactory owner) : base((ITypeInfo) ((IGenericTypeInfo) MetadataTypeInfo.CreateFrom(typeof(DuckBase<>))).Close(owner.InterfaceType)) =>
                Owner = owner;

            public override bool Build(CancellationToken cancellation)
            {
                if (Members is not null) return false;

                cancellation.ThrowIfCancellationRequested();

                Members = SourceType
                    .Constructors
                    .Select(ctor => DeclareCtor(ctor, Owner.Classes.Single()))
                    .ToArray();

                return true;
            }
        }
    }
}