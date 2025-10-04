using UnityEngine;

public class TowerSpawner : MonoBehaviour
{
  [Header("Refs")]
  [SerializeField] private Builder builder;

  [Tooltip("Ordered by level: [0]=L1.Tower, [1]=L2.Tower, [2]=L3.Tower, ...")]
  [SerializeField] private GameObject[] levelPrefabs;

  [SerializeField] private int[] costs = { 100, 200, 300 }; // same table as the label
  [SerializeField] private int maxLevels = 3;               // convenience

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

    int targetLevel = (!plot.isBuilt || plot.level < 0) ? 0 : plot.level + 1;

    // bounds
    if (targetLevel >= maxLevels) return;
    if (levelPrefabs == null || targetLevel >= levelPrefabs.Length || levelPrefabs[targetLevel] == null) return;

    // === NEW: cost + spend ===
    int cost = (targetLevel >= 0 && targetLevel < costs.Length) ? costs[targetLevel] : 0;
    if (PlayerSession.main != null)
    {
      if (!PlayerSession.main.TrySpendGold(cost))
      {
        // Optional: feedback (shake, sound, flash red)
        Debug.Log("Not enough gold.");
        return;
      }
    }

    if (plot.buildAnchor == null)
    {
      Debug.LogWarning("[TowerSpawner] Plot has no buildAnchor assigned.");
      return;
    }

    // shake, remove old, spawn new (unchanged)
    if (Camera.main != null)
    {
      var shaker = Camera.main.GetComponent<CameraShake>();
      if (shaker != null) shaker.Shake(0f);
    }

    if (plot.isBuilt && plot.currentTower != null)
    {
      Destroy(plot.currentTower);
      plot.currentTower = null;
    }

    var prefab = levelPrefabs[targetLevel];
    var instance = Instantiate(prefab, plot.buildAnchor.position, plot.buildAnchor.rotation);
    instance.transform.SetParent(plot.transform, worldPositionStays: true);

    plot.currentTower = instance;
    plot.isBuilt = true;
    plot.level = targetLevel;

    var label = plot.GetComponentInChildren<SelectorCostLabel>(includeInactive: true);
    if (label != null)
    {
      label.Show(); // or label.Refresh();
    }
  }
}

