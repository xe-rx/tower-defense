using System.Collections.Generic;
using UnityEngine;

public class PlotGraph : MonoBehaviour
{
    [Header("Build Mode")]
    [SerializeField] private bool useManualNeighbors = false;

    [Header("Proximity Mode (when manual is OFF)")]
    [Tooltip("Max distance to consider two plots neighbors.")]
    [SerializeField] private float neighborMaxDistance = 3.0f;
    [Tooltip("Two plots must be on same X or same Y within this tolerance to be neighbors (axis-only).")]
    [SerializeField] private float axisTolerance = 0.01f;

    [Header("Source of nodes")]
    [SerializeField] private Transform plotsRoot;

    private readonly List<PlotNode> _nodes = new();
    private readonly Dictionary<PlotNode, List<PlotNode>> _adj = new();

    public IReadOnlyList<PlotNode> Nodes => _nodes;
    public IReadOnlyDictionary<PlotNode, List<PlotNode>> Adjacency => _adj;

    private void Awake() => Rebuild();
    private void OnValidate() { if (plotsRoot != null) Rebuild(); }

    public void Rebuild()
    {
        _nodes.Clear();
        _adj.Clear();
        if (!plotsRoot) return;

        plotsRoot.GetComponentsInChildren(true, _nodes);

        // Initialize adjacency lists
        foreach (var n in _nodes) _adj[n] = new List<PlotNode>();

        if (useManualNeighbors)
        {
            // Respect inspector wiring
            foreach (var n in _nodes)
            {
                foreach (var m in n.manualNeighbors)
                {
                    if (m == null || m == n) continue;
                    if (!_adj[n].Contains(m)) _adj[n].Add(m);
                    if (!_adj[m].Contains(n)) _adj[m].Add(n);
                }
            }
        }
        else
        {
            // Auto-build by proximity + axis rule (same X or same Y)
            for (int i = 0; i < _nodes.Count; i++)
            {
                var a = _nodes[i];
                Vector2 pa = a.transform.position;
                for (int j = i + 1; j < _nodes.Count; j++)
                {
                    var b = _nodes[j];
                    Vector2 pb = b.transform.position;

                    bool axisAligned =
                        Mathf.Abs(pa.x - pb.x) <= axisTolerance ||
                        Mathf.Abs(pa.y - pb.y) <= axisTolerance;

                    if (!axisAligned) continue;

                    float dist = Vector2.Distance(pa, pb);
                    if (dist <= neighborMaxDistance)
                    {
                        _adj[a].Add(b);
                        _adj[b].Add(a);
                    }
                }
            }
        }

#if UNITY_EDITOR
        // Quick sanity logs
        int isolated = 0;
        foreach (var kv in _adj) if (kv.Value.Count == 0) isolated++;
        if (isolated > 0) Debug.LogWarning($"[PlotGraph] Found {isolated} isolated plots (no neighbors).");
#endif
    }

    // Helper to expose transforms in order
    public List<Transform> ToTransformPath(List<PlotNode> nodePath)
    {
        var list = new List<Transform>(nodePath.Count);
        foreach (var n in nodePath) list.Add(n.transform);
        return list;
    }
}

