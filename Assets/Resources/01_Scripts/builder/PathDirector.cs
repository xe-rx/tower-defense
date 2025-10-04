using System.Collections.Generic;
using UnityEngine;

public class PathDirector : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Parent Transform that contains all PlotNodes as children (at any depth).")]
    [SerializeField] private Transform plotsRoot;

    // --- Sign config ---
    [Header("Sign Rendering")]
    [Tooltip("Child name under each PlotNode that holds the SpriteRenderer for the sign.")]
    [SerializeField] private string signChildName = "Sign";
    [Tooltip("Sprites for numbers 1..N. Index 0 should be '1', index 1 '2', etc.")]
    [SerializeField] private Sprite[] numberSprites;
    [Tooltip("Sprite used when a plot isn't in the current path (blank/base sign). Optional.")]
    [SerializeField] private Sprite defaultSprite;

    private readonly List<PlotNode> _allPlots = new List<PlotNode>();
    private readonly System.Random _rng = new System.Random();

    private Builder _builder;
    private PlotNode _lastEndNode;
    private List<PlotNode> _lastPath;

    /// <summary>Call once (e.g., from WaveController.Start) to wire the builder and subscribe.</summary>
    public void Init(Builder builder)
    {
        _builder = builder;
        if (_builder != null)
            _builder.OnPathCompleted += HandleBuilderCompleted;

        CollectPlotsFromRoot();

        // Pick a stable start if none yet
        if (_lastEndNode == null && _allPlots.Count > 0)
            _lastEndNode = _allPlots[0];

        // Ensure scene starts with selectors hidden and signs cleared
        HideAllSelectorsOnce();
        ApplyPathNumbers(null);
    }

    private void OnDestroy()
    {
        if (_builder != null)
            _builder.OnPathCompleted -= HandleBuilderCompleted;
    }

    /// <summary>Called by WaveController at wave start. Builds a new path and starts the Builder.</summary>
    public void BeginWavePath()
    {
        if (_builder == null)
        {
            Debug.LogWarning("PathDirector: Builder not assigned.");
            return;
        }

        if (_allPlots.Count == 0)
        {
            CollectPlotsFromRoot();
            if (_allPlots.Count == 0)
            {
                Debug.LogError("PathDirector: No PlotNodes found under the assigned root.");
                return;
            }
        }

        // Make sure no selectors are visible before dwell begins
        HideAllSelectorsOnce();

        var start = _lastEndNode != null ? _lastEndNode : _allPlots[0];

        _lastPath = GenerateFullNeighborPath(start);

        if (_lastPath.Count < _allPlots.Count)
        {
            Debug.LogWarning($"PathDirector: Only reached {_lastPath.Count}/{_allPlots.Count} nodes from start '{start.name}'. " +
                             $"Check manualNeighbors connectivity.");
        }

        // Paint signs for this wave path
        ApplyPathNumbers(_lastPath);

        // Kick off builder
        var tPath = new List<Transform>(_lastPath.Count);
        foreach (var n in _lastPath) tPath.Add(n.transform);
        _builder.BeginPath(tPath);
    }

    // ---------- Internals ----------

    private void CollectPlotsFromRoot()
    {
        _allPlots.Clear();
        if (plotsRoot == null) return;

        var nodes = plotsRoot.GetComponentsInChildren<PlotNode>(includeInactive: true);
        if (nodes != null) _allPlots.AddRange(nodes);
    }

    private void HandleBuilderCompleted()
    {
        if (_lastPath != null && _lastPath.Count > 0)
            _lastEndNode = _lastPath[_lastPath.Count - 1];
    }

    private List<PlotNode> GenerateFullNeighborPath(PlotNode start)
    {
        var path = new List<PlotNode>(_allPlots.Count);
        var visited = new HashSet<PlotNode>();
        if (start == null) return path;

        void DFS(PlotNode node)
        {
            if (node == null || visited.Contains(node)) return;
            visited.Add(node);
            path.Add(node);

            var neigh = node.manualNeighbors ?? new List<PlotNode>();
            int n = neigh.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = neigh[i]; neigh[i] = neigh[j]; neigh[j] = tmp;
            }

            foreach (var next in neigh)
                if (next != null && !visited.Contains(next))
                    DFS(next);
        }

        DFS(start);
        return path;
    }

    // ---------- Sign painting ----------

    /// <summary>
    /// Paints all signs according to path order. If path is null/empty, clears to defaultSprite.
    /// Assumes numberSprites[0] corresponds to '1', [1] to '2', etc.
    /// </summary>
    private void ApplyPathNumbers(List<PlotNode> path)
    {
        // Build a quick index map: plot -> order (0-based)
        Dictionary<PlotNode, int> order = null;
        if (path != null && path.Count > 0)
        {
            order = new Dictionary<PlotNode, int>(path.Count);
            for (int i = 0; i < path.Count; i++)
                if (path[i] != null) order[path[i]] = i;
        }

        // Paint all signs
        foreach (var plot in _allPlots)
        {
            if (plot == null) continue;

            var signRenderer = FindSignRenderer(plot.transform, signChildName);
            if (signRenderer == null) continue;

            if (order != null && order.TryGetValue(plot, out int idx))
            {
                // convert 0-based to 1-based sprite index
                int spriteIndex = Mathf.Clamp(idx, 0, (numberSprites?.Length ?? 0) - 1);
                if (numberSprites != null && spriteIndex < numberSprites.Length && numberSprites[spriteIndex] != null)
                    signRenderer.sprite = numberSprites[spriteIndex];
                else
                    signRenderer.sprite = defaultSprite; // fallback if missing sprite
            }
            else
            {
                // not in path (or no path) → default
                signRenderer.sprite = defaultSprite;
            }
        }
    }

    private SpriteRenderer FindSignRenderer(Transform plotTransform, string childName)
    {
        if (plotTransform == null) return null;

        // Prefer an exact child named `childName`
        var sign = plotTransform.Find(childName);
        if (sign != null)
        {
            var sr = sign.GetComponent<SpriteRenderer>();
            if (sr != null) return sr;
        }

        // Fallback: any SpriteRenderer under the plot (include inactive)
        var srs = plotTransform.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        foreach (var sr in srs)
        {
            if (sr != null && sr.gameObject.name == childName) return sr;
        }
        return null;
    }

    // ---------- Selector hiding ----------

    /// <summary>
    /// Ensures all plot 'Selector' children are hidden. Safe to call multiple times.
    /// </summary>
    private void HideAllSelectorsOnce()
    {
        foreach (var plot in _allPlots)
        {
            if (!plot) continue;
            var sel = plot.transform.Find("Selector");
            if (sel && sel.gameObject.activeSelf)
                sel.gameObject.SetActive(false);
        }
    }
}

