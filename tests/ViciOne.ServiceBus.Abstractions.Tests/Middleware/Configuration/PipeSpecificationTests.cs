using System.Security.Principal;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Middleware.Configuration;

public sealed class PipeSpecificationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-SPECIFICATION", "selected-branch-and-following-segment")]
    public async Task CustomSpecification_RunsItsSelectedBranchBeforeTheFollowingSegment()
    {
        var trace = new List<string>();
        IPipe<RoutingContext> allowed = Pipe.Execute<RoutingContext>(_ => trace.Add("allowed"));
        IPipe<RoutingContext> rejected = Pipe.Execute<RoutingContext>(_ => trace.Add("rejected"));
        IPipe<RoutingContext> pipe = Pipe.New<RoutingContext>(configurator =>
        {
            configurator.AddPipeSpecification(
                new RoleRoutingSpecification<RoutingContext>(allowed, rejected, ["operator"]));
            configurator.UseExecute(_ => trace.Add("next"));
        });
        var context = new RoutingContext();
        context.GetOrAddPayload<IPrincipal>(
            () => new GenericPrincipal(new GenericIdentity("test-user"), ["operator"]));

        await pipe.Send(context);

        Assert.Equal(["allowed", "next"], trace);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-PIPE-SPECIFICATION", "missing-configuration-rejected")]
    public void MissingRoles_AreRejectedBeforeThePipeIsBuilt(bool useNull)
    {
        IPipe<RoutingContext> allowed = Pipe.Empty<RoutingContext>();
        IPipe<RoutingContext> rejected = Pipe.Empty<RoutingContext>();
        IReadOnlyCollection<string>? roles = useNull ? null : Array.Empty<string>();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => Pipe.New<RoutingContext>(
            configurator => configurator.AddPipeSpecification(
                new RoleRoutingSpecification<RoutingContext>(allowed, rejected, roles))));

        ValidationResult failure = Assert.Single(exception.Results);
        Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
        Assert.Equal("Roles", failure.Key);
        Assert.Equal("At least one role is required.", failure.Message);
        Assert.Contains("The pipe configuration is invalid:", exception.Message, StringComparison.Ordinal);
    }

    private sealed class RoutingContext : BasePipeContext
    {
    }

    private sealed class RoleRoutingSpecification<TContext>(
        IPipe<TContext> allowed,
        IPipe<TContext> rejected,
        IReadOnlyCollection<string>? allowedRoles) : IPipeSpecification<TContext>
        where TContext : class, PipeContext
    {
        public void Apply(IPipeBuilder<TContext> builder)
        {
            builder.AddFilter(new RoleRoutingFilter<TContext>(allowed, rejected, allowedRoles!));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (allowedRoles is not { Count: > 0 })
                yield return this.Failure("Roles", "At least one role is required.");
        }
    }

    private sealed class RoleRoutingFilter<TContext>(
        IPipe<TContext> allowed,
        IPipe<TContext> rejected,
        IReadOnlyCollection<string> allowedRoles) : IFilter<TContext>
        where TContext : class, PipeContext
    {
        public async Task Send(TContext context, IPipe<TContext> next)
        {
            if (context.TryGetPayload(out IPrincipal? principal)
                && principal is not null
                && allowedRoles.Any(principal.IsInRole))
            {
                await allowed.Send(context);
            }
            else
                await rejected.Send(context);

            await next.Send(context);
        }

        public void Probe(ProbeContext context)
        {
            context.CreateScope("roleRouter").Add("allowedRoles", allowedRoles);
        }
    }
}
