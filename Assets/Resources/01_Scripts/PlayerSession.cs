using TMPro;
using UnityEngine;

public class PlayerSession : MonoBehaviour
{
  public static PlayerSession main;

  [Header("Config")]
  [SerializeField] private int startingHealth = 20;
  [SerializeField] private int startingGold = 0;

  [Header("UI")]
  [SerializeField] private TextMeshProUGUI healthText;
  [SerializeField] private TextMeshProUGUI goldText;

  public int Health { get; private set; }
  public int Gold { get; private set; }

  // 🔔 NEW: event so selectors & UI can listen for gold changes
  public event System.Action OnGoldChanged;

  void Awake()
  {
    main = this;
  }

  void Start()
  {
    Health = startingHealth;
    Gold = startingGold;
    UpdateUI();
  }

  public void ApplyDamage(int amount)
  {
    Health -= amount;
    if (Health < 0) Health = 0;
    UpdateHealthUI();

    if (Health <= 0)
    {
      WaveController wc = FindFirstObjectByType<WaveController>();
      if (wc != null) wc.SetGameOver();

      // NEW: show Game Over UI
      if (GameScreens.main) GameScreens.main.ShowGameOver();

      Debug.Log("Game Over! (health <= 0)");
    }
  }
  public void AddGold(int amount)
  {
    Gold += amount;
    UpdateGoldUI();
    OnGoldChanged?.Invoke(); // 🔔 notify listeners
  }

  // Optional: safe spend method
  public bool TrySpendGold(int amount)
  {
    if (Gold >= amount)
    {
      Gold -= amount;
      UpdateGoldUI();
      OnGoldChanged?.Invoke(); // 🔔 notify listeners
      return true;
    }
    return false;
  }

  void UpdateUI()
  {
    UpdateHealthUI();
    UpdateGoldUI();
  }

  void UpdateHealthUI()
  {
    if (healthText != null)
      healthText.text = Health.ToString();
  }

  void UpdateGoldUI()
  {
    if (goldText != null)
      goldText.text = Gold.ToString();
  }
}

