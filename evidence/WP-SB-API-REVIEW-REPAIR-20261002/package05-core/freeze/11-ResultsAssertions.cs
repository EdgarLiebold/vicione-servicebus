// Package-only Research draft; Root owns fresh restore/build/native evidence.
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Configuration;
using Xunit;

namespace ViciOneReview.CorePackage05;

public static class ResultsAssertions
{
    public static Task MembershipAsync(bool withInner)
    {
        var specification = new Specification();
        ValidationResult failure = specification.Failure("Endpoint.Address", "owned-address", "The address is invalid");
        ValidationResult replacement = specification.Success("Endpoint.Address", "replacement-address", "The address is valid");
        Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
        Assert.Equal(ValidationResultDisposition.Success, replacement.Disposition);
        var input = new List<ValidationResult> { failure };
        var primary = new InvalidOperationException("owned-primary");
        ConfigurationException exception = withInner
            ? new ConfigurationException(input, "owned-message", primary)
            : new ConfigurationException(input, "owned-message");
        input[0] = replacement;
        input.Clear();
        Assert.Empty(input);
        Assert.Same(failure, Assert.Single(exception.Results));
        Assert.Equal("owned-message", exception.Message);
        Assert.Same(withInner ? primary : null, exception.InnerException);
        if (exception.Results is IList<ValidationResult> list)
        {
            // Arrays report IsReadOnly but permit index assignment; inspect actual membership.
            try { list[0] = replacement; }
            catch (NotSupportedException) { }
        }
        Assert.Same(failure, Assert.Single(exception.Results));
        Assert.Equal(ValidationResultDisposition.Failure, exception.Results[0].Disposition);
        Assert.Equal("Endpoint.Address", exception.Results[0].Key);
        Assert.Equal("owned-address", exception.Results[0].Value);
        Assert.Equal("The address is invalid", exception.Results[0].Message);
        Assert.Equal("owned-message", exception.Message);
        Assert.Same(withInner ? primary : null, exception.InnerException);
        return Task.CompletedTask;
    }

    public static Task NullSequenceAsync(bool withInner)
    {
        var inner = new InvalidOperationException("owned-primary");
        ArgumentNullException failure = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = withInner ? new ConfigurationException(null!, "message", inner)
                : new ConfigurationException(null!, "message");
        });
        Assert.Equal("results", failure.ParamName);
        return Task.CompletedTask;
    }

    public static Task EmptyAsync()
    {
        var inner = new InvalidOperationException("owned-primary");
        Assert.Empty(new ConfigurationException().Results);
        Assert.Empty(new ConfigurationException("message").Results);
        ConfigurationException exception = new("message", inner);
        Assert.Empty(exception.Results);
        Assert.Same(inner, exception.InnerException);
        return Task.CompletedTask;
    }

    private sealed class Specification : ISpecification
    {
        public IEnumerable<ValidationResult> Validate() => [];
    }
}
