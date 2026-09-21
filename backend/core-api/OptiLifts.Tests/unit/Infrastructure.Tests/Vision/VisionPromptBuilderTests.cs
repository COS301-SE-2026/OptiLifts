using System.Collections.Generic;
using FluentAssertions;
using OptiLifts.Infrastructure.Vision;
using Xunit;

namespace OptiLifts.Tests.Unit.Infrastructure.Tests.Vision;

public class VisionPromptBuilderTests
{
    private readonly VisionPromptBuilder _promptBuilder = new();

    [Fact]
    public void BuildPrompt_FormatsExactTemplate_WithExerciseAndAnomalies()
    {
        var anomalies = new List<string> { "knees caving in", "chest dropping" };

        var prompt = _promptBuilder.BuildPrompt("squat", anomalies);

        prompt.Should().Be("User squat errors: knees caving in, chest dropping. Write a concise, actionable 2-sentence coaching tip encouraging the user and instructing proper form.");
    }

    [Fact]
    public void GetPositiveReinforcement_ReturnsExactSpecifiedMessage()
    {
        var message = _promptBuilder.GetPositiveReinforcement();

        message.Should().Be("Clean reps! Your form looks solid, keep it up.");
    }

    [Fact]
    public void GetFallbackCoachingTip_WhenAnomaliesProvided_IncludesExerciseAndAnomalies()
    {
        var anomalies = new[] { "rounded back" };

        var fallback = _promptBuilder.GetFallbackCoachingTip("deadlift", anomalies);

        fallback.Should().Contain("deadlift");
        fallback.Should().Contain("rounded back");
    }

    [Fact]
    public void GetFallbackCoachingTip_WhenAnomaliesEmpty_ReturnsGeneralEncouragement()
    {
        var fallback = _promptBuilder.GetFallbackCoachingTip("bench press", null);

        fallback.Should().Contain("bench press");
        fallback.Should().Contain("core braced");
    }
}
