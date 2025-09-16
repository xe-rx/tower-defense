
using System.Collections.Generic;
using UnityEngine;

public class PlotGraph : MonoBehaviour
{
    [SerializeField] private Transform plotsRoot;

    private readonly List<PlotNode> _nodes = new();
    private readonly Dictionary<PlotNode, List<PlotNode>> _adj = new();

    public IReadOnlyList<PlotNode> Nodes => _nodes;
    public IReadOnlyDictionary<PlotNode, List<PlotNode>> Adjacency => _adj;

    private void Awake() => Rebuild();
    private void OnValidate() { if (plotsRoot) Rebuild(); }

    public void Rebuild()
    {
        _nodes.Clear();
        _adj.Clear();
        if (!plotsRoot) return;

        plotsRoot.GetComponentsInChildren(true, _nodes);

        // Build adjacency list strictly from manualNeighbors
        foreach (var n in _nodes)
            _adj[n] = new List<PlotNode>();

        foreach (var n in _nodes)
        {
            foreach (var m in n.manualNeighbors)
            {
                if (m == null || m == n) continue;

                if (!_adj[n].Contains(m))
                    _adj[n].Add(m);

                if (!_adj[m].Contains(n))
                    _adj[m].Add(n); // enforce bidirectional link
            }
        }

#if UNITY_EDITOR
        int isolated = 0;
        foreach (var kv in _adj)
            if (kv.Value.Count == 0) isolated++;
        if (isolated > 0)
            Debug.LogWarning($"[PlotGraph] {isolated} plot(s) have no neighbors assigned.");
#endif
    }

    public List<Transform> ToTransformPath(List<PlotNode> nodePath)
    {
        var list = new List<Transform>(nodePath.Count);
        foreach (var n in nodePath)
            list.Add(n.transform);
        return list;
    }
}

