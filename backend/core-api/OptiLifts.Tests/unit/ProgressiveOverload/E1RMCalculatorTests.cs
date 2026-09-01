using FluentAssertions;
using OptiLifts.Domain.ProgressiveOverload;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Tests.ProgressiveOverload;

public class E1RMCalculatorTests
{
    [Fact]
    public void CalculateE1RM_BodyweightExercise_UsesFiftyPercentOfBodyweight()
    {
        var result = E1RMCalculator.CalculateE1RM(0f, 10, "compound", ExerciseType.BodyweightReps, 80f);

        result.Should().BeApproximately(80d * 0.50d * (1 + 10 / 30d), 0.01d);
    }

    [Fact]
    public void CalculateE1RM_WeightedBodyweightExercise_UsesBodyweightPlusAddedWeight()
    {
        var result = E1RMCalculator.CalculateE1RM(20f, 8, "compound", ExerciseType.WeightedBodyweight, 80f);

        result.Should().BeApproximately((80d + 20d) * (1 + 8 / 30d), 0.01d);
    }

    [Fact]
    public void ReverseEpleyReps_WeightedCompoundExercise_UsesEpleyFormula()
    {
        var result = E1RMCalculator.ReverseEpleyReps(133.3d, 100f, "compound", ExerciseType.WeightReps);

        result.Should().Be(10);
    }

    [Fact]
    public void ReverseEpleyReps_WeightedIsolationExercise_ReturnsRoundedRepTarget()
    {
        var result = E1RMCalculator.ReverseEpleyReps(100d, 75f, null, ExerciseType.WeightReps);

        result.Should().Be(10);
    }

    [Fact]
    public void ReverseEpleyReps_BodyweightExercise_UsesFlatRatioForTargetReps()
    {
        var liftedWeight = 80f * 0.50f;
        var targetE1RM = liftedWeight * (1 + 10 / 30d);

        var result = E1RMCalculator.ReverseEpleyReps(targetE1RM, 0f, null, ExerciseType.BodyweightReps, 80f);

        result.Should().Be(10);
    }

    [Fact]
    public void ReverseEpleyReps_BodyweightExercise_WithoutBodyweight_ThrowsArgumentOutOfRangeException()
    {
        var action = () => E1RMCalculator.ReverseEpleyReps(12.6d, 0f, null, ExerciseType.BodyweightReps);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ReverseEpleyReps_WeightedBodyweightExercise_UsesBodyweightPlusAddedWeight()
    {
        var targetE1RM = 100d * (1 + 8 / 30d);

        var result = E1RMCalculator.ReverseEpleyReps(targetE1RM, 20f, null, ExerciseType.WeightedBodyweight, 80f);

        result.Should().Be(8);
    }

    [Fact]
    public void ReverseEpleyReps_WeightedExerciseWithNegativeCalculatedReps_ReturnsAtLeastOne()
    {
        var result = E1RMCalculator.ReverseEpleyReps(50d, 100f, null, ExerciseType.WeightReps);

        result.Should().Be(1);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void ReverseEpleyReps_InvalidTargetE1RM_ThrowsArgumentOutOfRangeException(double targetE1RM)
    {
        var action = () => E1RMCalculator.ReverseEpleyReps(targetE1RM, 75f, null, ExerciseType.WeightReps);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    public void ReverseEpleyReps_InvalidPreviousWeight_ThrowsArgumentOutOfRangeException(float previousWeight)
    {
        var action = () => E1RMCalculator.ReverseEpleyReps(100d, previousWeight, null, ExerciseType.WeightReps);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ReverseEpleyWeight_WeightedExercise_ReturnsTargetWeight()
    {
        var result = E1RMCalculator.ReverseEpleyWeight(100d, 10, null, ExerciseType.WeightReps);

        result.Should().BeApproximately(75.01875f, 0.001f);
    }

    [Fact]
    public void ReverseEpleyWeight_AndReverseEpleyReps_AreInverseForSameRepTarget()
    {
        const double targetE1RM = 120d;
        const int targetReps = 8;

        var targetWeight = E1RMCalculator.ReverseEpleyWeight(targetE1RM, targetReps, null, ExerciseType.WeightReps);
        var calculatedReps = E1RMCalculator.ReverseEpleyReps(targetE1RM, targetWeight, null, ExerciseType.WeightReps);

        calculatedReps.Should().Be(targetReps);
    }

    [Fact]
    public void ReverseEpleyWeight_CompoundExercise_MatchesEpleyE1RMFormula()
    {
        var result = E1RMCalculator.ReverseEpleyWeight(133.3d, 10, "compound", ExerciseType.WeightReps);

        var roundTripE1RM = E1RMCalculator.CalculateE1RM(result, 10, "compound", ExerciseType.WeightReps);
        roundTripE1RM.Should().BeApproximately(133.3d, 0.01d);
    }

    [Fact]
    public void ReverseEpleyWeight_BodyweightExercise_ThrowsInvalidOperationException()
    {
        var action = () => E1RMCalculator.ReverseEpleyWeight(100d, 8, null, ExerciseType.BodyweightReps);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReverseEpleyWeight_WeightedBodyweightExercise_ReturnsAddedWeight()
    {
        var targetE1RM = 100d * (1 + 8 / 30d);

        var result = E1RMCalculator.ReverseEpleyWeight(targetE1RM, 8, null, ExerciseType.WeightedBodyweight, 80f);

        result.Should().BeApproximately(20f, 0.01f);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReverseEpleyWeight_InvalidTargetReps_ThrowsArgumentOutOfRangeException(int targetReps)
    {
        var action = () => E1RMCalculator.ReverseEpleyWeight(100d, targetReps, null, ExerciseType.WeightReps);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}