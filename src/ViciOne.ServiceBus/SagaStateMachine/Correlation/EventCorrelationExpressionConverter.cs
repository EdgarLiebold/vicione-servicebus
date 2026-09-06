using System;
using System.Linq.Expressions;
using FastExpressionCompiler;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides an event correlation expression converter implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class EventCorrelationExpressionConverter<TInstance, TMessage> :
    ExpressionVisitor
    where TInstance : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public EventCorrelationExpressionConverter(ConsumeContext<TMessage> context)
    {
        _context = context;
    }

    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the visit member operation.
    /// </summary>
    /// <param name="m">The m value.</param>
    /// <returns>The result of the operation.</returns>
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
