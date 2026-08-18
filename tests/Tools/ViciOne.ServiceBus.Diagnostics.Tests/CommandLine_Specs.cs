namespace ViciOne.ServiceBus.Diagnostics.Tests;

using System;
using System.Collections.Generic;
using NUnit.Framework;


/// <summary>
/// What the command line accepts and what it refuses.
/// <para>
/// A diagnostic that silently ignores an unknown option measured something other than what was asked
/// for, and the reader has no way to tell. Every refusal below names the option it is about.
/// </para>
/// </summary>
[TestFixture]
public class Reading_the_command_line
{
    static readonly string[] Known = ["messages", "concurrency", "prefetch", "completion-limit-seconds", "output"];

    static Dictionary<string, string> Parse(params string[] args)
    {
        return Program.ParseOptions(args, Known);
    }

    [Test]
    public void Should_read_an_option_and_its_value()
    {
        Dictionary<string, string> options = Parse("publish-load", "--messages", "10");

        Assert.That(options["messages"], Is.EqualTo("10"));
    }

    [Test]
    public void Should_read_the_scenario_without_options()
    {
        Assert.That(Parse("publish-load"), Is.Empty);
    }

    [Test]
    public void Should_refuse_an_unknown_option_and_list_the_ones_it_takes()
    {
        var refused = Assert.Throws<ArgumentException>(() => Parse("publish-load", "--unknown", "5"));

        Assert.That(refused!.Message, Does.Contain("--unknown").And.Contain("--messages"));
    }

    [Test]
    public void Should_refuse_an_option_given_twice()
    {
        var refused = Assert.Throws<ArgumentException>(() => Parse("publish-load", "--messages", "1", "--messages", "2"));

        Assert.That(refused!.Message, Does.Contain("more than once"));
    }

    [Test]
    public void Should_refuse_an_option_without_a_value()
    {
        var refused = Assert.Throws<ArgumentException>(() => Parse("publish-load", "--messages"));

        Assert.That(refused!.Message, Does.Contain("without a value"));
    }

    [Test]
    public void Should_refuse_an_option_followed_by_another_option()
    {
        Assert.Throws<ArgumentException>(() => Parse("publish-load", "--messages", "--prefetch", "10"));
    }

    [Test]
    public void Should_refuse_a_positional_argument()
    {
        var refused = Assert.Throws<ArgumentException>(() => Parse("publish-load", "extra"));

        Assert.That(refused!.Message, Does.Contain("positional"));
    }

    [Test]
    public void Should_refuse_a_number_that_is_not_positive()
    {
        var refused = Assert.Throws<ArgumentException>(
            () => Program.Number(Parse("publish-load", "--messages", "0"), "messages", 1));

        Assert.That(refused!.Message, Does.Contain("positive number"));
    }

    [Test]
    public void Should_refuse_a_number_that_is_not_a_number()
    {
        Assert.Throws<ArgumentException>(
            () => Program.Number(Parse("publish-load", "--messages", "many"), "messages", 1));
    }

    [Test]
    public void Should_fall_back_when_an_option_is_absent()
    {
        Assert.That(Program.Number(Parse("publish-load"), "messages", 7), Is.EqualTo(7));
    }

    [Test]
    public void Should_show_the_canonical_runner_command_in_its_usage()
    {
        // The direct dotnet run form does not work: there is no default host, port or account.
        Assert.That(Program.Usage, Does.Contain("run_broker_category.py"));
    }
}
