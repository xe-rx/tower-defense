
using UnityEngine;

public class TowerSpawner : MonoBehaviour
{
  [Header("Refs")]
  [SerializeField] private Builder builder;

  [Tooltip("Ordered by level: [0]=L1.Tower, [1]=L2.Tower, [2]=L3.Tower, ...")]
  [SerializeField] private GameObject[] levelPrefabs;

  [SerializeField] private int[] costs = { 100, 200, 300 }; // same table as the label
  [SerializeField] private int maxLevels = 3;               // convenience

  // --- NEW: minimal victory helpers ---
  private bool AllPlotsMaxed()
  {
    // Level 3 == index 2 when maxLevels = 3, so check level >= maxLevels - 1
    var plots = FindObjectsByType<PlotNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    if (plots == null || plots.Length == 0) return false;

    int maxIndex = Mathf.Max(0, maxLevels - 1);
    for (int i = 0; i < plots.Length; i++)
    {
      var p = plots[i];
      if (!p || !p.isBuilt || p.level < maxIndex)
        return false;
    }
    return true;
  }

  private void WinNow(string reason)
  {
    var wc = FindFirstObjectByType<WaveController>();
    if (wc != null) wc.SetGameOver();              // stop wave logic/state machine
    if (GameScreens.main) GameScreens.main.ShowWin();
    Debug.Log($"[WIN] {reason}");
  }
  // --- END NEW ---

  private void OnEnable()
  {
    if (builder == null) return;

    builder.OnBuildRequested += HandleBuildRequested;

    // simple one-liner version of the precheck
    builder.CanStartBuild = (plot) =>
    {
      if (plot == null) return false;

      int targetLevel = (!plot.isBuilt || plot.level < 0) ? 0 : plot.level + 1;
      if (targetLevel >= maxLevels) return false;

      int cost = (targetLevel >= 0 && targetLevel < costs.Length) ? costs[targetLevel] : 0;
      return PlayerSession.main != null && PlayerSession.main.Gold >= cost;
    };
  }


  private void OnDisable()
  {
    if (builder != null) builder.OnBuildRequested -= HandleBuildRequested;

    if (builder != null) builder.CanStartBuild = null;
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
        if (builder != null) builder.RefuseFlash(1f);

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

    // --- NEW: one-liner victory check after a successful upgrade/build ---
    if (AllPlotsMaxed()) WinNow("All buildings are Level 3");
  }
}

