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
    public bool isBuilt;
    public GameObject currentTower;

    public bool CanBuild => !isBuilt && buildAnchor != null;
}

