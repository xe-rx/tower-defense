using UnityEngine;

public class DwellProgressBar : MonoBehaviour
{
  [Header("Wiring")]
  [SerializeField] private Builder builder;            // assign your Builder
  [SerializeField] private SpriteRenderer sr;          // SpriteRenderer on this GO
  [SerializeField] private Sprite[] sprites;           // 22 frames: [0]=full ... [21]=empty

  private float effectiveDuration;                     // dwellTime - inputBufferSeconds
  private float elapsed;
  private bool active;

  private void Reset()
  {
    sr = GetComponent<SpriteRenderer>();
    builder = GetComponentInParent<Builder>();
  }

  private void OnEnable()
  {
    if (builder != null)
    {
      builder.OnDwellStarted += HandleDwellStarted;
      builder.OnDwellCompleted += HandleDwellCompleted;
    }
    SetVisible(false);
  }

  private void OnDisable()
  {
    if (builder != null)
    {
      builder.OnDwellStarted -= HandleDwellStarted;
      builder.OnDwellCompleted -= HandleDwellCompleted;
    }
  }

  private void Update()
  {
    if (!active || sr == null || sprites == null || sprites.Length == 0) return;

    if (effectiveDuration <= 0f)
    {
      // No effective time: show empty (or hide)
      sr.sprite = sprites[sprites.Length - 1];
      return;
    }

    elapsed += Time.deltaTime;
    float t = Mathf.Clamp01(elapsed / effectiveDuration);

    if (elapsed >= effectiveDuration)
    {
      SetVisible(false);
      return;
    }

    // Otherwise update normally
    int idx = Mathf.Clamp(Mathf.FloorToInt((1f - t) * (sprites.Length - 1)), 0, sprites.Length - 1);
    sr.sprite = sprites[idx];
  }

  private void HandleDwellStarted(int nodeIndex, float dwellSeconds)
  {
    // Ask builder for its buffer so we exclude it (you already serialize inputBufferSeconds there)
    // If you prefer not to expose it, you can pass effective time via the OnDwellStarted event instead.
    float buffer = GetBuilderBufferOrDefault();
    effectiveDuration = Mathf.Max(0f, dwellSeconds - buffer);
    elapsed = 0f;
    active = true;
    SetVisible(true);
    // Start at full
    if (sprites != null && sprites.Length > 0 && sr != null)
      sr.sprite = sprites[0];
  }

  private void HandleDwellCompleted(int nodeIndex, bool pressed)
  {
    active = false;
    SetVisible(false);
  }

  private void SetVisible(bool v)
  {
    if (sr != null) sr.enabled = v;
  }

  private float GetBuilderBufferOrDefault()
  {
    return builder != null ? builder.InputBufferSeconds : 0f;
  }
}

