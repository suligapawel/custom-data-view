namespace CustomDataView.Domain.Services;

public static class BindingGraph
{
    public static BindingGraphOrder Order(IReadOnlyList<string> nodes, IEnumerable<(string From, string To)> edges)
    {
        var known = nodes.ToHashSet(StringComparer.Ordinal);
        var distinct = edges
            .Where(edge => known.Contains(edge.From) && known.Contains(edge.To) && edge.From != edge.To)
            .Distinct()
            .ToList();
        var incoming = nodes.ToDictionary(node => node, _ => 0, StringComparer.Ordinal);
        foreach (var edge in distinct)
        {
            incoming[edge.To]++;
        }

        var waves = new List<IReadOnlyList<string>>();
        var wave = nodes.Where(node => incoming[node] == 0).ToList();
        while (wave.Count > 0)
        {
            waves.Add(wave);
            var next = new List<string>();
            foreach (var edge in distinct.Where(edge => wave.Contains(edge.From)))
            {
                if (--incoming[edge.To] == 0)
                {
                    next.Add(edge.To);
                }
            }

            wave = nodes.Where(next.Contains).ToList();
        }

        var ordered = waves.SelectMany(nodesInWave => nodesInWave).ToHashSet(StringComparer.Ordinal);
        return new BindingGraphOrder(waves, nodes.Where(node => !ordered.Contains(node)).ToList());
    }
}

public sealed record BindingGraphOrder(IReadOnlyList<IReadOnlyList<string>> Waves, IReadOnlyList<string> Cycle);
