
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a full, neighbor-only, no-repeat path over PlotNodes for each wave.
/// Starts the next wave's path at the last node visited previously.
/// Auto-collects nodes from a single root Transform in the scene.
/// </summary>
public class PathDirector : MonoBehaviour
{
  [Header("Wiring")]
  [Tooltip("Parent Transform that contains all PlotNodes as children (at any depth).")]
  [SerializeField] private Transform plotsRoot;

  private readonly List<PlotNode> _allPlots = new List<PlotNode>();
  private readonly System.Random _rng = new System.Random();

  private Builder _builder;
  private PlotNode _lastEndNode;           // where the previous wave ended
  private List<PlotNode> _lastPath;        // last path we sent to the builder

  /// <summary>Call once (e.g., from WaveController.Start) to wire the builder and subscribe.</summary>
  public void Init(Builder builder)
  {
    _builder = builder;
    if (_builder != null)
      _builder.OnPathCompleted += HandleBuilderCompleted;

    CollectPlotsFromRoot();
    // If no last node yet, pick a stable start (first plot found)
    if (_lastEndNode == null && _allPlots.Count > 0)
      _lastEndNode = _allPlots[0];

    HideAllSelectorsOnce();
  }

  private void HideAllSelectorsOnce()
  {
    int plots = 0, sels = 0;
    foreach (var plot in _allPlots)
    {
      if (plot == null) continue;
      plots++;

      // Be robust: include inactive, search any depth
      var t = plot.transform;
      var selectors = t.GetComponentsInChildren<Transform>(includeInactive: true);

      foreach (var child in selectors)
      {
        if (child == null) continue;
        // Match by name (case-sensitive) — adjust if yours differ
        if (child.name == "Selector")
        {
          child.gameObject.SetActive(false);
          sels++;
        }
      }
    }
    Debug.Log($"[PathDirector] HideAllSelectorsOnce: plots={plots}, hiddenSelectors={sels}");
  }
  private void OnDestroy()
  {
    if (_builder != null)
      _builder.OnPathCompleted -= HandleBuilderCompleted;
  }

  /// <summary>
  /// Called by WaveController at wave start. Builds a new path (neighbor-only, no-repeats)
  /// starting at the previous wave's end node, and starts the Builder on it.
  /// </summary>
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

    var start = _lastEndNode != null ? _lastEndNode : _allPlots[0];

    // Build a full simple path via randomized DFS from 'start'.
    _lastPath = GenerateFullNeighborPath(start);

    // If you EVER see this warning, your graph is likely disconnected; the builder can't
    // visit nodes in other components without teleporting. Connect your manualNeighbors.
    if (_lastPath.Count < _allPlots.Count)
    {
      Debug.LogWarning($"PathDirector: Only reached {_lastPath.Count}/{_allPlots.Count} nodes from start '{start.name}'. " +
                       $"Check manualNeighbors connectivity.");
    }

    // Convert to transforms and kick off the builder
    var tPath = new List<Transform>(_lastPath.Count);
    foreach (var n in _lastPath) tPath.Add(n.transform);

    _builder.BeginPath(tPath);
  }

  // ---------- Internals ----------

  private void CollectPlotsFromRoot()
  {
    _allPlots.Clear();
    if (plotsRoot == null) return;

    // Collect all PlotNodes under the root (any depth)
    var nodes = plotsRoot.GetComponentsInChildren<PlotNode>(includeInactive: false);
    if (nodes != null) _allPlots.AddRange(nodes);
  }

  private void HandleBuilderCompleted()
  {
    // Use the last node of the last path we sent
    if (_lastPath != null && _lastPath.Count > 0)
      _lastEndNode = _lastPath[_lastPath.Count - 1];
  }

  /// <summary>
  /// Randomized DFS that visits each reachable node exactly once starting at 'start'.
  /// Assumes manualNeighbors define valid adjacency. Does NOT jump across disconnected components.
  /// </summary>
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

      // Randomize neighbor order for variety
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
}
