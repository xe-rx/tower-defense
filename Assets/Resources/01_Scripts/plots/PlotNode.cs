using System.Collections.Generic;
using UnityEngine;

public class PlotNode : MonoBehaviour
{
    [Tooltip("Assign neighbor nodes manually in the inspector.")]
    public List<PlotNode> manualNeighbors = new List<PlotNode>();

    [Header("Build Anchor")]
    [Tooltip("Drag your 'Selector' child here (exact spawn point).")]
    public Transform buildAnchor;

    [Header("Runtime State")]
    public bool isBuilt;                 // kept for backward compatibility
    public GameObject currentTower;

    [Tooltip("-1 = empty, 0 = L1, 1 = L2, 2 = L3, ...")]
    public int level = -1;

    // kept as-is, but no longer used by spawner for upgrades
    public bool CanBuild => !isBuilt && buildAnchor != null;

    public bool CanUpgrade(int maxLevels)
    {
        return isBuilt && (level + 1) < maxLevels;
    }
}

