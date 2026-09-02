using System;
using System.Collections.Generic;
using System.Linq;

namespace OptiLifts.Domain.ProgressiveOverload;

public static class BestFitEngine
{

    private const double predictionCap = 1.10; //10%
    private const double minGrowthFactor = 1.02; //2% floor so a flat trend still nudges forward

    //get line using OLS linear regression
    public static (double m, double c) GetBestFitLine(List<PODataPoint> points)
    {

        var firstD = points[0].Date;
        int n = points.Count;

        double sumX = 0;
        double sumY = 0;
        double sumX2 = 0;
        double sumXY = 0;

        foreach (var point in points)
        {
            double x = (point.Date - firstD).TotalDays;
            double y = point.Metric;

            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;

        }

        double denominator = (n * sumX2) - (sumX * sumX);
        if (denominator == 0)
        {
            return (0, sumY / n);
        }
        double m = ((n * sumXY) - (sumX * sumY)) / denominator;
        double c = (sumY - (m * sumX)) / n;
        return (m, c);
    }

    public static bool PlateauCheck(List<PODataPoint> points)
    {
        var recentPoints = points.TakeLast(4).ToList();

        var (m, _) = GetBestFitLine(recentPoints);
        return m <= 0.1;
    }

    public static double PredictNextVal(List<PODataPoint> points)
    {
        var (m, c) = GetBestFitLine(points);

        double gapTotal = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            gapTotal += (points[i].Date - points[i + 1].Date).TotalDays;
        }
        int avgGap = (int)Math.Round(gapTotal / (points.Count - 1));

        var lastP = points[0];
        double x = avgGap;
        var y = (m * x) + c;

        double flooredY = Math.Max(y, lastP.Metric * minGrowthFactor);

        double cappedY = lastP.Metric * predictionCap;
        return Math.Min(flooredY, cappedY);

    }
}