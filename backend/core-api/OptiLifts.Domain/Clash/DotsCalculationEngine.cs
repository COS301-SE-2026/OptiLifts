using System;

namespace OptiLifts.Domain.Clash;

public static class DotsCalculationEngine
{
    private readonly record struct DotsCoeffs(
        double A, double B, double C, double D, double E,
        float MinBodyweightKg, float MaxBodyweightKg);

    private static readonly DotsCoeffs MenCoeffs = new(
        A: -0.0000010930,
        B: 0.0007391293,
        C: -0.1918759221,
        D: 24.0900756,
        E: -307.75076,
        MinBodyweightKg: 40.0f,
        MaxBodyweightKg: 210.0f);

    private static readonly DotsCoeffs WomenCoeffs = new(
        A: -0.0000010706,
        B: 0.0005158568,
        C: -0.1126655495,
        D: 13.6175032,
        E: -57.96288,
        MinBodyweightKg: 40.0f,
        MaxBodyweightKg: 150.0f);

    public static float CalculateDots(float totalWeightKg, float bodyweightKg, string gender)
    {
        if (totalWeightKg <= 0f || bodyweightKg <= 0f)
        {
            return 0f;
        }

        var male = string.Equals(gender, "Male", StringComparison.OrdinalIgnoreCase);
        var Coeffs = male ? MenCoeffs : WomenCoeffs;

        var b = Math.Clamp(bodyweightKg, Coeffs.MinBodyweightKg, Coeffs.MaxBodyweightKg);

        var denom =
            Coeffs.A * Math.Pow(b, 4) +
            Coeffs.B * Math.Pow(b, 3) +
            Coeffs.C * Math.Pow(b, 2) +
            Coeffs.D * b +
            Coeffs.E;

        if (denom <= 0)
        {
            return 0f;
        }

        return (float)(totalWeightKg * (500.0 / denom));
    }
}
