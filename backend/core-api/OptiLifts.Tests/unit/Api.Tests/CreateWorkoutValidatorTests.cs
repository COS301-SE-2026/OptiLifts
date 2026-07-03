using FluentAssertions;
using OptiLifts.Application.Workouts.CreateWorkout;

namespace OptiLifts.Tests.Unit.Workouts;

public class CreateWorkoutValidatorTests
{
    private static CreateWorkoutExerciseRequest Exercise(int order, string? groupKey = null, int setCount = 0) =>
        new(Guid.NewGuid(), order, groupKey, Enumerable.Range(0, setCount)
        .Select(i => new CreateWorkoutSetRequest("Normal", 10, 60f, null, null, i, 60)).ToList());

    private static CreateWorkoutGroupRequest Group(string key, string type, int restTime = 60) =>
        new(key, type, restTime);

    private static CreateWorkoutRequest Request(
        IReadOnlyList<CreateWorkoutExerciseRequest> exercises,
        IReadOnlyList<CreateWorkoutGroupRequest> groups) =>
        new(null, "Test Workout", exercises, groups);

    [Fact]
    public void Valid_superset_of_two_has_no_errors()
    {
        var request = Request(
            new[] { Exercise(1, "g1"), Exercise(2, "g1") },
            new[] { Group("g1", "Superset") });

        CreateWorkoutValidator.Validate(request).Should().BeEmpty();
    }

    [Fact]
    public void Valid_circuit_of_three_has_no_errors()
    {
        var request = Request(
            new[] { Exercise(1, "c1"), Exercise(2, "c1"), Exercise(3, "c1") },
            new[] { Group("c1", "Circuit") });

        CreateWorkoutValidator.Validate(request).Should().BeEmpty();
    }

    [Fact]
    public void Superset_with_three_members_gets_rejected()
    {
        var request = Request(
            new[] { Exercise(1, "g1"), Exercise(2, "g1"), Exercise(3, "g1") },
            new[] { Group("g1", "Superset") });

        CreateWorkoutValidator.Validate(request).Should().ContainSingle(e => e.Contains("exactly two"));
    }

    [Fact]
    public void Circuit_with_two_members_gets_rejected()
    {
        var request = Request(
            new[] { Exercise(1, "c1"), Exercise(2, "c1") },
            new[] { Group("c1", "Circuit") });

        CreateWorkoutValidator.Validate(request).Should().ContainSingle(e => e.Contains("3 exercises"));
    }

    [Fact]
    public void Duplicate_group_key_gets_rejected()
    {
        var request = Request(
            new[] { Exercise(1, "g1"), Exercise(2, "g1") },
            new[] { Group("g1", "Superset"), Group("g1", "Superset") });

        CreateWorkoutValidator.Validate(request).Should().ContainSingle(e => e.Contains("Duplicate group key"));
    }

    [Fact]
    public void Invalid_group_type_gets_rejected()
    {
        var request = Request(
            new[] { Exercise(1, "g1"), Exercise(2, "g1") },
            new[] { Group("g1", "Banana") });

        CreateWorkoutValidator.Validate(request).Should().ContainSingle(e => e.Contains("isn't valid"));
    }

    [Fact]
    public void Exercise_referencing_unknown_group_gets_rejected()
    {
        var request = Request(
            new[] { Exercise(1, "ghost") },
            Array.Empty<CreateWorkoutGroupRequest>());

        CreateWorkoutValidator.Validate(request).Should().ContainSingle(e => e.Contains("invalid group key"));
    }

    [Fact]
    public void Standalone_only_exercises_has_no_errors()
    {
        var request = Request(
            new[] { Exercise(1), Exercise(2) },
            Array.Empty<CreateWorkoutGroupRequest>());

        CreateWorkoutValidator.Validate(request).Should().BeEmpty();
    }

    [Fact]
    public void Group_with_mismatched_set_counts_gets_rejected()
    {
        var request = Request(
            new[] { Exercise(1, "g1", setCount: 4), Exercise(2, "g1", setCount: 3) },
            new[] { Group("g1", "Superset") });

        CreateWorkoutValidator.Validate(request)
            .Should().Contain(e => e.Contains("same number of sets"));
    }

    [Fact]
    public void Group_with_equal_set_counts_has_no_errors()
    {
        var request = Request(
            new[] { Exercise(1, "g1", setCount: 5), Exercise(2, "g1", setCount: 5) },
            new[] { Group("g1", "Superset") });

        CreateWorkoutValidator.Validate(request).Should().BeEmpty();
    }

}