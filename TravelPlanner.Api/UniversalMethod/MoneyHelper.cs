namespace TravelPlanner.Api.UniversalMethod;

public static class MoneyHelper
{
    public static bool IsValid(decimal amount, bool allowZero = false)
    {
        return (allowZero ? amount >= 0 : amount > 0)
            && amount <= 9999999999.99m
            && decimal.Round(amount, 2) == amount;
    }

    public static Dictionary<int, decimal> SplitEqually(decimal amount, List<int> userIds)
    {
        if (!IsValid(amount) || userIds.Count == 0 || userIds.Distinct().Count() != userIds.Count)
            throw new ArgumentException("Проверьте сумму и список участников.");

        var sortedIds = userIds.OrderBy(id => id).ToList();
        var totalCents = (long)(amount * 100);
        var centsPerUser = totalCents / sortedIds.Count;
        var remainder = totalCents % sortedIds.Count;
        var shares = new Dictionary<int, decimal>();

        for (var index = 0; index < sortedIds.Count; index++)
        {
            var cents = centsPerUser + (index < remainder ? 1 : 0);
            shares.Add(sortedIds[index], cents / 100m);
        }

        return shares;
    }
}
