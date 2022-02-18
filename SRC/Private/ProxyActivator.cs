/********************************************************************************
* ProxyActivator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal static class ProxyActivator
    {
        public static Func<object, object> Create(Type proxyType)
        {
            ParameterExpression paramzTuple = Expression.Parameter(typeof(object), nameof(paramzTuple));

            LabelTarget ret = Expression.Label(typeof(object), nameof(ret));

            List<ParameterExpression> locals = new();

            List<Expression> block = proxyType
                .GetConstructors()
                .SelectMany(CreateIf)
                .ToList();

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

            Expression<Func<object, object>> lambda = Expression.Lambda<Func<object, object>>(Expression.Block(locals, block), paramzTuple);

            return lambda.Compile();

            IEnumerable<Expression> CreateIf(ConstructorInfo ctor)
            {
                Type[] itemTypes = ctor
                    .GetParameters()
                    .Select(p => p.ParameterType)
                    .ToArray();

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
                            Expression.Default(tupleType)),
                            Expression.Return
                            (
                                ret,
                                Expression.New
                                (
                                    ctor,
                                    itemTypes.Select((_, i) => GetItem(i)
                                )
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
