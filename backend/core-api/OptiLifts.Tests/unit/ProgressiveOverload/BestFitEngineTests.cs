using FluentAssertions;
using OptiLifts.Domain.ProgressiveOverload;

namespace OptiLifts.Tests.ProgressiveOverload;

public class BestFitEngineTests
{
    [Fact]
    public void GetBestFitLine_LinearData_ReturnsExpectedSlopeAndIntercept()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var points = new List<PODataPoint>
        {
            new(start, 10d),
            new(start.AddDays(1), 12d),
            new(start.AddDays(2), 14d)
        };

        var (slope, intercept) = BestFitEngine.GetBestFitLine(points);

        slope.Should().BeApproximately(2d, 0.0001d);
        intercept.Should().BeApproximately(10d, 0.0001d);
    }

    [Fact]
    public void GetBestFitLine_AllDatesEqual_ReturnsAverageMetricAndZeroSlope()
    {
        var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var points = new List<PODataPoint>
        {
            new(date, 10d),
            new(date, 20d),
            new(date, 30d)
        };

        var (slope, intercept) = BestFitEngine.GetBestFitLine(points);

        slope.Should().Be(0d);
        intercept.Should().Be(20d);
    }

    [Fact]
    public void PlateauCheck_FlatRecentTrend_ReturnsTrue()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var points = Enumerable.Range(0, 4)
            .Select(day => new PODataPoint(start.AddDays(day), 100d))
            .ToList();

        var result = BestFitEngine.PlateauCheck(points);

        result.Should().BeTrue();
    }

    [Fact]
    public void PlateauCheck_GrowingRecentTrend_ReturnsFalse()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var points = Enumerable.Range(0, 4)
            .Select(day => new PODataPoint(start.AddDays(day), 100d + day))
            .ToList();

        var result = BestFitEngine.PlateauCheck(points);

        result.Should().BeFalse();
    }

    [Fact]
    public void PredictNextVal_PredictionExceedsTenPercentCap_ReturnsCappedValue()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var points = new List<PODataPoint>
        {
            new(start.AddDays(21), 160d),
            new(start.AddDays(14), 140d),
            new(start.AddDays(7), 120d),
            new(start, 100d)
        };

        var result = BestFitEngine.PredictNextVal(points);

        result.Should().BeApproximately(176d, 0.0001d);
    }

    [Fact]
    public void PredictNextVal_PredictionBelowCap_AppliesMinimumGrowthFloor()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var points = new List<PODataPoint>
        {
            new(start.AddDays(21), 103d),
            new(start.AddDays(14), 102d),
            new(start.AddDays(7), 101d),
            new(start, 100d)
        };

        var result = BestFitEngine.PredictNextVal(points);

        result.Should().BeApproximately(105.06d, 0.0001d);
    }
}