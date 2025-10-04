using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class SelectorCostLabel : MonoBehaviour
{
  [Header("Config")]
  [SerializeField] private int[] towerCosts = { 100, 200, 300 };

  [Header("Wiring")]
  [SerializeField] private PlotNode plot;          // auto-found
  [SerializeField] private TextMeshPro tmp;        // auto-found

  [Header("Icon (auto)")]
  [SerializeField] private string spriteAssetResourcePath = "00_Art/_UI/StaticCoinAsset"; // Resources/StaticCoinAsset.asset
  [SerializeField] private string iconName = "coin";                            // glyph name inside the sprite asset

  private static TMP_SpriteAsset sIconAsset; // cached once for all labels

  private void Reset()
  {
    tmp = GetComponent<TextMeshPro>();
    plot = GetComponentInParent<PlotNode>();
  }

  private void Awake()
  {
    if (!tmp) tmp = GetComponent<TextMeshPro>();
    if (!plot) plot = GetComponentInParent<PlotNode>();

    // Load and cache the sprite asset once
    if (sIconAsset == null)
    {
      sIconAsset = Resources.Load<TMP_SpriteAsset>(spriteAssetResourcePath);
      if (sIconAsset == null)
        Debug.LogWarning($"[SelectorCostLabel] TMP SpriteAsset not found at Resources/{spriteAssetResourcePath}. " +
                         $"Icon will not render until this is fixed.");
    }

    if (tmp != null)
    {
      if (sIconAsset != null) tmp.spriteAsset = sIconAsset;
      tmp.gameObject.SetActive(false); // start hidden
    }
  }

  public void Show()
  {
    if (!tmp || !plot) return;

    int targetLevel = (!plot.isBuilt || plot.level < 0) ? 0 : plot.level + 1;

    if (targetLevel >= towerCosts.Length)
    {
      tmp.text = (sIconAsset != null)
          ? $"MAX<space=0.02em><size=90%><sprite name={iconName}></size>"
          : "MAX";
    }
    else
    {
      int cost = towerCosts[targetLevel];
      tmp.text = (sIconAsset != null)
          ? $"{cost}<space=0.02em><size=90%><sprite name={iconName}></size>"
          : cost.ToString();
    }

    tmp.gameObject.SetActive(true);
  }

  public void Hide()
  {
    if (tmp) tmp.gameObject.SetActive(false);
  }

  public void Refresh()
  {
    Show(); // recompute based on current plot.level
  }
}

