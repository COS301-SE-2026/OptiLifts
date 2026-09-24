namespace OptiLifts.Domain.Clash;

public static class WeightClassBrackets
{
    public static bool TryGetRange(string bracketId, out decimal minExclus, out decimal maxInclus)
    {
        switch (bracketId)
        {
            case "u59":
                minExclus = 0m; maxInclus = 59.00m; return true;
            case "u66":
                minExclus = 59.00m; maxInclus = 66.00m; return true;
            case "u74":
                minExclus = 66.00m; maxInclus = 74.00m; return true;
            case "u83":
                minExclus = 74.00m; maxInclus = 83.00m; return true;
            case "u93":
                minExclus = 83.00m; maxInclus = 93.00m; return true;
            case "u105":
                minExclus = 93.00m; maxInclus = 105.00m; return true;
            case "u120":
                minExclus = 105.00m; maxInclus = 120.00m; return true;
            case "120p":
                minExclus = 120.00m; maxInclus = decimal.MaxValue; return true;
            default:
                minExclus = 0m; maxInclus = 0m; return false;
        }
    }
}
