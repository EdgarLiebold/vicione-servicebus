using System;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class EventCorrelationExpressionConverter<TInstance, TMessage> :
    ExpressionVisitor
    where TInstance : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;

    public EventCorrelationExpressionConverter(ConsumeContext<TMessage> context)
    {
        _context = context;
    }

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
