using UnityEngine;
using System.Collections;

public class TowerSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Builder builder;         // drag your Builder here
    [SerializeField] private GameObject towerPrefab;  // the tower to spawn
    [SerializeField] private float spawnDelay = 1f;   // <-- new: configurable delay

    private void OnEnable()
    {
        if (builder != null) builder.OnBuildRequested += HandleBuildRequested;
    }

    private void OnDisable()
    {
        if (builder != null) builder.OnBuildRequested -= HandleBuildRequested;
    }

    private void HandleBuildRequested(PlotNode plot)
    {
        if (plot == null || !plot.CanBuild) return;
        if (towerPrefab == null)
        {
            Debug.LogWarning("[TowerSpawner] No towerPrefab assigned.");
            return;
        }

        // Start delayed spawn
        StartCoroutine(SpawnAfterDelay(plot));
    }

    private IEnumerator SpawnAfterDelay(PlotNode plot)
    {
        yield return new WaitForSeconds(spawnDelay);

        if (!plot.CanBuild) yield break; // player might have built another tower in the meantime

        var instance = Instantiate(towerPrefab, plot.buildAnchor.position, plot.buildAnchor.rotation);
        plot.currentTower = instance;
        plot.isBuilt = true;

        // Parent under the plot for tidy hierarchy
        instance.transform.SetParent(plot.transform, worldPositionStays: true);

        // Optionally hide selector
        if (plot.buildAnchor.gameObject.activeSelf)
            plot.buildAnchor.gameObject.SetActive(false);
    }
}

