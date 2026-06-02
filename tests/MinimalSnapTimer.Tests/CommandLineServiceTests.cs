using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;

namespace MinimalSnapTimer.Tests;

public sealed class CommandLineServiceTests
{
    [Fact]
    public void Parse_SupportsMinutesAndFlags()
    {
        var service = new CommandLineService();

        var options = service.Parse(new[] { "--minutes", "40", "--pure", "--always-on-top", "--paused" });

        Assert.True(options.IsValid);
        Assert.Equal(TimeSpan.FromMinutes(40), options.Duration);
        Assert.True(options.StartInPureMode);
        Assert.True(options.AlwaysOnTop);
        Assert.True(options.StartPaused);
    }

    [Fact]
    public void Parse_SupportsWorkflowMode()
    {
        var service = new CommandLineService();

        var options = service.Parse(new[] { "--mode", "sit" });

        Assert.Equal(WorkflowStage.Sit, options.Mode);
    }
}
