using Specforge.Core.Exceptions;
using Specforge.Mcp.Errors;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Errors;

public class ToolExceptionMapperTests
{
    private static IReadOnlySet<Type> ConcreteCoreExceptions() =>
        typeof(SpecforgeException).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(SpecforgeException).IsAssignableFrom(t))
            .ToHashSet();

    [Fact]
    public void EveryTypedException_HasAMapperArm()
    {
        IReadOnlySet<Type> reflected = ConcreteCoreExceptions();
        Assert.True(
            reflected.SetEquals(ToolExceptionMapper.HandledTypes),
            $"mapper HandledTypes ({ToolExceptionMapper.HandledTypes.Count}) drift from Core exceptions ({reflected.Count}): " +
            $"missing arm for [{string.Join(", ", reflected.Except(ToolExceptionMapper.HandledTypes).Select(t => t.Name))}]; " +
            $"stale arm for [{string.Join(", ", ToolExceptionMapper.HandledTypes.Except(reflected).Select(t => t.Name))}]");
    }

    [Fact]
    public void MapperArms_ProduceTheExpectedCodes()
    {
        ToolExceptionMapper mapper = new();
        foreach ((SpecforgeException ex, string expectedCode) in ExceptionFixtures.All)
        {
            MappedError? mapped = mapper.Map(ex);
            Assert.NotNull(mapped);
            Assert.Equal(expectedCode, mapped!.Code);
        }
    }

    [Fact]
    public void Fixtures_CoverEveryHandledType()
    {
        IReadOnlySet<Type> fixtureTypes = ExceptionFixtures.All.Select(f => f.Exception.GetType()).ToHashSet();
        Assert.True(fixtureTypes.SetEquals(ToolExceptionMapper.HandledTypes));
    }

    [Fact]
    public void UnknownException_MapsToNull()
    {
        Assert.Null(new ToolExceptionMapper().Map(new InvalidOperationException("boom")));
    }
}
