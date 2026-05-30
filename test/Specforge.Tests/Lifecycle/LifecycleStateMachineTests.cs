using Specforge.Core.Lifecycle;

using Xunit;

namespace Specforge.Tests.Lifecycle;

public class LifecycleStateMachineTests
{
    private static readonly LifecycleStateMachine Machine = new();

    [Theory]
    [InlineData("Draft", "Draft for user review")]
    [InlineData("Draft for user review", "Approved")]
    [InlineData("Draft for user review", "Draft")]
    [InlineData("Approved", "Superseded")]
    [InlineData("Approved", "Refined")]
    [InlineData("Not started", "Draft")]
    public void IsValidTransition_AllowsDocumentedTransitions(string from, string to) =>
        Assert.True(Machine.IsValidTransition(from, to));

    [Theory]
    [InlineData("Draft", "Approved")]
    [InlineData("Approved", "Draft")]
    [InlineData("Superseded", "Draft")]
    [InlineData("Withdrawn", "Approved")]
    [InlineData("Draft", "Bogus")]
    public void IsValidTransition_RejectsOthers(string from, string to) =>
        Assert.False(Machine.IsValidTransition(from, to));

    [Fact]
    public void AllowedTransitions_ListsNextStates()
    {
        IReadOnlyList<string> allowed = Machine.AllowedTransitions("Draft for user review");
        Assert.Contains("Approved", allowed);
        Assert.Contains("Withdrawn", allowed);
    }

    [Fact]
    public void AllowedTransitions_TerminalStateIsEmpty() =>
        Assert.Empty(Machine.AllowedTransitions("Superseded"));
}
