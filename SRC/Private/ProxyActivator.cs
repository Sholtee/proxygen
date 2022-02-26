/********************************************************************************
* ProxyActivator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

#if NETSTANDARD2_1_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Solti.Utils.Proxy.Internals
{
    internal static class ProxyActivator
    {
        #if NETSTANDARD2_1_OR_GREATER
        public delegate object Activator(ITuple? tuple);
        #else
        public delegate object Activator(object? tuple);
        #endif

        public static Activator Create(Type proxyType)
        {
            ParameterExpression paramzTuple = Expression.Parameter(typeof(object), nameof(paramzTuple));

            LabelTarget ret = Expression.Label(typeof(object), nameof(ret));

            List<ParameterExpression> locals = new();

            List<Expression> block = new();

            foreach (ConstructorInfo ctor in proxyType.GetConstructors())
            {
                block.AddRange
                (
                    CreateIf(ctor)
                );
            }

            block.Add
            (
                Expression.Throw
                (
                    Expression.New
                    (
                        typeof(MissingMemberException).GetConstructor(new[] { typeof(string), typeof(string) }) ?? throw new MissingMethodException(typeof(MissingMemberException).Name, "Ctor"),
                        Expression.Constant(proxyType.Name),
                        Expression.Constant("Ctor")
                    )
                )
            );
            block.Add(Expression.Label(ret, Expression.Default(typeof(object))));

            Expression<Activator> lambda = Expression.Lambda<Activator>(Expression.Block(locals, block), paramzTuple);

            return lambda.Compile();

            IEnumerable<Expression> CreateIf(ConstructorInfo ctor)
            {
                Type[] itemTypes = ctor
                    .GetParameters()
                    .Convert(p => p.ParameterType);

                if (itemTypes.Length is 0)
                {
                    yield return Expression.IfThen
                    (
                        Expression.Equal
                        (
                            paramzTuple,
                            Expression.Default(typeof(object))
                        ),
                        Expression.Return
                        (
                            ret,
                            Expression.New(ctor)
                        )
                    );
                }
                else
                {
                    Type tupleType = Type
                        .GetType($"System.Tuple`{itemTypes.Length}", throwOnError: true)
                        .MakeGenericType(itemTypes);

                    ParameterExpression target = Expression.Variable(tupleType);
                    locals.Add(target);

                    yield return Expression.Assign(target, Expression.TypeAs(paramzTuple, tupleType));

                    yield return Expression.IfThen
                    (
                        Expression.NotEqual
                        (
                            target,
                            Expression.Default(tupleType)
                        ),
                        Expression.Return
                        (
                            ret,
                            Expression.New
                            (
                                ctor,
                                itemTypes.Length.Times(GetItem)
                            )
                        )
                    );

                    Expression GetItem(int index)
                    {
                        string itemName = $"Item{index + 1}";

                        PropertyInfo item = tupleType.GetProperty(itemName) ?? throw new MissingMemberException(tupleType.Name, itemName);

                        return Expression.Property(target, item);
                    }
                }
            }
        }
    }
}
