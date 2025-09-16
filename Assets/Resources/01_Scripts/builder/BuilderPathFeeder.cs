using System.Collections.Generic;
using UnityEngine;

public class BuilderPathFeeder : MonoBehaviour
{
    [SerializeField] private Builder builder;
    [SerializeField] private PlotGraph graph;

    [Header("Run")]
    [SerializeField] private bool startOnPlay = true;
    [SerializeField] private int retries = 100;
    [SerializeField] private int seed = 0;     // 0 = random by time, nonzero = reproducible

    [Header("Builder Overrides (optional)")]
    [SerializeField] private float speedOverride = -1f;  // <0 = don't override
    [SerializeField] private float dwellOverride = -1f;  // <0 = don't override

    private void Start()
    {
        if (startOnPlay) StartBuilderRun();
    }

    public void StartBuilderRun()
    {
        if (builder == null || graph == null)
        {
            Debug.LogWarning("[BuilderPathFeeder] Missing references.");
            return;
        }

        var nodes = graph.Nodes;
        var adj = graph.Adjacency;
        if (nodes == null || nodes.Count == 0)
        {
            Debug.LogWarning("[BuilderPathFeeder] No nodes in graph.");
            return;
        }

        int? actualSeed = seed != 0 ? seed : (int?)null;
        List<PlotNode> nodePath = PathGenerator.GeneratePathCoverAll(nodes, adj, retries, actualSeed);
        if (nodePath == null || nodePath.Count == 0)
        {
            Debug.LogWarning("[BuilderPathFeeder] Path generation failed.");
            return;
        }

        var transformPath = graph.ToTransformPath(nodePath);

        float? spd = speedOverride >= 0f ? speedOverride : (float?)null;
        float? dwt = dwellOverride >= 0f ? dwellOverride : (float?)null;

        builder.BeginPath(transformPath, spd, dwt);
    }
}

