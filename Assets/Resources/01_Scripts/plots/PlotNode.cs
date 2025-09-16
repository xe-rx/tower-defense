using System.Collections.Generic;
using UnityEngine;

public class PlotNode : MonoBehaviour
{
    [Tooltip("Assign neighbor nodes manually in the inspector.")]
    public List<PlotNode> manualNeighbors = new List<PlotNode>();
}

