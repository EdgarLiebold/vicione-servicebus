using System;
using System.Linq.Expressions;
using FastExpressionCompiler;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Converts event correlation expression values.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class EventCorrelationExpressionConverter<TInstance, TMessage> :
    ExpressionVisitor
    where TInstance : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public EventCorrelationExpressionConverter(ConsumeContext<TMessage> context)
    {
        _context = context;
    }

    /// <summary>Converts the supplied value.</summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The converted value.</returns>
    public Expression<Func<TInstance, bool>> Convert(Expression<Func<TInstance, ConsumeContext<TMessage>, bool>> expression)
    {
        var result = Visit(expression) as LambdaExpression
            ?? throw new InvalidOperationException("The correlation expression could not be converted to a lambda expression.");

        return RemoveMessageParameter(result);
    }

    static Expression<Func<TInstance, bool>> RemoveMessageParameter(LambdaExpression lambda)
    {
        ParameterExpression[] parameters = { lambda.Parameters[0] };

        return Expression.Lambda<Func<TInstance, bool>>(lambda.Body, parameters);
    }

    /// <summary>Visits member.</summary>
    /// <param name="m">The <c>m</c> value.</param>
    /// <returns>The expression produced by the operation.</returns>
    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression == null)
            return base.VisitMember(m);

        if (m.Expression.NodeType == ExpressionType.Parameter && m.Expression.Type == typeof(ConsumeContext<TMessage>))
            return EvaluateConsumeContextAccess(m);

        return base.VisitMember(m);
    }

    Expression EvaluateConsumeContextAccess(MemberExpression exp)
    {
        var parameter = exp.Expression as ParameterExpression
            ?? throw new InvalidOperationException("The consume context access must originate from a parameter expression.");

        var fn = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(ConsumeContext<TMessage>), exp.Type), exp, parameter).CompileFast();

        return Expression.Constant(fn.DynamicInvoke(_context), exp.Type);
    }
}
