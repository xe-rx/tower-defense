using System.Collections.Generic;
using UnityEngine;

public static class PathGenerator
{
    /// <summary>
    /// Generate a neighbor-only, no-revisit path. Tries to visit all nodes.
    /// Returns the longest found if full coverage isn't reachable.
    /// </summary>
    public static List<PlotNode> GeneratePathCoverAll(
        IReadOnlyList<PlotNode> nodes,
        IReadOnlyDictionary<PlotNode, List<PlotNode>> adj,
        int retries = 50,
        int? seed = null)
    {
        var rng = (seed.HasValue ? new System.Random(seed.Value) : new System.Random());
        var best = new List<PlotNode>();

        for (int attempt = 0; attempt < retries; attempt++)
        {
            // start from a random node each attempt
            var start = nodes[rng.Next(nodes.Count)];
            var visited = new HashSet<PlotNode>();
            var path = new List<PlotNode>();

            if (TryGrowPathDFS(start, null, nodes.Count, adj, visited, path, rng))
            {
                // full coverage
                return path;
            }

            if (path.Count > best.Count) best = new List<PlotNode>(path);
        }

        return best; // longest we found
    }

    private static bool TryGrowPathDFS(
        PlotNode current,
        PlotNode previous,
        int targetLen,
        IReadOnlyDictionary<PlotNode, List<PlotNode>> adj,
        HashSet<PlotNode> visited,
        List<PlotNode> path,
        System.Random rng)
    {
        visited.Add(current);
        path.Add(current);

        if (path.Count >= targetLen)
            return true; // visited everyone

        // build shuffled neighbor order, preferring unvisited
        var neighbors = adj[current];
        var order = new List<PlotNode>(neighbors);
        Shuffle(order, rng);

        // try unvisited first
        foreach (var n in order)
        {
            if (visited.Contains(n)) continue;
            if (TryGrowPathDFS(n, current, targetLen, adj, visited, path, rng))
                return true;
        }

        // Optionally, allow one-step backtrack to escape dead-ends (already implicit via recursion)
        // Backtrack
        visited.Remove(current);
        path.RemoveAt(path.Count - 1);
        return false;
    }

    private static void Shuffle<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

