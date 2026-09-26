using FluentAssertions;
using OptiLifts.Domain.Clash;

namespace OptiLifts.Tests.Unit.Clash;

public class ClashDomainTests
{
    [Fact]
    public void CalculateDots_ReturnsZero_WhenTotalWeightIsZeroOrNegative()
    {
        DotsCalculationEngine.CalculateDots(0f, 80f, "Male").Should().Be(0f);
        DotsCalculationEngine.CalculateDots(-10f, 80f, "Male").Should().Be(0f);
    }

    [Fact]
    public void CalculateDots_ReturnsZero_WhenBodyweightIsZeroOrNegative()
    {
        DotsCalculationEngine.CalculateDots(500f, 0f, "Male").Should().Be(0f);
        DotsCalculationEngine.CalculateDots(500f, -5f, "Male").Should().Be(0f);
    }

    [Fact]
    public void CalculateDots_ClampsBodyweight_BelowMenMin()
    {
        var below = DotsCalculationEngine.CalculateDots(500f, 30f, "Male");
        var atMin = DotsCalculationEngine.CalculateDots(500f, 40f, "Male");

        below.Should().Be(atMin);
    }

    [Fact]
    public void CalculateDots_ClampsBodyweight_AboveMenMax()
    {
        var above = DotsCalculationEngine.CalculateDots(500f, 250f, "Male");
        var atMax = DotsCalculationEngine.CalculateDots(500f, 210f, "Male");

        above.Should().Be(atMax);
    }

    [Fact]
    public void CalculateDots_ClampsBodyweight_AboveWomenMax()
    {
        var above = DotsCalculationEngine.CalculateDots(500f, 200f, "Female");
        var atMax = DotsCalculationEngine.CalculateDots(500f, 150f, "Female");

        above.Should().Be(atMax);
    }

    [Fact]
    public void CalculateDots_Increases_AsTotalWeightIncreases()
    {
        var lower = DotsCalculationEngine.CalculateDots(400f, 80f, "Male");
        var higher = DotsCalculationEngine.CalculateDots(500f, 80f, "Male");

        higher.Should().BeGreaterThan(lower);
    }

    [Fact]
    public void CalculateDots_Differs_BetweenMaleAndFemaleCoeffs()
    {
        var male = DotsCalculationEngine.CalculateDots(500f, 80f, "Male");
        var female = DotsCalculationEngine.CalculateDots(500f, 80f, "Female");

        male.Should().NotBe(female);
    }

    [Theory]
    [InlineData("Male")]
    [InlineData("male")]
    [InlineData("MALE")]
    public void CalculateDots_UsesMaleCoeffs_OnlyForExactMaleMatch(string gender)
    {
        var res = DotsCalculationEngine.CalculateDots(500f, 80f, gender);
        var male = DotsCalculationEngine.CalculateDots(500f, 80f, "Male");

        res.Should().Be(male);
    }

    [Theory]
    [InlineData("Female")]
    [InlineData("other")]
    [InlineData("")]
    public void CalculateDots_DefaultsToFemaleCoeffs_ForAnyNonMaleGender(string gender)
    {
        var res = DotsCalculationEngine.CalculateDots(500f, 80f, gender);
        var female = DotsCalculationEngine.CalculateDots(500f, 80f, "Female");

        res.Should().Be(female);
    }

    [Fact]
    public void CalculateDots_ReturnsApproxKnownValue_ForMenAt100kgBW()
    {
        var res = DotsCalculationEngine.CalculateDots(500f, 100f, "Male");

        res.Should().BeApproximately(307.76f, 1f);
    }

    [Fact]
    public void CalculateE1RM_ReturnsZero_WhenWeightIsZeroOrNegative()
    {
        E1RMCalculationEngine.CalculateE1RM(0f, 5).Should().Be(0f);
        E1RMCalculationEngine.CalculateE1RM(-10f, 5).Should().Be(0f);
    }

    [Fact]
    public void CalculateE1RM_ReturnsZero_WhenRepsIsZeroOrNegative()
    {
        E1RMCalculationEngine.CalculateE1RM(100f, 0).Should().Be(0f);
        E1RMCalculationEngine.CalculateE1RM(100f, -1).Should().Be(0f);
    }

    [Fact]
    public void CalculateE1RM_MatchesExpectedFormula_ForFiveReps()
    {
        var res = E1RMCalculationEngine.CalculateE1RM(100f, 5);

        res.Should().BeApproximately(116.65f, 0.01f);
    }

    [Fact]
    public void CalculateE1RM_ClampsAtTenReps_WhenRepsExceedTen()
    {
        var atTen = E1RMCalculationEngine.CalculateE1RM(100f, 10);
        var above = E1RMCalculationEngine.CalculateE1RM(100f, 15);

        above.Should().Be(atTen);
    }

    [Fact]
    public void CalculateE1RM_Increases_AsRepsIncreaseUpToTen()
    {
        var lower = E1RMCalculationEngine.CalculateE1RM(100f, 3);
        var higher = E1RMCalculationEngine.CalculateE1RM(100f, 8);

        higher.Should().BeGreaterThan(lower);
    }

    [Theory]
    [InlineData("u59", 0, 59)]
    [InlineData("u66", 59, 66)]
    [InlineData("u74", 66, 74)]
    [InlineData("u83", 74, 83)]
    [InlineData("u93", 83, 93)]
    [InlineData("u105", 93, 105)]
    [InlineData("u120", 105, 120)]
    public void TryGetRange_ReturnsExpectedBounds_ForKnownBracket(string bracketId, decimal expMin, decimal expMax)
    {
        var found = WeightClassBrackets.TryGetRange(bracketId, out var min, out var max);

        found.Should().BeTrue();
        min.Should().Be(expMin);
        max.Should().Be(expMax);
    }

    [Fact]
    public void TryGetRange_ReturnsUnboundedMax_ForSuperHeavyweight()
    {
        var found = WeightClassBrackets.TryGetRange("120p", out var min, out var max);

        found.Should().BeTrue();
        min.Should().Be(120m);
        max.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public void TryGetRange_ReturnsFalse_ForUnknownBracket()
    {
        var found = WeightClassBrackets.TryGetRange("not-a-bracket", out var min, out var max);

        found.Should().BeFalse();
        min.Should().Be(0m);
        max.Should().Be(0m);
    }

    [Theory]
    [InlineData("Chest", "Chest")]
    [InlineData("Abdominals", "Core")]
    [InlineData("Obliques", "Core")]
    [InlineData("Shoulders", "Shoulders")]
    [InlineData("Rear Deltoid", "Shoulders")]
    [InlineData("Biceps", "Arms")]
    [InlineData("Forearms", "Arms")]
    [InlineData("Quadriceps", "Legs")]
    [InlineData("Glutes", "Legs")]
    [InlineData("Lats", "Back")]
    [InlineData("Trapezius", "Back")]
    public void GetRadarGroup_MapsMuscle_ToExpectedGroup(string muscle, string expGroup)
    {
        MuscleGroupMapper.GetRadarGroup(muscle).Should().Be(expGroup);
    }

    [Fact]
    public void GetRadarGroup_ReturnsNull_ForUnknownMuscle()
    {
        MuscleGroupMapper.GetRadarGroup("Not A Muscle").Should().BeNull();
    }

    [Fact]
    public void RadarGroups_ContainsExactlySixGroups()
    {
        MuscleGroupMapper.RadarGroups.Should().HaveCount(6);
        MuscleGroupMapper.RadarGroups.Should().Contain(new[] { "Chest", "Core", "Shoulders", "Arms", "Legs", "Back" });
    }
}
