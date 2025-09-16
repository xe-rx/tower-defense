using System.Collections.Generic;
using UnityEngine;

public class PlotRegistry : MonoBehaviour
{
    [SerializeField] private Transform plotsRoot;

    public IReadOnlyList<Transform> AllPlots => _allPlots;
    private readonly List<Transform> _allPlots = new();

    private void Awake() => Refresh();
    private void OnValidate() { if (plotsRoot) Refresh(); }

    public void Refresh()
    {
        _allPlots.Clear();
        if (!plotsRoot) return;

        // Collect all direct children as plots (change to GetComponentsInChildren if you nest deeper)
        for (int i = 0; i < plotsRoot.childCount; i++)
        {
            var child = plotsRoot.GetChild(i);
            if (child.gameObject.activeInHierarchy)
                _allPlots.Add(child);
        }
    }
}

