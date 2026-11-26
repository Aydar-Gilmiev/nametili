using TravelPlanner.Api.Responses;

namespace TravelPlanner.Api.UniversalMethod;

public static class SettlementHelper
{
    // Суммы считаем в копейках. Подсказки не создают возвраты и не меняют БД.
    public static SettlementResponse Calculate(List<BalanceResponse> members)
    {
        var people = members.Where(member => member.Balance != 0).OrderBy(member => member.UserId).ToList();
        var amounts = people.Select(member => checked((long)(member.Balance * 100))).ToArray();
        if (amounts.Sum() != 0) throw new InvalidOperationException("Сумма балансов должна быть равна нулю.");
        var best = Greedy((long[])amounts.Clone());
        var path = new List<(int From, int To, long Amount)>();
        var visited = new Dictionary<string, int>();
        var nodes = 0;
        var lowerBound = Math.Max(amounts.Count(value => value < 0), amounts.Count(value => value > 0));
        var stopped = people.Count > 16 && best.Count > lowerBound;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        if (best.Count > lowerBound && !stopped) Search();
        return new SettlementResponse
        {
            IsOptimal = !stopped || best.Count == lowerBound,
            Transfers = best.Select(value => new TransferResponse
            {
                FromUserId = people[value.From].UserId, FromName = people[value.From].Name,
                ToUserId = people[value.To].UserId, ToName = people[value.To].Name,
                Amount = value.Amount / 100m
            }).ToList()
        };

        void Search()
        {
            if (++nodes > 200_000 || (nodes % 1024 == 0 && timer.ElapsedMilliseconds > 250)) { stopped = true; return; }
            var remaining = Math.Max(amounts.Count(value => value < 0), amounts.Count(value => value > 0));
            if (remaining == 0) { best = path.ToList(); return; }
            if (path.Count + remaining >= best.Count || best.Count == lowerBound) return;
            var key = string.Join(",", amounts.Where(value => value != 0).OrderBy(value => value));
            if (visited.TryGetValue(key, out var previous) && previous <= path.Count) return;
            visited[key] = path.Count;
            var tried = new HashSet<(long, long)>();
            for (var from = 0; from < amounts.Length && !stopped; from++)
            {
                if (amounts[from] >= 0) continue;
                for (var to = 0; to < amounts.Length && !stopped; to++)
                {
                    if (amounts[to] <= 0 || !tried.Add((amounts[from], amounts[to]))) continue;
                    var amount = Math.Min(-amounts[from], amounts[to]);
                    amounts[from] += amount; amounts[to] -= amount;
                    path.Add((from, to, amount));
                    Search();
                    path.RemoveAt(path.Count - 1);
                    amounts[from] -= amount; amounts[to] += amount;
                    if (best.Count == lowerBound) return;
                }
            }
        }
    }

    private static List<(int From, int To, long Amount)> Greedy(long[] amounts)
    {
        var result = new List<(int, int, long)>();
        while (amounts.Any(value => value < 0))
        {
            var from = Array.IndexOf(amounts, amounts.Min());
            var to = Array.IndexOf(amounts, amounts.Max());
            var amount = Math.Min(-amounts[from], amounts[to]);
            result.Add((from, to, amount));
            amounts[from] += amount; amounts[to] -= amount;
        }
        return result;
    }
}
