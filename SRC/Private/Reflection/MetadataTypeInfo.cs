/********************************************************************************
* MetadataTypeInfo.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal class MetadataTypeInfo : ITypeInfo
    {
        protected Type UnderlyingType { get; }

        protected MetadataTypeInfo(Type underlyingType) => UnderlyingType = underlyingType;

        public static ITypeInfo CreateFrom(Type underlyingType)
        {
            while (underlyingType.IsByRef)
            {
                underlyingType = underlyingType.GetElementType();
            }

            return underlyingType switch
            {
                _ when underlyingType.IsArray => new MetadataArrayTypeInfo(underlyingType),
                _ when underlyingType.GetOwnGenericArguments().Any() => new MetadataGenericTypeInfo(underlyingType),
                _ => new MetadataTypeInfo(underlyingType)
            };
        }

        public override bool Equals(object obj) => obj is MetadataTypeInfo that && UnderlyingType.Equals(that.UnderlyingType);

        public override int GetHashCode() => UnderlyingType.GetHashCode();

        public override string ToString() => UnderlyingType.ToString();

        private IAssemblyInfo? FDeclaringAssembly;
        public IAssemblyInfo DeclaringAssembly => FDeclaringAssembly ??= MetadataAssemblyInfo.CreateFrom(UnderlyingType.Assembly);

        public bool IsVoid => UnderlyingType == typeof(void);

        private ITypeInfo? FEnclosingType;
        public ITypeInfo? EnclosingType
        {
            get 
            {
                if (FEnclosingType is null)
                {
                    Type? enclosingType = UnderlyingType.GetEnclosingType();

                    if (enclosingType is not null)
                        FEnclosingType = CreateFrom(enclosingType);
                }
                return FEnclosingType;
            }
        }

        private IReadOnlyList<ITypeInfo>? FInterfaces;
        public IReadOnlyList<ITypeInfo> Interfaces => FInterfaces ??= UnderlyingType
            .GetInterfaces()
            .Select(CreateFrom)
            .ToArray();

        private ITypeInfo? FBaseType;
        public ITypeInfo? BaseType => UnderlyingType.BaseType is not null
            ? FBaseType ??= CreateFrom(UnderlyingType.BaseType)
            : null;

        public virtual string Name => UnderlyingType.GetFriendlyName();

        public RefType RefType => UnderlyingType switch
        {
            // _ when UnderlyingType.IsByRef => RefType.Ref, // FIXME: ezt nem kene kikommentelni de ugy tunik a Type.IsByRef-nek nincs megfeleloje az INamedTypeInfo-ban (lasd: PassingByReference_ShouldNotAffectTheParameterType test)
            _ when UnderlyingType.IsPointer => RefType.Pointer,
            _ when UnderlyingType.IsArray => RefType.Array,
            _ => RefType.None
        };

        private ITypeInfo? FElementType;
        public ITypeInfo? ElementType
        {
            get
            {
                if (FElementType is null)
                {
                    Type? realType = UnderlyingType.GetElementType();

                    if (realType is not null)
                        FElementType = CreateFrom(realType);
                }
                return FElementType;
            }
        }

        //
        // "Cica<T>.Mica<TT>"-nal a "TT" is beagyazott ami nekunk nem jo
        //

        public bool IsNested => UnderlyingType.IsNested() && !IsGenericParameter;

        public bool IsInterface => UnderlyingType.IsInterface;

        private IReadOnlyList<IPropertyInfo>? FProperties;
        public IReadOnlyList<IPropertyInfo> Properties => FProperties ??= UnderlyingType
            .ListProperties(includeStatic: true)
            .Select(MetadataPropertyInfo.CreateFrom)
            .ToArray();

        private IReadOnlyList<IEventInfo>? FEvents;
        public IReadOnlyList<IEventInfo> Events => FEvents ??= UnderlyingType
            .ListEvents(includeStatic: true)
            .Select(MetadataEventInfo.CreateFrom)
            .ToArray();

        //
        // Ezeket a metodusok forditas idoben nem leteznek igy a SymbolTypeInfo-ban sem fognak szerepelni.
        //

        private static readonly IReadOnlyList<Func<MethodInfo, bool>> MethodsToSkip = new Func<MethodInfo, bool>[]
        {
            m => m.Name == "Finalize", // destructor
            m => m.DeclaringType.IsArray && m.Name == "Get", // = array[i]
            m => m.DeclaringType.IsArray && m.Name == "Set", // array[i] =
            m => m.DeclaringType.IsArray && m.Name == "Address", // = ref array[i]
            m => typeof(Delegate).IsAssignableFrom(m.DeclaringType) && m.Name == "Invoke" // delegate.Invoke(...)
        };

        private IReadOnlyList<IMethodInfo>? FMethods;
        public IReadOnlyList<IMethodInfo> Methods => FMethods ??= UnderlyingType
            .ListMethods(includeStatic: true)
            .Where(m => !MethodsToSkip.Any(skip => skip(m)))
            .Select(MetadataMethodInfo.CreateFrom)
            .ToArray();

        private IReadOnlyList<IConstructorInfo>? FConstructors;
        public IReadOnlyList<IConstructorInfo> Constructors => FConstructors ??=
            UnderlyingType.IsArray
                ? Array.Empty<IConstructorInfo>() // tomb egy geci specialis allatfaj: fordito generalja hozza a konstruktorokat -> futas idoben mar leteznek forditaskor meg nem
                : UnderlyingType
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(ctor => ctor.GetAccessModifiers() != AccessModifiers.Private)
                    .Select(MetadataMethodInfo.CreateFrom)
                    .Cast<IConstructorInfo>()
                    .ToArray();

        public string? AssemblyQualifiedName => FullName is not null //  (UnderlyingType.IsGenericType ? UnderlyingType.GetGenericTypeDefinition() : UnderlyingType).AssemblyQualifiedName;
            ? $"{FullName}, {UnderlyingType.Assembly}"
            : null;

        public bool IsGenericParameter => (UnderlyingType.GetElementType(recurse: true) ?? UnderlyingType).IsGenericParameter;

        public string? FullName => UnderlyingType
            .GetFullName()
            ?.TrimEnd('&'); // FIXME: ez nem kene de ugy tunik a Type.IsByRef-nek nincs megfeleloje az INamedTypeInfo-ban

        public bool IsClass => UnderlyingType.IsClass();

        public bool IsFinal => UnderlyingType.IsSealed;

        public bool IsAbstract => UnderlyingType.IsAbstract();

        private IHasName? FContainingMember;
        public IHasName? ContainingMember
        {
            get
            {
                if (FContainingMember is null)
                {
                    Type concreteType = UnderlyingType.GetElementType(recurse: true) ?? UnderlyingType;

                    FContainingMember = concreteType switch
                    {
                        _ when concreteType.IsGenericParameter /*kulonben a DeclaringMethod megbaszodik*/ && concreteType.DeclaringMethod is not null => MetadataMethodInfo.CreateFrom(concreteType.DeclaringMethod),
                        _ when /*Ez lehet T es nested is*/ concreteType.DeclaringType is not null => MetadataTypeInfo.CreateFrom(concreteType.DeclaringType),
                        _ => null
                    };
                }
                return FContainingMember;
            }
        }

        private sealed class MetadataGenericTypeInfo : MetadataTypeInfo, IGenericTypeInfo
        {
            public MetadataGenericTypeInfo(Type underlyingType) : base(underlyingType) { }

            public bool IsGenericDefinition => UnderlyingType.GetGenericArguments().All(ga => ga.IsGenericParameter);

            public IReadOnlyList<ITypeInfo> GenericArguments => UnderlyingType
                .GetOwnGenericArguments()
                .Select(CreateFrom)
                .ToArray();

            public override string Name => !UnderlyingType.IsGenericType || UnderlyingType.IsGenericTypeDefinition // FIXME: Type.GetFriendlyName() lezart generikusokat nem eszi meg (igaz elvileg nem is kell hivjuk lezart generikusra)
                ? base.Name
                : GenericDefinition.Name;

            public IGenericTypeInfo GenericDefinition => new MetadataGenericTypeInfo(UnderlyingType.GetGenericTypeDefinition());

            IGeneric IGeneric.GenericDefinition => GenericDefinition;

            public IGeneric Close(params ITypeInfo[] genericArgs)
            {
                if (UnderlyingType.IsNested) throw new NotSupportedException(); // TODO: implementalni ha hasznalni kell majd

                return (IGeneric) CreateFrom
                (
                    UnderlyingType.MakeGenericType
                    (
                        genericArgs
                            .Select(arg => arg.ToMetadata())
                            .ToArray()
                    )
                );
            }
        }

        private sealed class MetadataArrayTypeInfo : MetadataTypeInfo, IArrayTypeInfo 
        {
            public MetadataArrayTypeInfo(Type underlyingType) : base(underlyingType) { }

            public int Rank => UnderlyingType.GetArrayRank();
        }
    }
}
