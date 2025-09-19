using UnityEngine;

public class TowerSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Builder builder;

    [Tooltip("Ordered by level: [0]=L1.Tower, [1]=L2.Tower, [2]=L3.Tower, ...")]
    [SerializeField] private GameObject[] levelPrefabs;

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
        if (plot == null) return;

        if (levelPrefabs == null || levelPrefabs.Length == 0 || levelPrefabs[0] == null)
        {
            Debug.LogWarning("[TowerSpawner] levelPrefabs not set correctly (need at least L1 at index 0).");
            return;
        }

        // Decide target level: build L1 if empty, else upgrade to next if possible
        int targetLevel = (!plot.isBuilt || plot.level < 0) ? 0 : plot.level + 1;

        if (targetLevel >= levelPrefabs.Length || levelPrefabs[targetLevel] == null)
        {
            // Already max level or missing prefab for next level
            return;
        }

        if (plot.buildAnchor == null)
        {
            Debug.LogWarning("[TowerSpawner] Plot has no buildAnchor assigned.");
            return;
        }

        // SCREEN SHAKE immediately at impact (no delay, no coroutine)
        if (Camera.main != null)
        {
            var shaker = Camera.main.GetComponent<CameraShake>();
            if (shaker != null) shaker.Shake(0f);
        }

        // Upgrade: remove old tower
        if (plot.isBuilt && plot.currentTower != null)
        {
            Destroy(plot.currentTower);
            plot.currentTower = null;
        }

        // Spawn / Upgrade instantly
        var prefab = levelPrefabs[targetLevel];
        var instance = Instantiate(prefab, plot.buildAnchor.position, plot.buildAnchor.rotation);
        instance.transform.SetParent(plot.transform, worldPositionStays: true);

        // Update state
        plot.currentTower = instance;
        plot.isBuilt = true;
        plot.level = targetLevel;
    }
}

